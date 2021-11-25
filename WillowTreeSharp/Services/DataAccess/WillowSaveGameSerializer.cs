using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using WillowTreeSharp.Domain;
using X360.IO;
using X360.Other;
using X360.STFS;

namespace WillowTree.Services.DataAccess
{
    public class WillowSaveGameSerializer
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

        protected static IEnumerable<Skill> ReadSkills(BinaryReader reader, ByteOrder byteOrder)
        {
            var skillsCount = ReadInt32(reader, byteOrder);
            for (var progress = 0; progress < skillsCount; progress++)
            {
                yield return ReadSkill(reader, byteOrder);
            }
        }

        private static Skill ReadSkill(BinaryReader reader, ByteOrder byteOrder)
        {
            var name = ReadString(reader, byteOrder);
            var level = ReadInt32(reader, byteOrder);
            var experience = ReadInt32(reader, byteOrder);
            var inUse = ReadInt32(reader, byteOrder);
            return new Skill(name, level, experience, inUse);
        }

        protected static IEnumerable<QuestTable> ReadQuestTables(BinaryReader reader, ByteOrder byteOrder)
        {
            var count = ReadInt32(reader, byteOrder);
            for (var index = 0; index < count; index++)
            {
                yield return ReadQuestTable(reader, byteOrder);
            }
        }

        private static QuestTable ReadQuestTable(BinaryReader reader, ByteOrder byteOrder)
        {
            var index = ReadInt32(reader, byteOrder);
            var currentQuest = ReadString(reader, byteOrder);
            var entries = Enumerable.ToList<QuestEntry>(ReadSequence(reader, byteOrder, ReadQuestEntry));

            if (currentQuest == "None" & entries.Count > 0)
            {
                currentQuest = entries[0].Name;
            }

            return new QuestTable
            {
                Index = index,
                CurrentQuest = currentQuest,
                Quests = entries,
                TotalQuests = entries.Count
            };
        }

        private static IEnumerable<T> ReadSequence<T>(
            BinaryReader reader,
            ByteOrder byteOrder,
            Func<BinaryReader, ByteOrder, T> readInstance)
        {
            var count = ReadInt32(reader, byteOrder);
            for (var i = 0; i < count; i++)
            {
                yield return readInstance(reader, byteOrder);
            }
        }

        private static QuestEntry ReadQuestEntry(BinaryReader reader, ByteOrder endianWsg)
        {
            var name = ReadString(reader, endianWsg);
            var progress = ReadInt32(reader, endianWsg);
            var dlcValue1 = ReadInt32(reader, endianWsg);
            var dlcValue2 = ReadInt32(reader, endianWsg);
            var objectives = Enumerable.ToArray<QuestObjective>(ReadQuestObjectives(reader, endianWsg));

            var questEntry = new QuestEntry
            {
                Name = name,
                Progress = progress,
                DlcValue1 = dlcValue1,
                DlcValue2 = dlcValue2,
                NumberOfObjectives = objectives.Length,
                Objectives = objectives
            };

            return questEntry;
        }

        private static IEnumerable<QuestObjective> ReadQuestObjectives(BinaryReader reader, ByteOrder byteOrder)
        {
            var count = ReadInt32(reader, byteOrder);
            for (var i = 0; i < count; i++)
            {
                yield return ReadQuestObjective(reader, byteOrder);
            }
        }

        private static QuestObjective ReadQuestObjective(BinaryReader reader, ByteOrder endianWsg)
        {
            var description = ReadString(reader, endianWsg);
            var progress = ReadInt32(reader, endianWsg);
            var objective = new QuestObjective
            {
                Description = description,
                Progress = progress
            };
            return objective;
        }

