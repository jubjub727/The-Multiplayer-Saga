using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
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
        //[DllImport("user32.dll")]
        //static extern short GetAsyncKeyState(int vKey);

        private const float HorizontalSpeed = 0.01f;

        private const int W = 0x57;
        private const int A = 0x41;
        private const int S = 0x53;
        private const int D = 0x44;

        private const int PAGE_DOWN = 0x22;

        private Client RiptideClient;

        private List<NetworkedPlayer> PlayerPool = new List<NetworkedPlayer>();

        private NetworkedPlayer _LocalPlayer;

        private UInt64 CurrentTick = 0;

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
                NetworkedPlayer localPlayerNetworked = new NetworkedPlayer(RiptideClient.Id, Utils.DummyClientName);
                localPlayerNetworked.IsLocal = true;
                localPlayerNetworked.Transform.Z = -1f;

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
                    return;
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

                NetworkMessage tickMessage = new NetworkMessage(dataSegments, CurrentTick);

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
                CurrentTick = tickMessage.Tick;

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
                    RiptideLogger.Log(LogType.Info, "TMPS", String.Format("{0} left the game", PlayerPool[i].Name));
                    PlayerPool.RemoveAt(i);
                    return;
                }
            }
            RiptideLogger.Log(LogType.Error, "TMPS", String.Format("Couldn't find leaving Player with ID - {0}", playerDisconnectedEvent.Id));
        }

        private void JumpEvent(float amount)
        {
            if (_FirstConnect)
            {
                NetworkedAction jumpAction = new NetworkedAction(_LocalPlayer.PlayerId, Utils.JUMP_ACTION_ID, amount, CurrentTick);

                DataSegment[] dataSegments = new DataSegment[1];
                dataSegments[0] = new DataSegment(jumpAction);

                NetworkMessage actionMessage = new NetworkMessage(dataSegments, CurrentTick);

                Message message = Message.Create(MessageSendMode.Unreliable, Utils.CLIENT_ACTION_MESSAGE_ID);
                message.AddBytes(actionMessage.Serialize());

                RiptideClient.Send(message);
            }
        }

        private void MainLoop()
        {
            int count = 0;
            bool justJumped = false;
            Stopwatch timeElapsed = new Stopwatch();
            Stopwatch timeSinceJump = new Stopwatch();
            timeSinceJump.Start();

            while (true)
            {
                timeElapsed.Start();

                if (_FirstConnect)
                {
                    /*if (GetAsyncKeyState(A) != 0)
                    {
                        _LocalPlayer.Transform.X = _LocalPlayer.Transform.X + HorizontalSpeed;
                    }

                    if (GetAsyncKeyState(D) != 0)
                    {
                        _LocalPlayer.Transform.X = _LocalPlayer.Transform.X - HorizontalSpeed;
                    }

                    if (GetAsyncKeyState(S) != 0)
                    {
                        _LocalPlayer.Transform.Z = _LocalPlayer.Transform.Z + HorizontalSpeed;
                    }

                    if (GetAsyncKeyState(W) != 0)
                    {
                        _LocalPlayer.Transform.Z = _LocalPlayer.Transform.Z - HorizontalSpeed;
                    }*/

                    if (timeSinceJump.ElapsedMilliseconds > 1500)//&& GetAsyncKeyState(PAGE_DOWN) != 0)
                    {
                        _LocalPlayer.Transform.Z -= HorizontalSpeed;
                        if (!justJumped)
                        {
                            JumpEvent(0.56406253576278687f);
                            justJumped=true;
                        }
                        
                        timeSinceJump.Restart();
                    }
                    else
                    {
                        _LocalPlayer.Transform.Z += HorizontalSpeed;

                        if (justJumped)
                        {
                            justJumped = false;
                        }
                    }

                    if (count > 240)
                    {
                        Console.WriteLine("X - {0}, Y - {1}, Z - {2}", _LocalPlayer.Transform.X, _LocalPlayer.Transform.Y, _LocalPlayer.Transform.Z);
                        count = 0;
                    }
                }

                OnUpdate();

                while (timeElapsed.ElapsedTicks < (TimeSpan.TicksPerMicrosecond * 4166))
                {
                    continue;
                }
                timeElapsed.Stop();
                timeElapsed.Reset();

                count++;
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
