using System.Diagnostics;
using OMP.LSWTSS.CApi1;
using Networking;
using Riptide;
using Riptide.Utils;
using tmpsclient;
using System.Runtime.InteropServices;
using gameutil;

namespace OMP.LSWTSS;

public class TMPSClient
{
    [DllImport("user32.dll")]
    static extern short GetAsyncKeyState(int vKey);

    private const int PAGE_UP = 0x21;

    CFuncHook1<GameFramework.UpdateMethod.Delegate>? GameFrameworkUpdateMethodHook;

    ServerInfo ServerInfo = new ServerInfo(@"ServerInfo.cfg");

    private Client RiptideClient;

    private Interpolation Interpolation;

    private List<NetworkedPlayer> PlayerPool = new List<NetworkedPlayer>();

    private NetworkedPlayer _LocalPlayer;

    private Stopwatch TimeSinceLastTick = new Stopwatch();

    private bool _FirstConnect = false;

    private bool _RiptideConnected = false;

    private bool _ReadyToConnect = false;

    private void Startup()
    {
        SetupHooks();

        RiptideLogger.Log(LogType.Info, "TMPS", "Finished setting up hooks");

        Interpolation = new Interpolation(TimeSinceLastTick);

        Serialization.LoadAvailableTypes();

        RiptideLogger.Log(LogType.Info, "TMPS", "Finished loading types");

        RiptideClient.MessageReceived += MessageHandler;
        RiptideClient.ClientConnected += PlayerConnected;
        RiptideClient.ClientDisconnected += PlayerDisconnected;
    }

    private void SetupHooks()
    {
        GameFrameworkUpdateMethodHook = new(
            GameFramework.UpdateMethod.Ptr,
            (handle) =>
            {
                OnUpdate();
                return GameFrameworkUpdateMethodHook!.Trampoline!(handle);
            }
        );

        GameFrameworkUpdateMethodHook.Enable();
    }

    private void CopyTransformToNetworkedPlayer()
    {
        apiTransformComponent.Handle transformComponent = (apiTransformComponent.Handle)(nint)_LocalPlayer.Entity.FindComponentByTypeName("apiTransformComponent");

        transformComponent.GetPosition(out _LocalPlayer.Transform.X, out _LocalPlayer.Transform.Y, out _LocalPlayer.Transform.Z);
        transformComponent.GetRotation(out _LocalPlayer.Transform.RX, out _LocalPlayer.Transform.RY, out _LocalPlayer.Transform.RZ);
    }

    private bool CheckIfReady()
    {
        if (!_ReadyToConnect && (GetAsyncKeyState(PAGE_UP) != 0))
        {
            RiptideLogger.Log(LogType.Info, "TMPS", "Ready to connect to server");
            _ReadyToConnect = true;
        }

        return _ReadyToConnect;
    }

    private bool PlayerEntityReady()
    {
        try
        {
            apiEntity.Handle entity = _LocalPlayer.Entity;
            return entity != nint.Zero;
        }
        catch
        {
            return false;
        }
    }

    public void OnUpdate()
    {
        if (CheckIfReady() == false) return;

        GameUtil.StartProcessingScopes();

        if (!_RiptideConnected)
        {
            RiptideClient.Connect(ServerInfo.ConnectionString, useMessageHandlers: false);

            _RiptideConnected = true;
        }

        if (_FirstConnect == true && PlayerEntityReady())
        {
            CharacterSpawnManager.ConsumeTasks();

            CopyTransformToNetworkedPlayer();
        }

        if (RiptideClient.IsConnected && _FirstConnect == false)
        {
            _LocalPlayer = new NetworkedPlayer(RiptideClient.Id, ServerInfo.Name);
            _LocalPlayer.IsLocal = true;

            _FirstConnect = true;

            TimeSinceLastTick.Start();
        }

        GameUtil.StopProcessingScopes();

        RiptideClient.Update();

        //Interpolation.Interpolate(PlayerPool);
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

        if (_FirstConnect && networkedPlayer.PlayerId != _LocalPlayer.PlayerId)
        {
            networkedPlayer.SetTransform(networkedPlayer.Transform, TimeSinceLastTick.ElapsedTicks);

            CharacterSpawnManager.SpawnCharacter(networkedPlayer);

            PlayerPool.Add(networkedPlayer);

            RiptideLogger.Log(LogType.Info, "TMPS", String.Format("Added {0}({1}) to PlayerPool", networkedPlayer.Name, networkedPlayer.PlayerId));
        }
        
    }

    private void TickReply()
    {
        if (_FirstConnect)
        {
            DataSegment[] dataSegments = new DataSegment[1];
            dataSegments[0] = new DataSegment(_LocalPlayer);

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

            if (tickMessage.DataSegments != null)
            {
                foreach (DataSegment dataSegment in tickMessage.DataSegments)
                {
                    if (dataSegment.Data is NetworkedPlayer)
                    {
                        ProcessNetworkedPlayer(dataSegment.Data);
                    }
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
                string playerName = PlayerPool[i].Name;
                PlayerPool.RemoveAt(i);
                RiptideLogger.Log(LogType.Info, "TMPS", String.Format("{0} left the game", playerName));
                return;
            }
        }
        RiptideLogger.Log(LogType.Error, "TMPS", String.Format("Couldn't find leaving Player with ID - {0}", playerDisconnectedEvent.Id));
    }

    public TMPSClient()
    {
        RiptideLogger.Initialize(Console.WriteLine, true);
        RiptideClient = new Client();

        Startup();
    }
}