        public static byte[] Serialize(WillowSaveGame saveGame)
        {
            var outStream = new MemoryStream();
            var writer = new BinaryWriter(outStream);

            SplitInventoryIntoPacks(saveGame);

            writer.Write(Encoding.ASCII.GetBytes(saveGame.MagicHeader));
            Write(writer, saveGame.VersionNumber, saveGame.EndianWsg);
            writer.Write(Encoding.ASCII.GetBytes(saveGame.Plyr));
            Write(writer, saveGame.RevisionNumber, saveGame.EndianWsg);
            Write(writer, saveGame.Class, saveGame.EndianWsg);
            Write(writer, saveGame.Level, saveGame.EndianWsg);
            Write(writer, saveGame.Experience, saveGame.EndianWsg);
            Write(writer, saveGame.SkillPoints, saveGame.EndianWsg);
            Write(writer, saveGame.Unknown1, saveGame.EndianWsg);
            Write(writer, saveGame.Cash, saveGame.EndianWsg);
            Write(writer, saveGame.FinishedPlaythrough1, saveGame.EndianWsg);
            Write(writer, saveGame.NumberOfSkills, saveGame.EndianWsg);

            for (var progress = 0; progress < saveGame.NumberOfSkills; progress++) //Write Skills
            {
                Write(writer, saveGame.SkillNames[progress], saveGame.EndianWsg);
                Write(writer, saveGame.LevelOfSkills[progress], saveGame.EndianWsg);
                Write(writer, saveGame.ExpOfSkills[progress], saveGame.EndianWsg);
                Write(writer, saveGame.InUse[progress], saveGame.EndianWsg);
            }

            Write(writer, saveGame.Vehi1Color, saveGame.EndianWsg);
            Write(writer, saveGame.Vehi2Color, saveGame.EndianWsg);
            Write(writer, saveGame.Vehi1Type, saveGame.EndianWsg);
            Write(writer, saveGame.Vehi2Type, saveGame.EndianWsg);
            Write(writer, saveGame.NumberOfPools, saveGame.EndianWsg);

            for (var progress = 0x0; progress < saveGame.NumberOfPools; progress++) //Write Ammo Pools
            {
                Write(writer, saveGame.ResourcePools[progress], saveGame.EndianWsg);
                Write(writer, saveGame.AmmoPools[progress], saveGame.EndianWsg);
                Write(writer, saveGame.RemainingPools[progress], saveGame.EndianWsg);
                Write(writer, saveGame.PoolLevels[progress], saveGame.EndianWsg);
            }

            WriteObjects(writer, saveGame.Items1, saveGame.EndianWsg, saveGame.RevisionNumber); //Write Items

            Write(writer, saveGame.BackpackSize, saveGame.EndianWsg);
            Write(writer, saveGame.EquipSlots, saveGame.EndianWsg);

            WriteObjects(writer, saveGame.Weapons1, saveGame.EndianWsg, saveGame.RevisionNumber); //Write Weapons

            var count = (short)saveGame.challenges.Count;
            Write(writer, count * 0x7 + 0xA, saveGame.EndianWsg);
            Write(writer, saveGame.ChallengeDataBlockId, saveGame.EndianWsg);
            Write(writer, count * 0x7 + 0x2, saveGame.EndianWsg);
            Write(writer, count, saveGame.EndianWsg);
            foreach (var challenge in saveGame.challenges)
            {
                Write(writer, challenge.Id, saveGame.EndianWsg);
                writer.Write(challenge.TypeId);
                Write(writer, challenge.Value, saveGame.EndianWsg);
            }

            Write(writer, saveGame.TotalLocations, saveGame.EndianWsg);

            for (var progress = 0x0; progress < saveGame.TotalLocations; progress++) //Write Locations
            {
                Write(writer, saveGame.LocationStrings[progress], saveGame.EndianWsg);
            }

            Write(writer, saveGame.CurrentLocation, saveGame.EndianWsg);
            Write(writer, saveGame.SaveInfo1To5[0x0], saveGame.EndianWsg);
            Write(writer, saveGame.SaveInfo1To5[0x1], saveGame.EndianWsg);
            Write(writer, saveGame.SaveInfo1To5[0x2], saveGame.EndianWsg);
            Write(writer, saveGame.SaveInfo1To5[0x3], saveGame.EndianWsg);
            Write(writer, saveGame.SaveInfo1To5[0x4], saveGame.EndianWsg);
            Write(writer, saveGame.SaveNumber, saveGame.EndianWsg);
            Write(writer, saveGame.SaveInfo7To10[0x0], saveGame.EndianWsg);
            Write(writer, saveGame.SaveInfo7To10[0x1], saveGame.EndianWsg);
            Write(writer, saveGame.NumberOfQuestLists, saveGame.EndianWsg);

            for (var listIndex = 0x0; listIndex < saveGame.NumberOfQuestLists; listIndex++)
            {
                var qt = saveGame.QuestLists[listIndex];
                Write(writer, qt.Index, saveGame.EndianWsg);
                Write(writer, qt.CurrentQuest, saveGame.EndianWsg);
                Write(writer, qt.TotalQuests, saveGame.EndianWsg);

                var questCount = qt.TotalQuests;
                for (var questIndex = 0x0; questIndex < questCount; questIndex++) //Write Playthrough 1 Quests
                {
                    var qe = qt.Quests[questIndex];
                    Write(writer, qe.Name, saveGame.EndianWsg);
                    Write(writer, qe.Progress, saveGame.EndianWsg);
                    Write(writer, qe.DlcValue1, saveGame.EndianWsg);
                    Write(writer, qe.DlcValue2, saveGame.EndianWsg);

                    var objectiveCount = qe.NumberOfObjectives;
                    Write(writer, objectiveCount, saveGame.EndianWsg);

                    for (var i = 0x0; i < objectiveCount; i++)
                    {
                        Write(writer, qe.Objectives[i].Description, saveGame.EndianWsg);
                        Write(writer, qe.Objectives[i].Progress, saveGame.EndianWsg);
                    }
                }
            }

            Write(writer, saveGame.TotalPlayTime, saveGame.EndianWsg);
            Write(writer, saveGame.LastPlayedDate, saveGame.EndianWsg);
            Write(writer, saveGame.CharacterName, saveGame.EndianWsg);
            Write(writer, saveGame.Color1, saveGame.EndianWsg); //ABGR Big (X360, PS3), RGBA Little (PC)
            Write(writer, saveGame.Color2, saveGame.EndianWsg); //ABGR Big (X360, PS3), RGBA Little (PC)
            Write(writer, saveGame.Color3, saveGame.EndianWsg); //ABGR Big (X360, PS3), RGBA Little (PC)
            Write(writer, saveGame.Head, saveGame.EndianWsg);

            if (saveGame.RevisionNumber >= EnhancedVersion)
            {
                Write(writer, saveGame.Unknown2);
            }

            var numberOfPromoCodesUsed = saveGame.PromoCodesUsed.Count;
            Write(writer, numberOfPromoCodesUsed, saveGame.EndianWsg);
            for (var i = 0x0; i < numberOfPromoCodesUsed; i++)
            {
                Write(writer, saveGame.PromoCodesUsed[i], saveGame.EndianWsg);
            }

            var numberOfPromoCodesRequiringNotification = saveGame.PromoCodesRequiringNotification.Count;
            Write(writer, numberOfPromoCodesRequiringNotification, saveGame.EndianWsg);
            for (var i = 0x0; i < numberOfPromoCodesRequiringNotification; i++)
            {
                Write(writer, saveGame.PromoCodesRequiringNotification[i], saveGame.EndianWsg);
            }

            Write(writer, saveGame.NumberOfEchoLists, saveGame.EndianWsg);
            for (var listIndex = 0x0; listIndex < saveGame.NumberOfEchoLists; listIndex++)
            {
                var et = saveGame.EchoLists[listIndex];
                Write(writer, et.Index, saveGame.EndianWsg);
                Write(writer, et.TotalEchoes, saveGame.EndianWsg);

                for (var echoIndex = 0x0; echoIndex < et.TotalEchoes; echoIndex++) //Write Locations
                {
                    var ee = et.Echoes[echoIndex];
                    Write(writer, ee.Name, saveGame.EndianWsg);
                    Write(writer, ee.DlcValue1, saveGame.EndianWsg);
                    Write(writer, ee.DlcValue2, saveGame.EndianWsg);
                }
            }

            saveGame.Dlc.DlcSize = 0x0;
            // This loop writes the base data for each section into byte[]
            // BaseData so its size can be obtained and it can easily be
            // written to the output stream as a single block.  Calculate
            // DLC.DLC_Size as it goes since that has to be written before
            // the blocks are written to the output stream.
            foreach (var section in saveGame.Dlc.DataSections)
            {
                var tempStream = new MemoryStream();
                var memoryWriter = new BinaryWriter(tempStream);
                switch (section.Id)
                {
                    case Section1Id:
                        memoryWriter.Write(saveGame.Dlc.DlcUnknown1);
                        Write(memoryWriter, saveGame.Dlc.BankSize, saveGame.EndianWsg);
                        Write(memoryWriter, saveGame.Dlc.BankInventory.Count, saveGame.EndianWsg);
                        for (var i = 0x0; i < saveGame.Dlc.BankInventory.Count; i++)
                        {
                            var bankEntry = saveGame.Dlc.BankInventory[i];
                            var bankEntryBytes = Serialize(bankEntry, saveGame.EndianWsg);
                            Write(memoryWriter, bankEntryBytes);
                        }

                        break;

                    case Section2Id:
                        Write(memoryWriter, saveGame.Dlc.DlcUnknown2, saveGame.EndianWsg);
                        Write(memoryWriter, saveGame.Dlc.DlcUnknown3, saveGame.EndianWsg);
                        Write(memoryWriter, saveGame.Dlc.DlcUnknown4, saveGame.EndianWsg);
                        Write(memoryWriter, saveGame.Dlc.SkipDlc2Intro, saveGame.EndianWsg);
                        break;

                    case Section3Id:
                        memoryWriter.Write(saveGame.Dlc.DlcUnknown5);
                        break;

                    case Section4Id:
                        memoryWriter.Write(saveGame.Dlc.SecondaryPackEnabled);
                        // The DLC backpack items
                        WriteObjects(memoryWriter, saveGame.Items2, saveGame.EndianWsg, saveGame.RevisionNumber);
                        // The DLC backpack weapons
                        WriteObjects(memoryWriter, saveGame.Weapons2, saveGame.EndianWsg, saveGame.RevisionNumber);
                        break;
                }

                section.BaseData = tempStream.ToArray();
                saveGame.Dlc.DlcSize +=
                    section.BaseData.Length + section.RawData.Length + 0x8; // 8 = 4 bytes for id, 4 bytes for length
            }

            // Now its time to actually write all the data sections to the output stream
            Write(writer, saveGame.Dlc.DlcSize, saveGame.EndianWsg);
            foreach (var section in saveGame.Dlc.DataSections)
            {
                Write(writer, section.Id, saveGame.EndianWsg);
                var sectionLength = section.BaseData.Length + section.RawData.Length;
                Write(writer, sectionLength, saveGame.EndianWsg);
                writer.Write(section.BaseData);
                writer.Write(section.RawData);
                section.BaseData = null; // BaseData isn't needed anymore.  Free it.
            }

            if (saveGame.RevisionNumber >= EnhancedVersion)
            {
                //Past end padding
                Write(writer, saveGame.Unknown3);
            }

            // Clear the temporary lists used to split primary and DLC pack data
            saveGame.Items1 = null;
            saveGame.Items2 = null;
            saveGame.Weapons1 = null;
            saveGame.Weapons2 = null;
            return outStream.ToArray();
        }

