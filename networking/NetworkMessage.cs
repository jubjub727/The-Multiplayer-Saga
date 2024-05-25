namespace Networking
{
    public class NetworkMessage
    {
        public UInt32 Size = 0;
        public Int32 NumberOfDataSegments;
        public UInt64 Tick;
        public DataSegment[] DataSegments = new DataSegment[0];

        public NetworkMessage(DataSegment[] dataSegments, UInt64 tick)
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

            Tick = tick;
        }

        public NetworkMessage()
        {
            
        }

        public Byte[] Serialize()
        {
            Packet packet = new Packet(Size);

            packet.WriteUInt32(Size);
            packet.WriteUInt64(Tick);
            packet.WriteInt32(NumberOfDataSegments);

            foreach (DataSegment dataSegment in DataSegments)
            {
                dataSegment.Serialize(packet);
            }

            return packet.GetData();
        }
    }
}
