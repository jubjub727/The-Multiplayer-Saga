using LSWTSS.OMP.Game.Api;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Networking;
using System.Net;
using Riptide.Utils;

namespace tmpsclient
{
    public static class GameUtil
    {
        delegate nint nttSceneGraphResourceConstructor(nint handle, int something);

        public static PlayerControlSystem.Handle _PlayerControlSystemHandle;

        public static nttSceneGraphResourceHandle.Handle _SceneGraphResourceHandle;

        private static bool _ResourceLoaded = false;
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

        public static bool LoadResource()
        {
            if (_ResourceLoaded == false && _SceneGraphResourceHandle != nint.Zero)
            {
                if (_SceneGraphResourceHandle.IsLoaded())
                {
                    RiptideLogger.Log(LogType.Info, "TMPS", String.Format("LOADED: " + _SceneGraphResourceHandle.get_ResourcePath()));

                    _ResourceLoaded = true;

                    return true;
                }
            }

            return false;
        }
    }
}