        ///<summary>
        /// Split the weapon and item lists into two parts: one for the primary pack and one for DLC backpack
        /// </summary>
        public static void SplitInventoryIntoPacks(WillowSaveGame saveGame)
        {
            saveGame.Items1 = new List<Item>();
            saveGame.Items2 = new List<Item>();
            saveGame.Weapons1 = new List<Weapon>();
            saveGame.Weapons2 = new List<Weapon>();
            // Split items and weapons into two lists each so they can be put into the
            // DLC backpack or regular backpack area as needed.  Any item with a level
            // override and special dlc items go in the DLC backpack.  All others go
            // in the regular inventory.
            if (!saveGame.Dlc.HasSection4 || saveGame.Dlc.SecondaryPackEnabled == 0)
            {
                // no secondary pack so put it all in primary pack
                foreach (var item in saveGame.Items)
                {
                    saveGame.Items1.Add(item);
                }

                foreach (var weapon in saveGame.Weapons)
                {
                    saveGame.Weapons1.Add(weapon);
                }

                return;
            }

            foreach (var item in saveGame.Items)
            {
                if (item.Level == 0 && item.Strings[0].Substring(0, 3) != "dlc")
                {
                    saveGame.Items1.Add(item);
                }
                else
                {
                    saveGame.Items2.Add(item);
                }
            }

            foreach (var weapon in saveGame.Weapons)
            {
                if (weapon.Level == 0 && weapon.Strings[0].Substring(0, 3) != "dlc")
                {
                    saveGame.Weapons1.Add(weapon);
                }
                else
                {
                    saveGame.Weapons2.Add(weapon);
                }
            }
        }

