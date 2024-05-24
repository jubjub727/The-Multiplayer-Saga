﻿using OMP.LSWTSS.CApi1;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Networking
{
    // Type Index 4
    [Networked(4)]
    public class NetworkedAction
    {
        public UInt16 PlayerId;
        public UInt16 ActionId;
        public float Amount;

        [NotNetworked]
        public NetworkedPlayer Player;

        public NetworkedAction()
        {

        }

        public NetworkedAction(UInt16 playerId, UInt16 actionId, float amount)
        {
            PlayerId = playerId;
            ActionId = actionId;
            Amount = amount;
        }

        public void AssignPlayer(NetworkedPlayer player)
        {
            Player = player;
        }

        private void Jump()
        {
            unsafe
            {
                CharacterMoverComponent.Handle characterMoverComponent = (CharacterMoverComponent.Handle)(nint)Player.Entity.FindComponentByTypeNameRecursive("CharacterMoverComponent", false);

                int* moveModePtr = (int*)((nint)characterMoverComponent + 0x110);
                *moveModePtr = 2;

                //bool snapToGroundOn = false;

                //characterMoverComponent.set_SnapToGroundOn(ref snapToGroundOn);
                characterMoverComponent.Jump(ref Amount);
                //characterMoverComponent.set_SnapToGroundOn(ref Player.Transform.SnapToGroundOn);
                //*moveModePtr = 0;
            }

        }

        public void ProcessAction()
        {
            switch (ActionId)
            {
                // Jump!
                case Utils.JUMP_ACTION_ID:
                    Jump();
                    break;
                default:
                    throw new Exception("Received Uknown ActionId");
            }
        }
    }
}
