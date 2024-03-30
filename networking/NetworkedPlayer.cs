using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Networking
{
    // Type Index 3
    [Networked(3)]
    public class NetworkedPlayer
    {
        public UInt16 PlayerId;
        public Transform Transform;
        public string Name = "";
        public NetworkedPlayer(UInt16 playerId)
        {
            PlayerId = playerId;
            Transform = new Transform();
        }
        public NetworkedPlayer()
        {

        }
    }
}
