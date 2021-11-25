using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

namespace WillowTree.Services.DataAccess
{
    public class WillowSaveGameBase
    {
        public const byte SubPart = 0x20;
        public const int Section1Id = 0x43211234;
        public const int Section2Id = 0x02151984;
        public const int Section3Id = 0x32235947;
        public const int Section4Id = 0x234BA901;
        protected const int EnhancedVersion = 0x27;

        protected static byte[] ReadBytes(BinaryReader reader, int fieldSize, ByteOrder byteOrder)
        {
            var bytes = reader.ReadBytes(fieldSize);
            if (bytes.Length != fieldSize)
            {
                throw new EndOfStreamException();
            }

            if (BitConverter.IsLittleEndian)
            {
                if (byteOrder == ByteOrder.BigEndian)
                {
                    Array.Reverse(bytes);
                }
            }
            else
            {
                if (byteOrder == ByteOrder.LittleEndian)
                {
                    Array.Reverse(bytes);
                }
            }

            return bytes;
        }

        protected static byte[] ReadBytes(byte[] inBytes, int fieldSize, ByteOrder byteOrder)
        {
            Debug.Assert(inBytes != null, "inBytes != null");
            Debug.Assert(inBytes.Length >= fieldSize, "inBytes.Length >= fieldSize");

            var outBytes = new byte[fieldSize];
            Buffer.BlockCopy(inBytes, 0, outBytes, 0, fieldSize);

            if (BitConverter.IsLittleEndian)
            {
                if (byteOrder == ByteOrder.BigEndian)
                {
                    Array.Reverse(outBytes, 0, fieldSize);
                }
            }
            else
            {
                if (byteOrder == ByteOrder.LittleEndian)
                {
                    Array.Reverse(outBytes, 0, fieldSize);
                }
            }

            return outBytes;
        }

        protected static float ReadSingle(BinaryReader reader, ByteOrder endian)
        {
            return BitConverter.ToSingle(ReadBytes(reader, sizeof(float), endian), 0);
        }

        protected static int ReadInt32(BinaryReader reader, ByteOrder endian)
        {
            return BitConverter.ToInt32(ReadBytes(reader, sizeof(int), endian), 0);
        }

        protected static short ReadInt16(BinaryReader reader, ByteOrder endian)
        {
            return BitConverter.ToInt16(ReadBytes(reader, sizeof(short), endian), 0);
        }

        protected static List<int> ReadListInt32(BinaryReader reader, ByteOrder endian)
        {
            var count = ReadInt32(reader, endian);
            var list = new List<int>(count);
            for (var i = 0; i < count; i++)
            {
                var value = ReadInt32(reader, endian);
                list.Add(value);
            }

            return list;
        }

        protected static void Write(BinaryWriter writer, float value, ByteOrder endian)
        {
            writer.Write(BitConverter.ToSingle(ReadBytes(BitConverter.GetBytes(value), sizeof(float), endian), 0));
        }

        protected static void Write(BinaryWriter writer, int value, ByteOrder endian)
        {
            writer.Write(BitConverter.ToInt32(ReadBytes(BitConverter.GetBytes(value), sizeof(int), endian), 0));
        }

        protected static void Write(BinaryWriter writer, short value, ByteOrder endian)
        {
            writer.Write(ReadBytes(BitConverter.GetBytes(value), sizeof(short), endian));
        }

        protected static byte[] GetBytesFromInt(int value, ByteOrder endian)
        {
            return ReadBytes(BitConverter.GetBytes(value), sizeof(int), endian);
        }

        protected static byte[] GetBytesFromInt(uint value, ByteOrder endian)
        {
            return ReadBytes(BitConverter.GetBytes(value), sizeof(int), endian);
        }

