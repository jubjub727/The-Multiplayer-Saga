using OMP.LSWTSS.CApi1;
using gameutil;
using static gameutil.GameUtil;
using static OMP.LSWTSS.CApi1.CommonEvents.Interaction.Data;

namespace Networking
{
    // Type Index 3
    [Networked(3)]
    public class NetworkedPlayer
    {
        //[NotNetworked]
        //private int Count = 0;

        public UInt16 PlayerId;
        public Transform Transform;
        public string Name = "";
        public string PrefabPath = "Chars/Minifig/Stormtrooper/Stormtrooper.prefab_baked";

        [NotNetworked]
        public bool IsLocal = false;

        [NotNetworked]
        public List<PreviousTransform> PreviousTransforms = new List<PreviousTransform>();

        [NotNetworked]
        private apiEntity.Handle _Entity = (apiEntity.Handle)nint.Zero;

        [NotNetworked]
        public apiEntity.Handle Entity
        {
            get
            {
                if (IsLocal)
                {
                    PlayerControlSystem.Handle playerControlSystemHandle = PlayerControlSystem.GetFromGlobalFunc.Execute(GameUtil.GetCurrentApiWorldHandle().GetUniverse());

                    apiEntity.Handle localPlayerEntity = playerControlSystemHandle.GetPlayerEntityForPlayerIdx(0);

                    if (localPlayerEntity == nint.Zero)
                        throw new Exception("Tried to access LocalPlayer Entity but retrieved 0");

                    return localPlayerEntity;
                }
                else
                {
                    return _Entity;
                }
            }
            set
            {
                if (IsLocal)
                    throw new Exception("Tried to set Entity on LocalPlayer");
                else
                {
                    _Entity = value;
                }
            }
        }

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

        public NetworkedPlayer()
        {
            Transform = new Transform();
        }
        public void ApplyTransform(Transform transform)
        {
            if (transform.IsBadTransform())
            {
                throw new Exception("Tried to apply bad Transform on NetworkedPlayer");
            }

            float X, Y, Z;
            apiTransformComponent.Handle currentTransform = (apiTransformComponent.Handle)(nint)_Entity.FindComponentByTypeName("apiTransformComponent");
            if (currentTransform == nint.Zero)
            {
                throw new Exception("Couldn't find Transform for entity");
            }

            currentTransform.GetPosition(out X, out Y, out Z);
            currentTransform.SetRotation(transform.RX, transform.RY, transform.RZ);

            NuVec distance = new NuVec();
            distance.X = Math.Clamp((transform.X - X) * Utils.Tickrate, -5f, 5f);
            distance.Y = Math.Clamp((transform.Y - Y) * Utils.Tickrate, -5f, 5f);
            distance.Z = Math.Clamp((transform.Z - Z) * Utils.Tickrate, -5f, 5f);

            /*if (Count > 64)
            {
                Console.WriteLine("{3} Distance:X - {0}, Y - {1}, Z - {2} | Current Position: X - {4}, Y - {5}, Z - {6} | Desired Position: X - {7}, Y - {8}, Z - {9}", distance.Z, distance.Y, distance.Z, Name, X, Y, Z, transform.X, transform.Y, transform.Z);

                Count = 0;
            }*/

            HorizontalCharacterMover.Handle horizontalMover = (HorizontalCharacterMover.Handle)(nint)_Entity.FindComponentByTypeNameRecursive("HorizontalCharacterMover", false);
            if (horizontalMover == nint.Zero)
            {
                throw new Exception("Couldn't find HorizontalCharacterMover for entity");
            }

            /*CharacterMoverComponent.Handle characterMover = (CharacterMoverComponent.Handle)(nint)_Entity.FindComponentByTypeNameRecursive("CharacterMoverComponent", false);
            if (horizontalMover == nint.Zero)
            {
                throw new Exception("Couldn't find CharacterMoverComponent for entity");
            }

            if (characterMover.OnGround())
            {
                characterMover.set_SnapToGroundOn(ref transform.SnapToGroundOn);
            }*/

            unsafe
            {
                NuVec* distancePtr = &distance;

                horizontalMover.SetMoveLaneVelocity((NuVec3.Handle)(nint)distancePtr);
            }

            //Count++;
        }
    }
}
