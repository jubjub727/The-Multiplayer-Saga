namespace Networking
{
    public class Transform
    {
        // Vec3 Position
        public float X = 0f;
        public float Y = 0f;
        public float Z = 0f;

        // Vec3 Rotation
        public float RX = 0f;
        public float RY = 0f;
        public float RZ = 0f;

        // Vec3 Velocity
        public float VX = 0f;
        public float VY = 0f;
        public float VZ = 0f;

        public bool IsBadTransform()
        {
            if (Utils.IsBadFloat(X))
            {
                return true;
            }
            if (Utils.IsBadFloat(Y))
            {
                return true;
            }
            if (Utils.IsBadFloat(Z))
            {
                return true;
            }
            if (Utils.IsBadFloat(RX))
            {
                return true;
            }
            if (Utils.IsBadFloat(RY))
            {
                return true;
            }
            if (Utils.IsBadFloat(RZ))
            {
                return true;
            }
            if (Utils.IsBadFloat(VX))
            {
                return true;
            }
            if (Utils.IsBadFloat(VY))
            {
                return true;
            }
            if (Utils.IsBadFloat(VZ))
            {
                return true;
            }
            return false;
        }

        public Transform()
        {
            
        }
    }
}
