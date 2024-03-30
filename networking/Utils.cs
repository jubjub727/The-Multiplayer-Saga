using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Networking
{
    public class Utils
    {
        public static bool IsBadFloat(float x)
        {
            return float.IsNaN(x) || float.IsInfinity(x);
        }

        public static bool IsBadTransform(Transform transform)
        {
            if (IsBadFloat(transform.X))
            {
                return true;
            }
            if (IsBadFloat(transform.Y))
            {
                return true;
            }
            if (IsBadFloat(transform.Z))
            {
                return true;
            }
            if (IsBadFloat(transform.RX))
            {
                return true;
            }
            if (IsBadFloat(transform.RY))
            {
                return true;
            }
            if (IsBadFloat(transform.RZ))
            {
                return true;
            }
            return false;
        }
    }
}
