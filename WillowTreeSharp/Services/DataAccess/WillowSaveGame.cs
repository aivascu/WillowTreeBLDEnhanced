using System.Collections.Generic;
using WillowTreeSharp.Domain;

namespace WillowTree.Services.DataAccess
{
    public class WillowSaveGame : WillowSaveGameBase
    {
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

        public List<ChallengeDataEntry> challenges;

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
    }
}
