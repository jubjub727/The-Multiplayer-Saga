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
        public TransformData Transform;
        public List<PreviousTransform> PreviousTransforms = new List<PreviousTransform>();

        public apiEntity.Handle Entity;

        private bool IsLocal = false;

        public Player(UInt16 playerId, apiEntity.Handle entity)
        {
            PlayerId = playerId;
            Entity = entity;
            Transform = new TransformData(playerId);
        }

        public Player(UInt16 playerId)
        {
            PlayerId = playerId;
            IsLocal = true;
            Transform = new TransformData(playerId);
        }

        public void SetTransform(TransformData transform, long elapsedTime)
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

        public void ApplyTransform(TransformData transform)
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
