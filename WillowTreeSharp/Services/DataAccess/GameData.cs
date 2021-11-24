﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using WillowTree.Inventory;

namespace WillowTree.Services.DataAccess
{
    public static class GameData
    {
        private static string openedLocker; //Keep tracking of last open locker file
        private static Dictionary<string, string> nameLookup;

        public static string DataPath { get; } = Constants.DataPath;

        private static readonly List<string> skillFiles = new List<string>
        {
            DataPath + "gd_skills_common.txt",
            DataPath + "gd_Skills2_Roland.txt",
            DataPath + "gd_Skills2_Lilith.txt",
            DataPath + "gd_skills2_Mordecai.txt",
            DataPath + "gd_Skills2_Brick.txt"
        };

        public static string XmlPath { get; } = Path.Combine(DataPath, "Xml") + Path.DirectorySeparatorChar;

        public static XmlFile EchoesXml { get; } = new XmlFile(DataPath + "Echos.ini");
        public static XmlFile LocationsXml { get; } = new XmlFile(DataPath + "Locations.ini");
        public static XmlFile QuestsXml { get; } = new XmlFile(DataPath + "Quests.ini");
        public static XmlFile SkillsCommonXml { get; } = new XmlFile(DataPath + "gd_skills_common.txt");
        public static XmlFile SkillsSoldierXml { get; } = new XmlFile(DataPath + "gd_skills2_Roland.txt");
        public static XmlFile SkillsSirenXml { get; } = new XmlFile(DataPath + "gd_Skills2_Lilith.txt");
        public static XmlFile SkillsHunterXml { get; } = new XmlFile(DataPath + "gd_skills2_Mordecai.txt");
        public static XmlFile SkillsBerserkerXml { get; } = new XmlFile(DataPath + "gd_Skills2_Brick.txt");
        public static XmlFile SkillsAllXml { get; } = new XmlFile(skillFiles, XmlPath + "gd_skills.xml");
        public static XmlFile PartNamesXml { get; } = new XmlFile(DataPath + "partnames.ini");

        public static InventoryList WeaponList { get; } = new InventoryList(InventoryType.Weapon);
        public static InventoryList ItemList { get; } = new InventoryList(InventoryType.Item);
        public static InventoryList BankList { get; } = new InventoryList(InventoryType.Any);
        public static InventoryList LockerList { get; } = new InventoryList(InventoryType.Any);

        public static int[] XPChart { get; } = new int[71];

        public static void SetXPchart()
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

        private static int keyIndex;

        public static string CreateUniqueKey()
        {
            return keyIndex++.ToString();
        }

        public static string OpenedLockerFilename()
        {
            return string.IsNullOrEmpty(openedLocker)
                ? DataPath + "default.xml"
                : openedLocker;
        }

        public static void OpenedLockerFilename(string sOpenedLocker)
        {
            openedLocker = sOpenedLocker;
        }

        public static string GetName(string part)
        {
            // This method fetches the name of a part from the NameLookup dictionary
            // which only contains name data extracted from each part.  The name
            // could be fetched from the individual part data by using
            // GetPartName(string part) or GetPartAttribute(string part, "PartName"),
            // but since those have to search through lots of Xml nodes to find the
            // data this should be faster.
            return !nameLookup.TryGetValue(part, out var value) ? "" : value;
        }

        public static string GetPartAttribute(string part, string attributeName)
        {
            string database = part.Before('.');
            if (database == "")
            {
                return string.Empty;
            }

            string partName = part.After('.');

            string dbFileName = DataPath + database + ".txt";
            if (!File.Exists(dbFileName))
            {
                return "";
            }

            XmlFile dataFile = XmlCache.XmlFileFromCache(dbFileName);

            return dataFile.XmlReadValue(partName, attributeName);
        }

        public static List<string> GetPartSection(string part)
        {
            string database = part.Before('.');
            if (database == "")
            {
                return null;
            }

            string partName = part.After('.');

            string dbFileName = XmlPath + database + ".xml";
            if (!File.Exists(dbFileName))
            {
                return null;
            }

            XmlFile dataFile = XmlCache.XmlFileFromCache(dbFileName);

            return dataFile.XmlReadSection(partName);
        }

        public static int GetPartRarity(string part)
        {
            string componentText = GetPartAttribute(part, "Rarity");
            return Parse.AsInt(componentText, null);
        }

