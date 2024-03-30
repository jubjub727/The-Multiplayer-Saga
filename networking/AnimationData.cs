using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Networking
{
    // Type Index 2
    [Networked(2)]
    public class AnimationData
    {
        public string Animation = "";

        public AnimationData(string animation)
        {
            Animation = animation;
        }

        public AnimationData()
        {
            
        }
    }
}
