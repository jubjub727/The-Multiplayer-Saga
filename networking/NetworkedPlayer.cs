using OMP.LSWTSS.CApi1;

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
                throw new Exception("Tried to set bad Transform on NetworkedPlayer");
            }

            PreviousTransform previousTransform = new PreviousTransform(transform, elapsedTime);
            PreviousTransforms.Add(previousTransform);

            if (PreviousTransforms.Count > 5)
            {
                PreviousTransforms.RemoveAt(0);
            }
        }

        public void AssignEntity(apiEntity.Handle entity)
        {
            Entity = entity;
        }

        public NetworkedPlayer(UInt16 playerId, string name)
        {
            PlayerId = playerId;
            Name = name;
            Transform = new Transform();
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
                throw new Exception("Tried to apply bad Transform on NetworkedPlayer");
            }

            // Do stuff here to apply the transform
        }
    }
}
