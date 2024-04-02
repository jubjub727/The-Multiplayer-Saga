using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using LSWTSS.OMP.Game.Api;
using MathNet.Numerics;
using Networking;
using Riptide;
using Riptide.Utils;
using tmpsclient;
using static LSWTSS.OMP.Game.Api.CommonEvents.Interaction.Data;

namespace LSWTSS.OMP;

public class TMPSClient : IDisposable
{
    ServerInfo ServerInfo = new ServerInfo(@"ipaddress.data");

    private Client RiptideClient;

    private Interpolation Interpolation;

    private List<NetworkedPlayer> PlayerPool = new List<NetworkedPlayer>();

    private NetworkedPlayer LocalPlayer;

    private Stopwatch TimeSinceLastTick = new Stopwatch();

    private bool _FirstConnect = false;

    private bool _ResourceLoaded = false;

    private void Startup()
    {
        Interpolation = new Interpolation(TimeSinceLastTick);

        RiptideClient.MessageReceived += MessageHandler;
        RiptideClient.ClientConnected += PlayerConnected;
        RiptideClient.ClientDisconnected += PlayerDisconnected;
    }

    public void OnUpdate()
    {
        if (!_ResourceLoaded && GameUtil.LoadedResource())
        {
            RiptideClient.Connect(ServerInfo.ConnectionString, useMessageHandlers: false);

            _ResourceLoaded = true;
        }

        if (_FirstConnect == true && LocalPlayer.Entity != nint.Zero)
        {
            apiTransformComponent.Handle transformComponent = (apiTransformComponent.Handle)(nint)LocalPlayer.Entity.FindComponentByTypeName("apiTransformComponent");
            transformComponent.GetPosition(out LocalPlayer.Transform.X, out LocalPlayer.Transform.Y, out LocalPlayer.Transform.Z);
            transformComponent.GetRotation(out LocalPlayer.Transform.RX, out LocalPlayer.Transform.RY, out LocalPlayer.Transform.RZ);
        }

        if (RiptideClient.IsConnected && _FirstConnect == false)
        {
            LocalPlayer = new NetworkedPlayer(RiptideClient.Id);
            _FirstConnect = true;
            TimeSinceLastTick.Start();
        }

        RiptideClient.Update();

        Interpolation.Interpolate(PlayerPool);
    }

    private void ProcessNetworkedPlayer(NetworkedPlayer networkedPlayer)
    {
        for (int i = 0; i < PlayerPool.Count; i++)
        {
            if (PlayerPool[i].PlayerId == networkedPlayer.PlayerId)
            {
                PlayerPool[i].SetTransform(networkedPlayer.Transform, TimeSinceLastTick.ElapsedTicks);
            }
        }

        if (_FirstConnect && networkedPlayer.PlayerId != LocalPlayer.PlayerId)
        {
            var createdEntity = GameUtil.CreateEntity(networkedPlayer.Transform);

            NetworkedPlayer newPlayer = new NetworkedPlayer(networkedPlayer.PlayerId, createdEntity, networkedPlayer.Transform);
            newPlayer.SetTransform(networkedPlayer.Transform, TimeSinceLastTick.ElapsedTicks);

            PlayerPool.Add(newPlayer);

            RiptideLogger.Log(LogType.Info, "TMPS", String.Format("Added new Player to PlayerPool with PlayerId - {0}", networkedPlayer.PlayerId));
        }
        
    }

    private void TickReply()
    {
        if (_FirstConnect)
        {
            DataSegment[] dataSegments = new DataSegment[1];
            dataSegments[0] = new DataSegment(LocalPlayer);

            NetworkMessage tickMessage = new NetworkMessage(dataSegments);

            Message message = Message.Create(MessageSendMode.Unreliable, Utils.CLIENT_TICK_MESSAGE_ID);
            message.AddBytes(tickMessage.Serialize());

            RiptideClient.Send(message);
        }
    }

    private void MessageHandler(object sender, MessageReceivedEventArgs messageReceivedArgs)
    {
        if (messageReceivedArgs.MessageId == Utils.SERVER_TICK_MESSAGE_ID)
        {
            TimeSinceLastTick.Stop();
            Message message = messageReceivedArgs.Message;

            Packet packet = new Packet(message.GetBytes());
            NetworkMessage tickMessage = packet.Deserialize();

            foreach (DataSegment dataSegment in tickMessage.DataSegments)
            {
                if (dataSegment.Data is NetworkedPlayer)
                {
                    ProcessNetworkedPlayer(dataSegment.Data);
                }
            }

            TickReply();
            TimeSinceLastTick.Restart();
        }
    }

    private void PlayerConnected(object sender, ClientConnectedEventArgs playerConnectedEvent)
    {

    }

    private void PlayerDisconnected(object sender, ClientDisconnectedEventArgs playerDisconnectedEvent)
    {
        for (int i = PlayerPool.Count - 1; i >= 0; i--)
        {
            if (PlayerPool[i].PlayerId == playerDisconnectedEvent.Id)
            {
                apiEntity.Handle entity = (apiEntity.Handle)PlayerPool[i].Entity;
                entity.Delete();
                PlayerPool.RemoveAt(i);
                Console.WriteLine("Player Left with ID - {0}", playerDisconnectedEvent.Id);
                return;
            }
        }
        Console.WriteLine("Couldn't find leaving Player with ID - {0}", playerDisconnectedEvent.Id);
    }

    public TMPSClient()
    {
        RiptideLogger.Initialize(Console.WriteLine, true);
        RiptideClient = new Client();

        Startup();
    }

    public void Dispose()
    {
        RiptideClient.Disconnect();
    }

    ~TMPSClient()
    {
        Dispose();
    }
}
