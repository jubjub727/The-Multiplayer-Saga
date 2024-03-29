﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Networking
{
    public class Tick
    {
        public UInt32 Size = 0;
        public Int32 NumberOfDataSegments;
        public DataSegment[]? DataSegments;

        public Tick(DataSegment[] dataSegments)
        {
            // Increment Size with the value of its own size (4 bytes)
            Size += sizeof(UInt32);

            NumberOfDataSegments = dataSegments.Length;
            Size += sizeof(Int32);

            DataSegments = dataSegments;

            foreach (DataSegment dataSegment in DataSegments)
            {
                Size += dataSegment.Size;
            }
        }

        public Tick()
        {
            
        }

        public Byte[] Serialize()
        {
            Packet packet = new Packet(Size);

            packet.WriteUInt32(Size);
            packet.WriteInt32(NumberOfDataSegments);

            foreach (DataSegment dataSegment in DataSegments)
            {
                dataSegment.Serialize(packet);
            }

            return packet.GetData();
        }
    }
}
