using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using gameutil;
using Networking;
using Riptide;
using Riptide.Utils;

namespace dummy_client
{
    public class DummyClient
    {
        private Client RiptideClient;

        private List<NetworkedPlayer> PlayerPool = new List<NetworkedPlayer>();

        private NetworkedPlayer _LocalPlayer;

        private bool _FirstConnect = false;

        private bool _RiptideConnected = false;

        private string _ConnectionString = "";

        private void Startup()
        {

            RiptideLogger.Log(LogType.Info, "TMPS", "Finished setting up hooks");

            Serialization.LoadAvailableTypes();

            RiptideLogger.Log(LogType.Info, "TMPS", "Finished loading types");

            RiptideClient.MessageReceived += MessageHandler;
            RiptideClient.ClientConnected += PlayerConnected;
            RiptideClient.ClientDisconnected += PlayerDisconnected;
        }

        private void OnUpdate()
        {
            if (!_RiptideConnected)
            {
                RiptideClient.Connect(_ConnectionString, useMessageHandlers: false);

                _RiptideConnected = true;
            } 

            if (RiptideClient.IsConnected && _FirstConnect == false)
            {
                NetworkedPlayer localPlayerNetworked = new NetworkedPlayer(RiptideClient.Id, _ConnectionString);
                localPlayerNetworked.IsLocal = true;

                _LocalPlayer = localPlayerNetworked;

                _FirstConnect = true;
            }

            RiptideClient.Update();
        }
        private void ProcessNetworkedPlayer(NetworkedPlayer networkedPlayer)
        {
            for (int i = 0; i < PlayerPool.Count; i++)
            {
                if (PlayerPool[i].PlayerId == networkedPlayer.PlayerId)
                {
                    PlayerPool[i].Transform = networkedPlayer.Transform;
                }
            }

            if (_FirstConnect && networkedPlayer.PlayerId != _LocalPlayer.PlayerId)
            {
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
                    string playerName = PlayerPool[i].Name;
                    PlayerPool.RemoveAt(i);
                    RiptideLogger.Log(LogType.Info, "TMPS", String.Format("{0} left the game", playerName));
                    return;
                }
            }
            RiptideLogger.Log(LogType.Error, "TMPS", String.Format("Couldn't find leaving Player with ID - {0}", playerDisconnectedEvent.Id));
        }

        private void MainLoop()
        {
            Stopwatch timeElapsed = new Stopwatch();

            while (true)
            {
                timeElapsed.Start();

                OnUpdate();

                while (timeElapsed.ElapsedTicks < (78125)) // 78125 = 128 tick
                {
                    continue;
                }
                timeElapsed.Stop();
                timeElapsed.Reset();
            }
        }

        public DummyClient(string ipAdress)
        {
            RiptideLogger.Initialize(Console.WriteLine, true);
            RiptideClient = new Client();

            _ConnectionString = ipAdress;

            Startup();

            Thread tickThread = new Thread(MainLoop);
            tickThread.Start();
        }
    }
}
