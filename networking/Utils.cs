using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Networking
{
    public class Utils
    {
        public const ushort SERVER_TICK_MESSAGE_ID = 1;
        public const ushort CLIENT_TICK_MESSAGE_ID = 2;

        public static bool IsBadFloat(float x)
        {
            return float.IsNaN(x) || float.IsInfinity(x);
        }
    }
}
