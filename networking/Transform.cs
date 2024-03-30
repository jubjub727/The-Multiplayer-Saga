using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

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
            return false;
        }

        public Transform()
        {
            
        }
    }
}