        protected static void Write(BinaryWriter writer, string value, ByteOrder endian)
        {
            // Null and empty strings are treated the same (with an output
            // length of zero).
            if (string.IsNullOrEmpty(value))
            {
                writer.Write(0);
                return;
            }

            bool requiresUnicode = IsUnicode(value);
            // Generate the bytes (either single-byte or Unicode, depending on input).
            if (!requiresUnicode)
            {
                // Write character length (including null terminator).
                Write(writer, value.Length + 1, endian);

                // Write single-byte encoded string.
                writer.Write(SaveEncoding.SingleByteEncoding.GetBytes(value));

                // Write null terminator.
                writer.Write((byte)0);
            }
            else
            {
                // Write character length (including null terminator).
                Write(writer, -1 - value.Length, endian);

                // Write UTF-16 encoded string.
                writer.Write(Encoding.Unicode.GetBytes(value));

                // Write null terminator.
                writer.Write((short)0);
            }
        }

        /// <summary> Look for any non-ASCII characters in the input.</summary>
        private static bool IsUnicode(string value)
        {
            return value.Any(t => t > 256);
        }

        protected static byte[] GetBytesFromString(string value, ByteOrder endian)
        {
            var bytes = new List<byte>();
            // Null and empty strings are treated the same (with an output
            // length of zero).
            if (string.IsNullOrEmpty(value))
            {
                return bytes.ToArray();
            }

            bool requiresUnicode = IsUnicode(value);
            // Generate the bytes (either single-byte or Unicode, depending on input).
            if (!requiresUnicode)
            {
                bytes.AddRange(GetBytesFromInt(value.Length + 1, endian));
                bytes.AddRange(SaveEncoding.SingleByteEncoding.GetBytes(value));
                bytes.Add(0);
            }
            else
            {
                bytes.AddRange(GetBytesFromInt(-1 - value.Length, endian));
                bytes.AddRange(Encoding.Unicode.GetBytes(value));
                bytes.Add(0);
                bytes.Add(0);
            }

            return bytes.ToArray();
        }

        ///<summary>Reads a string in the format used by the WSGs</summary>
        protected static string ReadString(BinaryReader reader, ByteOrder endian)
        {
            int tempLengthValue = ReadInt32(reader, endian);
            if (tempLengthValue == 0)
            {
                return string.Empty;
            }

            string value;

            // matt911: Borderlands doesn't ever use unicode strings as far
            // as I can tell.  All text seems to be single-byte encoded with a code
            // page for the current culture so tempLengthValue is always positive.
            //
            // da_fileserver implemented the unicode string reading to agree with the
            // way that unreal engine 3 uses it I think, but I can't test this code
            // because I know of no place where Borderlands itself actually uses it.
            //
            // It appears to me that ReadString and WriteString are filled with a lot of
            // unnecessary code to deal with unicode, but since the code is already
            // implemented and I haven't had any problems I'd rather leave in code that
            // is not necessary than break something that works already

            // Read string data (either single-byte or Unicode).
            if (tempLengthValue < 0)
            {
                // Convert the length value into a unicode byte count.
                tempLengthValue = -tempLengthValue * 2;

                // If the string length is over 4K assume that the string is invalid.
                // This prevents an out of memory exception in the case of invalid data.
                if (tempLengthValue > 4096)
                {
                    throw new InvalidDataException("String length was too long.");
                }

                // Read the byte data (and ensure that the number of bytes
                // read matches the number of bytes it was supposed to read--
                // BinaryReader may not return the same number of bytes read).
                byte[] data = reader.ReadBytes(tempLengthValue);
                if (data.Length != tempLengthValue)
                {
                    throw new EndOfStreamException();
                }

                // Convert the byte data into a string.
                value = Encoding.Unicode.GetString(data);
            }
            else
            {
                // If the string length is over 4K assume that the string is invalid.
                // This prevents an out of memory exception in the case of invalid data.
                if (tempLengthValue > 4096)
                {
                    throw new InvalidDataException("String length was too long.");
                }

                // Read the byte data (and ensure that the number of bytes
                // read matches the number of bytes it was supposed to read--
                // BinaryReader may not return the same number of bytes read).
                byte[] data = reader.ReadBytes(tempLengthValue);
                if (data.Length != tempLengthValue)
                {
                    throw new EndOfStreamException();
                }

                // Convert the byte data into a string.
                value = SaveEncoding.SingleByteEncoding.GetString(data);
            }

            // Look for the null terminator character. If not found then then string is
            // probably corrupt.
            int nullTerminatorIndex = value.IndexOf('\0');
            if (nullTerminatorIndex != value.Length - 1)
            {
                throw new InvalidDataException("String was not properly terminated with a null character.");
            }

            // Return the string, excluding the null terminator
            return value.Substring(0, nullTerminatorIndex);
        }

