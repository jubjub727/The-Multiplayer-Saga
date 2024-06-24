using OMP.LSWTSS.CApi1;
using gameutil;
using static gameutil.GameUtil;
using static OMP.LSWTSS.CApi1.CommonEvents.Interaction.Data;
using System.Diagnostics;

namespace Networking
{
    // Type Index 3
    [Networked(3)]
    public class NetworkedPlayer
    {
        //[NotNetworked]
        //private int Count = 0;

        //public bool JustJumped = false;

        //public bool SnapToGroundOn = false;
        public UInt16 PlayerId;
        public Transform Transform;
        public string Name = "";
        public string PrefabPath = "Chars/Minifig/Stormtrooper/Stormtrooper.prefab_baked";

        [NotNetworked]
        public PID PidX;

        [NotNetworked]
        public PID PidY;

        [NotNetworked]
        public PID PidZ;

        [NotNetworked]
        public Stopwatch TimeSinceLastApply = Stopwatch.StartNew();

        [NotNetworked]
        public bool IsLocal = false;

        [NotNetworked]
        public List<PreviousTransform> PreviousTransforms = new List<PreviousTransform>();

        [NotNetworked]
        private apiTransformComponent.Handle _apiTransformComponent = (apiTransformComponent.Handle)nint.Zero;

        [NotNetworked]
        public apiTransformComponent.Handle apiTransformComponent
        {
            get
            {
                if (_apiTransformComponent != nint.Zero)
                {
                    return _apiTransformComponent;
                }
                else
                {
                    _apiTransformComponent = (apiTransformComponent.Handle)(nint)Entity.FindComponentByTypeName("apiTransformComponent");
                    if (_apiTransformComponent == nint.Zero)
                    {
                        throw new Exception("Could not find apiTransformComponent");
                    }
                    return _apiTransformComponent;
                }
            }
        }

        [NotNetworked]
        private CharacterMoverComponent.Handle _CharacterMoverComponent = (CharacterMoverComponent.Handle)nint.Zero;

        [NotNetworked]
        public CharacterMoverComponent.Handle CharacterMoverComponent
        {
            get
            {
                if (_CharacterMoverComponent != nint.Zero)
                {
                    return _CharacterMoverComponent;
                }
                else
                {
                    _CharacterMoverComponent = (CharacterMoverComponent.Handle)(nint)Entity.FindComponentByTypeNameRecursive("CharacterMoverComponent", false);
                    if (_CharacterMoverComponent == nint.Zero)
                    {
                        throw new Exception("Could not find CharacterMoverComponent");
                    }
                    return _CharacterMoverComponent;
                }
            }
        }

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

            if (PreviousTransforms.Count > Utils.PacketDepth)
            {
                PreviousTransforms.RemoveAt(0);
            }

            Transform = transform;
        }

        public NetworkedPlayer(UInt16 playerId, string name)
        {
            PlayerId = playerId;
            Name = name;
            Transform = new Transform();
            PidX = new PID(Utils.P, Utils.I, Utils.D, Utils.N, Utils.OutputUpperLimit, Utils.OutputLowerLimit);
            PidY = new PID(Utils.P, Utils.I, Utils.D, Utils.N, Utils.OutputUpperLimit, Utils.OutputLowerLimit);
            PidZ = new PID(Utils.P, Utils.I, Utils.D, Utils.N, Utils.OutputUpperLimit, Utils.OutputLowerLimit);
        }

        public NetworkedPlayer(UInt16 playerId)
        {
            PlayerId = playerId;
            Transform = new Transform();
            PidX = new PID(Utils.P, Utils.I, Utils.D, Utils.N, Utils.OutputUpperLimit, Utils.OutputLowerLimit);
            PidY = new PID(Utils.P, Utils.I, Utils.D, Utils.N, Utils.OutputUpperLimit, Utils.OutputLowerLimit);
            PidZ = new PID(Utils.P, Utils.I, Utils.D, Utils.N, Utils.OutputUpperLimit, Utils.OutputLowerLimit);
        }

        public NetworkedPlayer()
        {
            Transform = new Transform();
            PidX = new PID(Utils.P, Utils.I, Utils.D, Utils.N, Utils.OutputUpperLimit, Utils.OutputLowerLimit);
            PidY = new PID(Utils.P, Utils.I, Utils.D, Utils.N, Utils.OutputUpperLimit, Utils.OutputLowerLimit);
            PidZ = new PID(Utils.P, Utils.I, Utils.D, Utils.N, Utils.OutputUpperLimit, Utils.OutputLowerLimit);
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

            //Console.WriteLine("Pos X - {0} | Pos Y - {1} | - Pos Z - {2}", X, Y, Z);

            HorizontalCharacterMover.Handle horizontalMover = (HorizontalCharacterMover.Handle)(nint)_Entity.FindComponentByTypeNameRecursive("HorizontalCharacterMover", false);
            if (horizontalMover == nint.Zero)
            {
                throw new Exception("Couldn't find HorizontalCharacterMover for entity");
            }

            CharacterMoverComponent.Handle characterMoverComponent = (CharacterMoverComponent.Handle)(nint)_Entity.FindComponentByTypeNameRecursive("CharacterMoverComponent", false);
            if (characterMoverComponent == nint.Zero)
            {
                throw new Exception("Couldn't find CharacterMoverComponent for entity");
            }

            if (Utils.IsBadFloat(transform.VX))
            {
                transform.VX = 0;
            }

            if (Utils.IsBadFloat(transform.VY))
            {
                transform.VY = 0;
            }

            if (Utils.IsBadFloat(transform.VZ))
            {
                transform.VZ = 0;
            }

            /*NuVec newVelocity = new NuVec();
            newVelocity.X = transform.VX + (transform.X - X);
            newVelocity.Y = transform.VY + (transform.Y - Y);
            newVelocity.Z = transform.VZ + (transform.Z - Z);*/

            float deltaX = transform.X - X;
            float deltaY = transform.Y - Y;
            float deltaZ = transform.Z - Z;

            /*if ((deltaX > 3f || deltaX < -3f) || (deltaY > 3f || deltaY < -3f) || (deltaZ > 3f || deltaZ < -3f))
            {
                currentTransform.SetPosition(transform.X, transform.Y+0.1f, transform.Z);
                TimeSinceLastApply.Restart();
            }
            else
            {*/
                TimeSinceLastApply.Stop();

                double newX = PidX.PID_iterate(transform.X, X, TimeSinceLastApply.Elapsed);
                double newY = 0.0;
                //double newY = PidY.PID_iterate(transform.Y, Y, TimeSinceLastApply.Elapsed);
                double newZ = PidZ.PID_iterate(transform.Z, Z, TimeSinceLastApply.Elapsed);

                //Console.WriteLine("X - {0} | Y - {1} | - Z - {2}", newX, newY, newZ);

                TimeSinceLastApply.Restart();

                NuVec newVelocity = new NuVec();
                newVelocity.X = (float)newX;
                newVelocity.Y = (float)newY;
                newVelocity.Z = (float)newZ;

                Console.WriteLine("Distance: {0} | Z - {1} | Requested Z - {2}", deltaZ, Z, transform.Z);

                unsafe
                {
                    NuVec* distancePtr = &newVelocity;

                    horizontalMover.SetMoveLaneVelocity((NuVec3.Handle)(nint)distancePtr);
                }
            //}
        }
    }
}
