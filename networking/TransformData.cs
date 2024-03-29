using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Networking
{
    // Type Index 1
    [Networked(1)]
    public class TransformData
    {
        public UInt16 PlayerId;

        // Vec3 Position
        public float X = 0f;
        public float Y = 0f;
        public float Z = 0f;

        // Vec3 Rotation
        public float RX = 0f;
        public float RY = 0f;
        public float RZ = 0f;

        public TransformData(UInt16 playerID)
        {
            PlayerId = playerID;
        }

        public TransformData()
        {
            
        }
    }
}
