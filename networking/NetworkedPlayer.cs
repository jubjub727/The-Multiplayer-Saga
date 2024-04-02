using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LSWTSS.OMP.Game.Api;

namespace Networking
{
    // Type Index 3
    [Networked(3)]
    public class NetworkedPlayer
    {
        public UInt16 PlayerId;
        public Transform Transform;
        public string Name = "";

        [NotNetworked]
        public apiEntity.Handle Entity = (apiEntity.Handle)nint.Zero;

        [NotNetworked]
        public List<PreviousTransform> PreviousTransforms = new List<PreviousTransform>();

        public void SetTransform(Transform transform, long elapsedTime)
        {
            if (transform.IsBadTransform())
            {
                throw new Exception("Tried to set bad Transform");
            }

            PreviousTransform previousTransform = new PreviousTransform(transform, elapsedTime);
            PreviousTransforms.Add(previousTransform);

            if (PreviousTransforms.Count > 5)
            {
                PreviousTransforms.RemoveAt(0);
            }
        }
        public NetworkedPlayer(UInt16 playerId, apiEntity.Handle entity, Transform transform)
        {
            PlayerId = playerId;
            Transform = transform;
            Entity = entity;
        }
        public NetworkedPlayer(UInt16 playerId)
        {
            PlayerId = playerId;
            Transform = new Transform();
        }
        public void ApplyTransform(Transform transform)
        {
            if (transform.IsBadTransform())
            {
                throw new Exception("Tried to apply bad Transform");
            }

            // Do stuff here to apply the transform
        }
    }
}
