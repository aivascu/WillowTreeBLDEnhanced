using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using X360.IO;
using X360.Other;
using X360.STFS;

namespace WillowTree.Services.DataAccess
{
    public class WillowSaveGame : WillowSaveGameBase
    {
        #region Members

        private static readonly int EnhancedVersion = 0x27;
        public ByteOrder EndianWsg;

        public string Platform;
        public string OpenedWsg;
        public bool ContainsRawData;

        // Whether WSG should try to automatically repair or discard any invalid data
        // to recover from an invalid state.  This will allow partial data loss but
        // may allow partial data recovery as well.
        public bool AutoRepair = false;

        public bool RequiredRepair;

        //General Info
        public string MagicHeader;

        public int VersionNumber;
        public string Plyr;
        public int RevisionNumber;
        public static int ExportValuesCount;
        public static int BankValuesCount;
        public string Class;
        public int Level;
        public int Experience;
        public int SkillPoints;
        public int Unknown1;
        public int Cash;
        public int FinishedPlaythrough1;

        //Skill Arrays
        public int NumberOfSkills;

        public string[] SkillNames;
        public int[] LevelOfSkills;
        public int[] ExpOfSkills;
        public int[] InUse;

        //Vehicle Info
        public int Vehi1Color;

        public int Vehi2Color;
        public int Vehi1Type; // 0 = rocket, 1 = machine gun
        public int Vehi2Type;

        //Ammo Pool Arrays
        public int NumberOfPools;

        public string[] ResourcePools;
        public string[] AmmoPools;
        public float[] RemainingPools;
        public int[] PoolLevels;

        //Delegate for read String and Value

        public abstract class WillowObject
        {
            protected int[] values = new int[0x6];

            public ReadStringsFunction ReadStrings { get; set; }
            public ReadValuesFunction ReadValues { get; set; } = ReadObjectValues;

            protected WillowObject()
            {
            }

            public List<string> Strings { get; set; } = new List<string>();

            public void SetValues(List<int> values)
            {
                this.values = values.ToArray();
            }

            public List<int> GetValues()
            {
                return values.ToList();
            }

            public int Quality
            {
                get => values[0x1];
                set => values[0x1] = value;
            }

            public int EquipedSlot
            {
                get => values[0x2];
                set => values[0x2] = value;
            }

            public int Level
            {
                get => values[0x3];
                set => values[0x3] = value;
            }

            public int Junk
            {
                get => values[0x4];
                set => values[0x4] = value;
            }

            public int Locked
            {
                get => values[0x5];
                set => values[0x5] = value;
            }
        }

        public class Item : WillowObject
        {
            public Item()
            {
                ReadStrings = ReadItemStrings;
            }

            public int Quantity
            {
                get => values[0x0];
                set => values[0x0] = value;
            }
        }

        public class Weapon : WillowObject
        {
            public Weapon()
            {
                ReadStrings = ReadWeaponStrings;
            }

            public int Ammo
            {
                get => values[0];
                set => values[0] = value;
            }
        }

        //Item Arrays
        public List<Item> Items = new List<Item>();

        public List<Weapon> Weapons = new List<Weapon>();

        //Backpack Info
        public int BackpackSize;

        public int EquipSlots;

        //Challenge related data
        public int ChallengeDataBlockLength;

        public int ChallengeDataBlockId;
        public int ChallengeDataLength;
        public short ChallengeDataEntries;

        private List<ChallengeDataEntry> _challenges;

        public byte[] ChallengeData;
        public int TotalLocations;
        public string[] LocationStrings;
        public string CurrentLocation;
        public int[] SaveInfo1To5 = new int[5];
        public int SaveNumber;
        public int[] SaveInfo7To10 = new int[4];

        public int NumberOfQuestLists;
        public List<QuestTable> QuestLists = new List<QuestTable>();

        //More unknowns and color info.
        public int TotalPlayTime;

        public string LastPlayedDate;
        public string CharacterName;
        public int Color1;
        public int Color2;
        public int Color3;
        public int Head;

        public byte[] Unknown2; //Seam to be a fixed Array of Boolean since i saw only 01 or 00 in it Only on 38 >

        public List<int> PromoCodesUsed;
        public List<int> PromoCodesRequiringNotification;

        //Echo Info
        public int NumberOfEchoLists;

        public List<EchoTable> EchoLists = new List<EchoTable>();

        // Temporary lists used for primary pack data when the inventory is split
        public List<Item> Items1 = new List<Item>();

        public List<Weapon> Weapons1 = new List<Weapon>();

        // Temporary lists used for primary pack data when the inventory is split
        public List<Item> Items2 = new List<Item>();

        public List<Weapon> Weapons2 = new List<Weapon>();

        public byte[] Unknown3; //80 bytes of 00 at the end

        public DlcData Dlc = new DlcData();

        //Xbox 360 only
        public long ProfileId;

        public byte[] DeviceId;
        public byte[] ConImage;
        public string TitleDisplay;
        public string TitlePackage;
        public uint TitleId = 0x545407E7;

        #endregion Members