        ///<summary>Reads a string in the format used by the WSGs</summary>
        protected static byte[] SearchForString(BinaryReader reader, ByteOrder endian)
        {
            var bytes = new List<byte>();

            while (true)
            {
                var position = reader.BaseStream.Position;
                int tempLengthValue = ReadInt32(reader, endian);
                bool isLess = false;
                if (tempLengthValue < 0)
                {
                    tempLengthValue = -tempLengthValue * 2;
                    isLess = true;
                }

                if (tempLengthValue < 5 || tempLengthValue > 4096)
                {
                    bytes.AddRange(GetBytesFromInt(tempLengthValue, endian));
                    continue;
                }

                var data = reader.ReadBytes(tempLengthValue);
                if (data.Length != tempLengthValue)
                {
                    throw new EndOfStreamException();
                }

                var value = isLess
                    ? Encoding.Unicode.GetString(data)
                    : SaveEncoding.SingleByteEncoding.GetString(data);
                int nullTerminatorIndex = value.IndexOf('\0');
                if (nullTerminatorIndex != value.Length - 1)
                {
                    bytes.AddRange(GetBytesFromInt(tempLengthValue, endian));
                }
                else
                {
                    Console.WriteLine("Value :" + value);
                    //Found String put the file cursor just before
                    reader.BaseStream.Position = position;
                    break;
                }
            }

            // Return the string, excluding the null terminator
            return bytes.ToArray();
        }

        public static List<string> ReadItemStrings(BinaryReader reader, ByteOrder byteOrder)
        {
            List<string> strings = new List<string>();
            for (int index = 0; index < 9; index++)
            {
                strings.Add(ReadString(reader, byteOrder));
            }

            return strings;
        }

        public static List<string> ReadWeaponStrings(BinaryReader reader, ByteOrder bo)
        {
            List<string> strings = new List<string>();
            for (int index = 0; index < 14; index++)
            {
                strings.Add(ReadString(reader, bo));
            }

            return strings;
        }

        protected static bool IsEndOfFile(BinaryReader binaryReader)
        {
            var bs = binaryReader.BaseStream;
            return (bs.Position == bs.Length);
        }

        ///<summary>Reports back the expected platform this WSG was created on.</summary>
        protected static string ReadPlatform(Stream inputWsg)
        {
            BinaryReader saveReader = new BinaryReader(inputWsg);

            byte byte1 = saveReader.ReadByte();
            byte byte2 = saveReader.ReadByte();
            byte byte3 = saveReader.ReadByte();
            if (byte1 == 'C' && byte2 == 'O' && byte3 == 'N')
            {
                byte byte4 = saveReader.ReadByte();
                if (byte4 != ' ') return "Not WSG";

                // This is a really lame way to check for the WSG data...
                saveReader.BaseStream.Seek(0xCFFC, SeekOrigin.Current);

                byte1 = saveReader.ReadByte();
                byte2 = saveReader.ReadByte();
                byte3 = saveReader.ReadByte();
                if (byte1 == 'W' && byte2 == 'S' && byte3 == 'G')
                {
                    saveReader.BaseStream.Seek(0x360, SeekOrigin.Begin);
                    uint titleId = ((uint)saveReader.ReadByte() << 0x18) +
                                   ((uint)saveReader.ReadByte() << 0x10) +
                                   ((uint)saveReader.ReadByte() << 0x8) +
                                   saveReader.ReadByte();
                    switch (titleId)
                    {
                        case 0x545407E7:
                            return "X360";
                        case 0x54540866:
                            return "X360JP";
                        default:
                            return "unknown";
                    }
                }
            }
            else if (byte1 == 'W' && byte2 == 'S' && byte3 == 'G')
            {
                int wsgVersion = saveReader.ReadInt32();

                // BinaryReader.ReadInt32 always uses little-endian byte order.
                bool littleEndian;
                switch (wsgVersion)
                {
                    case 0x02000000: // 33554432 decimal
                        littleEndian = false;
                        break;

                    case 0x00000002:
                        littleEndian = true;
                        break;

                    default:
                        return "unknown";
                }

                return littleEndian ? "PC" : "PS3";
            }

            return "Not WSG";
        }

