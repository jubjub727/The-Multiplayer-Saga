using OMP.LSWTSS.CApi1;
using Networking;
using Riptide.Utils;
using System.Runtime.InteropServices;

namespace tmpsclient
{
    public static class GameUtil
    {
        // |DELEGATES|
        public delegate void nttUniverseProcessingScopeConstructorDelegate(nttUniverseProcessingScope.Handle handle, nttUniverse.Handle universe, bool flag);

        public delegate void nttUniverseProcessingScopeDestructorDelegate(nttUniverseProcessingScope.Handle handle);

        public delegate void ApiWorldProcessingScopeConstructorDelegate(ApiWorldProcessingScope.Handle handle, ApiWorld.Handle universe, bool flag);

        public delegate void ApiWorldProcessingScopeDestructorDelegate(ApiWorldProcessingScope.Handle handle);

        public delegate nint nttSceneGraphResourceConstructor(nint handle, int something);

        // |OFFSETS|
        public static uint CreateUniverseOffset = 0x2E47420;

        public static uint SceneGraphResourceConstructorOffset = 0x2DCDE60;

        public static uint CurrentApiWorldOffset = 0x5f129f8;

        public static uint nttUniverseProcessingScopeConstructorOffset = 0x2E4B3F0;

        public static uint ApiWorldProcessingScopeConstructorOffset = 0x2E4C050;

        public static uint apiWorldProcessingScopeDestructorOffset = 0x2E4C110;

        public static uint nttUniverseProcessingScopeDestructorOffset = 0x2E4B500;

        // |STRING CONSTANTS|
        public const string UniverseName = "MainUniverse";

        public const string DefaultName = "Bob";

        // |HANDLES|
        public static PlayerControlSystem.Handle PlayerControlSystemHandle;

        public static nttSceneGraphResourceHandle.Handle CharacterPrefabResourceHandle;

        public static nttUniverseProcessingScope.Handle nttUniverseProcessingScopeHandle;

        public static ApiWorldProcessingScope.Handle apiWorldProcessingScopeHandle;

        // |FLAGS|
        private static bool _ResourceLoaded = false;

        // Creates an entity at the position specified in transform
        public static apiEntity.Handle CreateEntity(Transform transform)
        {
            if (PlayerControlSystemHandle != nint.Zero)
            {
                var p1 = PlayerControlSystemHandle.GetPlayerEntityForPlayerIdx(0);

                if (p1 != nint.Zero)
                {
                    var p1Parent = p1.GetParent();

                    var graph = CharacterPrefabResourceHandle.Get();

                    var graphRoot = graph.GetRoot();

                    var newEntity = graphRoot.Clone();
                    newEntity.SetParent(p1Parent);
                    var newEntityTransform = (apiTransformComponent.Handle)(nint)newEntity.FindComponentByTypeName("apiTransformComponent");

                    newEntityTransform.SetPosition(transform.X, transform.Y, transform.Z);
                    newEntityTransform.SetRotation(transform.RX, transform.RY, transform.RZ);

                    return newEntity;
                }
            }

            throw new Exception("Could not create entity");
        }

        // Checks if the resource is loaded and loads it if it's not loaded
        public static bool LoadedResource()
        {
            if (_ResourceLoaded == false)
            {
                if (CharacterPrefabResourceHandle != nint.Zero)
                {
                    if (CharacterPrefabResourceHandle.IsLoaded())
                    {
                        RiptideLogger.Log(LogType.Info, "TMPS", String.Format("LOADED: " + CharacterPrefabResourceHandle.get_ResourcePath()));

                        _ResourceLoaded = true;

                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
                else
                {
                    StartLoadingResourceHandle();
                    return false;
                }
            }

            return false;
        }

        // Start the process of loading our resource
        private static void StartLoadingResourceHandle()
        {
            nttSceneGraphResourceConstructor nttSceneGraphResourceConstructor = Marshal.GetDelegateForFunctionPointer<nttSceneGraphResourceConstructor>(NativeFunc.GetPtr(SceneGraphResourceConstructorOffset));

            CharacterPrefabResourceHandle = (nttSceneGraphResourceHandle.Handle)Marshal.AllocHGlobal(0x88);

            for (int i = 0; i < 0x88; i++)
            {
                Marshal.WriteByte(CharacterPrefabResourceHandle, i, 0);
            }

            nttSceneGraphResourceConstructor(CharacterPrefabResourceHandle, 2);

            CharacterPrefabResourceHandle.set_ResourcePath("Chars/Minifig/Stormtrooper/Stormtrooper.prefab_baked");

            CharacterPrefabResourceHandle.AsyncLoad();
        }

        // Gets the current ApiWorld
        public static ApiWorld.Handle GetCurrentApiWorldHandle()
        {
            return (ApiWorld.Handle)Marshal.ReadIntPtr(NativeFunc.GetPtr(CurrentApiWorldOffset));
        }
    }
}