        protected static IEnumerable<T> ReadObjects<T>(BinaryReader reader, int groupSize, ByteOrder byteOrder, int revisionNumber, IObjectReader valueReader)
            where T : WillowObject, new()
        {
            for (var progress = 0; progress < groupSize; progress++)
            {
                var strings = valueReader.ReadStrings(reader, byteOrder).ToList();
                var values = valueReader.ReadValues(reader, byteOrder, revisionNumber).ToList();
                var item = new T
                {
                    Strings = strings
                };
                item.SetValues(values);
                yield return item;
            }
        }

        ///<summary>Extracts a WSG from a CON (Xbox 360 Container File).</summary>
        public static MemoryStream ReadXBoxSection(Stream stream, out XBoxId xboxId)
        {
            var reader = new BinaryReader(stream);
            var fileInMemory = reader.ReadBytes((int)stream.Length);
            if (fileInMemory.Length != stream.Length)
            {
                throw new EndOfStreamException();
            }

            try
            {
                var con = new STFSPackage(new DJsIO(fileInMemory, true), new LogRecord());
                
                var profileId = con.Header.ProfileID;
                var deviceId = con.Header.DeviceID;
                xboxId = new XBoxId(profileId, deviceId);

                return new MemoryStream(con.GetFile("SaveGame.sav").GetTempIO(true).ReadStream(), false);
            }
            catch
            {
                try
                {
                    var manual = new DJsIO(fileInMemory, true);
                    manual.ReadBytes(0x371);
                    var profileId = manual.ReadInt64();
                    manual.ReadBytes(0x84);
                    var deviceId = manual.ReadBytes(0x14);
                    manual.ReadBytes(0xBC23);
                    var size = manual.ReadInt32();
                    manual.ReadBytes(0xFC8);
                    xboxId = new XBoxId(profileId, deviceId);
                    return new MemoryStream(manual.ReadBytes(size), false);
                }
                catch
                {
                    xboxId = new XBoxId(0, Array.Empty<byte>());
                    return null;
                }
            }
        }

        public static WillowSaveGame ReadFile(string path, bool autoRepair = false)
        {
            using (var fileStream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                var platform = ReadPlatform(fileStream);

                fileStream.Seek(0, SeekOrigin.Begin);

                switch (platform)
                {
                    case "X360":
                    case "X360JP":
                        using (var x360FileStream = ReadXBoxSection(fileStream, out var xboxId))
                        {
                            var save = ReadSave(x360FileStream, platform, path, autoRepair);
                            save.DeviceId = xboxId.DeviceId;
                            save.ProfileId = xboxId.ProfileId;
                            return save;
                        }

                    case "PS3":
                    case "PC":
                        return ReadSave(fileStream, platform, path, autoRepair);

                    default:
                        throw new FileFormatException($"Input file is not a WSG (platform is {platform}).");
                }
            }
        }