        private static double GetExtraStats(string[] weaponParts, string statName)
        {
            double bonus = 0;
            double penalty = 0;
            double value;
            try
            {
                double extraDamage = 0;
                for (int i = 3; i < 14; i++)
                {
                    string valuestring = GetPartAttribute(weaponParts[i], statName);
                    if (valuestring.Contains(','))
                    {
                        // TODO: I think there are some entries that have two numbers
                        // with a comma between them.  Need to figure out how to properly
                        // handle them.  This breakpoint will catch it when debugging so
                        // so I can figure it out.
                        //Debugger.Break();
                        value = Parse.AsDouble(GetPartAttribute(weaponParts[i], statName), 0);
                    }
                    else
                    {
                        value = Parse.AsDouble(GetPartAttribute(weaponParts[i], statName), 0);
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

                extraDamage = ((1 + bonus) / (1 + penalty)) - 1;
                return extraDamage;
            }
            catch
            {
                return -1;
            }
        }

        public static int GetEffectiveLevelItem(string[] itemParts, int quality, int levelIndex)
        {
            if (levelIndex != 0)
            {
                return levelIndex - 2;
            }

            string manufacturer = itemParts[6].After("gd_manufacturers.Manufacturers.");
            string levelIndexText = GetPartAttribute(itemParts[0], manufacturer + quality);
            return Parse.AsInt(levelIndexText, 2) - 2;
        }

        public static int GetEffectiveLevelWeapon(string[] weaponParts, int quality, int levelIndex)
        {
            if (levelIndex != 0)
            {
                return levelIndex - 2;
            }

            // There may be a case below where the manufacturer is invalid or blank
            string manufacturer = weaponParts[1].After("gd_manufacturers.Manufacturers.");
            string levelIndexText = GetPartAttribute(weaponParts[0], manufacturer + quality);
            return Parse.AsInt(levelIndexText, 2) - 2;
        }

        private static int GetWeaponDamage(string[] weaponParts, int quality, int levelIndex)
        {
            try
            {
                double penaltyDamage = 0;
                double bonusDamage = 0;
                double multiplier;
                if (weaponParts[2] == "gd_weap_repeater_pistol.A_Weapon.WeaponType_repeater_pistol")
                {
                    multiplier = 1;
                }
                else
                {
                    multiplier = Parse.AsDouble(GetPartAttribute(weaponParts[2], "WeaponDamageFormulaMultiplier"), 1);
                }

                int level = GetEffectiveLevelWeapon(weaponParts, quality, levelIndex);
                double power = 1.3;
                double offset = 9;
                for (int i = 3; i < 14; i++)
                {
                    if (weaponParts[i].Contains("."))
                    {
                        double partDamage = Parse.AsDouble(GetPartAttribute(weaponParts[i], "WeaponDamage"), 0);
                        if (partDamage < 0)
                        {
                            penaltyDamage -= partDamage;
                        }
                        else
                        {
                            bonusDamage += partDamage;
                        }
                    }
                }

                double dmgScaler = (1 + bonusDamage) / (1 + penaltyDamage);
                double baseDamage = 0.8 * multiplier * (Math.Pow(level + 2, power) + offset);
                double scaledDamage = baseDamage * dmgScaler;
                return (int)Math.Truncate(scaledDamage + 1);
            }
            catch
            {
                return -1;
            }
        }

        public static string WeaponInfo(InventoryEntry invEntry)
        {
            string weaponInfo;
            string[] parts = invEntry.Parts.ToArray<string>();

            int damage = GetWeaponDamage(parts, invEntry.QualityIndex, invEntry.LevelIndex);
            weaponInfo = $"Expected Damage: {damage}";

            double statvalue = GetExtraStats(parts, "TechLevelIncrease");
            if (statvalue != 0)
            {
                weaponInfo += $"\r\nElemental Tech Level: {statvalue}";
            }

            statvalue = GetExtraStats(parts, "WeaponDamage");
            if (statvalue != 0)
            {
                weaponInfo += $"\r\n{statvalue:P} Damage";
            }

            statvalue = GetExtraStats(parts, "WeaponFireRate");
            if (statvalue != 0)
            {
                weaponInfo += $"\r\n{statvalue:P} Rate of Fire";
            }

            statvalue = GetExtraStats(parts, "WeaponCritBonus");
            if (statvalue != 0)
            {
                weaponInfo += $"\r\n{statvalue:P} Critical Damage";
            }

            statvalue = GetExtraStats(parts, "WeaponReloadSpeed");
            if (statvalue != 0)
            {
                weaponInfo += $"\r\n{statvalue:P} Reload Speed";
            }

            statvalue = GetExtraStats(parts, "WeaponSpread");
            if (statvalue != 0)
            {
                weaponInfo += $"\r\n{statvalue:P} Spread";
            }

            statvalue = GetExtraStats(parts, "MaxAccuracy");
            if (statvalue != 0)
            {
                weaponInfo += $"\r\n{statvalue:P} Max Accuracy";
            }

            statvalue = GetExtraStats(parts, "MinAccuracy");
            if (statvalue != 0)
            {
                weaponInfo += $"\r\n{statvalue:P} Min Accuracy";
            }

            return weaponInfo;
        }

        public static void InitializeNameLookup()
        {
            nameLookup = new Dictionary<string, string>();
            {
                XmlFile names = PartNamesXml;

                foreach (string section in names.StListSectionNames())
                {
                    foreach (string entry in names.XmlReadSection(section))
                    {
                        int index = entry.IndexOf(':');
                        string part = entry.Substring(0, index);
                        string name = entry.Substring(index + 1);
                        nameLookup.Add(part, name);
                    }
                }
            }
        }
    }
}
