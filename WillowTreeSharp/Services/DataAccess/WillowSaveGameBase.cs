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
        protected static byte[] ReadBytes(BinaryReader reader, int fieldSize, ByteOrder byteOrder)
        {
            byte[] bytes = reader.ReadBytes(fieldSize);
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

        protected static List<string> ReadItemStrings(BinaryReader reader, ByteOrder byteOrder)
        {
            List<string> strings = new List<string>();
            for (int index = 0; index < 9; index++)
            {
                strings.Add(ReadString(reader, byteOrder));
            }

            return strings;
        }

        protected static List<string> ReadWeaponStrings(BinaryReader reader, ByteOrder bo)
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
    }
}