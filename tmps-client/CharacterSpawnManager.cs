using Networking;
using OMP.LSWTSS.CApi1;
using System.Runtime.InteropServices;
using gameutil;

namespace tmpsclient;
public static class CharacterSpawnManager
{
    public static int IdentifierIndex = 0;

    private static List<SpawnCharacterTask> CharacterTasks = new List<SpawnCharacterTask>();

    private static void LoadResource(SpawnCharacterTask task)
    {
        GameUtil.nttSceneGraphResourceConstructor nttSceneGraphResourceConstructor = Marshal.GetDelegateForFunctionPointer<GameUtil.nttSceneGraphResourceConstructor>(NativeFunc.GetPtr(GameUtil.SceneGraphResourceConstructorOffset));

        task.CharacterPrefabResourceHandle = (nttSceneGraphResourceHandle.Handle)Marshal.AllocHGlobal(0x88);

        for (int i = 0; i < 0x88; i++)
        {
            Marshal.WriteByte(task.CharacterPrefabResourceHandle, i, 0);
        }

        nttSceneGraphResourceConstructor(task.CharacterPrefabResourceHandle, 2);

        task.CharacterPrefabResourceHandle.set_ResourcePath(task.Player.PrefabPath);

        task.CharacterPrefabResourceHandle.AsyncLoad();

        task.State = CharacterSpawnState.LoadingResource;
    }

    private static void CheckResourceLoaded(SpawnCharacterTask task)
    {
        if (task.CharacterPrefabResourceHandle.IsLoaded())
        {
            task.State = CharacterSpawnState.ResourceLoaded;
        }
    }

    private static void CreateCharacterEntity(SpawnCharacterTask task)
    {
        PlayerControlSystem.Handle PlayerControlSystemHandle = PlayerControlSystem.GetFromGlobalFunc.Execute(GameUtil.GetCurrentApiWorldHandle().GetUniverse());

        if (PlayerControlSystemHandle != nint.Zero)
        {
            apiEntity.Handle playerOne = PlayerControlSystemHandle.GetPlayerEntityForPlayerIdx(0);

            if (playerOne != nint.Zero)
            {
                apiEntity.Handle playerOneParent = playerOne.GetParent();

                apiSceneGraphResource.Handle sceneGraphHandle = task.CharacterPrefabResourceHandle.Get();

                apiEntity.Handle graphRoot = sceneGraphHandle.GetRoot();

                apiEntity.Handle createdEntity = graphRoot.Clone();
                createdEntity.SetParent(playerOneParent);

                apiTransformComponent.Handle createdEntityTransform = (apiTransformComponent.Handle)(nint)createdEntity.FindComponentByTypeName("apiTransformComponent");

                createdEntityTransform.SetPosition(task.Player.Transform.X, task.Player.Transform.Y, task.Player.Transform.Z);
                createdEntityTransform.SetRotation(task.Player.Transform.RX, task.Player.Transform.RY, task.Player.Transform.RZ);

                task.Player.Entity = createdEntity;
            }
        }
        else
        {
            throw new Exception("Could not create character entity");
        }
    }

    private static void CleanUpTask(SpawnCharacterTask task)
    {
        for(int i = 0; i < CharacterTasks.Count; i++)
        {
            if (task.Identifier == CharacterTasks[i].Identifier)
            {
                CharacterTasks.RemoveAt(i);
            }
        }

        throw new Exception("Could not find task to cleanup");
    }

    public static void ConsumeTasks()
    {
        foreach (SpawnCharacterTask task in CharacterTasks)
        {
            switch (task.State)
            {
                case CharacterSpawnState.ReadyToStart:
                    LoadResource(task);
                    break;
                case CharacterSpawnState.LoadingResource:
                    CheckResourceLoaded(task);
                    break;
                case CharacterSpawnState.ResourceLoaded:
                    CreateCharacterEntity(task);
                    break;
                case CharacterSpawnState.CharacterSpawned:
                    CleanUpTask(task);
                    break;
                default:
                    break;
            }
        }
    }

    public static void SpawnCharacter(NetworkedPlayer player)
    {
        SpawnCharacterTask task = new SpawnCharacterTask(player);
        CharacterTasks.Add(task);
    }
}