        public static WillowSaveGame ReadSave(Stream fileStream, string platform, string path, bool autoRepair)
        {
            var saveGame = new WillowSaveGame
            {
                Platform = platform,
                OpenedWsg = path,
                AutoRepair = autoRepair
            };
            var testReader = new BinaryReader(fileStream, Encoding.ASCII);

            saveGame.ContainsRawData = false;
            saveGame.RequiredRepair = false;
            saveGame.MagicHeader = new string(testReader.ReadChars(0x3));
            saveGame.VersionNumber = testReader.ReadInt32();

            switch (saveGame.VersionNumber)
            {
                case 0x2:
                    saveGame.EndianWsg = ByteOrder.LittleEndian;
                    break;

                case 0x02000000:
                    saveGame.VersionNumber = 0x2;
                    saveGame.EndianWsg = ByteOrder.BigEndian;
                    break;

                default:
                    throw new FileFormatException(
                        $"WSG version number does match any known version ({saveGame.VersionNumber}).");
            }

            saveGame.Plyr = new string(testReader.ReadChars(0x4));
            if (!string.Equals(saveGame.Plyr, "PLYR", StringComparison.Ordinal))
            {
                throw new FileFormatException("Player header does not match expected value.");
            }

            saveGame.RevisionNumber = ReadInt32(testReader, saveGame.EndianWsg);
            WillowSaveGame.ExportValuesCount = saveGame.RevisionNumber < EnhancedVersion ? 0x4 : 0x6;
            saveGame.Class = ReadString(testReader, saveGame.EndianWsg);
            saveGame.Level = ReadInt32(testReader, saveGame.EndianWsg);
            saveGame.Experience = ReadInt32(testReader, saveGame.EndianWsg);
            saveGame.SkillPoints = ReadInt32(testReader, saveGame.EndianWsg);
            saveGame.Unknown1 = ReadInt32(testReader, saveGame.EndianWsg);
            saveGame.Cash = ReadInt32(testReader, saveGame.EndianWsg);
            saveGame.FinishedPlaythrough1 = ReadInt32(testReader, saveGame.EndianWsg);

            var skills = ReadSkills(testReader, saveGame.EndianWsg).ToList();

            saveGame.SkillNames = new string[skills.Count];
            saveGame.LevelOfSkills = new int[skills.Count];
            saveGame.ExpOfSkills = new int[skills.Count];
            saveGame.InUse = new int[skills.Count];

            for (var index = 0; index < skills.Count; index++)
            {
                var skill = skills[index];
                saveGame.SkillNames[index] = skill.Name;
                saveGame.LevelOfSkills[index] = skill.Level;
                saveGame.ExpOfSkills[index] = skill.Experience;
                saveGame.InUse[index] = skill.InUse;
            }
            saveGame.NumberOfSkills = skills.Count;

            saveGame.Vehi1Color = ReadInt32(testReader, saveGame.EndianWsg);
            saveGame.Vehi2Color = ReadInt32(testReader, saveGame.EndianWsg);
            saveGame.Vehi1Type = ReadInt32(testReader, saveGame.EndianWsg);
            saveGame.Vehi2Type = ReadInt32(testReader, saveGame.EndianWsg);

            var pools = ReadAmmoPools(testReader, saveGame.EndianWsg).ToArray();
            saveGame.ResourcePools = new string[pools.Length];
            saveGame.AmmoPools = new string[pools.Length];
            saveGame.RemainingPools = new float[pools.Length];
            saveGame.PoolLevels = new int[pools.Length];

            for (var ammoPoolIndex = 0; ammoPoolIndex < pools.Length; ammoPoolIndex++)
            {
                saveGame.ResourcePools[ammoPoolIndex] = pools[ammoPoolIndex].Resource;
                saveGame.AmmoPools[ammoPoolIndex] = pools[ammoPoolIndex].Name;
                saveGame.RemainingPools[ammoPoolIndex] = pools[ammoPoolIndex].Remaining;
                saveGame.PoolLevels[ammoPoolIndex] = pools[ammoPoolIndex].Level;
            }

            saveGame.NumberOfPools = pools.Length;
            Console.WriteLine("====== ENTER ITEM ======");
            var itemCount = ReadInt32(testReader, saveGame.EndianWsg);
            var items = ReadObjects<Item>(testReader, itemCount, saveGame.EndianWsg, saveGame.RevisionNumber, new ItemsReader());
            saveGame.Items.AddRange(items);
            Console.WriteLine("====== EXIT ITEM ======");
            saveGame.BackpackSize = ReadInt32(testReader, saveGame.EndianWsg);
            saveGame.EquipSlots = ReadInt32(testReader, saveGame.EndianWsg);
            Console.WriteLine("====== ENTER WEAPON ======");
            var weaponCount = ReadInt32(testReader, saveGame.EndianWsg);
            var weapons = ReadObjects<Weapon>(testReader, weaponCount, saveGame.EndianWsg, saveGame.RevisionNumber, new WeaponsReader());
            saveGame.Weapons.AddRange(weapons);
            Console.WriteLine("====== EXIT WEAPON ======");

            saveGame.ChallengeDataBlockLength = ReadInt32(testReader, saveGame.EndianWsg);
            var challengeDataBlock = testReader.ReadBytes(saveGame.ChallengeDataBlockLength);
            if (challengeDataBlock.Length != saveGame.ChallengeDataBlockLength)
            {
                throw new EndOfStreamException();
            }

            using (var challengeReader = new BinaryReader(new MemoryStream(challengeDataBlock, false), Encoding.ASCII))
            {
                saveGame.ChallengeDataBlockId = ReadInt32(challengeReader, saveGame.EndianWsg);
                saveGame.ChallengeDataLength = ReadInt32(challengeReader, saveGame.EndianWsg);
                saveGame.ChallengeDataEntries = ReadInt16(challengeReader, saveGame.EndianWsg);
                saveGame.challenges = new List<ChallengeDataEntry>();
                for (var i = 0; i < saveGame.ChallengeDataEntries; i++)
                {
                    ChallengeDataEntry challenge;
                    challenge.Id = ReadInt16(challengeReader, saveGame.EndianWsg);
                    challenge.TypeId = challengeReader.ReadByte();
                    challenge.Value = ReadInt32(challengeReader, saveGame.EndianWsg);
                    saveGame.challenges.Add(challenge);
                }
            }

            saveGame.LocationStrings = ReadLocations(testReader, saveGame.EndianWsg).ToArray();
            saveGame.TotalLocations = saveGame.LocationStrings.Length;
            saveGame.CurrentLocation = ReadString(testReader, saveGame.EndianWsg);
            saveGame.SaveInfo1To5[0x0] = ReadInt32(testReader, saveGame.EndianWsg);
            saveGame.SaveInfo1To5[0x1] = ReadInt32(testReader, saveGame.EndianWsg);
            saveGame.SaveInfo1To5[0x2] = ReadInt32(testReader, saveGame.EndianWsg);
            saveGame.SaveInfo1To5[0x3] = ReadInt32(testReader, saveGame.EndianWsg);
            saveGame.SaveInfo1To5[0x4] = ReadInt32(testReader, saveGame.EndianWsg);
            saveGame.SaveNumber = ReadInt32(testReader, saveGame.EndianWsg);
            saveGame.SaveInfo7To10[0x0] = ReadInt32(testReader, saveGame.EndianWsg);
            saveGame.SaveInfo7To10[0x1] = ReadInt32(testReader, saveGame.EndianWsg);

            saveGame.QuestLists = ReadQuestTables(testReader, saveGame.EndianWsg).ToList();
            saveGame.NumberOfQuestLists = saveGame.QuestLists.Count;

            saveGame.TotalPlayTime = ReadInt32(testReader, saveGame.EndianWsg);
            saveGame.LastPlayedDate = ReadString(testReader, saveGame.EndianWsg); //YYYYMMDDHHMMSS
            saveGame.CharacterName = ReadString(testReader, saveGame.EndianWsg);
            saveGame.Color1 = ReadInt32(testReader, saveGame.EndianWsg); //ABGR Big (X360, PS3), RGBA Little (PC)
            saveGame.Color2 = ReadInt32(testReader, saveGame.EndianWsg); //ABGR Big (X360, PS3), RGBA Little (PC)
            saveGame.Color3 = ReadInt32(testReader, saveGame.EndianWsg); //ABGR Big (X360, PS3), RGBA Little (PC)
            saveGame.Head = ReadInt32(testReader, saveGame.EndianWsg);

            if (saveGame.RevisionNumber >= EnhancedVersion)
            {
                saveGame.Unknown2 = ReadBytes(testReader, 0x55, saveGame.EndianWsg);
            }

            saveGame.PromoCodesUsed = ReadListInt32(testReader, saveGame.EndianWsg);
            saveGame.PromoCodesRequiringNotification = ReadListInt32(testReader, saveGame.EndianWsg);
            saveGame.EchoLists = ReadEchoTables(testReader, saveGame.EndianWsg).ToList();
            saveGame.NumberOfEchoLists = saveGame.EchoLists.Count;

            saveGame.Dlc.DataSections = new List<DlcSection>();
            saveGame.Dlc.DlcSize = ReadInt32(testReader, saveGame.EndianWsg);
            var dlcDataBlock = testReader.ReadBytes(saveGame.Dlc.DlcSize);
            if (dlcDataBlock.Length != saveGame.Dlc.DlcSize)
            {
                throw new EndOfStreamException();
            }

            using (var dlcDataReader = new BinaryReader(new MemoryStream(dlcDataBlock, false), Encoding.ASCII))
            {
                var remainingBytes = saveGame.Dlc.DlcSize;
                while (remainingBytes > 0x0)
                {
                    var section = new DlcSection
                    {
                        Id = ReadInt32(dlcDataReader, saveGame.EndianWsg)
                    };
                    var sectionLength = ReadInt32(dlcDataReader, saveGame.EndianWsg);
                    long sectionStartPos = (int)dlcDataReader.BaseStream.Position;
                    switch (section.Id)
                    {
                        case Section1Id: // 0x43211234
                            saveGame.Dlc.HasSection1 = true;
                            saveGame.Dlc.DlcUnknown1 = dlcDataReader.ReadByte();
                            saveGame.Dlc.BankSize = ReadInt32(dlcDataReader, saveGame.EndianWsg);
                            var bankEntriesCount = ReadInt32(dlcDataReader, saveGame.EndianWsg);
                            saveGame.Dlc.BankInventory = new List<BankEntry>();
                            Console.WriteLine("====== ENTER BANK ======");
                            WillowSaveGame.BankValuesCount = WillowSaveGame.ExportValuesCount;
                            for (var i = 0x0; i < bankEntriesCount; i++)
                            {
                                var previous = saveGame.Dlc.BankInventory.LastOrDefault();
                                var bankEntry = CreateBankEntry(dlcDataReader, saveGame.EndianWsg, previous);
                                saveGame.Dlc.BankInventory.Add(bankEntry);
                            }

                            Console.WriteLine("====== EXIT BANK ======");
                            break;

                        case Section2Id: // 0x02151984
                            saveGame.Dlc.HasSection2 = true;
                            saveGame.Dlc.DlcUnknown2 = ReadInt32(dlcDataReader, saveGame.EndianWsg);
                            saveGame.Dlc.DlcUnknown3 = ReadInt32(dlcDataReader, saveGame.EndianWsg);
                            saveGame.Dlc.DlcUnknown4 = ReadInt32(dlcDataReader, saveGame.EndianWsg);
                            saveGame.Dlc.SkipDlc2Intro = ReadInt32(dlcDataReader, saveGame.EndianWsg);
                            break;

                        case Section3Id: // 0x32235947
                            saveGame.Dlc.HasSection3 = true;
                            saveGame.Dlc.DlcUnknown5 = dlcDataReader.ReadByte();
                            break;

                        case Section4Id: // 0x234ba901
                            saveGame.Dlc.HasSection4 = true;
                            saveGame.Dlc.SecondaryPackEnabled = dlcDataReader.ReadByte();

                            try
                            {
                                Console.WriteLine("====== ENTER DLC ITEM ======");
                                var dlcItemsCount = ReadInt32(dlcDataReader, saveGame.EndianWsg);
                                var dlcItems = ReadObjects<Item>(dlcDataReader, dlcItemsCount, saveGame.EndianWsg, saveGame.RevisionNumber, new ItemsReader());
                                saveGame.Items.AddRange(dlcItems);
                                Console.WriteLine("====== EXIT DLC ITEM ======");
                            }
                            catch
                            {
                                // The data was invalid so the processing ran into an exception.
                                // See if the user wants to ignore the invalid data and just try
                                // to recover partial data.  If not, just re-throw the exception.
                                if (!saveGame.AutoRepair)
                                {
                                    throw;
                                }

                                // Set the flag to indicate that repair was required to load the savegame
                                saveGame.RequiredRepair = true;

                                // If the data is invalid here, the whole DLC weapon list is invalid so
                                // set its length to 0 and be done
                                saveGame.Dlc.NumberOfWeapons = 0x0;

                                // Skip to the end of the section to discard any raw data that is left over
                                dlcDataReader.BaseStream.Position = sectionStartPos + sectionLength;
                            }

                            try
                            {
                                Console.WriteLine("====== ENTER DLC WEAPON ======");
                                var dlcWeaponsCount = ReadInt32(dlcDataReader, saveGame.EndianWsg);
                                var dlcWeapons = ReadObjects<Weapon>(dlcDataReader, dlcWeaponsCount, saveGame.EndianWsg,
                                    saveGame.RevisionNumber, new WeaponsReader());
                                saveGame.Weapons.AddRange(dlcWeapons);
                                Console.WriteLine("====== EXIT DLC WEAPON ======");
                            }
                            catch
                            {
                                // The data was invalid so the processing ran into an exception.
                                // See if the user wants to ignore the invalid data and just try
                                // to recover partial data.  If not, just re-throw the exception.
                                if (!saveGame.AutoRepair)
                                {
                                    throw;
                                }

                                // Set the flag to indicate that repair was required to load the savegame
                                saveGame.RequiredRepair = true;

                                // Skip to the end of the section to discard any raw data that is left over
                                dlcDataReader.BaseStream.Position = sectionStartPos + sectionLength;
                            }
                            break;
                    }

                    // I don't pretend to know if any of the DLC sections will ever expand
                    // and store more data.  RawData stores any extra data at the end of
                    // the known data in any section and stores the entirety of sections
                    // with unknown ids in a buffer in its raw byte order dependent form.
                    var rawDataCount = sectionLength - (int)(dlcDataReader.BaseStream.Position - sectionStartPos);

                    section.RawData = dlcDataReader.ReadBytes(rawDataCount);
                    if (rawDataCount > 0)
                    {
                        saveGame.ContainsRawData = true;
                    }

                    remainingBytes -= sectionLength + 0x8;
                    saveGame.Dlc.DataSections.Add(section);
                }

                if (saveGame.RevisionNumber < EnhancedVersion)
                {
                    return saveGame;
                }

                //Padding at the end of file, don't know exactly why
                var temp = new List<byte>();
                while (!IsEndOfFile(testReader))
                {
                    temp.Add(testReader.ReadByte());
                }

                saveGame.Unknown3 = temp.ToArray();
            }

            return saveGame;
        }