        ///<summary>Extracts a WSG from a CON (Xbox 360 Container File).</summary>
        public MemoryStream OpenXboxWsgStream(Stream inputX360File)
        {
            var br = new BinaryReader(inputX360File);
            var fileInMemory = br.ReadBytes((int)inputX360File.Length);
            if (fileInMemory.Length != inputX360File.Length)
            {
                throw new EndOfStreamException();
            }

            try
            {
                var con = new STFSPackage(new DJsIO(fileInMemory, true), new LogRecord());
                ProfileId = con.Header.ProfileID;
                DeviceId = con.Header.DeviceID;

                return new MemoryStream(con.GetFile("SaveGame.sav").GetTempIO(true).ReadStream(), false);
            }
            catch
            {
                try
                {
                    var manual = new DJsIO(fileInMemory, true);
                    manual.ReadBytes(0x371);
                    ProfileId = manual.ReadInt64();
                    manual.ReadBytes(0x84);
                    DeviceId = manual.ReadBytes(0x14);
                    manual.ReadBytes(0xBC23);
                    var size = manual.ReadInt32();
                    manual.ReadBytes(0xFC8);
                    return new MemoryStream(manual.ReadBytes(size), false);
                }
                catch
                {
                    return null;
                }
            }
        }

        ///<summary>Reads savegame data from a file</summary>
        public void LoadWsg(string inputFile)
        {
            using (var fileStream = new FileStream(inputFile, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                Platform = ReadPlatform(fileStream);
                fileStream.Seek(0x0, SeekOrigin.Begin);

                switch (Platform)
                {
                    case "X360":
                    case "X360JP":
                        using (var x360FileStream = OpenXboxWsgStream(fileStream))
                        {
                            ReadWsg(x360FileStream);
                        }

                        break;

                    case "PS3":
                    case "PC":
                        ReadWsg(fileStream);
                        break;

                    default:
                        throw new FileFormatException($"Input file is not a WSG (platform is {Platform}).");
                }

                OpenedWsg = inputFile;
            }
        }

        private void BuildXboxPackage(string packageFileName, string saveFileName, int locale)
        {
            var package = new CreateSTFS
            {
                STFSType = STFSType.Type1,
                HeaderData =
                {
                    ProfileID = ProfileId,
                    DeviceID = DeviceId
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
            package.HeaderData.Title_Display = $"{CharacterName} - Level {Level} - {CurrentLocation}";
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

        private static List<int> ReadObjectValues(BinaryReader reader, ByteOrder byteOrder, int revisionNumber)
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

        private T ReadObject<T>(BinaryReader reader) where T : WillowObject, new()
        {
            var item = new T();
            item.Strings = item.ReadStrings(reader, EndianWsg);
            item.SetValues(item.ReadValues(reader, EndianWsg, RevisionNumber));
            return item;
        }

        private void ReadObjects<T>(BinaryReader reader, ref List<T> objects, int groupSize)
            where T : WillowObject, new()
        {
            for (var progress = 0; progress < groupSize; progress++)
            {
                var item = ReadObject<T>(reader);
                objects.Add(item);
            }
        }

        public void SaveWsg(string filename)
        {
            switch (Platform)
            {
                case "PS3":
                case "PC":
                    {
                        using (var save = new BinaryWriter(new FileStream(filename, FileMode.Create)))
                        {
                            save.Write(WriteWsg());
                        }

                        break;
                    }
                case "X360":
                    {
                        var tempSaveName = $"{filename}.temp";
                        using (var save = new BinaryWriter(new FileStream(tempSaveName, FileMode.Create)))
                        {
                            save.Write(WriteWsg());
                        }

                        BuildXboxPackage(filename, tempSaveName, 0x1);
                        File.Delete(tempSaveName);
                        break;
                    }
                case "X360JP":
                    {
                        var tempSaveName = $"{filename}.temp";
                        using (var save = new BinaryWriter(new FileStream(tempSaveName, FileMode.Create)))
                        {
                            save.Write(WriteWsg());
                        }

                        BuildXboxPackage(filename, tempSaveName, 0x2);
                        File.Delete(tempSaveName);
                        break;
                    }
            }
        }

        ///<summary>Read savegame data from an open stream</summary>
        public void ReadWsg(Stream fileStream)
        {
            var testReader = new BinaryReader(fileStream, Encoding.ASCII);

            ContainsRawData = false;
            RequiredRepair = false;
            MagicHeader = new string(testReader.ReadChars(0x3));
            VersionNumber = testReader.ReadInt32();

            switch (VersionNumber)
            {
                case 0x2:
                    EndianWsg = ByteOrder.LittleEndian;
                    break;

                case 0x02000000:
                    VersionNumber = 0x2;
                    EndianWsg = ByteOrder.BigEndian;
                    break;

                default:
                    throw new FileFormatException(
                        $"WSG version number does match any known version ({VersionNumber}).");
            }

            Plyr = new string(testReader.ReadChars(0x4));
            if (!string.Equals(Plyr, "PLYR", StringComparison.Ordinal))
            {
                throw new FileFormatException("Player header does not match expected value.");
            }

            RevisionNumber = ReadInt32(testReader, EndianWsg);
            ExportValuesCount = RevisionNumber < EnhancedVersion ? 0x4 : 0x6;
            Class = ReadString(testReader, EndianWsg);
            Level = ReadInt32(testReader, EndianWsg);
            Experience = ReadInt32(testReader, EndianWsg);
            SkillPoints = ReadInt32(testReader, EndianWsg);
            Unknown1 = ReadInt32(testReader, EndianWsg);
            Cash = ReadInt32(testReader, EndianWsg);
            FinishedPlaythrough1 = ReadInt32(testReader, EndianWsg);
            NumberOfSkills = ReadSkills(testReader, EndianWsg);
            Vehi1Color = ReadInt32(testReader, EndianWsg);
            Vehi2Color = ReadInt32(testReader, EndianWsg);
            Vehi1Type = ReadInt32(testReader, EndianWsg);
            Vehi2Type = ReadInt32(testReader, EndianWsg);
            NumberOfPools = ReadAmmo(testReader, EndianWsg);
            Console.WriteLine(@"====== ENTER ITEM ======");
            ReadObjects(testReader, ref Items, ReadInt32(testReader, EndianWsg));
            Console.WriteLine(@"====== EXIT ITEM ======");
            BackpackSize = ReadInt32(testReader, EndianWsg);
            EquipSlots = ReadInt32(testReader, EndianWsg);
            Console.WriteLine(@"====== ENTER WEAPON ======");
            ReadObjects(testReader, ref Weapons, ReadInt32(testReader, EndianWsg));
            Console.WriteLine(@"====== EXIT WEAPON ======");

            ChallengeDataBlockLength = ReadInt32(testReader, EndianWsg);
            var challengeDataBlock = testReader.ReadBytes(ChallengeDataBlockLength);
            if (challengeDataBlock.Length != ChallengeDataBlockLength)
            {
                throw new EndOfStreamException();
            }

            using (var challengeReader = new BinaryReader(new MemoryStream(challengeDataBlock, false), Encoding.ASCII))
            {
                ChallengeDataBlockId = ReadInt32(challengeReader, EndianWsg);
                ChallengeDataLength = ReadInt32(challengeReader, EndianWsg);
                ChallengeDataEntries = ReadInt16(challengeReader, EndianWsg);
                _challenges = new List<ChallengeDataEntry>();
                for (var i = 0x0; i < ChallengeDataEntries; i++)
                {
                    ChallengeDataEntry challenge;
                    challenge.Id = ReadInt16(challengeReader, EndianWsg);
                    challenge.TypeId = challengeReader.ReadByte();
                    challenge.Value = ReadInt32(challengeReader, EndianWsg);
                    _challenges.Add(challenge);
                }
            }

            TotalLocations = ReadLocations(testReader, EndianWsg);
            CurrentLocation = ReadString(testReader, EndianWsg);
            SaveInfo1To5[0x0] = ReadInt32(testReader, EndianWsg);
            SaveInfo1To5[0x1] = ReadInt32(testReader, EndianWsg);
            SaveInfo1To5[0x2] = ReadInt32(testReader, EndianWsg);
            SaveInfo1To5[0x3] = ReadInt32(testReader, EndianWsg);
            SaveInfo1To5[0x4] = ReadInt32(testReader, EndianWsg);
            SaveNumber = ReadInt32(testReader, EndianWsg);
            SaveInfo7To10[0x0] = ReadInt32(testReader, EndianWsg);
            SaveInfo7To10[0x1] = ReadInt32(testReader, EndianWsg);
            NumberOfQuestLists = ReadQuests(testReader, EndianWsg);

            TotalPlayTime = ReadInt32(testReader, EndianWsg);
            LastPlayedDate = ReadString(testReader, EndianWsg); //YYYYMMDDHHMMSS
            CharacterName = ReadString(testReader, EndianWsg);
            Color1 = ReadInt32(testReader, EndianWsg); //ABGR Big (X360, PS3), RGBA Little (PC)
            Color2 = ReadInt32(testReader, EndianWsg); //ABGR Big (X360, PS3), RGBA Little (PC)
            Color3 = ReadInt32(testReader, EndianWsg); //ABGR Big (X360, PS3), RGBA Little (PC)
            Head = ReadInt32(testReader, EndianWsg);

            if (RevisionNumber >= EnhancedVersion)
            {
                Unknown2 = ReadBytes(testReader, 0x55, EndianWsg);
            }

            PromoCodesUsed = ReadListInt32(testReader, EndianWsg);
            PromoCodesRequiringNotification = ReadListInt32(testReader, EndianWsg);
            NumberOfEchoLists = ReadEchoes(testReader, EndianWsg);

            Dlc.DataSections = new List<DlcSection>();
            Dlc.DlcSize = ReadInt32(testReader, EndianWsg);
            var dlcDataBlock = testReader.ReadBytes(Dlc.DlcSize);
            if (dlcDataBlock.Length != Dlc.DlcSize)
            {
                throw new EndOfStreamException();
            }

            using (var dlcDataReader = new BinaryReader(new MemoryStream(dlcDataBlock, false), Encoding.ASCII))
            {
                var remainingBytes = Dlc.DlcSize;
                while (remainingBytes > 0x0)
                {
                    var section = new DlcSection
                    {
                        Id = ReadInt32(dlcDataReader, EndianWsg)
                    };
                    var sectionLength = ReadInt32(dlcDataReader, EndianWsg);
                    long sectionStartPos = (int)dlcDataReader.BaseStream.Position;
                    switch (section.Id)
                    {
                        case DlcData.Section1Id: // 0x43211234
                            Dlc.HasSection1 = true;
                            Dlc.DlcUnknown1 = dlcDataReader.ReadByte();
                            Dlc.BankSize = ReadInt32(dlcDataReader, EndianWsg);
                            var bankEntriesCount = ReadInt32(dlcDataReader, EndianWsg);
                            Dlc.BankInventory = new List<BankEntry>();
                            Console.WriteLine(@"====== ENTER BANK ======");
                            BankValuesCount = ExportValuesCount;
                            for (var i = 0x0; i < bankEntriesCount; i++)
                            {
                                Dlc.BankInventory.Add(CreateBankEntry(dlcDataReader));
                            }

                            Console.WriteLine(@"====== EXIT BANK ======");
                            break;

                        case DlcData.Section2Id: // 0x02151984
                            Dlc.HasSection2 = true;
                            Dlc.DlcUnknown2 = ReadInt32(dlcDataReader, EndianWsg);
                            Dlc.DlcUnknown3 = ReadInt32(dlcDataReader, EndianWsg);
                            Dlc.DlcUnknown4 = ReadInt32(dlcDataReader, EndianWsg);
                            Dlc.SkipDlc2Intro = ReadInt32(dlcDataReader, EndianWsg);
                            break;

                        case DlcData.Section3Id: // 0x32235947
                            Dlc.HasSection3 = true;
                            Dlc.DlcUnknown5 = dlcDataReader.ReadByte();
                            break;

                        case DlcData.Section4Id: // 0x234ba901
                            Dlc.HasSection4 = true;
                            Dlc.SecondaryPackEnabled = dlcDataReader.ReadByte();

                            try
                            {
                                Console.WriteLine(@"====== ENTER DLC ITEM ======");
                                ReadObjects(dlcDataReader, ref Items, ReadInt32(dlcDataReader, EndianWsg));
                                Console.WriteLine(@"====== EXIT DLC ITEM ======");
                            }
                            catch
                            {
                                // The data was invalid so the processing ran into an exception.
                                // See if the user wants to ignore the invalid data and just try
                                // to recover partial data.  If not, just re-throw the exception.
                                if (!AutoRepair)
                                {
                                    throw;
                                }

                                // Set the flag to indicate that repair was required to load the savegame
                                RequiredRepair = true;

                                // If the data is invalid here, the whole DLC weapon list is invalid so
                                // set its length to 0 and be done
                                Dlc.NumberOfWeapons = 0x0;

                                // Skip to the end of the section to discard any raw data that is left over
                                dlcDataReader.BaseStream.Position = sectionStartPos + sectionLength;
                            }

                            try
                            {
                                Console.WriteLine(@"====== ENTER DLC WEAPON ======");
                                ReadObjects(dlcDataReader, ref Weapons, ReadInt32(dlcDataReader, EndianWsg));
                                Console.WriteLine(@"====== EXIT DLC WEAPON ======");
                            }
                            catch
                            {
                                // The data was invalid so the processing ran into an exception.
                                // See if the user wants to ignore the invalid data and just try
                                // to recover partial data.  If not, just re-throw the exception.
                                if (!AutoRepair)
                                {
                                    throw;
                                }

                                // Set the flag to indicate that repair was required to load the savegame
                                RequiredRepair = true;

                                // Skip to the end of the section to discard any raw data that is left over
                                dlcDataReader.BaseStream.Position = sectionStartPos + sectionLength;
                            }

                            //NumberOfWeapons += DLC.NumberOfWeapons;
                            break;
                    }

                    // I don't pretend to know if any of the DLC sections will ever expand
                    // and store more data.  RawData stores any extra data at the end of
                    // the known data in any section and stores the entirety of sections
                    // with unknown ids in a buffer in its raw byte order dependent form.
                    var rawDataCount = sectionLength - (int)(dlcDataReader.BaseStream.Position - sectionStartPos);

                    section.RawData = dlcDataReader.ReadBytes(rawDataCount);
                    if (rawDataCount > 0x0)
                    {
                        ContainsRawData = true;
                    }

                    remainingBytes -= sectionLength + 0x8;
                    Dlc.DataSections.Add(section);
                }

                if (RevisionNumber < EnhancedVersion)
                {
                    return;
                }

                //Padding at the end of file, don't know exactly why
                var temp = new List<byte>();
                while (!IsEndOfFile(testReader))
                {
                    temp.Add(ReadBytes(testReader, 0x1, EndianWsg)[0x0]);
                }

                Unknown3 = temp.ToArray();
            }
        }

        private int ReadEchoes(BinaryReader reader, ByteOrder endianWsg)
        {
            var echoListCount = ReadInt32(reader, endianWsg);

            EchoLists.Clear();
            for (var i = 0; i < echoListCount; i++)
            {
                var echoTable = new EchoTable
                {
                    Index = ReadInt32(reader, endianWsg),
                    TotalEchoes = ReadInt32(reader, endianWsg),
                    Echoes = new List<EchoEntry>()
                };

                for (var echoIndex = 0x0; echoIndex < echoTable.TotalEchoes; echoIndex++)
                {
                    var echoEntry = new EchoEntry
                    {
                        Name = ReadString(reader, endianWsg),
                        DlcValue1 = ReadInt32(reader, endianWsg),
                        DlcValue2 = ReadInt32(reader, endianWsg)
                    };
                    echoTable.Echoes.Add(echoEntry);
                }

                EchoLists.Add(echoTable);
            }

            return echoListCount;
        }

        private int ReadQuests(BinaryReader reader, ByteOrder endianWsg)
        {
            var numberOfQuestList = ReadInt32(reader, endianWsg);

            QuestLists.Clear();
            for (var listIndex = 0x0; listIndex < numberOfQuestList; listIndex++)
            {
                var questTable = new QuestTable
                {
                    Index = ReadInt32(reader, endianWsg),
                    CurrentQuest = ReadString(reader, endianWsg),
                    TotalQuests = ReadInt32(reader, endianWsg),
                    Quests = new List<QuestEntry>()
                };
                var questCount = questTable.TotalQuests;

                for (var questIndex = 0x0; questIndex < questCount; questIndex++)
                {
                    var questEntry = new QuestEntry
                    {
                        Name = ReadString(reader, endianWsg),
                        Progress = ReadInt32(reader, endianWsg),
                        DlcValue1 = ReadInt32(reader, endianWsg),
                        DlcValue2 = ReadInt32(reader, endianWsg)
                    };

                    var objectiveCount = ReadInt32(reader, endianWsg);
                    questEntry.NumberOfObjectives = objectiveCount;
                    questEntry.Objectives = new QuestObjective[objectiveCount];

                    for (var objectiveIndex = 0x0; objectiveIndex < objectiveCount; objectiveIndex++)
                    {
                        questEntry.Objectives[objectiveIndex].Description = ReadString(reader, endianWsg);
                        questEntry.Objectives[objectiveIndex].Progress = ReadInt32(reader, endianWsg);
                    }

                    questTable.Quests.Add(questEntry);
                }

                if (questTable.CurrentQuest == "None" & questTable.Quests.Count > 0x0)
                {
                    questTable.CurrentQuest = questTable.Quests[0x0].Name;
                }

                QuestLists.Add(questTable);
            }

            return numberOfQuestList;
        }

        private int ReadSkills(BinaryReader reader, ByteOrder endianWsg)
        {
            var skillsCount = ReadInt32(reader, endianWsg);

            var tempSkillNames = new string[skillsCount];
            var tempLevelOfSkills = new int[skillsCount];
            var tempExpOfSkills = new int[skillsCount];
            var tempInUse = new int[skillsCount];

            for (var progress = 0x0; progress < skillsCount; progress++)
            {
                tempSkillNames[progress] = ReadString(reader, endianWsg);
                tempLevelOfSkills[progress] = ReadInt32(reader, endianWsg);
                tempExpOfSkills[progress] = ReadInt32(reader, endianWsg);
                tempInUse[progress] = ReadInt32(reader, endianWsg);
            }

            SkillNames = tempSkillNames;
            LevelOfSkills = tempLevelOfSkills;
            ExpOfSkills = tempExpOfSkills;
            InUse = tempInUse;

            return skillsCount;
        }

        private int ReadAmmo(BinaryReader reader, ByteOrder endianWsg)
        {
            var poolsCount = ReadInt32(reader, endianWsg);

            var tempResourcePools = new string[poolsCount];
            var tempAmmoPools = new string[poolsCount];
            var tempRemainingPools = new float[poolsCount];
            var tempPoolLevels = new int[poolsCount];

            for (var progress = 0x0; progress < poolsCount; progress++)
            {
                tempResourcePools[progress] = ReadString(reader, endianWsg);
                tempAmmoPools[progress] = ReadString(reader, endianWsg);
                tempRemainingPools[progress] = ReadSingle(reader, endianWsg);
                tempPoolLevels[progress] = ReadInt32(reader, endianWsg);
            }

            ResourcePools = tempResourcePools;
            AmmoPools = tempAmmoPools;
            RemainingPools = tempRemainingPools;
            PoolLevels = tempPoolLevels;

            return poolsCount;
        }

        private int ReadLocations(BinaryReader reader, ByteOrder endianWsg)
        {
            var locationCount = ReadInt32(reader, endianWsg);
            var tempLocationStrings = new string[locationCount];

            for (var progress = 0x0; progress < locationCount; progress++)
            {
                tempLocationStrings[progress] = ReadString(reader, endianWsg);
            }

            LocationStrings = tempLocationStrings;
            return locationCount;
        }

        public void DiscardRawData()
        {
            // Make a list of all the known data sections to compare against.
            var knownSectionIds = new List<int>()
            {
                DlcData.Section1Id,
                DlcData.Section2Id,
                DlcData.Section3Id,
                DlcData.Section4Id,
            };

            // Traverse the list of data sections from end to beginning because when
            // an item gets deleted it does not affect the index of the ones before it,
            // but it does change the index of the ones after it.
            for (var i = Dlc.DataSections.Count - 0x1; i >= 0x0; i--)
            {
                var section = Dlc.DataSections[i];

                if (knownSectionIds.Contains(section.Id))
                {
                    // clear the raw data in this DLC data section
                    section.RawData = Array.Empty<byte>();
                }
                else
                {
                    // if the section id is not recognized remove it completely
                    section.RawData = null;
                    Dlc.DataSections.RemoveAt(i);
                }
            }

            // Now that all the raw data has been removed, reset the raw data flag
            ContainsRawData = false;
        }

        private void WriteValues(BinaryWriter writer, IReadOnlyList<int> values)
        {
            Write(writer, values[0x0], EndianWsg);
            var tempLevelQuality = (ushort)values[0x1] + (ushort)values[0x3] * (uint)0x10000;
            Write(writer, (int)tempLevelQuality, EndianWsg);
            Write(writer, values[0x2], EndianWsg);
            if (RevisionNumber < EnhancedVersion)
            {
                return;
            }

            Write(writer, values[0x4], EndianWsg);
            Write(writer, values[0x5], EndianWsg);
        }

        private void WriteStrings(BinaryWriter writer, List<string> strings)
        {
            foreach (var s in strings)
            {
                Write(writer, s, EndianWsg);
            }
        }

        private void WriteObject<T>(BinaryWriter writer, T obj) where T : WillowObject
        {
            WriteStrings(writer, obj.Strings);
            WriteValues(writer, obj.GetValues());
        }

        private void WriteObjects<T>(BinaryWriter writer, List<T> objects) where T : WillowObject
        {
            Write(writer, objects.Count, EndianWsg);
            foreach (var obj in objects)
            {
                WriteObject(writer, obj);
            }
        }

        /// <summary>
        /// Save the current data to a WSG as a byte[]
        /// </summary>
        public byte[] WriteWsg()
        {
            var outStream = new MemoryStream();
            var writer = new BinaryWriter(outStream);

            SplitInventoryIntoPacks();

            writer.Write(Encoding.ASCII.GetBytes(MagicHeader));
            Write(writer, VersionNumber, EndianWsg);
            writer.Write(Encoding.ASCII.GetBytes(Plyr));
            Write(writer, RevisionNumber, EndianWsg);
            Write(writer, Class, EndianWsg);
            Write(writer, Level, EndianWsg);
            Write(writer, Experience, EndianWsg);
            Write(writer, SkillPoints, EndianWsg);
            Write(writer, Unknown1, EndianWsg);
            Write(writer, Cash, EndianWsg);
            Write(writer, FinishedPlaythrough1, EndianWsg);
            Write(writer, NumberOfSkills, EndianWsg);

            for (var progress = 0x0; progress < NumberOfSkills; progress++) //Write Skills
            {
                Write(writer, SkillNames[progress], EndianWsg);
                Write(writer, LevelOfSkills[progress], EndianWsg);
                Write(writer, ExpOfSkills[progress], EndianWsg);
                Write(writer, InUse[progress], EndianWsg);
            }

            Write(writer, Vehi1Color, EndianWsg);
            Write(writer, Vehi2Color, EndianWsg);
            Write(writer, Vehi1Type, EndianWsg);
            Write(writer, Vehi2Type, EndianWsg);
            Write(writer, NumberOfPools, EndianWsg);

            for (var progress = 0x0; progress < NumberOfPools; progress++) //Write Ammo Pools
            {
                Write(writer, ResourcePools[progress], EndianWsg);
                Write(writer, AmmoPools[progress], EndianWsg);
                Write(writer, RemainingPools[progress], EndianWsg);
                Write(writer, PoolLevels[progress], EndianWsg);
            }

            WriteObjects(writer, Items1); //Write Items

            Write(writer, BackpackSize, EndianWsg);
            Write(writer, EquipSlots, EndianWsg);

            WriteObjects(writer, Weapons1); //Write Weapons

            var count = (short)_challenges.Count;
            Write(writer, count * 0x7 + 0xA, EndianWsg);
            Write(writer, ChallengeDataBlockId, EndianWsg);
            Write(writer, count * 0x7 + 0x2, EndianWsg);
            Write(writer, count, EndianWsg);
            foreach (var challenge in _challenges)
            {
                Write(writer, challenge.Id, EndianWsg);
                writer.Write(challenge.TypeId);
                Write(writer, challenge.Value, EndianWsg);
            }

            Write(writer, TotalLocations, EndianWsg);

            for (var progress = 0x0; progress < TotalLocations; progress++) //Write Locations
            {
                Write(writer, LocationStrings[progress], EndianWsg);
            }

            Write(writer, CurrentLocation, EndianWsg);
            Write(writer, SaveInfo1To5[0x0], EndianWsg);
            Write(writer, SaveInfo1To5[0x1], EndianWsg);
            Write(writer, SaveInfo1To5[0x2], EndianWsg);
            Write(writer, SaveInfo1To5[0x3], EndianWsg);
            Write(writer, SaveInfo1To5[0x4], EndianWsg);
            Write(writer, SaveNumber, EndianWsg);
            Write(writer, SaveInfo7To10[0x0], EndianWsg);
            Write(writer, SaveInfo7To10[0x1], EndianWsg);
            Write(writer, NumberOfQuestLists, EndianWsg);

            for (var listIndex = 0x0; listIndex < NumberOfQuestLists; listIndex++)
            {
                var qt = QuestLists[listIndex];
                Write(writer, qt.Index, EndianWsg);
                Write(writer, qt.CurrentQuest, EndianWsg);
                Write(writer, qt.TotalQuests, EndianWsg);

                var questCount = qt.TotalQuests;
                for (var questIndex = 0x0; questIndex < questCount; questIndex++) //Write Playthrough 1 Quests
                {
                    var qe = qt.Quests[questIndex];
                    Write(writer, qe.Name, EndianWsg);
                    Write(writer, qe.Progress, EndianWsg);
                    Write(writer, qe.DlcValue1, EndianWsg);
                    Write(writer, qe.DlcValue2, EndianWsg);

                    var objectiveCount = qe.NumberOfObjectives;
                    Write(writer, objectiveCount, EndianWsg);

                    for (var i = 0x0; i < objectiveCount; i++)
                    {
                        Write(writer, qe.Objectives[i].Description, EndianWsg);
                        Write(writer, qe.Objectives[i].Progress, EndianWsg);
                    }
                }
            }

            Write(writer, TotalPlayTime, EndianWsg);
            Write(writer, LastPlayedDate, EndianWsg);
            Write(writer, CharacterName, EndianWsg);
            Write(writer, Color1, EndianWsg); //ABGR Big (X360, PS3), RGBA Little (PC)
            Write(writer, Color2, EndianWsg); //ABGR Big (X360, PS3), RGBA Little (PC)
            Write(writer, Color3, EndianWsg); //ABGR Big (X360, PS3), RGBA Little (PC)
            Write(writer, Head, EndianWsg);

            if (RevisionNumber >= EnhancedVersion)
            {
                Write(writer, Unknown2);
            }

            var numberOfPromoCodesUsed = PromoCodesUsed.Count;
            Write(writer, numberOfPromoCodesUsed, EndianWsg);
            for (var i = 0x0; i < numberOfPromoCodesUsed; i++)
            {
                Write(writer, PromoCodesUsed[i], EndianWsg);
            }

            var numberOfPromoCodesRequiringNotification = PromoCodesRequiringNotification.Count;
            Write(writer, numberOfPromoCodesRequiringNotification, EndianWsg);
            for (var i = 0x0; i < numberOfPromoCodesRequiringNotification; i++)
            {
                Write(writer, PromoCodesRequiringNotification[i], EndianWsg);
            }

            Write(writer, NumberOfEchoLists, EndianWsg);
            for (var listIndex = 0x0; listIndex < NumberOfEchoLists; listIndex++)
            {
                var et = EchoLists[listIndex];
                Write(writer, et.Index, EndianWsg);
                Write(writer, et.TotalEchoes, EndianWsg);

                for (var echoIndex = 0x0; echoIndex < et.TotalEchoes; echoIndex++) //Write Locations
                {
                    var ee = et.Echoes[echoIndex];
                    Write(writer, ee.Name, EndianWsg);
                    Write(writer, ee.DlcValue1, EndianWsg);
                    Write(writer, ee.DlcValue2, EndianWsg);
                }
            }

            Dlc.DlcSize = 0x0;
            // This loop writes the base data for each section into byte[]
            // BaseData so its size can be obtained and it can easily be
            // written to the output stream as a single block.  Calculate
            // DLC.DLC_Size as it goes since that has to be written before
            // the blocks are written to the output stream.
            foreach (var section in Dlc.DataSections)
            {
                var tempStream = new MemoryStream();
                var memoryWriter = new BinaryWriter(tempStream);
                switch (section.Id)
                {
                    case DlcData.Section1Id:
                        memoryWriter.Write(Dlc.DlcUnknown1);
                        Write(memoryWriter, Dlc.BankSize, EndianWsg);
                        Write(memoryWriter, Dlc.BankInventory.Count, EndianWsg);
                        for (var i = 0x0; i < Dlc.BankInventory.Count; i++)
                        {
                            Write(memoryWriter, Dlc.BankInventory[i].Serialize(EndianWsg));
                        }

                        break;

                    case DlcData.Section2Id:
                        Write(memoryWriter, Dlc.DlcUnknown2, EndianWsg);
                        Write(memoryWriter, Dlc.DlcUnknown3, EndianWsg);
                        Write(memoryWriter, Dlc.DlcUnknown4, EndianWsg);
                        Write(memoryWriter, Dlc.SkipDlc2Intro, EndianWsg);
                        break;

                    case DlcData.Section3Id:
                        memoryWriter.Write(Dlc.DlcUnknown5);
                        break;

                    case DlcData.Section4Id:
                        memoryWriter.Write(Dlc.SecondaryPackEnabled);
                        // The DLC backpack items
                        WriteObjects(memoryWriter, Items2);
                        // The DLC backpack weapons
                        WriteObjects(memoryWriter, Weapons2);
                        break;
                }

                section.BaseData = tempStream.ToArray();
                Dlc.DlcSize +=
                    section.BaseData.Length + section.RawData.Length + 0x8; // 8 = 4 bytes for id, 4 bytes for length
            }

            // Now its time to actually write all the data sections to the output stream
            Write(writer, Dlc.DlcSize, EndianWsg);
            foreach (var section in Dlc.DataSections)
            {
                Write(writer, section.Id, EndianWsg);
                var sectionLength = section.BaseData.Length + section.RawData.Length;
                Write(writer, sectionLength, EndianWsg);
                writer.Write(section.BaseData);
                writer.Write(section.RawData);
                section.BaseData = null; // BaseData isn't needed anymore.  Free it.
            }

            if (RevisionNumber >= EnhancedVersion)
            {
                //Past end padding
                Write(writer, Unknown3);
            }

            // Clear the temporary lists used to split primary and DLC pack data
            Items1 = null;
            Items2 = null;
            Weapons1 = null;
            Weapons2 = null;
            return outStream.ToArray();
        }

        ///<summary>
        /// Split the weapon and item lists into two parts: one for the primary pack and one for DLC backpack
        /// </summary>
        public void SplitInventoryIntoPacks()
        {
            Items1 = new List<Item>();
            Items2 = new List<Item>();
            Weapons1 = new List<Weapon>();
            Weapons2 = new List<Weapon>();
            // Split items and weapons into two lists each so they can be put into the
            // DLC backpack or regular backpack area as needed.  Any item with a level
            // override and special dlc items go in the DLC backpack.  All others go
            // in the regular inventory.
            if (!Dlc.HasSection4 || Dlc.SecondaryPackEnabled == 0x0)
            {
                // no secondary pack so put it all in primary pack
                foreach (var item in Items)
                {
                    Items1.Add(item);
                }

                foreach (var weapon in Weapons)
                {
                    Weapons1.Add(weapon);
                }

                return;
            }

            foreach (var item in Items)
            {
                if (item.Level == 0x0 && item.Strings[0x0].Substring(0x0, 0x3) != "dlc")
                {
                    Items1.Add(item);
                }
                else
                {
                    Items2.Add(item);
                }
            }

            foreach (var weapon in Weapons)
            {
                if (weapon.Level == 0x0 && weapon.Strings[0x0].Substring(0x0, 0x3) != "dlc")
                {
                    Weapons1.Add(weapon);
                }
                else
                {
                    Weapons2.Add(weapon);
                }
            }
        }

        private const byte SubPart = 0x20;

        public sealed class BankEntry : WillowObject
        {
            public byte TypeId { get; set; }

            public int Quantity
            {
                get => values[0x0];
                set => values[0x0] = value;
            }

            public byte[] Serialize(ByteOrder endian)
            {
                var bytes = new List<byte>();
                if (TypeId != 0x1 && TypeId != 0x2)
                {
                    throw new FormatException($"Bank entry to be written has an invalid Type ID.  TypeId = {TypeId}");
                }

                bytes.Add(TypeId);
                var count = 0x0;
                foreach (var component in Strings)
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
                        bytes.AddRange(GetBytesFromInt((ushort)Quality + (ushort)Level * (uint)0x10000, endian));
                    }

                    count++;
                }

                bytes.AddRange(new byte[0x8]);
                bytes.Add((byte)EquipedSlot);
                bytes.Add(0x1);
                if (ExportValuesCount > 0x4)
                {
                    bytes.Add((byte)Junk);
                    bytes.Add((byte)Locked);
                }

                if (TypeId == 0x1)
                {
                    bytes.AddRange(GetBytesFromInt(Quantity, endian));
                }
                else
                {
                    if (ExportValuesCount > 0x4)
                    {
                        bytes.Add((byte)Locked);
                    }
                    else
                    {
                        bytes.Add((byte)Quantity);
                    }
                }

                return bytes.ToArray();
            }

            private void DeserializePart(BinaryReader reader, ByteOrder endian, out string part, int index)
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
                    Quality = (short)(temp % 0x10000);
                    Level = (short)(temp / 0x10000);
                }
            }

            private static void ReadOldFooter(BankEntry entry, BinaryReader reader, ByteOrder endian)
            {
                var footer = reader.ReadBytes(0xA);
                entry.EquipedSlot = footer[0x8];
                entry.Quantity = entry.TypeId == 0x1 ? ReadInt32(reader, endian) : reader.ReadByte();
            }

            private static void ReadNewFooter(BankEntry entry, BinaryReader reader, ByteOrder endian)
            {
                var footer = reader.ReadBytes(0xC);
                entry.EquipedSlot = footer[0x8];
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

            public void Deserialize(BinaryReader reader, ByteOrder endian, BankEntry previous)
            {
                TypeId = reader.ReadByte();
                if (TypeId != 0x1 && TypeId != 0x2)
                {
                    //Try to repair broken item
                    if (previous != null)
                    {
                        RepairItem(reader, endian, previous, 0x1);
                        TypeId = reader.ReadByte();
                        Console.WriteLine($"{TypeId} {reader.ReadByte()}");
                        reader.BaseStream.Position--;
                        if (TypeId != 0x1 && TypeId != 0x2)
                        {
                            reader.BaseStream.Position -= 0x1 + (previous.TypeId == 0x1 ? 0x4 : 0x1);
                            SearchNextItem(reader, endian);
                            TypeId = reader.ReadByte();
                        }
                        else
                        {
                            BankValuesCount = 0x4;
                        }
                    }
                }

                Strings = new List<string>();
                Strings.AddRange(new string[TypeId == 0x1 ? 0xE : 0x9]);
                for (var i = 0x0; i < Strings.Count; i++)
                {
                    DeserializePart(reader, endian, out var part, i);
                    Strings[i] = part;
                }

                if (BankValuesCount > 0x4)
                {
                    ReadNewFooter(this, reader, endian);
                }
                else
                {
                    ReadOldFooter(this, reader, endian);
                }
            }

            private static byte[] SearchNextItem(BinaryReader reader, ByteOrder endian)
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
        }

        private BankEntry CreateBankEntry(BinaryReader reader)
        {
            //Create new entry
            var entry = new BankEntry();
            var previous = Dlc.BankInventory.Count == 0x0
                ? null
                : Dlc.BankInventory[Dlc.BankInventory.Count - 0x1];
            entry.Deserialize(reader, EndianWsg, previous);
            return entry;
        }
    }

