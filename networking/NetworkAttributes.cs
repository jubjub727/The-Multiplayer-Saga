﻿namespace Networking
{
    [System.AttributeUsage(System.AttributeTargets.Class)]
    public class Networked : System.Attribute
    {
        public int TypeIndex;
        public Networked(int typeIndex)
        {
            TypeIndex = typeIndex;
        }
    }

    [System.AttributeUsage(System.AttributeTargets.Field)]
    public class NotNetworked : System.Attribute
    {
        public NotNetworked()
        {

        }
    }
}