        protected static void Write(BinaryWriter writer, byte[] value)
        {
            writer.Write(value);
        }

        public static void ReadOldFooter(BankEntry entry, BinaryReader reader, ByteOrder endian)
        {
            var footer = reader.ReadBytes(0xA);
            entry.EquippedSlot = footer[0x8];
            entry.Quantity = entry.TypeId == 0x1 ? ReadInt32(reader, endian) : reader.ReadByte();
        }

        public static void ReadNewFooter(BankEntry entry, BinaryReader reader, ByteOrder endian)
        {
            var footer = reader.ReadBytes(0xC);
            entry.EquippedSlot = footer[0x8];
            if (entry.TypeId == 0x1)
            {
                entry.Quantity = ReadInt32(reader, endian); //Ammo
                entry.Junk = footer[0xA];
                entry.Locked = footer[0xB];
            }
            else
            {
                entry.Quantity = footer[0xA]; //Ammo
                entry.Junk = footer[0xB];
                entry.Locked = reader.ReadByte();
            }
        }

        public static void RepairItem(BinaryReader reader, ByteOrder endian, BankEntry entry, int offset)
        {
            reader.BaseStream.Position -= offset + (entry.TypeId == 0x1 ? 0x10 : 0xB);
            ReadOldFooter(entry, reader, endian);
        }

        public static byte[] SearchNextItem(BinaryReader reader, ByteOrder endian)
        {
            var bytes = new List<byte>();
            var b = ReadBytes(reader, 0x1, endian);
            short val = b[0x0];
            if (val == SubPart)
            {
                reader.BaseStream.Position -= 0x2;
                return bytes.ToArray();
            }

            bytes.AddRange(b);

            //Looking for next byte != 0
            while (val != SubPart)
            {
                b = ReadBytes(reader, 0x1, endian);
                val = b[0x0];
                if (val != SubPart)
                {
                    bytes.AddRange(b);
                }
                else
                {
                    bytes.RemoveAt(bytes.Count - 0x1);
                    reader.BaseStream.Position -= 0x2;
                }
            }

            return bytes.ToArray();
        }

        protected static BankEntry CreateBankEntry(BinaryReader reader, ByteOrder byteOrder, BankEntry previous)
        {
            var entry = new BankEntry();
            Deserialize(entry, reader, byteOrder, previous);
            return entry;
        }

