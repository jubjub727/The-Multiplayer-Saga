using OMP.LSWTSS.CApi1;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace gameutil
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

        public static uint MoveModeOffset = 0x110;

        // |STRING CONSTANTS|
        public const string UniverseName = "MainUniverse";

        // |HANDLES|
        public static PlayerControlSystem.Handle PlayerControlSystemHandle;

        public static nttUniverseProcessingScope.Handle nttUniverseProcessingScopeHandle;

        public static ApiWorldProcessingScope.Handle apiWorldProcessingScopeHandle;

        // |TIMERS|
        public static Stopwatch TimeSinceLastFrame = Stopwatch.StartNew();

        // |STRUCTS|
        [StructLayout(LayoutKind.Sequential)]
        public struct NuVec
        {
            public float X;
            public float Y;
            public float Z;
        }

        // Gets the current ApiWorld
        public static ApiWorld.Handle GetCurrentApiWorldHandle()
        {
            ApiWorld.Handle apiWorldHandle = (ApiWorld.Handle)Marshal.ReadIntPtr(NativeFunc.GetPtr(CurrentApiWorldOffset));

            if (apiWorldHandle == IntPtr.Zero)
                throw new Exception("Couldn't retrieve current ApiWorldHandle (Are you calling this too early?)");

            return apiWorldHandle;
        }

        // Starts processing scopes
        public static void StartProcessingScopes()
        {
            GameUtil.nttUniverseProcessingScopeHandle = (nttUniverseProcessingScope.Handle)Marshal.AllocHGlobal(0x20);

            GameUtil.nttUniverseProcessingScopeConstructorDelegate nttUniverseProcessingScopeConstructor = NativeFunc.GetExecute<GameUtil.nttUniverseProcessingScopeConstructorDelegate>(NativeFunc.GetPtr(GameUtil.nttUniverseProcessingScopeConstructorOffset));

            nttUniverseProcessingScopeConstructor(GameUtil.nttUniverseProcessingScopeHandle, GameUtil.GetCurrentApiWorldHandle().GetUniverse(), true);

            GameUtil.apiWorldProcessingScopeHandle = (ApiWorldProcessingScope.Handle)Marshal.AllocHGlobal(0x20);

            GameUtil.ApiWorldProcessingScopeConstructorDelegate apiWorldProcessingScopeConstructor = NativeFunc.GetExecute<GameUtil.ApiWorldProcessingScopeConstructorDelegate>(NativeFunc.GetPtr(GameUtil.ApiWorldProcessingScopeConstructorOffset));

            apiWorldProcessingScopeConstructor(GameUtil.apiWorldProcessingScopeHandle, GameUtil.GetCurrentApiWorldHandle(), true);
        }

        // Stops processing scopes
        public static void StopProcessingScopes()
        {
            GameUtil.ApiWorldProcessingScopeDestructorDelegate apiWorldProcessingScopeDestructor = NativeFunc.GetExecute<GameUtil.ApiWorldProcessingScopeDestructorDelegate>(NativeFunc.GetPtr(GameUtil.apiWorldProcessingScopeDestructorOffset));

            apiWorldProcessingScopeDestructor(GameUtil.apiWorldProcessingScopeHandle);

            GameUtil.nttUniverseProcessingScopeDestructorDelegate nttUniverseProcessingScopeDestructor = NativeFunc.GetExecute<GameUtil.nttUniverseProcessingScopeDestructorDelegate>(NativeFunc.GetPtr(GameUtil.nttUniverseProcessingScopeDestructorOffset));

            nttUniverseProcessingScopeDestructor(GameUtil.nttUniverseProcessingScopeHandle);
        }
    }
}
