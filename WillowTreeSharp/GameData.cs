using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using WillowTree.Inventory;

namespace WillowTree
{
    public static class GameData
    {
        private static Dictionary<string, string> NameLookup;
        public static readonly string AppPath = WillowSaveGame.AppPath;
        public static readonly string DataPath = WillowSaveGame.DataPath;
        public static string XmlPath = DataPath + "Xml" + Path.DirectorySeparatorChar;
        private static string OpenedLocker; //Keep tracking of last open locker file

        private static readonly List<string> skillfiles = new List<string>()
        {
            DataPath + "gd_skills_common.txt",
            DataPath + "gd_Skills2_Roland.txt",
            DataPath + "gd_Skills2_Lilith.txt",
            DataPath + "gd_skills2_Mordecai.txt",
            DataPath + "gd_Skills2_Brick.txt"
        };

        public static XmlFile EchoesXml = new XmlFile(DataPath + "Echos.ini");
        public static XmlFile LocationsXml = new XmlFile(DataPath + "Locations.ini");
        public static XmlFile QuestsXml = new XmlFile(DataPath + "Quests.ini");
        public static XmlFile SkillsCommonXml = new XmlFile(DataPath + "gd_skills_common.txt");
        public static XmlFile SkillsSoldierXml = new XmlFile(DataPath + "gd_skills2_Roland.txt");
        public static XmlFile SkillsSirenXml = new XmlFile(DataPath + "gd_Skills2_Lilith.txt");
        public static XmlFile SkillsHunterXml = new XmlFile(DataPath + "gd_skills2_Mordecai.txt");
        public static XmlFile SkillsBerserkerXml = new XmlFile(DataPath + "gd_Skills2_Brick.txt");
        public static XmlFile SkillsAllXml = new XmlFile(skillfiles, XmlPath + "gd_skills.xml");
        public static XmlFile PartNamesXml = new XmlFile(DataPath + "partnames.ini");

        public static int[] XPChart = new int[71];

        public static void setXPchart()
        {
            XPChart[0] = 0;
            XPChart[1] = 0;
            XPChart[2] = 358;
            XPChart[3] = 1241;
            XPChart[4] = 2850;
            XPChart[5] = 5376;
            XPChart[6] = 8997;
            XPChart[7] = 13886;
            XPChart[8] = 20208;
            XPChart[9] = 28126;
            XPChart[10] = 37798;
            XPChart[11] = 49377;
            XPChart[12] = 63016;
            XPChart[13] = 78861;
            XPChart[14] = 97061;
            XPChart[15] = 117757;
            XPChart[16] = 141092;
            XPChart[17] = 167207;
            XPChart[18] = 196238;
            XPChart[19] = 228322;
            XPChart[20] = 263595;
            XPChart[21] = 302190;
            XPChart[22] = 344238;
            XPChart[23] = 389873;
            XPChart[24] = 439222;
            XPChart[25] = 492414;
            XPChart[26] = 549578;
            XPChart[27] = 610840;
            XPChart[28] = 676325;
            XPChart[29] = 746158;
            XPChart[30] = 820463;
            XPChart[31] = 899363;
            XPChart[32] = 982980;
            XPChart[33] = 1071436;
            XPChart[34] = 1164850;
            XPChart[35] = 1263343;
            XPChart[36] = 1367034;
            XPChart[37] = 1476041;
            XPChart[38] = 1590483;
            XPChart[39] = 1710476;
            XPChart[40] = 1836137;
            XPChart[41] = 1967582;
            XPChart[42] = 2104926;
            XPChart[43] = 2248285;
            XPChart[44] = 2397772;
            XPChart[45] = 2553561;
            XPChart[46] = 2715586;
            XPChart[47] = 2884139;
            XPChart[48] = 3059273;
            XPChart[49] = 3241098;
            XPChart[50] = 3429728;
            // lvl 50 says 3625271
            XPChart[51] = 3628272;
            XPChart[52] = 3827841;
            XPChart[53] = 4037544;
            XPChart[54] = 4254492;
            XPChart[55] = 4478793;
            XPChart[56] = 4710557;
            XPChart[57] = 4949891;
            XPChart[58] = 5196904;
            XPChart[59] = 5451702;
            XPChart[60] = 5714395;
            XPChart[61] = 5985086;
            //Knoxx-only
            XPChart[62] = 6263885;
            XPChart[63] = 6550897;
            XPChart[64] = 6846227;
            XPChart[65] = 7149982;
            XPChart[66] = 7462266;
            XPChart[67] = 7783184;
            XPChart[68] = 8112840;
            XPChart[69] = 8451340;

            XPChart[70] = 2147483647;
        }

        public static InventoryList WeaponList = new InventoryList(InventoryType.Weapon);
        public static InventoryList ItemList = new InventoryList(InventoryType.Item);
        public static InventoryList BankList = new InventoryList(InventoryType.Any);
        public static InventoryList LockerList = new InventoryList(InventoryType.Any);

        private static int _keyIndex;

        public static string CreateUniqueKey()
        {
            return _keyIndex++.ToString();
        }

