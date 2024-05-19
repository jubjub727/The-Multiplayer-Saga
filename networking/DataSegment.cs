namespace Networking
{
    public class DataSegment
    {
        public UInt32 Size = 0;
        public Int32 TypeIndex;
        public dynamic Data;

        public DataSegment(dynamic data)
        {
            // Increment Size with the value of its own size (4 bytes)
            Size += sizeof(UInt32);

            TypeIndex = Serialization.GetTypeIndex(data);
            Size += sizeof(Int32);

            Data = data;
            Size += Serialization.GetSize(data);
        }

        public DataSegment()
        {
            
        }

        public void Serialize(Packet packet)
        {
            packet.WriteUInt32(Size);
            packet.WriteInt32(TypeIndex);
            Serialization.Serialize(packet, Data);
        }

        public static DataSegment Deserialize(Packet packet)
        {
            DataSegment dataSegment = new DataSegment();
            dataSegment.Size = packet.ReadUInt32();
            dataSegment.TypeIndex = packet.ReadInt32();

            dataSegment.Data = Serialization.Deserialize(packet, dataSegment.TypeIndex);

            return dataSegment;
        }
    }
}
