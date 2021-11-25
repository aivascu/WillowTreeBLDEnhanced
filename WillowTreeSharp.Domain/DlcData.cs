using System.Collections.Generic;

namespace WillowTreeSharp.Domain
{
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
        public List<BankEntry> BankInventory = new List<BankEntry>();

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
}