    public delegate List<int> ReadValuesFunction(BinaryReader reader, ByteOrder bo, int revisionNumber);

    public delegate List<string> ReadStringsFunction(BinaryReader reader, ByteOrder bo);

    public struct QuestObjective
    {
        public int Progress;
        public string Description;
    }

    public struct ChallengeDataEntry
    {
        public short Id;
        public byte TypeId;
        public int Value;
    }

    public class QuestTable
    {
        public List<QuestEntry> Quests;
        public int Index;
        public string CurrentQuest;
        public int TotalQuests;
    }

    public class QuestEntry
    {
        public string Name;
        public int Progress;
        public int DlcValue1;
        public int DlcValue2;
        public int NumberOfObjectives;
        public QuestObjective[] Objectives;
    }

    public class EchoTable
    {
        public int Index;
        public int TotalEchoes;
        public List<EchoEntry> Echoes;
    };

    public class EchoEntry
    {
        public string Name;
        public int DlcValue1;
        public int DlcValue2;
    }

    public class DlcData
    {
        public const int Section1Id = 0x43211234;
        public const int Section2Id = 0x02151984;
        public const int Section3Id = 0x32235947;
        public const int Section4Id = 0x234BA901;

        public bool HasSection1;
        public bool HasSection2;
        public bool HasSection3;
        public bool HasSection4;

        public List<DlcSection> DataSections;

        public int DlcSize;

        // DLC Section 1 Data (bank data)
        public byte DlcUnknown1; // Read only flag. Always resets to 1 in ver 1.41.  Probably CanAccessBank.

        public int BankSize;
        public List<WillowSaveGame.BankEntry> BankInventory = new List<WillowSaveGame.BankEntry>();

        // DLC Section 2 Data (don't know)
        public int DlcUnknown2; // All four of these are boolean flags.

        public int DlcUnknown3; // If you set them to any value except 0
        public int DlcUnknown4; // the game will rewrite them as 1.
        public int SkipDlc2Intro; //

        // DLC Section 3 Data (related to the level cap.  removing this section will delevel your character to 50)
        public byte DlcUnknown5; // Read only flag. Always resets to 1 in ver 1.41.  Probably CanExceedLevel50

        // DLC Section 4 Data (DLC backpack)
        public byte SecondaryPackEnabled; // Read only flag. Always resets to 1 in ver 1.41.

        public int NumberOfWeapons;
    }

    public class DlcSection
    {
        public int Id;
        public byte[] RawData;
        public byte[] BaseData; // used temporarily in SaveWSG to store the base data for a section as a byte array
    }
}
