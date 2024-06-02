using System.Diagnostics;
using Networking;
using Riptide;
using Riptide.Utils;

namespace tmpsserver
{
    public class GameServer
    {
        public Server RiptideServer;

        public ushort Port;
        public ushort MaxPlayers;

        public List<NetworkedPlayer> PlayerPool = new List<NetworkedPlayer>();

        private UInt64 TickCount = 0;


        private void Startup()
        {
            RiptideServer.ClientConnected += PlayerConnected;
            RiptideServer.ClientDisconnected += PlayerLeft;
            RiptideServer.MessageReceived += MessageHandler;

            while (!Serialization.TypesLoaded)
            {
                continue;
            }

            RiptideLogger.Log(LogType.Info, "TMPS", "Finished Loading Types");

            RiptideServer.TimeoutTime = Utils.DefaultTimeout;

            RiptideServer.Start(Port, MaxPlayers, 0, false);
        }

        private void LoadTypes()
        {
            Serialization.LoadAvailableTypes();
        }

        private void MainLoop()
        {
            Stopwatch timeElapsed = Stopwatch.StartNew();

            Thread typeLoadingThread = new Thread(LoadTypes);
            typeLoadingThread.Start();

            Startup();

            bool ranTick = false;

            while (true)
            {
                if (!ranTick)
                {
                    RunTick();
                    ranTick = true;
                }

                while (timeElapsed.ElapsedTicks < (TimeSpan.TicksPerMicrosecond * 15625)) // 15625 = 64 tick
                {
                    continue;
                }

                double executionTime = (double)timeElapsed.ElapsedTicks / (double)TimeSpan.TicksPerMillisecond;

                var cursorPos = Console.GetCursorPosition();
                Console.Write("| ");

                foreach (Connection connection in RiptideServer.Clients)
                {
                    NetworkedPlayer? player = FindPlayer(connection.Id);
                    if (player != null)
                    {
                        Console.Write("{0} - {1} | ", player.Name, connection.RTT);
                    }
                }

                Console.Write("Execution Time: {0}ms                               ", executionTime);

                Console.SetCursorPosition(cursorPos.Left, cursorPos.Top);

                ranTick = false;

                timeElapsed.Restart();
            }
        }

        private NetworkedPlayer? FindPlayer(int playerId)
        {
            foreach (NetworkedPlayer networkedPlayer in PlayerPool)
            {
                if (networkedPlayer.PlayerId == playerId)
                {
                    return networkedPlayer;
                }
            }

            return null;
        }

        private void RunTick()
        {
            RiptideServer.Update();

            List<DataSegment> dataSegmentList = new List<DataSegment>();

            foreach (NetworkedPlayer networkedPlayer in PlayerPool)
            {
                DataSegment dataSegment = new DataSegment(networkedPlayer);
                dataSegmentList.Add(dataSegment);
            }

            NetworkMessage packet = new NetworkMessage(dataSegmentList.ToArray(), TickCount);

            Message message = Message.Create(MessageSendMode.Unreliable, Utils.SERVER_TICK_MESSAGE_ID);
            message.AddBytes(packet.Serialize());

            RiptideServer.SendToAll(message);

            TickCount++;
        }

        private void ProcessNetworkedPlayer(NetworkedPlayer networkedPlayer)
        {
            for (int i = 0; i < PlayerPool.Count; i++)
            {
                if (PlayerPool[i].PlayerId == networkedPlayer.PlayerId)
                {
                    PlayerPool[i] = networkedPlayer;
                    return;
                }
            }

            RiptideLogger.Log(LogType.Error, "TMPS", String.Format("Could not process NetworkedPlayer for {0}({1})", networkedPlayer.Name, networkedPlayer.PlayerId));
            throw new Exception("YOU SHALL NOT PASS!");
        }

        private void HandleTickReply(Message message)
        {
            Packet packet = new Packet(message.GetBytes());
            NetworkMessage tickMessage = packet.Deserialize();

            foreach (DataSegment dataSegment in tickMessage.DataSegments)
            {
                if (dataSegment.Data is NetworkedPlayer)
                {
                    ProcessNetworkedPlayer(dataSegment.Data);
                }
            }
        }

        private void HandleAction(Message message)
        {
            Packet packet = new Packet(message.GetBytes());
            NetworkMessage tickMessage = packet.Deserialize();

            if (tickMessage.DataSegments != null)
            {
                foreach (DataSegment dataSegment in tickMessage.DataSegments)
                {
                    if (dataSegment.Data is NetworkedAction)
                    {
                        NetworkedAction networkedAction = dataSegment.Data;

                        DataSegment[] dataSegments = new DataSegment[1];
                        dataSegments[0] = new DataSegment(networkedAction);

                        NetworkMessage actionMessage = new NetworkMessage(dataSegments, TickCount);

                        Message newMessage = Message.Create(MessageSendMode.Unreliable, Utils.SERVER_ACTION_MESSAGE_ID);
                        newMessage.AddBytes(actionMessage.Serialize());

                        RiptideServer.SendToAll(newMessage);
                    }
                }
            }
        }

        private void MessageHandler(object sender, MessageReceivedEventArgs messageReceivedArgs)
        {
            Message message = messageReceivedArgs.Message;

            switch(messageReceivedArgs.MessageId)
            {
                case Utils.CLIENT_TICK_MESSAGE_ID:
                    HandleTickReply(message);
                    break;
                case Utils.CLIENT_ACTION_MESSAGE_ID:
                    HandleAction(message);
                    break;
                default:
                    RiptideLogger.Log(LogType.Error, "TMPS", String.Format("Received Uknown Message ID - {0}", messageReceivedArgs.MessageId));
                    break;
            }
        }

        private void PlayerConnected(object sender, ServerConnectedEventArgs playerConnectedEvent)
        {
            NetworkedPlayer player = new NetworkedPlayer(playerConnectedEvent.Client.Id);
            PlayerPool.Add(player);
            RiptideLogger.Log(LogType.Info, "TMPS", String.Format("Added Player to PlayerPool with ID - {0}", playerConnectedEvent.Client.Id));
        }

        private void PlayerLeft(object sender, ServerDisconnectedEventArgs playerDisconnectedEvent)
        {
            for (int i = PlayerPool.Count - 1; i >= 0; i--)
            {
                if (PlayerPool[i].PlayerId == playerDisconnectedEvent.Client.Id)
                {
                    string name = PlayerPool[i].Name;
                    PlayerPool.RemoveAt(i);
                    RiptideLogger.Log(LogType.Info, "TMPS", String.Format("Removed {0}({1}) from PlayerPool", name, playerDisconnectedEvent.Client.Id));
                    return;
                }
            }

            RiptideLogger.Log(LogType.Error, "TMPS", String.Format("Couldn't find leaving Player with ID - {0}", playerDisconnectedEvent.Client.Id));
        }

        public GameServer(ushort port, ushort maxPlayers)
        {
            Port = port;
            MaxPlayers = maxPlayers;

            RiptideLogger.Initialize(Console.WriteLine, true);
            RiptideServer = new Server();

            Thread tickThread = new Thread(MainLoop);
            tickThread.Start();
        }
    }
}
