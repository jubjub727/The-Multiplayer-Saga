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

    CFuncHook1<JumpContext.OnEnterMethod.Delegate>? JumpHook;

    CFuncHook1<DoubleJumpContext.OnEnterMethod.Delegate>? DoubleJumpHook;

    ServerInfo ServerInfo = new ServerInfo(@"ServerInfo.cfg");

    PrefabList PrefabList = new PrefabList(@"CharacterPrefabs.txt");

    private Client RiptideClient;

    private Interpolation Interpolation;

    private List<NetworkedPlayer> PlayerPool = new List<NetworkedPlayer>();

    private List<NetworkedAction> ActionPool = new List<NetworkedAction>();

    private NetworkedPlayer _LocalPlayer;

    private Stopwatch TimeSinceLastTick = new Stopwatch();

    private UInt64 CurrentTick = 0;

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

        JumpHook = new(
            JumpContext.OnEnterMethod.Ptr,
            (handle, param0) =>
            {
                JumpEvent(0.56406253576278687f);

                JumpHook!.Trampoline!(handle, param0);
            }
        );

        DoubleJumpHook = new(
            DoubleJumpContext.OnEnterMethod.Ptr,
            (handle, param0) =>
            {
                JumpEvent(0.40000000596046448f);

                DoubleJumpHook!.Trampoline!(handle, param0);
            }
        );

        JumpHook.Enable();

        DoubleJumpHook.Enable();

        GameFrameworkUpdateMethodHook.Enable();
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

    private void CopyTransformToNetworkedPlayer()
    {
        apiTransformComponent.Handle transformComponent = (apiTransformComponent.Handle)(nint)_LocalPlayer.Entity.FindComponentByTypeName("apiTransformComponent");
        if (transformComponent == nint.Zero)
        {
            throw new Exception("Couldn't find Transform for LocalPlayer");
        }

        transformComponent.GetPosition(out _LocalPlayer.Transform.X, out _LocalPlayer.Transform.Y, out _LocalPlayer.Transform.Z);
        transformComponent.GetRotation(out _LocalPlayer.Transform.RX, out _LocalPlayer.Transform.RY, out _LocalPlayer.Transform.RZ);

        CharacterMoverComponent.Handle characterMoverComponent = (CharacterMoverComponent.Handle)(nint)_LocalPlayer.Entity.FindComponentByTypeNameRecursive("characterMoverComponent", false);
        if (characterMoverComponent == nint.Zero)
        {
            throw new Exception("Couldn't find CharacterMoverComponent for LocalPlayer");
        }

        _LocalPlayer.PrefabPath = PrefabList.GetPrefabFromCharacterName(_LocalPlayer.Entity.GetName());

        //_LocalPlayer.Transform.SnapToGroundOn = characterMoverComponent.get_SnapToGroundOn();
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

    private void ProcessActions()
    {
        foreach (NetworkedAction action in ActionPool)
        {
            if (action.Tick < CurrentTick+Utils.PacketOffset)
            {
                action.ProcessAction();
            }
        }
    }

    public void OnUpdate()
    {
        if (CheckIfReady() == false) return;

        GameUtil.StartProcessingScopes();

        if (!_RiptideConnected)
        {
            RiptideClient.Connect(ServerInfo.ConnectionString, useMessageHandlers: false);

            RiptideClient.TimeoutTime = Utils.DefaultTimeout;

            _RiptideConnected = true;

            TimeSinceLastTick.Start();
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
        }

        RiptideClient.Update();

        Interpolation.Interpolate(PlayerPool);

        GameUtil.StopProcessingScopes();
    }

    private void ProcessNetworkedPlayer(NetworkedPlayer networkedPlayer)
    {
        for (int i = 0; i < PlayerPool.Count; i++)
        {
            if (PlayerPool[i].PlayerId == networkedPlayer.PlayerId)
            {
                if (!networkedPlayer.Transform.IsBadTransform())
                {
                    PlayerPool[i].SetTransform(networkedPlayer.Transform, TimeSinceLastTick.ElapsedTicks);
                }
                else
                {
                    RiptideLogger.Log(LogType.Error, "TMPS", String.Format("Received bad Transform for {0}({1})", networkedPlayer.Name, networkedPlayer.PlayerId));
                }

                if (networkedPlayer.PrefabPath != PlayerPool[i].PrefabPath)
                {
                    PlayerPool[i].Entity.Delete();

                    PlayerPool[i].PrefabPath = networkedPlayer.PrefabPath;

                    PlayerPool[i].Transform = networkedPlayer.Transform;

                    CharacterSpawnManager.SpawnCharacter(PlayerPool[i]);

                    RiptideLogger.Log(LogType.Info, "TMPS", String.Format("{0}({1}) changed Character to {2}", networkedPlayer.Name, networkedPlayer.PlayerId, PrefabList.GetCharacterNameFromPrefab(networkedPlayer.PrefabPath)));
                }

                return;
            }
        }

        if (_FirstConnect && networkedPlayer.PlayerId != _LocalPlayer.PlayerId)
        {
            PlayerPool.Add(networkedPlayer);

            CharacterSpawnManager.SpawnCharacter(networkedPlayer);

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

    private void HandleTick(Message message)
    {
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

        ProcessActions();

        TickReply();
        TimeSinceLastTick.Restart();
    }

    private NetworkedPlayer? FindPlayer(UInt16 playerId)
    {
        foreach (NetworkedPlayer player in PlayerPool)
        {
            if (player.PlayerId == playerId)
            {
                return player;
            }
        }
        return null;
    }

    private void HandleAction(Message message)
    {
        Packet packet = new Packet(message.GetBytes());
        NetworkMessage actionMessage = packet.Deserialize();

        if (actionMessage.DataSegments != null)
        {
            foreach (DataSegment dataSegment in actionMessage.DataSegments)
            {
                if (dataSegment.Data is NetworkedAction)
                {
                    NetworkedAction networkedAction = dataSegment.Data;

                    if (networkedAction.PlayerId == _LocalPlayer.PlayerId)
                    {
                        break;
                    }

                    NetworkedPlayer? networkedPlayer = FindPlayer(networkedAction.PlayerId);
                    if (networkedPlayer == null)
                    {
                        RiptideLogger.Log(LogType.Error, "TMPS", String.Format("Could not find NetworkedPlayer with PlayerId({0})", networkedAction.PlayerId));
                        break;
                    }

                    networkedAction.AssignPlayer(networkedPlayer);

                    ActionPool.Add(networkedAction);
                }
            }
        }
    }

    private void MessageHandler(object sender, MessageReceivedEventArgs messageReceivedArgs)
    {
        switch (messageReceivedArgs.MessageId)
        {
            case Utils.SERVER_TICK_MESSAGE_ID:
                HandleTick(messageReceivedArgs.Message);
                break;
            case Utils.SERVER_ACTION_MESSAGE_ID:
                HandleAction(messageReceivedArgs.Message);
                break;
            default:
                break;
        }
        if (messageReceivedArgs.MessageId == Utils.SERVER_TICK_MESSAGE_ID)
        {

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
