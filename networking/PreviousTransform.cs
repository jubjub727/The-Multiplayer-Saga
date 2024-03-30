using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Networking
{
    public class PreviousTransform
    {
        public Transform Transform;
        public long ElapsedTime;

        public PreviousTransform(Transform transform, long elapsedTime)
        {
            Transform = transform;
            ElapsedTime = elapsedTime;
        }
    }
}