        public static void DiscardRawData(WillowSaveGame saveGame)
        {
            // Make a list of all the known data sections to compare against.
            var knownSectionIds = new List<int>
            {
                Section1Id,
                Section2Id,
                Section3Id,
                Section4Id,
            };

            // Traverse the list of data sections from end to beginning because when
            // an item gets deleted it does not affect the index of the ones before it,
            // but it does change the index of the ones after it.
            for (var i = saveGame.Dlc.DataSections.Count - 1; i >= 0; i--)
            {
                var section = saveGame.Dlc.DataSections[i];

                if (knownSectionIds.Contains(section.Id))
                {
                    // clear the raw data in this DLC data section
                    section.RawData = Array.Empty<byte>();
                }
                else
                {
                    // if the section id is not recognized remove it completely
                    section.RawData = null;
                    saveGame.Dlc.DataSections.RemoveAt(i);
                }
            }

            // Now that all the raw data has been removed, reset the raw data flag
            saveGame.ContainsRawData = false;
        }

        public static void WriteToFile(WillowSaveGame saveGame, string filename)
        {
            switch (saveGame.Platform)
            {
                case "PS3":
                case "PC":
                    {
                        using (var writer = new BinaryWriter(new FileStream(filename, FileMode.Create)))
                        {
                            writer.Write(Serialize(saveGame));
                        }

                        break;
                    }
                case "X360":
                    {
                        var tempSaveName = $"{filename}.temp";
                        using (var writer = new BinaryWriter(new FileStream(tempSaveName, FileMode.Create)))
                        {
                            writer.Write(Serialize(saveGame));
                        }

                        PackageXBoxContainer(saveGame, filename, tempSaveName, 0x1);
                        File.Delete(tempSaveName);
                        break;
                    }
                case "X360JP":
                    {
                        var tempSaveName = $"{filename}.temp";
                        using (var writer = new BinaryWriter(new FileStream(tempSaveName, FileMode.Create)))
                        {
                            writer.Write(Serialize(saveGame));
                        }

                        PackageXBoxContainer(saveGame, filename, tempSaveName, 0x2);
                        File.Delete(tempSaveName);
                        break;
                    }
            }
        }

