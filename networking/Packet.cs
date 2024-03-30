using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static LSWTSS.OMP.Game.Api.collectables;

namespace Networking
{
    public class Packet
    {
        public int writeHead = 0;
        public int readHead = 0;
        public Byte[] data;
        public NetworkMessage tick;

        public void WriteString(string input)
        {
            Byte[] bytes = Encoding.ASCII.GetBytes(input);
            Int32 len = bytes.Length;

            WriteInt32(len);

            for (int i = 0; i < len; i++)
            {
                data[writeHead] = bytes[i];
                writeHead++;
            }
        }

        public void WriteBytes(byte[] bytes)
        {
            foreach (Byte value in bytes)
            {
                data[writeHead] = value;
                writeHead++;
            }
        }

        public void WriteInt32(Int32 num)
        {
            Byte[] bytes = BitConverter.GetBytes(num);
            foreach (Byte value in bytes)
            {
                data[writeHead] = value;
                writeHead++;
            }
        }

        public void WriteUInt32(UInt32 num)
        {
            Byte[] bytes = BitConverter.GetBytes(num);
            foreach (Byte value in bytes)
            {
                data[writeHead] = value;
                writeHead++;
            }
        }

        public void WriteUInt16(UInt16 num)
        {
            Byte[] bytes = BitConverter.GetBytes(num);
            foreach (Byte value in bytes)
            {
                data[writeHead] = value;
                writeHead++;
            }
        }

        public void WriteUInt64(UInt64 num)
        {
            Byte[] bytes = BitConverter.GetBytes(num);
            foreach (Byte value in bytes)
            {
                data[writeHead] = value;
                writeHead++;
            }
        }

        public void WriteFloat(float num)
        {
            Byte[] bytes = BitConverter.GetBytes(num);
            foreach (Byte value in bytes)
            {
                data[writeHead] = value;
                writeHead++;
            }
        }

        public string ReadString()
        {
            Int32 len = ReadInt32();

            string output = Encoding.ASCII.GetString(data, readHead, len);

            readHead += len;

            return output;
        }

        public Int32 ReadInt32()
        {
            Int32 value = BitConverter.ToInt32(data, readHead);
            readHead += sizeof(Int32);

            return value;
        }

        public UInt32 ReadUInt32()
        {
            UInt32 value = BitConverter.ToUInt32(data, readHead);
            readHead += sizeof(UInt32);

            return value;
        }

        public UInt16 ReadUInt16()
        {
            UInt16 value = BitConverter.ToUInt16(data, readHead);
            readHead += sizeof(UInt16);

            return value;
        }

        public UInt64 ReadUInt64()
        {
            UInt64 value = BitConverter.ToUInt64(data, readHead);
            readHead += sizeof(UInt64);

            return value;
        }

        public float ReadFloat()
        {
            float value = BitConverter.ToSingle(data, readHead);
            readHead += sizeof(float);

            return value;
        }

        public Byte[] GetData()
        {
            return data;
        }

        public NetworkMessage Deserialize()
        {
            tick.Size = ReadUInt32();
            tick.NumberOfDataSegments = ReadInt32();
            tick.DataSegments = new DataSegment[tick.NumberOfDataSegments];

            for (int i = 0; i < tick.DataSegments.Length; i++)
            {
                tick.DataSegments[i] = DataSegment.Deserialize(this);
            }

            return tick;
        }

        public Packet(UInt32 size)
        {
            tick = new NetworkMessage();
            data = new Byte[size];
        }

        public Packet(Byte[] packet)
        {
            tick = new NetworkMessage();
            data = packet;
        }
    }
}
