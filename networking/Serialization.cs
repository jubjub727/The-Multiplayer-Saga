using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;

namespace Networking
{
    public static class Serialization
    {
        public static bool TypesLoaded = false;

        private static Type[] AvailableTypes;

        public static void LoadAvailableTypes()
        {
            AvailableTypes = Assembly.GetExecutingAssembly().GetTypes();
            TypesLoaded = true;
        }
        private static Networked? GetNetworkedAttribute(Attribute[] attributes)
        {
            foreach (Attribute attribute in attributes)
            {
                if (attribute is Networked networked)
                {
                    return networked;
                }
            }

            return null;
        }
        private static bool IsNetworked(Attribute[] attributes)
        {
            foreach (Attribute attribute in attributes)
            {
                if (attribute is Networked)
                {
                    return true;
                }
            }

            return false;
        }

        private static bool IsNotNetworked(Attribute[] attributes)
        {
            foreach (Attribute attribute in attributes)
            {
                if (attribute is NotNetworked)
                {
                    return true;
                }
            }

            return false;
        }

        private static dynamic ReadField(Packet packet, FieldInfo field)
        {
            switch (Type.GetTypeCode(field.FieldType))
            {
                case TypeCode.Single:
                    return packet.ReadFloat();
                case TypeCode.Int32:
                    return packet.ReadInt32();
                case TypeCode.UInt32:
                    return packet.ReadUInt32();
                case TypeCode.UInt64:
                    return packet.ReadUInt64();
                case TypeCode.UInt16:
                    return packet.ReadUInt16();
                case TypeCode.String:
                    return packet.ReadString();
                case TypeCode.Object:
                    var value = Activator.CreateInstance(field.FieldType);
                    IterateFieldsAndRead(packet, value);
                    return value;
                default:
                    throw new Exception("Found Unsupported Field on Networked class");
            }
        }

        private static void WriteField(Packet packet, FieldInfo field, dynamic data)
        {
            switch (Type.GetTypeCode(field.FieldType))
            {
                case TypeCode.Single:
                    float? floatValue = (float?)field.GetValue(data);
                    if (floatValue == null)
                    {
                        throw new Exception("Received Null Value for Field (" + field.Name + ")");
                    }
                    packet.WriteFloat(floatValue.Value);
                    return;
                case TypeCode.Int32:
                    Int32? int32Value = (Int32?)field.GetValue(data);
                    if (int32Value == null)
                    {
                        throw new Exception("Received Null Value for Field (" + field.Name + ")");
                    }
                    packet.WriteInt32(int32Value.Value);
                    return;
                case TypeCode.UInt32:
                    UInt32? uint32Value = (UInt32?)field.GetValue(data);
                    if (uint32Value == null)
                    {
                        throw new Exception("Received Null Value for Field (" + field.Name + ")");
                    }
                    packet.WriteUInt32(uint32Value.Value);
                    return;
                case TypeCode.UInt64:
                    UInt64? uint64Value = (UInt64?)field.GetValue(data);
                    if (uint64Value == null)
                    {
                        throw new Exception("Received Null Value for Field (" + field.Name + ")");
                    }
                    packet.WriteUInt64(uint64Value.Value);
                    return;
                case TypeCode.UInt16:
                    UInt16? uint16Value = (UInt16?)field.GetValue(data);
                    if (uint16Value == null)
                    {
                        throw new Exception("Received Null Value for Field (" + field.Name + ")");
                    }
                    packet.WriteUInt16(uint16Value.Value);
                    return;
                case TypeCode.String:
                    string? stringValue = (string?)field.GetValue(data);
                    if (stringValue == null)
                    {
                        throw new Exception("Received Null Value for Field (" + field.Name + ")");
                    }
                    packet.WriteString(stringValue);
                    return;
                case TypeCode.Object:
                    var value = (object?)field.GetValue(data);
                    if (value == null)
                    {
                        throw new Exception("Received Null Value for Field (" + field.Name + ")");
                    }
                    IterateFieldsAndWrite(packet, value);
                    return;
                default:
                    throw new Exception("Found Unsupported Field on Networked class");
            }
        }

        private static void IterateFieldsAndRead(Packet packet, dynamic data)
        {
            Type type = data.GetType();

            foreach (FieldInfo field in type.GetFields())
            {
                if (IsNotNetworked(Attribute.GetCustomAttributes(field)))
                {
                    continue;
                }

                field.SetValue(data, ReadField(packet, field));
            }
        }

        private static void IterateFieldsAndWrite(Packet packet, dynamic data)
        {
            Type type = data.GetType();

            foreach (FieldInfo field in type.GetFields())
            {
                if (IsNotNetworked(Attribute.GetCustomAttributes(field)))
                {
                    continue;
                }

                WriteField(packet, field, data);
            }
        }

        public static void Serialize(Packet packet, dynamic data)
        {
            if (IsNetworked(Attribute.GetCustomAttributes(data.GetType())))
            {
                IterateFieldsAndWrite(packet, data);
            }
            else
            {
                throw new Exception("Attempted to Serialize class without Networked attribute");
            }
        }

        public static dynamic Deserialize(Packet packet, int TypeIndex)
        {
            Type type = FindDataType(TypeIndex);
            var data = Activator.CreateInstance(type);

            if (IsNetworked(Attribute.GetCustomAttributes(type)))
            {
                IterateFieldsAndRead(packet, data);
            }
            else
            {
                throw new Exception("Attempted to Deserialize class without Networked attribute");
            }

            return data;
        }

        private static UInt32 GetStringSize(string input)
        {
            return Convert.ToUInt32(Encoding.ASCII.GetBytes(input).Length) + sizeof(Int32);
        }

        private static UInt32 IterateFieldsAndCount(Type type, dynamic data)
        {
            UInt32 size = 0;

            foreach (FieldInfo field in type.GetFields())
            {
                if (IsNotNetworked(Attribute.GetCustomAttributes(field)))
                {
                    continue;
                }

                if (field.FieldType == typeof(string))
                {
                    size += GetStringSize(field.GetValue(data));
                }
                else if (Type.GetTypeCode(field.FieldType) == TypeCode.Object)
                {
                    size += IterateFieldsAndCount(field.FieldType, field.GetValue(data));
                }
                else
                {
                    size += Convert.ToUInt32(Marshal.SizeOf(field.FieldType));
                }
            }

            return size;
        }

        public static UInt32 GetSize(dynamic data)
        {
            Type type = data.GetType();
            if (IsNetworked(Attribute.GetCustomAttributes(type)))
            {
                return IterateFieldsAndCount(type, data);
            }
            else
            {
                throw new Exception("Attempted to Get Size for a class without Networked attribute");
            }
        }

        private static Type FindDataType(int TypeIndex)
        {
            foreach (Type type in AvailableTypes)
            {
                Networked? attribute = GetNetworkedAttribute(Attribute.GetCustomAttributes(type));
                if (attribute != null)
                {
                    if (attribute.TypeIndex == TypeIndex)
                    {
                        return type;
                    }
                }
            }
            throw new Exception(string.Format("Could not find a Type with the TypeIndex - {0}", TypeIndex));
        }

        public static int GetTypeIndex(dynamic data)
        {
            Type type = data.GetType();

            Networked? attribute = GetNetworkedAttribute(Attribute.GetCustomAttributes(type));
            if (attribute != null)
            {
                return attribute.TypeIndex;
            }

            throw new Exception(string.Format("Could not find a TypeIndex for Data Type (" + type.Name + ")"));
        }
    }
}