        public static byte[] Serialize(BankEntry entry, ByteOrder endian)
        {
            var bytes = new List<byte>();
            if (entry.TypeId != 0x1 && entry.TypeId != 0x2)
            {
                throw new FormatException($"Bank entry to be written has an invalid Type ID.  TypeId = {entry.TypeId}");
            }

            bytes.Add(entry.TypeId);
            var count = 0x0;
            foreach (var component in entry.Strings)
            {
                if (component == "None")
                {
                    bytes.AddRange(new byte[0x19]);
                    continue;
                }

                bytes.Add(0x20);
                var subComponentArray = component.Split('.');
                bytes.AddRange(new byte[(0x6 - subComponentArray.Length) * 0x4]);
                foreach (var subComponent in subComponentArray)
                {
                    bytes.AddRange(GetBytesFromString(subComponent, endian));
                }

                if (count == 0x2)
                {
                    bytes.AddRange(GetBytesFromInt((ushort)entry.Quality + (ushort)entry.Level * (uint)0x10000, endian));
                }

                count++;
            }

            bytes.AddRange(new byte[0x8]);
            bytes.Add((byte)entry.EquippedSlot);
            bytes.Add(0x1);
            if (WillowSaveGame.ExportValuesCount > 0x4)
            {
                bytes.Add((byte)entry.Junk);
                bytes.Add((byte)entry.Locked);
            }

            if (entry.TypeId == 0x1)
            {
                bytes.AddRange(GetBytesFromInt(entry.Quantity, endian));
            }
            else
            {
                if (WillowSaveGame.ExportValuesCount > 0x4)
                {
                    bytes.Add((byte)entry.Locked);
                }
                else
                {
                    bytes.Add((byte)entry.Quantity);
                }
            }

            return bytes.ToArray();
        }

        private static void DeserializePart(BankEntry entry, BinaryReader reader, ByteOrder endian, out string part, int index)
        {
            var mask = reader.ReadByte();
            if (mask == 0x0)
            {
                part = "None";
                ReadBytes(reader, 0x18, endian);
                return;
            }

            var padding = SearchForString(reader, endian);
            var partName = "";
            for (var i = 0x0; i < (padding.Length == 0x8 ? 0x4 : 0x3); i++)
            {
                var tmp = ReadString(reader, endian);
                if (i != 0x0)
                {
                    partName += $".{tmp}";
                }
                else
                {
                    partName += tmp;
                }
            }

            part = partName;
            if (index == 0x2)
            {
                var temp = (uint)ReadInt32(reader, endian);
                entry.Quality = (short)(temp % 0x10000);
                entry.Level = (short)(temp / 0x10000);
            }
        }

        public static void Deserialize(BankEntry entry, BinaryReader reader, ByteOrder endian, BankEntry previous)
        {
            entry.TypeId = reader.ReadByte();
            if (entry.TypeId != 1 && entry.TypeId != 2)
            {
                //Try to repair broken item
                if (previous != null)
                {
                    RepairItem(reader, endian, previous, 1);
                    entry.TypeId = reader.ReadByte();
                    Console.WriteLine($"{entry.TypeId} {reader.ReadByte()}");
                    reader.BaseStream.Position--;
                    if (entry.TypeId != 1 && entry.TypeId != 2)
                    {
                        reader.BaseStream.Position -= 1 + (previous.TypeId == 1 ? 4 : 1);
                        SearchNextItem(reader, endian);
                        entry.TypeId = reader.ReadByte();
                    }
                    else
                    {
                        WillowSaveGame.BankValuesCount = 4;
                    }
                }
            }

            entry.Strings = new List<string>();
            entry.Strings.AddRange(new string[entry.TypeId == 1 ? 14 : 9]);
            for (var index = 0; index < entry.Strings.Count; index++)
            {
                DeserializePart(entry, reader, endian, out var part, index);
                entry.Strings[index] = part;
            }

            if (WillowSaveGame.BankValuesCount > 4)
            {
                ReadNewFooter(entry, reader, endian);
            }
            else
            {
                ReadOldFooter(entry, reader, endian);
            }
        }

        private static void WriteValues(BinaryWriter writer, IReadOnlyList<int> values, ByteOrder endianWsg, int revisionNumber)
        {
            Write(writer, values[0x0], endianWsg);
            var tempLevelQuality = (ushort)values[0x1] + (ushort)values[0x3] * (uint)0x10000;
            Write(writer, (int)tempLevelQuality, endianWsg);
            Write(writer, values[0x2], endianWsg);
            if (revisionNumber < EnhancedVersion)
            {
                return;
            }

            Write(writer, values[0x4], endianWsg);
            Write(writer, values[0x5], endianWsg);
        }

        private static void WriteStrings(BinaryWriter writer, List<string> strings, ByteOrder endianWsg)
        {
            foreach (var s in strings)
            {
                Write(writer, s, endianWsg);
            }
        }

