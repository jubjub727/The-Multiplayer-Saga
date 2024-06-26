﻿namespace Networking
{
    public class Utils
    {
        public const ushort SERVER_TICK_MESSAGE_ID = 1;
        public const ushort CLIENT_TICK_MESSAGE_ID = 2;

        public const ushort CLIENT_ACTION_MESSAGE_ID = 3;
        public const ushort SERVER_ACTION_MESSAGE_ID = 4;

        public const ushort JUMP_ACTION_ID = 0;

        public const string DefaultName = "Bob";
        public const string DummyClientName = "DummyClient";

        public const UInt32 Tickrate = 64;

        public const int DefaultTimeout = 3000;

        public const int PacketDepth = 24;
        public const int PacketOffset = 16;

        public const double P = 16;
        public const double I = 0;
        public const double D = 0;
        public const double N = 1;
        public const double OutputUpperLimit = 2.5;
        public const double OutputLowerLimit = -2.5;


        public static bool IsBadFloat(float x)
        {
            return float.IsNaN(x) || float.IsInfinity(x);
        }
    }
}
