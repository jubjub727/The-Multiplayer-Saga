using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LSWTSS.OMP.Game.Api;
using LSWTSS.OMP;

namespace Networking
{
    public class Player
    {
        public UInt16 PlayerId;
        public Transform Transform;
        public List<PreviousTransform> PreviousTransforms = new List<PreviousTransform>();

        public apiEntity.Handle Entity;

        private bool IsLocal = false;

        public Player(UInt16 playerId, apiEntity.Handle entity)
        {
            PlayerId = playerId;
            Entity = entity;
            Transform = new Transform();
        }

        public Player(UInt16 playerId)
        {
            PlayerId = playerId;
            IsLocal = true;
            Transform = new Transform();
        }

        public void SetTransform(Transform transform, long elapsedTime)
        {
            if (Utils.IsBadTransform(transform))
            {
                return;
            }

            PreviousTransform previousTransform = new PreviousTransform(transform, elapsedTime);
            PreviousTransforms.Add(previousTransform);

            if (PreviousTransforms.Count > 5)
            {
                PreviousTransforms.RemoveAt(0);
            }
        }

        public void ApplyTransform(Transform transform)
        {
            apiTransformComponent.Handle transformComponent = (apiTransformComponent.Handle)(nint)Entity.FindComponentByTypeName("apiTransformComponent");

            if (Utils.IsBadTransform(transform) || transformComponent == nint.Zero)
            {
                return;
            }

            Transform = transform;

            transformComponent.SetPosition(transform.X, transform.Y, transform.Z);
            transformComponent.SetRotation(transform.RX, transform.RY, transform.RZ);
        }
    }
}
