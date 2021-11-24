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
                return this.values.ToList();
            }

            public int Quality
            {
                get => this.values[0x1];
                set => this.values[0x1] = value;
            }

            public int EquipedSlot
            {
                get => this.values[0x2];
                set => this.values[0x2] = value;
            }

            public int Level
            {
                get => this.values[0x3];
                set => this.values[0x3] = value;
            }

            public int Junk
            {
                get => this.values[0x4];
                set => this.values[0x4] = value;
            }

            public int Locked
            {
                get => this.values[0x5];
                set => this.values[0x5] = value;
            }
        }

        public class Item : WillowObject
        {
            public Item()
            {
                this.ReadStrings = ReadItemStrings;
            }

            public int Quantity
            {
                get => this.values[0x0];
                set => this.values[0x0] = value;
            }
        }

        public class Weapon : WillowObject
        {
            public Weapon()
            {
                this.ReadStrings = ReadWeaponStrings;
            }

            public int Ammo
            {
                get => this.values[0];
                set => this.values[0] = value;
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
                this.ProfileId = con.Header.ProfileID;
                this.DeviceId = con.Header.DeviceID;

                return new MemoryStream(con.GetFile("SaveGame.sav").GetTempIO(true).ReadStream(), false);
            }
            catch
            {
                try
                {
                    var manual = new DJsIO(fileInMemory, true);
                    manual.ReadBytes(0x371);
                    this.ProfileId = manual.ReadInt64();
                    manual.ReadBytes(0x84);
                    this.DeviceId = manual.ReadBytes(0x14);
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
                this.Platform = ReadPlatform(fileStream);
                fileStream.Seek(0x0, SeekOrigin.Begin);

                switch (this.Platform)
                {
                    case "X360":
                    case "X360JP":
                        using (var x360FileStream = this.OpenXboxWsgStream(fileStream))
                        {
                            this.ReadWsg(x360FileStream);
                        }

                        break;

                    case "PS3":
                    case "PC":
                        this.ReadWsg(fileStream);
                        break;

                    default:
                        throw new FileFormatException($"Input file is not a WSG (platform is {this.Platform}).");
                }

                this.OpenedWsg = inputFile;
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
            package.HeaderData.Title_Display = $"{this.CharacterName} - Level {this.Level} - {this.CurrentLocation}";
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
            item.Strings = item.ReadStrings(reader, this.EndianWsg);
            item.SetValues(item.ReadValues(reader, this.EndianWsg, this.RevisionNumber));
            return item;
        }

        private void ReadObjects<T>(BinaryReader reader, ref List<T> objects, int groupSize)
            where T : WillowObject, new()
        {
            for (var progress = 0; progress < groupSize; progress++)
            {
                var item = this.ReadObject<T>(reader);
                objects.Add(item);
            }
        }

        public void SaveWsg(string filename)
        {
            switch (this.Platform)
            {
                case "PS3":
                case "PC":
                    {
                        using (var save = new BinaryWriter(new FileStream(filename, FileMode.Create)))
                        {
                            save.Write(this.WriteWsg());
                        }

                        break;
                    }
                case "X360":
                    {
                        var tempSaveName = $"{filename}.temp";
                        using (var save = new BinaryWriter(new FileStream(tempSaveName, FileMode.Create)))
                        {
                            save.Write(this.WriteWsg());
                        }

                        this.BuildXboxPackage(filename, tempSaveName, 0x1);
                        File.Delete(tempSaveName);
                        break;
                    }
                case "X360JP":
                    {
                        var tempSaveName = $"{filename}.temp";
                        using (var save = new BinaryWriter(new FileStream(tempSaveName, FileMode.Create)))
                        {
                            save.Write(this.WriteWsg());
                        }

                        this.BuildXboxPackage(filename, tempSaveName, 0x2);
                        File.Delete(tempSaveName);
                        break;
                    }
            }
        }

        ///<summary>Read savegame data from an open stream</summary>
        public void ReadWsg(Stream fileStream)
        {
            var testReader = new BinaryReader(fileStream, Encoding.ASCII);

            this.ContainsRawData = false;
            this.RequiredRepair = false;
            this.MagicHeader = new string(testReader.ReadChars(0x3));
            this.VersionNumber = testReader.ReadInt32();

            switch (this.VersionNumber)
            {
                case 0x2:
                    this.EndianWsg = ByteOrder.LittleEndian;
                    break;

                case 0x02000000:
                    this.VersionNumber = 0x2;
                    this.EndianWsg = ByteOrder.BigEndian;
                    break;

                default:
                    throw new FileFormatException(
                        $"WSG version number does match any known version ({this.VersionNumber}).");
            }

            this.Plyr = new string(testReader.ReadChars(0x4));
            if (!string.Equals(this.Plyr, "PLYR", StringComparison.Ordinal))
            {
                throw new FileFormatException("Player header does not match expected value.");
            }

            this.RevisionNumber = ReadInt32(testReader, this.EndianWsg);
            ExportValuesCount = this.RevisionNumber < EnhancedVersion ? 0x4 : 0x6;
            this.Class = ReadString(testReader, this.EndianWsg);
            this.Level = ReadInt32(testReader, this.EndianWsg);
            this.Experience = ReadInt32(testReader, this.EndianWsg);
            this.SkillPoints = ReadInt32(testReader, this.EndianWsg);
            this.Unknown1 = ReadInt32(testReader, this.EndianWsg);
            this.Cash = ReadInt32(testReader, this.EndianWsg);
            this.FinishedPlaythrough1 = ReadInt32(testReader, this.EndianWsg);
            this.NumberOfSkills = this.ReadSkills(testReader, this.EndianWsg);
            this.Vehi1Color = ReadInt32(testReader, this.EndianWsg);
            this.Vehi2Color = ReadInt32(testReader, this.EndianWsg);
            this.Vehi1Type = ReadInt32(testReader, this.EndianWsg);
            this.Vehi2Type = ReadInt32(testReader, this.EndianWsg);
            this.NumberOfPools = this.ReadAmmo(testReader, this.EndianWsg);
            Console.WriteLine(@"====== ENTER ITEM ======");
            this.ReadObjects(testReader, ref this.Items, ReadInt32(testReader, this.EndianWsg));
            Console.WriteLine(@"====== EXIT ITEM ======");
            this.BackpackSize = ReadInt32(testReader, this.EndianWsg);
            this.EquipSlots = ReadInt32(testReader, this.EndianWsg);
            Console.WriteLine(@"====== ENTER WEAPON ======");
            this.ReadObjects(testReader, ref this.Weapons, ReadInt32(testReader, this.EndianWsg));
            Console.WriteLine(@"====== EXIT WEAPON ======");

            this.ChallengeDataBlockLength = ReadInt32(testReader, this.EndianWsg);
            var challengeDataBlock = testReader.ReadBytes(this.ChallengeDataBlockLength);
            if (challengeDataBlock.Length != this.ChallengeDataBlockLength)
            {
                throw new EndOfStreamException();
            }

            using (var challengeReader = new BinaryReader(new MemoryStream(challengeDataBlock, false), Encoding.ASCII))
            {
                this.ChallengeDataBlockId = ReadInt32(challengeReader, this.EndianWsg);
                this.ChallengeDataLength = ReadInt32(challengeReader, this.EndianWsg);
                this.ChallengeDataEntries = ReadInt16(challengeReader, this.EndianWsg);
                this._challenges = new List<ChallengeDataEntry>();
                for (var i = 0x0; i < this.ChallengeDataEntries; i++)
                {
                    ChallengeDataEntry challenge;
                    challenge.Id = ReadInt16(challengeReader, this.EndianWsg);
                    challenge.TypeId = challengeReader.ReadByte();
                    challenge.Value = ReadInt32(challengeReader, this.EndianWsg);
                    this._challenges.Add(challenge);
                }
            }

            this.TotalLocations = this.ReadLocations(testReader, this.EndianWsg);
            this.CurrentLocation = ReadString(testReader, this.EndianWsg);
            this.SaveInfo1To5[0x0] = ReadInt32(testReader, this.EndianWsg);
            this.SaveInfo1To5[0x1] = ReadInt32(testReader, this.EndianWsg);
            this.SaveInfo1To5[0x2] = ReadInt32(testReader, this.EndianWsg);
            this.SaveInfo1To5[0x3] = ReadInt32(testReader, this.EndianWsg);
            this.SaveInfo1To5[0x4] = ReadInt32(testReader, this.EndianWsg);
            this.SaveNumber = ReadInt32(testReader, this.EndianWsg);
            this.SaveInfo7To10[0x0] = ReadInt32(testReader, this.EndianWsg);
            this.SaveInfo7To10[0x1] = ReadInt32(testReader, this.EndianWsg);
            this.NumberOfQuestLists = this.ReadQuests(testReader, this.EndianWsg);

            this.TotalPlayTime = ReadInt32(testReader, this.EndianWsg);
            this.LastPlayedDate = ReadString(testReader, this.EndianWsg); //YYYYMMDDHHMMSS
            this.CharacterName = ReadString(testReader, this.EndianWsg);
            this.Color1 = ReadInt32(testReader, this.EndianWsg); //ABGR Big (X360, PS3), RGBA Little (PC)
            this.Color2 = ReadInt32(testReader, this.EndianWsg); //ABGR Big (X360, PS3), RGBA Little (PC)
            this.Color3 = ReadInt32(testReader, this.EndianWsg); //ABGR Big (X360, PS3), RGBA Little (PC)
            this.Head = ReadInt32(testReader, this.EndianWsg);

            if (this.RevisionNumber >= EnhancedVersion)
            {
                this.Unknown2 = ReadBytes(testReader, 0x55, this.EndianWsg);
            }

            this.PromoCodesUsed = ReadListInt32(testReader, this.EndianWsg);
            this.PromoCodesRequiringNotification = ReadListInt32(testReader, this.EndianWsg);
            this.NumberOfEchoLists = this.ReadEchoes(testReader, this.EndianWsg);

            this.Dlc.DataSections = new List<DlcSection>();
            this.Dlc.DlcSize = ReadInt32(testReader, this.EndianWsg);
            var dlcDataBlock = testReader.ReadBytes(this.Dlc.DlcSize);
            if (dlcDataBlock.Length != this.Dlc.DlcSize)
            {
                throw new EndOfStreamException();
            }

            using (var dlcDataReader = new BinaryReader(new MemoryStream(dlcDataBlock, false), Encoding.ASCII))
            {
                var remainingBytes = this.Dlc.DlcSize;
                while (remainingBytes > 0x0)
                {
                    var section = new DlcSection
                    {
                        Id = ReadInt32(dlcDataReader, this.EndianWsg)
                    };
                    var sectionLength = ReadInt32(dlcDataReader, this.EndianWsg);
                    long sectionStartPos = (int)dlcDataReader.BaseStream.Position;
                    switch (section.Id)
                    {
                        case Section1Id: // 0x43211234
                            this.Dlc.HasSection1 = true;
                            this.Dlc.DlcUnknown1 = dlcDataReader.ReadByte();
                            this.Dlc.BankSize = ReadInt32(dlcDataReader, this.EndianWsg);
                            var bankEntriesCount = ReadInt32(dlcDataReader, this.EndianWsg);
                            this.Dlc.BankInventory = new List<BankEntry>();
                            Console.WriteLine(@"====== ENTER BANK ======");
                            BankValuesCount = ExportValuesCount;
                            for (var i = 0x0; i < bankEntriesCount; i++)
                            {
                                this.Dlc.BankInventory.Add(this.CreateBankEntry(dlcDataReader));
                            }

                            Console.WriteLine(@"====== EXIT BANK ======");
                            break;

                        case Section2Id: // 0x02151984
                            this.Dlc.HasSection2 = true;
                            this.Dlc.DlcUnknown2 = ReadInt32(dlcDataReader, this.EndianWsg);
                            this.Dlc.DlcUnknown3 = ReadInt32(dlcDataReader, this.EndianWsg);
                            this.Dlc.DlcUnknown4 = ReadInt32(dlcDataReader, this.EndianWsg);
                            this.Dlc.SkipDlc2Intro = ReadInt32(dlcDataReader, this.EndianWsg);
                            break;

                        case Section3Id: // 0x32235947
                            this.Dlc.HasSection3 = true;
                            this.Dlc.DlcUnknown5 = dlcDataReader.ReadByte();
                            break;

                        case Section4Id: // 0x234ba901
                            this.Dlc.HasSection4 = true;
                            this.Dlc.SecondaryPackEnabled = dlcDataReader.ReadByte();

                            try
                            {
                                Console.WriteLine(@"====== ENTER DLC ITEM ======");
                                this.ReadObjects(dlcDataReader, ref this.Items, ReadInt32(dlcDataReader, this.EndianWsg));
                                Console.WriteLine(@"====== EXIT DLC ITEM ======");
                            }
                            catch
                            {
                                // The data was invalid so the processing ran into an exception.
                                // See if the user wants to ignore the invalid data and just try
                                // to recover partial data.  If not, just re-throw the exception.
                                if (!this.AutoRepair)
                                {
                                    throw;
                                }

                                // Set the flag to indicate that repair was required to load the savegame
                                this.RequiredRepair = true;

                                // If the data is invalid here, the whole DLC weapon list is invalid so
                                // set its length to 0 and be done
                                this.Dlc.NumberOfWeapons = 0x0;

                                // Skip to the end of the section to discard any raw data that is left over
                                dlcDataReader.BaseStream.Position = sectionStartPos + sectionLength;
                            }

                            try
                            {
                                Console.WriteLine(@"====== ENTER DLC WEAPON ======");
                                this.ReadObjects(dlcDataReader, ref this.Weapons, ReadInt32(dlcDataReader, this.EndianWsg));
                                Console.WriteLine(@"====== EXIT DLC WEAPON ======");
                            }
                            catch
                            {
                                // The data was invalid so the processing ran into an exception.
                                // See if the user wants to ignore the invalid data and just try
                                // to recover partial data.  If not, just re-throw the exception.
                                if (!this.AutoRepair)
                                {
                                    throw;
                                }

                                // Set the flag to indicate that repair was required to load the savegame
                                this.RequiredRepair = true;

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
                        this.ContainsRawData = true;
                    }

                    remainingBytes -= sectionLength + 0x8;
                    this.Dlc.DataSections.Add(section);
                }

                if (this.RevisionNumber < EnhancedVersion)
                {
                    return;
                }

                //Padding at the end of file, don't know exactly why
                var temp = new List<byte>();
                while (!IsEndOfFile(testReader))
                {
                    temp.Add(ReadBytes(testReader, 0x1, this.EndianWsg)[0x0]);
                }

                this.Unknown3 = temp.ToArray();
            }
        }

        private int ReadEchoes(BinaryReader reader, ByteOrder endianWsg)
        {
            var echoListCount = ReadInt32(reader, endianWsg);

            this.EchoLists.Clear();
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

                this.EchoLists.Add(echoTable);
            }

            return echoListCount;
        }

        private int ReadQuests(BinaryReader reader, ByteOrder endianWsg)
        {
            var numberOfQuestList = ReadInt32(reader, endianWsg);

            this.QuestLists.Clear();
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

                this.QuestLists.Add(questTable);
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

            this.SkillNames = tempSkillNames;
            this.LevelOfSkills = tempLevelOfSkills;
            this.ExpOfSkills = tempExpOfSkills;
            this.InUse = tempInUse;

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

            this.ResourcePools = tempResourcePools;
            this.AmmoPools = tempAmmoPools;
            this.RemainingPools = tempRemainingPools;
            this.PoolLevels = tempPoolLevels;

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

            this.LocationStrings = tempLocationStrings;
            return locationCount;
        }

        public void DiscardRawData()
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
            for (var i = this.Dlc.DataSections.Count - 0x1; i >= 0x0; i--)
            {
                var section = this.Dlc.DataSections[i];

                if (knownSectionIds.Contains(section.Id))
                {
                    // clear the raw data in this DLC data section
                    section.RawData = Array.Empty<byte>();
                }
                else
                {
                    // if the section id is not recognized remove it completely
                    section.RawData = null;
                    this.Dlc.DataSections.RemoveAt(i);
                }
            }

            // Now that all the raw data has been removed, reset the raw data flag
            this.ContainsRawData = false;
        }

        private void WriteValues(BinaryWriter writer, IReadOnlyList<int> values)
        {
            Write(writer, values[0x0], this.EndianWsg);
            var tempLevelQuality = (ushort)values[0x1] + (ushort)values[0x3] * (uint)0x10000;
            Write(writer, (int)tempLevelQuality, this.EndianWsg);
            Write(writer, values[0x2], this.EndianWsg);
            if (this.RevisionNumber < EnhancedVersion)
            {
                return;
            }

            Write(writer, values[0x4], this.EndianWsg);
            Write(writer, values[0x5], this.EndianWsg);
        }

        private void WriteStrings(BinaryWriter writer, List<string> strings)
        {
            foreach (var s in strings)
            {
                Write(writer, s, this.EndianWsg);
            }
        }

        private void WriteObject<T>(BinaryWriter writer, T obj) where T : WillowObject
        {
            this.WriteStrings(writer, obj.Strings);
            this.WriteValues(writer, obj.GetValues());
        }

        private void WriteObjects<T>(BinaryWriter writer, List<T> objects) where T : WillowObject
        {
            Write(writer, objects.Count, this.EndianWsg);
            foreach (var obj in objects)
            {
                this.WriteObject(writer, obj);
            }
        }

        /// <summary>
        /// Save the current data to a WSG as a byte[]
        /// </summary>
        public byte[] WriteWsg()
        {
            var outStream = new MemoryStream();
            var writer = new BinaryWriter(outStream);

            this.SplitInventoryIntoPacks();

            writer.Write(Encoding.ASCII.GetBytes(this.MagicHeader));
            Write(writer, this.VersionNumber, this.EndianWsg);
            writer.Write(Encoding.ASCII.GetBytes(this.Plyr));
            Write(writer, this.RevisionNumber, this.EndianWsg);
            Write(writer, this.Class, this.EndianWsg);
            Write(writer, this.Level, this.EndianWsg);
            Write(writer, this.Experience, this.EndianWsg);
            Write(writer, this.SkillPoints, this.EndianWsg);
            Write(writer, this.Unknown1, this.EndianWsg);
            Write(writer, this.Cash, this.EndianWsg);
            Write(writer, this.FinishedPlaythrough1, this.EndianWsg);
            Write(writer, this.NumberOfSkills, this.EndianWsg);

            for (var progress = 0x0; progress < this.NumberOfSkills; progress++) //Write Skills
            {
                Write(writer, this.SkillNames[progress], this.EndianWsg);
                Write(writer, this.LevelOfSkills[progress], this.EndianWsg);
                Write(writer, this.ExpOfSkills[progress], this.EndianWsg);
                Write(writer, this.InUse[progress], this.EndianWsg);
            }

            Write(writer, this.Vehi1Color, this.EndianWsg);
            Write(writer, this.Vehi2Color, this.EndianWsg);
            Write(writer, this.Vehi1Type, this.EndianWsg);
            Write(writer, this.Vehi2Type, this.EndianWsg);
            Write(writer, this.NumberOfPools, this.EndianWsg);

            for (var progress = 0x0; progress < this.NumberOfPools; progress++) //Write Ammo Pools
            {
                Write(writer, this.ResourcePools[progress], this.EndianWsg);
                Write(writer, this.AmmoPools[progress], this.EndianWsg);
                Write(writer, this.RemainingPools[progress], this.EndianWsg);
                Write(writer, this.PoolLevels[progress], this.EndianWsg);
            }

            this.WriteObjects(writer, this.Items1); //Write Items

            Write(writer, this.BackpackSize, this.EndianWsg);
            Write(writer, this.EquipSlots, this.EndianWsg);

            this.WriteObjects(writer, this.Weapons1); //Write Weapons

            var count = (short)this._challenges.Count;
            Write(writer, count * 0x7 + 0xA, this.EndianWsg);
            Write(writer, this.ChallengeDataBlockId, this.EndianWsg);
            Write(writer, count * 0x7 + 0x2, this.EndianWsg);
            Write(writer, count, this.EndianWsg);
            foreach (var challenge in this._challenges)
            {
                Write(writer, challenge.Id, this.EndianWsg);
                writer.Write(challenge.TypeId);
                Write(writer, challenge.Value, this.EndianWsg);
            }

            Write(writer, this.TotalLocations, this.EndianWsg);

            for (var progress = 0x0; progress < this.TotalLocations; progress++) //Write Locations
            {
                Write(writer, this.LocationStrings[progress], this.EndianWsg);
            }

            Write(writer, this.CurrentLocation, this.EndianWsg);
            Write(writer, this.SaveInfo1To5[0x0], this.EndianWsg);
            Write(writer, this.SaveInfo1To5[0x1], this.EndianWsg);
            Write(writer, this.SaveInfo1To5[0x2], this.EndianWsg);
            Write(writer, this.SaveInfo1To5[0x3], this.EndianWsg);
            Write(writer, this.SaveInfo1To5[0x4], this.EndianWsg);
            Write(writer, this.SaveNumber, this.EndianWsg);
            Write(writer, this.SaveInfo7To10[0x0], this.EndianWsg);
            Write(writer, this.SaveInfo7To10[0x1], this.EndianWsg);
            Write(writer, this.NumberOfQuestLists, this.EndianWsg);

            for (var listIndex = 0x0; listIndex < this.NumberOfQuestLists; listIndex++)
            {
                var qt = this.QuestLists[listIndex];
                Write(writer, qt.Index, this.EndianWsg);
                Write(writer, qt.CurrentQuest, this.EndianWsg);
                Write(writer, qt.TotalQuests, this.EndianWsg);

                var questCount = qt.TotalQuests;
                for (var questIndex = 0x0; questIndex < questCount; questIndex++) //Write Playthrough 1 Quests
                {
                    var qe = qt.Quests[questIndex];
                    Write(writer, qe.Name, this.EndianWsg);
                    Write(writer, qe.Progress, this.EndianWsg);
                    Write(writer, qe.DlcValue1, this.EndianWsg);
                    Write(writer, qe.DlcValue2, this.EndianWsg);

                    var objectiveCount = qe.NumberOfObjectives;
                    Write(writer, objectiveCount, this.EndianWsg);

                    for (var i = 0x0; i < objectiveCount; i++)
                    {
                        Write(writer, qe.Objectives[i].Description, this.EndianWsg);
                        Write(writer, qe.Objectives[i].Progress, this.EndianWsg);
                    }
                }
            }

            Write(writer, this.TotalPlayTime, this.EndianWsg);
            Write(writer, this.LastPlayedDate, this.EndianWsg);
            Write(writer, this.CharacterName, this.EndianWsg);
            Write(writer, this.Color1, this.EndianWsg); //ABGR Big (X360, PS3), RGBA Little (PC)
            Write(writer, this.Color2, this.EndianWsg); //ABGR Big (X360, PS3), RGBA Little (PC)
            Write(writer, this.Color3, this.EndianWsg); //ABGR Big (X360, PS3), RGBA Little (PC)
            Write(writer, this.Head, this.EndianWsg);

            if (this.RevisionNumber >= EnhancedVersion)
            {
                Write(writer, this.Unknown2);
            }

            var numberOfPromoCodesUsed = this.PromoCodesUsed.Count;
            Write(writer, numberOfPromoCodesUsed, this.EndianWsg);
            for (var i = 0x0; i < numberOfPromoCodesUsed; i++)
            {
                Write(writer, this.PromoCodesUsed[i], this.EndianWsg);
            }

            var numberOfPromoCodesRequiringNotification = this.PromoCodesRequiringNotification.Count;
            Write(writer, numberOfPromoCodesRequiringNotification, this.EndianWsg);
            for (var i = 0x0; i < numberOfPromoCodesRequiringNotification; i++)
            {
                Write(writer, this.PromoCodesRequiringNotification[i], this.EndianWsg);
            }

            Write(writer, this.NumberOfEchoLists, this.EndianWsg);
            for (var listIndex = 0x0; listIndex < this.NumberOfEchoLists; listIndex++)
            {
                var et = this.EchoLists[listIndex];
                Write(writer, et.Index, this.EndianWsg);
                Write(writer, et.TotalEchoes, this.EndianWsg);

                for (var echoIndex = 0x0; echoIndex < et.TotalEchoes; echoIndex++) //Write Locations
                {
                    var ee = et.Echoes[echoIndex];
                    Write(writer, ee.Name, this.EndianWsg);
                    Write(writer, ee.DlcValue1, this.EndianWsg);
                    Write(writer, ee.DlcValue2, this.EndianWsg);
                }
            }

            this.Dlc.DlcSize = 0x0;
            // This loop writes the base data for each section into byte[]
            // BaseData so its size can be obtained and it can easily be
            // written to the output stream as a single block.  Calculate
            // DLC.DLC_Size as it goes since that has to be written before
            // the blocks are written to the output stream.
            foreach (var section in this.Dlc.DataSections)
            {
                var tempStream = new MemoryStream();
                var memoryWriter = new BinaryWriter(tempStream);
                switch (section.Id)
                {
                    case Section1Id:
                        memoryWriter.Write(this.Dlc.DlcUnknown1);
                        Write(memoryWriter, this.Dlc.BankSize, this.EndianWsg);
                        Write(memoryWriter, this.Dlc.BankInventory.Count, this.EndianWsg);
                        for (var i = 0x0; i < this.Dlc.BankInventory.Count; i++)
                        {
                            Write(memoryWriter, this.Dlc.BankInventory[i].Serialize(this.EndianWsg));
                        }

                        break;

                    case Section2Id:
                        Write(memoryWriter, this.Dlc.DlcUnknown2, this.EndianWsg);
                        Write(memoryWriter, this.Dlc.DlcUnknown3, this.EndianWsg);
                        Write(memoryWriter, this.Dlc.DlcUnknown4, this.EndianWsg);
                        Write(memoryWriter, this.Dlc.SkipDlc2Intro, this.EndianWsg);
                        break;

                    case Section3Id:
                        memoryWriter.Write(this.Dlc.DlcUnknown5);
                        break;

                    case Section4Id:
                        memoryWriter.Write(this.Dlc.SecondaryPackEnabled);
                        // The DLC backpack items
                        this.WriteObjects(memoryWriter, this.Items2);
                        // The DLC backpack weapons
                        this.WriteObjects(memoryWriter, this.Weapons2);
                        break;
                }

                section.BaseData = tempStream.ToArray();
                this.Dlc.DlcSize +=
                    section.BaseData.Length + section.RawData.Length + 0x8; // 8 = 4 bytes for id, 4 bytes for length
            }

            // Now its time to actually write all the data sections to the output stream
            Write(writer, this.Dlc.DlcSize, this.EndianWsg);
            foreach (var section in this.Dlc.DataSections)
            {
                Write(writer, section.Id, this.EndianWsg);
                var sectionLength = section.BaseData.Length + section.RawData.Length;
                Write(writer, sectionLength, this.EndianWsg);
                writer.Write(section.BaseData);
                writer.Write(section.RawData);
                section.BaseData = null; // BaseData isn't needed anymore.  Free it.
            }

            if (this.RevisionNumber >= EnhancedVersion)
            {
                //Past end padding
                Write(writer, this.Unknown3);
            }

            // Clear the temporary lists used to split primary and DLC pack data
            this.Items1 = null;
            this.Items2 = null;
            this.Weapons1 = null;
            this.Weapons2 = null;
            return outStream.ToArray();
        }

        ///<summary>
        /// Split the weapon and item lists into two parts: one for the primary pack and one for DLC backpack
        /// </summary>
        public void SplitInventoryIntoPacks()
        {
            this.Items1 = new List<Item>();
            this.Items2 = new List<Item>();
            this.Weapons1 = new List<Weapon>();
            this.Weapons2 = new List<Weapon>();
            // Split items and weapons into two lists each so they can be put into the
            // DLC backpack or regular backpack area as needed.  Any item with a level
            // override and special dlc items go in the DLC backpack.  All others go
            // in the regular inventory.
            if (!this.Dlc.HasSection4 || this.Dlc.SecondaryPackEnabled == 0x0)
            {
                // no secondary pack so put it all in primary pack
                foreach (var item in this.Items)
                {
                    this.Items1.Add(item);
                }

                foreach (var weapon in this.Weapons)
                {
                    this.Weapons1.Add(weapon);
                }

                return;
            }

            foreach (var item in this.Items)
            {
                if (item.Level == 0x0 && item.Strings[0x0].Substring(0x0, 0x3) != "dlc")
                {
                    this.Items1.Add(item);
                }
                else
                {
                    this.Items2.Add(item);
                }
            }

            foreach (var weapon in this.Weapons)
            {
                if (weapon.Level == 0x0 && weapon.Strings[0x0].Substring(0x0, 0x3) != "dlc")
                {
                    this.Weapons1.Add(weapon);
                }
                else
                {
                    this.Weapons2.Add(weapon);
                }
            }
        }

        public sealed class BankEntry : WillowObject
        {
            public byte TypeId { get; set; }

            public int Quantity
            {
                get => this.values[0x0];
                set => this.values[0x0] = value;
            }

            public byte[] Serialize(ByteOrder endian)
            {
                var bytes = new List<byte>();
                if (this.TypeId != 0x1 && this.TypeId != 0x2)
                {
                    throw new FormatException($"Bank entry to be written has an invalid Type ID.  TypeId = {this.TypeId}");
                }

                bytes.Add(this.TypeId);
                var count = 0x0;
                foreach (var component in this.Strings)
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
                        bytes.AddRange(GetBytesFromInt((ushort)this.Quality + (ushort)this.Level * (uint)0x10000, endian));
                    }

                    count++;
                }

                bytes.AddRange(new byte[0x8]);
                bytes.Add((byte)this.EquipedSlot);
                bytes.Add(0x1);
                if (ExportValuesCount > 0x4)
                {
                    bytes.Add((byte)this.Junk);
                    bytes.Add((byte)this.Locked);
                }

                if (this.TypeId == 0x1)
                {
                    bytes.AddRange(GetBytesFromInt(this.Quantity, endian));
                }
                else
                {
                    if (ExportValuesCount > 0x4)
                    {
                        bytes.Add((byte)this.Locked);
                    }
                    else
                    {
                        bytes.Add((byte)this.Quantity);
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
                    this.Quality = (short)(temp % 0x10000);
                    this.Level = (short)(temp / 0x10000);
                }
            }

            public void Deserialize(BinaryReader reader, ByteOrder endian, BankEntry previous)
            {
                this.TypeId = reader.ReadByte();
                if (this.TypeId != 0x1 && this.TypeId != 0x2)
                {
                    //Try to repair broken item
                    if (previous != null)
                    {
                        RepairItem(reader, endian, previous, 0x1);
                        this.TypeId = reader.ReadByte();
                        Console.WriteLine($"{this.TypeId} {reader.ReadByte()}");
                        reader.BaseStream.Position--;
                        if (this.TypeId != 0x1 && this.TypeId != 0x2)
                        {
                            reader.BaseStream.Position -= 0x1 + (previous.TypeId == 0x1 ? 0x4 : 0x1);
                            SearchNextItem(reader, endian);
                            this.TypeId = reader.ReadByte();
                        }
                        else
                        {
                            BankValuesCount = 0x4;
                        }
                    }
                }

                this.Strings = new List<string>();
                this.Strings.AddRange(new string[this.TypeId == 0x1 ? 0xE : 0x9]);
                for (var index = 0; index < this.Strings.Count; index++)
                {
                    this.DeserializePart(reader, endian, out var part, index);
                    this.Strings[index] = part;
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
        }

        private BankEntry CreateBankEntry(BinaryReader reader)
        {
            //Create new entry
            var entry = new BankEntry();
            var previous = this.Dlc.BankInventory.Count == 0x0
                ? null
                : this.Dlc.BankInventory[this.Dlc.BankInventory.Count - 0x1];
            entry.Deserialize(reader, this.EndianWsg, previous);
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