        protected static void WriteObject<T>(BinaryWriter writer, T obj, ByteOrder endianWsg, int revisionNumber) where T : WillowObject
        {
            WriteStrings(writer, obj.Strings, endianWsg);
            WriteValues(writer, obj.GetValues(), endianWsg, revisionNumber);
        }

        protected static void WriteObjects<T>(BinaryWriter writer, List<T> objects, ByteOrder endianWsg, int revisionNumber) where T : WillowObject
        {
            Write(writer, objects.Count, endianWsg);
            foreach (var obj in objects)
            {
                WriteObject(writer, obj, endianWsg, revisionNumber);
            }
        }

        public static List<int> ReadObjectValues(BinaryReader reader, ByteOrder byteOrder, int revisionNumber)
        {
            var ammoQuantityCount = ReadInt32(reader, byteOrder);
            var tempLevelQuality = (uint)ReadInt32(reader, byteOrder);
            var quality = (short)(tempLevelQuality % 0x10000);
            var level = (short)(tempLevelQuality / 0x10000);
            var equippedSlot = ReadInt32(reader, byteOrder);

            var values = new List<int>()
            {
                ammoQuantityCount,
                quality,
                equippedSlot,
                level
            };

            if (revisionNumber < EnhancedVersion)
            {
                return values;
            }

            var junk = ReadInt32(reader, byteOrder);
            var locked = ReadInt32(reader, byteOrder);

            if (locked != 0x0 && locked != 0x1)
            {
                reader.BaseStream.Position -= 0x4;
                values.Add(0x0);
            }
            else
            {
                values.Add(junk);
            }

            if (locked != 0x0 && locked != 0x1)
            {
                reader.BaseStream.Position -= 0x4;
                values.Add(0x0);
            }
            else
            {
                values.Add(locked);
            }

            return values;
        }

        protected static IEnumerable<EchoTable> ReadEchoTables(BinaryReader reader, ByteOrder endianWsg)
        {
            var echoListCount = ReadInt32(reader, endianWsg);
            for (var i = 0; i < echoListCount; i++)
            {
                yield return ReadEchoTable(reader, endianWsg);
            }
        }

        private static EchoTable ReadEchoTable(BinaryReader reader, ByteOrder endianWsg)
        {
            var echoTable = new EchoTable
            {
                Index = ReadInt32(reader, endianWsg),
                TotalEchoes = ReadInt32(reader, endianWsg),
                Echoes = new List<EchoEntry>()
            };

            for (var echoIndex = 0; echoIndex < echoTable.TotalEchoes; echoIndex++)
            {
                var echoEntry = ReadEchoEntry(reader, endianWsg);
                echoTable.Echoes.Add(echoEntry);
            }

            return echoTable;
        }

        private static EchoEntry ReadEchoEntry(BinaryReader reader, ByteOrder endianWsg)
        {
            return new EchoEntry
            {
                Name = ReadString(reader, endianWsg),
                DlcValue1 = ReadInt32(reader, endianWsg),
                DlcValue2 = ReadInt32(reader, endianWsg)
            };
        }

        protected static IEnumerable<string> ReadLocations(BinaryReader reader, ByteOrder byteOrder)
        {
            var locationCount = ReadInt32(reader, byteOrder);
            for (var i = 0; i < locationCount; i++)
            {
                yield return ReadString(reader, byteOrder);
            }
        }

        protected static IEnumerable<AmmoPool> ReadAmmoPools(BinaryReader reader, ByteOrder byteOrder)
        {
            var poolsCount = ReadInt32(reader, byteOrder);
            for (var i = 0; i < poolsCount; i++)
            {
                var resource = ReadString(reader, byteOrder);
                var name = ReadString(reader, byteOrder);
                var remaining = ReadSingle(reader, byteOrder);
                var level = ReadInt32(reader, byteOrder);
                yield return new AmmoPool(resource, name, remaining, level);
            }
        }
    }
}
