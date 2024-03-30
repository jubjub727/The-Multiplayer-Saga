using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LSWTSS.OMP.Game;
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
            Serialization.LoadAvailableTypes();

            RiptideLogger.Log(LogType.Info, "TMPS", "Loaded Types");

            RiptideServer.ClientConnected += PlayerConnected;
            RiptideServer.ClientDisconnected += PlayerLeft;
            RiptideServer.MessageReceived += MessageHandler;

            RiptideServer.Start(Port, MaxPlayers, 0, false);
        }

        private void MainLoop()
        {
            Stopwatch timeElapsed = new Stopwatch();

            Startup();

            while (true)
            {
                var cursorPos = Console.GetCursorPosition();
                timeElapsed.Start();

                RunTick();

                while (timeElapsed.ElapsedTicks < (TimeSpan.TicksPerMicrosecond * 15625)) // 15625 = 64 tick
                {
                    continue;
                }
                timeElapsed.Stop();
                timeElapsed.Reset();
            }
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

            NetworkMessage tick = new NetworkMessage(dataSegmentList.ToArray());

            Message message = Message.Create(MessageSendMode.Unreliable, Utils.SERVER_TICK_MESSAGE_ID);
            message.AddBytes(tick.Serialize());

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
                }
            }

            RiptideLogger.Log(LogType.Error, "TMPS", String.Format("Could not process NetworkedPlayer with PlayerId - {0}", networkedPlayer.PlayerId));
        }

        private void HandleTickReply(NetworkMessage tickMessage)
        {
            foreach (DataSegment dataSegment in tickMessage.DataSegments)
            {
                if (dataSegment.Data is NetworkedPlayer)
                {
                    ProcessNetworkedPlayer(dataSegment.Data);
                }
            }
        }

        private void MessageHandler(object sender, MessageReceivedEventArgs messageReceivedArgs)
        {
            Message message = messageReceivedArgs.Message;

            Packet packet = new Packet(message.GetBytes());
            NetworkMessage tickMessage = packet.Deserialize();
            switch(messageReceivedArgs.MessageId)
            {
                case Utils.CLIENT_TICK_MESSAGE_ID:
                    HandleTickReply(tickMessage);
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
                    PlayerPool.RemoveAt(i);
                    RiptideLogger.Log(LogType.Info, "TMPS", String.Format("Removed Player from PlayerPool with ID - {0}", playerDisconnectedEvent.Client.Id));
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