        private static void PackageXBoxContainer(WillowSaveGame saveGame, string packageFileName, string saveFileName, int locale)
        {
            var package = new CreateSTFS
            {
                STFSType = STFSType.Type1,
                HeaderData =
                {
                    ProfileID = saveGame.ProfileId,
                    DeviceID = saveGame.DeviceId
                }
            };

            // WARNING: GetManifestResourceStream is case-sensitive.
            var wtIcon = Assembly
                .GetExecutingAssembly()
                .GetManifestResourceStream("WillowTree.Resources.WT_CON.png");

            if (wtIcon is null)
            {
                throw new NoNullAllowedException("wtIcon don't found.");
            }

            package.HeaderData.ContentImage = Image.FromStream(wtIcon);
            package.HeaderData.PackageImage = package.HeaderData.ContentImage;
            package.HeaderData.Title_Display = $"{saveGame.CharacterName} - Level {saveGame.Level} - {saveGame.CurrentLocation}";
            package.HeaderData.Title_Package = "Borderlands";

            switch (locale)
            {
                case 0x1: // US or International version
                    package.HeaderData.Title_Package = "Borderlands";
                    package.HeaderData.TitleID = 0x545407E7;
                    break;

                case 0x2: // JP version
                    package.HeaderData.Title_Package = "Borderlands (JP)";
                    package.HeaderData.TitleID = 0x54540866;
                    break;
            }

            package.AddFile(saveFileName, "SaveGame.sav");

            var xKvLocation = Path.Combine(Constants.DataPath, "KV.bin");
            var con = new STFSPackage(
                package,
                new RSAParams(xKvLocation),
                packageFileName,
                new LogRecord());

            con.FlushPackage(new RSAParams(xKvLocation));
            con.CloseIO();
            wtIcon.Close();
        }
    }
}