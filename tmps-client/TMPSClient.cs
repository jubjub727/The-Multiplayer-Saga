using System.Diagnostics;
using OMP.LSWTSS.CApi1;
using Networking;
using Riptide;
using Riptide.Utils;
using tmpsclient;
using System.Runtime.InteropServices;

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

    private NetworkedPlayer LocalPlayer;

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

    private void StartProcessingScopes()
    {
        GameUtil.nttUniverseProcessingScopeHandle = (nttUniverseProcessingScope.Handle)Marshal.AllocHGlobal(0x20);

        GameUtil.nttUniverseProcessingScopeConstructorDelegate nttUniverseProcessingScopeConstructor = NativeFunc.GetExecute<GameUtil.nttUniverseProcessingScopeConstructorDelegate>(NativeFunc.GetPtr(GameUtil.nttUniverseProcessingScopeConstructorOffset));

        nttUniverseProcessingScopeConstructor(GameUtil.nttUniverseProcessingScopeHandle, GameUtil.GetCurrentApiWorldHandle().GetUniverse(), true);

        GameUtil.apiWorldProcessingScopeHandle = (ApiWorldProcessingScope.Handle)Marshal.AllocHGlobal(0x20);

        GameUtil.ApiWorldProcessingScopeConstructorDelegate apiWorldProcessingScopeConstructor = NativeFunc.GetExecute<GameUtil.ApiWorldProcessingScopeConstructorDelegate>(NativeFunc.GetPtr(GameUtil.ApiWorldProcessingScopeConstructorOffset));

        apiWorldProcessingScopeConstructor(GameUtil.apiWorldProcessingScopeHandle, GameUtil.GetCurrentApiWorldHandle(), true);
    }

    private void StopProcessingScopes()
    {
        GameUtil.ApiWorldProcessingScopeDestructorDelegate apiWorldProcessingScopeDestructor = NativeFunc.GetExecute<GameUtil.ApiWorldProcessingScopeDestructorDelegate>(NativeFunc.GetPtr(GameUtil.apiWorldProcessingScopeDestructorOffset));

        apiWorldProcessingScopeDestructor(GameUtil.apiWorldProcessingScopeHandle);

        GameUtil.nttUniverseProcessingScopeDestructorDelegate nttUniverseProcessingScopeDestructor = NativeFunc.GetExecute<GameUtil.nttUniverseProcessingScopeDestructorDelegate>(NativeFunc.GetPtr(GameUtil.nttUniverseProcessingScopeDestructorOffset));

        nttUniverseProcessingScopeDestructor(GameUtil.nttUniverseProcessingScopeHandle);
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

    public void OnUpdate()
    {
        if (CheckIfReady() == false) return;

        StartProcessingScopes();

        if (!_RiptideConnected && GameUtil.LoadedResource()) // GameUtil.LoadedResource() actually loads the resource here but once _RiptideConnected is true the function no longer gets called
        {
            RiptideClient.Connect(ServerInfo.ConnectionString, useMessageHandlers: false);

            _RiptideConnected = true;
        }

        if (_FirstConnect == true && LocalPlayer.Entity != nint.Zero)
        {
            /*Console.WriteLine("Stopping Processing Scopes");
            StopProcessingScopes();
            Console.WriteLine("Getting Local Transform");
            apiTransformComponent.Handle transformComponent = (apiTransformComponent.Handle)(nint)LocalPlayer.Entity.FindComponentByTypeName("apiTransformComponent");
            Console.WriteLine("Got Local Transform");
            transformComponent.GetPosition(out LocalPlayer.Transform.X, out LocalPlayer.Transform.Y, out LocalPlayer.Transform.Z);
            Console.WriteLine("Got Position");
            transformComponent.GetRotation(out LocalPlayer.Transform.RX, out LocalPlayer.Transform.RY, out LocalPlayer.Transform.RZ);
            Console.WriteLine("Got Rotation");
            StartProcessingScopes();
            Console.WriteLine("Starting Processing Scopes");*/
        }

        if (RiptideClient.IsConnected && _FirstConnect == false)
        {
            LocalPlayer = new NetworkedPlayer(RiptideClient.Id, ServerInfo.Name);

            Console.WriteLine("Created LocalPlayer");

            PlayerControlSystem.Handle _PlayerControlSystemHandle = PlayerControlSystem.GetFromGlobalFunc.Execute(GameUtil.GetCurrentApiWorldHandle().GetUniverse());

            Console.WriteLine("Retrieved PlayerControlSystem");

            apiEntity.Handle _LocalPlayerEntity = _PlayerControlSystemHandle.GetPlayerEntityForPlayerIdx(0);
            Console.WriteLine("Retrieved LocalPlayerEntity - {0}", (nint)_LocalPlayerEntity);

            LocalPlayer.AssignEntity(_LocalPlayerEntity);

            Console.WriteLine("Assigned LocalPlayerEntity");

            _FirstConnect = true;

            TimeSinceLastTick.Start();

            Console.WriteLine("Started Tick Stopwatch");
        }

        RiptideClient.Update();

        //Interpolation.Interpolate(PlayerPool);

        Console.WriteLine("Stopping Processing Scopes");
        StopProcessingScopes();
        Console.WriteLine("Stopped Processing Scopes");
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
            apiEntity.Handle createdEntity = GameUtil.CreateEntity(networkedPlayer.Transform);

            networkedPlayer.AssignEntity(createdEntity);
            networkedPlayer.SetTransform(networkedPlayer.Transform, TimeSinceLastTick.ElapsedTicks);

            PlayerPool.Add(networkedPlayer);

            RiptideLogger.Log(LogType.Info, "TMPS", String.Format("Added {0}({1}) to PlayerPool", networkedPlayer.Name, networkedPlayer.PlayerId));
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