        public static string OpenedLockerFilename()
        {
            if (string.IsNullOrEmpty(OpenedLocker))
            {
                return DataPath + "default.xml";
            }
            else
            {
                return OpenedLocker;
            }
        }

        public static void OpenedLockerFilename(string sOpenedLocker)
        {
            OpenedLocker = sOpenedLocker;
        }

        public static string GetName(string part)
        {
            // This method fetches the name of a part from the NameLookup dictionary
            // which only contains name data extracted from each part.  The name
            // could be fetched from the individual part data by using
            // GetPartName(string part) or GetPartAttribute(string part, "PartName"),
            // but since those have to search through lots of Xml nodes to find the
            // data this should be faster.
            string value;
            if (NameLookup.TryGetValue(part, out value) == false)
            {
                return "";
            }

            return value;
        }

        public static string GetPartAttribute(string part, string AttributeName)
        {
            string Database = part.Before('.');
            if (Database == "")
            {
                return "";
            }

            string PartName = part.After('.');

            string DbFileName = DataPath + Database + ".txt";
            if (!File.Exists(DbFileName))
            {
                return "";
            }

            XmlFile DataFile = XmlFile.XmlFileFromCache(DbFileName);

            return DataFile.XmlReadValue(PartName, AttributeName);
        }

        public static List<string> GetPartSection(string part)
        {
            string Database = part.Before('.');
            if (Database == "")
            {
                return null;
            }

            string PartName = part.After('.');

            string DbFileName = XmlPath + Database + ".xml";
            if (!File.Exists(DbFileName))
            {
                return null;
            }

            XmlFile DataFile = XmlFile.XmlFileFromCache(DbFileName);

            return DataFile.XmlReadSection(PartName);
        }

        public static int GetPartRarity(string part)
        {
            string ComponentText = GetPartAttribute(part, "Rarity");
            return Parse.AsInt(ComponentText, null);
        }

        public static Color RarityToColorItem(int rarity)
        {
            Color color;

            if (rarity <= 4) { color = GlobalSettings.RarityColor[0]; }
            else if (rarity <= 9) { color = GlobalSettings.RarityColor[1]; }
            else if (rarity <= 13) { color = GlobalSettings.RarityColor[2]; }
            else if (rarity <= 49) { color = GlobalSettings.RarityColor[3]; }
            else if (rarity <= 60) { color = GlobalSettings.RarityColor[4]; }
            else if (rarity <= 65) { color = GlobalSettings.RarityColor[5]; }
            else if (rarity <= 100) { color = GlobalSettings.RarityColor[6]; }
            else if (rarity <= 169) { color = GlobalSettings.RarityColor[7]; }
            else if (rarity <= 170) { color = GlobalSettings.RarityColor[8]; }
            else if (rarity <= 171) { color = GlobalSettings.RarityColor[9]; }
            else if (rarity <= 179) { color = GlobalSettings.RarityColor[10]; }
            else
            {
                color = GlobalSettings.RarityColor[11];
            }

            return color;
        }

        public static Color RarityToColorWeapon(int rarity)
        {
            Color color;

            if (rarity <= 4) { color = GlobalSettings.RarityColor[0]; }
            else if (rarity <= 10) { color = GlobalSettings.RarityColor[1]; }
            else if (rarity <= 15) { color = GlobalSettings.RarityColor[2]; }
            else if (rarity <= 49) { color = GlobalSettings.RarityColor[3]; }
            else if (rarity <= 60) { color = GlobalSettings.RarityColor[4]; }
            else if (rarity <= 65) { color = GlobalSettings.RarityColor[5]; }
            else if (rarity <= 100) { color = GlobalSettings.RarityColor[6]; }
            else if (rarity <= 169) { color = GlobalSettings.RarityColor[7]; }
            else if (rarity <= 170) { color = GlobalSettings.RarityColor[8]; }
            else if (rarity <= 171) { color = GlobalSettings.RarityColor[9]; }
            else if (rarity <= 179) { color = GlobalSettings.RarityColor[10]; }
            else
            {
                color = GlobalSettings.RarityColor[11];
            }

            return color;
        }

        private static double GetExtraStats(string[] WeaponParts, string StatName)
        {
            double bonus = 0;
            double penalty = 0;
            double value;
            try
            {
                double ExtraDamage = 0;
                for (int i = 3; i < 14; i++)
                {
                    string valuestring = GetPartAttribute(WeaponParts[i], StatName);
                    if (valuestring.Contains(','))
                    {
                        // TODO: I think there are some entries that have two numbers
                        // with a comma between them.  Need to figure out how to properly
                        // handle them.  This breakpoint will catch it when debugging so
                        // so I can figure it out.
                        //Debugger.Break();
                        value = Parse.AsDouble(GetPartAttribute(WeaponParts[i], StatName), 0);
                    }
                    else
                    {
                        value = Parse.AsDouble(GetPartAttribute(WeaponParts[i], StatName), 0);
                    }

                    if (value >= 0)
                    {
                        bonus += value;
                    }
                    else
                    {
                        penalty -= value;
                    }
                }
                ExtraDamage = ((1 + bonus) / (1 + penalty)) - 1;
                return ExtraDamage;
            }
            catch
            {
                return -1;
            }
        }

