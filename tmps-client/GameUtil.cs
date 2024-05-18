using OMP.LSWTSS.CApi1;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Networking;
using System.Net;
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

        public static uint SceneGraphResourceConstructorOffset = 0x2dcde60;

        public static uint CurrentApiWorldOffset = 0x5f129f8;

        public static uint nttUniverseProcessingScopeConstructorOffset = 0x2E4B3F0;

        public static uint ApiWorldProcessingScopeConstructorOffset = 0x2E4C050;

        public static uint apiWorldProcessingScopeDestructorOffset = 0x2E4C110;
        public static uint nttUniverseProcessingScopeDestructorOffset = 0x2E4B500;

        // |STRING CONSTANTS|
        public const string UniverseName = "MainUniverse";

        public const string DefaultName = "Bob";

        // |HANDLES|
        public static nttUniverse.Handle MainUniverse = (nttUniverse.Handle)nint.Zero;

        public static PlayerControlSystem.Handle _PlayerControlSystemHandle;

        public static nttSceneGraphResourceHandle.Handle _SceneGraphResourceHandle;

        public static nttUniverseProcessingScope.Handle _nttUniverseProcessingScopeHandle;

        public static ApiWorldProcessingScope.Handle _apiWorldProcessingScopeHandle;

        // |FLAGS|
        private static bool _ResourceLoaded = false;

        // Creates an entity at the position specified in transform
        public static apiEntity.Handle CreateEntity(Transform transform)
        {
            if (_PlayerControlSystemHandle != nint.Zero)
            {
                var p1 = _PlayerControlSystemHandle.GetPlayerEntityForPlayerIdx(0);

                if (p1 != nint.Zero)
                {
                    var p1Parent = p1.GetParent();

                    var graph = _SceneGraphResourceHandle.Get();

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
            Console.WriteLine("Loading resource stuff");
            if (_ResourceLoaded == false)
            {
                Console.WriteLine("Resource not loaded");
                if (_SceneGraphResourceHandle != nint.Zero)
                {
                    Console.WriteLine("Resource handle Exists");
                    if (_SceneGraphResourceHandle.IsLoaded())
                    {
                        RiptideLogger.Log(LogType.Info, "TMPS", String.Format("LOADED: " + _SceneGraphResourceHandle.get_ResourcePath()));

                        _ResourceLoaded = true;

                        return true;
                    }
                    else
                    {
                        Console.WriteLine("Resource handle still loading");
                        return false;
                    }
                }
                else
                {
                    if (MainUniverse != nint.Zero)
                    {
                        Console.WriteLine("MainUniverse exists");
                        StartLoadingResourceHandle();
                        return false;
                    }
                    else
                    {
                        Console.WriteLine("MainUniverse doesn't exist");
                        return false;
                    }
                }
            }

            return true;
        }

        // Start the process of loading our resource
        private static void StartLoadingResourceHandle()
        {
            Console.WriteLine("Starting to load resource");
            if (MainUniverse == nint.Zero)
            {
                RiptideLogger.Log(LogType.Error, "TMPS", String.Format("Tried to load resource handle before MainUniverse was set"));

                throw new Exception("Tried to access MainUniverse before it was assigned a value");
            }

            _PlayerControlSystemHandle = PlayerControlSystem.GetFromGlobalFunc.Execute(MainUniverse);

            nttSceneGraphResourceConstructor _nttSceneGraphResourceConstructor = Marshal.GetDelegateForFunctionPointer<nttSceneGraphResourceConstructor>(NativeFunc.GetPtr(SceneGraphResourceConstructorOffset));

            nint _nttSceneGraphResourceRawHandle = Marshal.AllocHGlobal(0x88);

            for (int i = 0; i < 0x88; i++)
            {
                Marshal.WriteByte(_nttSceneGraphResourceRawHandle, i, 0);
            }

            _nttSceneGraphResourceConstructor(_nttSceneGraphResourceRawHandle, 2);

            _SceneGraphResourceHandle = (nttSceneGraphResourceHandle.Handle)_nttSceneGraphResourceRawHandle;

            _SceneGraphResourceHandle.set_ResourcePath("Chars/Minifig/Stormtrooper/Stormtrooper.prefab_baked");
            _SceneGraphResourceHandle.AsyncLoad();
        }

        // Gets the current ApiWorld
        public static ApiWorld.Handle GetCurrentApiWorldHandle()
        {
            return (ApiWorld.Handle)Marshal.ReadIntPtr(NativeFunc.GetPtr(CurrentApiWorldOffset));
        }
    }
}