        public static int GetEffectiveLevelItem(string[] ItemParts, int Quality, int LevelIndex)
        {
            if (LevelIndex != 0)
            {
                return LevelIndex - 2;
            }

            string Manufacturer = ItemParts[6].After("gd_manufacturers.Manufacturers.");
            string LevelIndexText = GetPartAttribute(ItemParts[0], Manufacturer + Quality);
            return Parse.AsInt(LevelIndexText, 2) - 2;
        }

        public static int GetEffectiveLevelWeapon(string[] WeaponParts, int Quality, int LevelIndex)
        {
            if (LevelIndex != 0)
            {
                return LevelIndex - 2;
            }

            // There may be a case below where the manufacturer is invalid or blank
            string Manufacturer = WeaponParts[1].After("gd_manufacturers.Manufacturers.");
            string LevelIndexText = GetPartAttribute(WeaponParts[0], Manufacturer + Quality);
            return Parse.AsInt(LevelIndexText, 2) - 2;
        }

        private static int GetWeaponDamage(string[] WeaponParts, int Quality, int LevelIndex)
        {
            try
            {
                double PenaltyDamage = 0;
                double BonusDamage = 0;
                double Multiplier;
                if (WeaponParts[2] == "gd_weap_repeater_pistol.A_Weapon.WeaponType_repeater_pistol")
                {
                    Multiplier = 1;
                }
                else
                {
                    Multiplier = Parse.AsDouble(GetPartAttribute(WeaponParts[2], "WeaponDamageFormulaMultiplier"), 1);
                }

                int Level = GetEffectiveLevelWeapon(WeaponParts, Quality, LevelIndex);
                double Power = 1.3;
                double Offset = 9;
                for (int i = 3; i < 14; i++)
                {
                    if (WeaponParts[i].Contains("."))
                    {
                        double PartDamage = Parse.AsDouble(GetPartAttribute(WeaponParts[i], "WeaponDamage"), 0);
                        if (PartDamage < 0)
                        {
                            PenaltyDamage -= PartDamage;
                        }
                        else
                        {
                            BonusDamage += PartDamage;
                        }
                    }
                }

                double DmgScaler = (1 + BonusDamage) / (1 + PenaltyDamage);
                double BaseDamage = 0.8 * Multiplier * (Math.Pow(Level + 2, Power) + Offset);
                double ScaledDamage = BaseDamage * DmgScaler;
                return (int)Math.Truncate(ScaledDamage + 1);
            }
            catch
            {
                return -1;
            }
        }

        public static string WeaponInfo(InventoryEntry invEntry)
        {
            string WeaponInfo;
            string[] parts = invEntry.Parts.ToArray<string>();

            int Damage = GetWeaponDamage(parts, invEntry.QualityIndex, invEntry.LevelIndex);
            WeaponInfo = "Expected Damage: " + Damage;

            double statvalue = GetExtraStats(parts, "TechLevelIncrease");
            if (statvalue != 0)
            {
                WeaponInfo += "\r\nElemental Tech Level: " + statvalue;
            }

            statvalue = GetExtraStats(parts, "WeaponDamage");
            if (statvalue != 0)
            {
                WeaponInfo += "\r\n" + statvalue.ToString("P") + " Damage";
            }

            statvalue = GetExtraStats(parts, "WeaponFireRate");
            if (statvalue != 0)
            {
                WeaponInfo += "\r\n" + statvalue.ToString("P") + " Rate of Fire";
            }

            statvalue = GetExtraStats(parts, "WeaponCritBonus");
            if (statvalue != 0)
            {
                WeaponInfo += "\r\n" + statvalue.ToString("P") + " Critical Damage";
            }

            statvalue = GetExtraStats(parts, "WeaponReloadSpeed");
            if (statvalue != 0)
            {
                WeaponInfo += "\r\n" + statvalue.ToString("P") + " Reload Speed";
            }

            statvalue = GetExtraStats(parts, "WeaponSpread");
            if (statvalue != 0)
            {
                WeaponInfo += "\r\n" + statvalue.ToString("P") + " Spread";
            }

            statvalue = GetExtraStats(parts, "MaxAccuracy");
            if (statvalue != 0)
            {
                WeaponInfo += "\r\n" + statvalue.ToString("P") + " Max Accuracy";
            }

            statvalue = GetExtraStats(parts, "MinAccuracy");
            if (statvalue != 0)
            {
                WeaponInfo += "\r\n" + statvalue.ToString("P") + " Min Accuracy";
            }

            return WeaponInfo;
        }

        public static void InitializeNameLookup()
        {
            NameLookup = new Dictionary<string, string>();
            {
                XmlFile names = PartNamesXml;

                foreach (string section in names.stListSectionNames())
                {
                    foreach (string entry in names.XmlReadSection(section))
                    {
                        int index = entry.IndexOf(':');
                        string part = entry.Substring(0, index);
                        string name = entry.Substring(index + 1);
                        NameLookup.Add(part, name);
                    }
                }
            }
        }
    }
}
