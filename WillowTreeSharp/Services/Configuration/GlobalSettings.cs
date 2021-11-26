using System;
using System.Drawing;
using System.Globalization;
using System.IO.Abstractions;
using System.Text;
using System.Xml;
using WillowTree.Common;
using WillowTree.Services.DataAccess;

namespace WillowTree.Services.Configuration
{
    public static class GlobalSettings
    {
        private static bool useHexInAdvancedMode;
        private static bool partSelectorTracking = true;
        private static bool showManufacturer;
        private static bool useColor;
        private static bool showRarity;
        private static bool showLevel = true;
        private static string lastLockerFile = string.Empty;
        private static Color backgroundColor = Color.FromArgb(0x30, 0x30, 0x30);

        // ------- MAX VALUES ---------
        // All values that exceed these sanity limits will be adjusted by the UI and give the
        // user a warning except for cash.  Cash increases too often automatically through
        // gameplay so it will be adjusted silently to prevent receiving a warning every time
        // the savegame is opened.
        //
        // All max values can be set to any number up to 2147483647 (int.MaxValue) by editing
        // the limits in 'options.xml'.  These values are the defaults if 'options.xml' has
        // not yet been created or its values are missing or corrupt.
        // ----------------------------

        // Borderlands 1.4.2.1 (Steam PC) has a bug that allows the cash to go to an extreme
        // negative number if you exceed int.MaxValue when picking up a money bag off the ground.
        // You cannot buy anything in that state until you sell an item to the shop, which will
        // set your money back to int.MaxValue again and repeat the cycle when you again pick
        // up money off the ground.  The default for MaxCash is set to 1 billion to stay clear
        // of int.MaxValue and avoid that problem.  Bank and Backpack limits exist to keep the
        // savegame file from becoming excessively large which slows down loading.  Borderlands
        // can support much higher values but these are safe and sane.
        private static int maxCash = 1000000000;

        private static int maxExperience = 8451341;
        private static int maxLevel = 69;
        private static int maxBackpackSlots = 1000;
        private static int maxBankSlots = 1000;
        private static int maxSkillPoints = 500;

        private static Color[] rarityColor =
        {
            Color.White,
            Color.FromArgb(0x3d, 0xe6, 0x0b),
            Color.FromArgb(0x2f, 0x78, 0xff),
            Color.FromArgb(0xB9, 0x40, 0xFF),
            Color.FromArgb(0xFF, 0xDC ,0x35),
            Color.FromArgb(0xff, 0x96, 0x00),
            Color.DarkOrange,
            Color.Cyan,
            Color.FromArgb(0x3d, 0xe6, 0x0b),
            Color.Red,
            Color.GhostWhite,
            Color.Yellow,
        };

        public static IFile File { get; set; } = new FileWrapper(new FileSystem());
        public static InputMode InputMode { get; set; } = InputMode.Standard;
        public static bool UseHexInAdvancedMode { get => useHexInAdvancedMode; set => useHexInAdvancedMode = value; }
        public static bool PartSelectorTracking { get => partSelectorTracking; set => partSelectorTracking = value; }
        public static bool ShowManufacturer { get => showManufacturer; set => showManufacturer = value; }
        public static bool UseColor { get => useColor; set => useColor = value; }
        public static bool ShowRarity { get => showRarity; set => showRarity = value; }
        public static bool ShowLevel { get => showLevel; set => showLevel = value; }
        public static string LastLockerFile { get => lastLockerFile; set => lastLockerFile = value; }
        public static Color BackgroundColor { get => backgroundColor; set => backgroundColor = value; }
        public static int MaxCash { get => maxCash; set => maxCash = value; }
        public static int MaxExperience { get => maxExperience; set => maxExperience = value; }
        public static int MaxLevel { get => maxLevel; set => maxLevel = value; }
        public static int MaxBackpackSlots { get => maxBackpackSlots; set => maxBackpackSlots = value; }
        public static int MaxBankSlots { get => maxBankSlots; set => maxBankSlots = value; }
        public static int MaxSkillPoints { get => maxSkillPoints; set => maxSkillPoints = value; }
        public static Color[] RarityColor { get => rarityColor; set => rarityColor = value; }

        public static void Save(string filename)
        {
            using (XmlTextWriter writer = new XmlTextWriter(filename, Encoding.UTF8))
            {
                writer.WriteStartDocument();
                writer.Formatting = Formatting.Indented;
                writer.WriteStartElement("Settings");

                writer.WriteElementString("InputMode", InputMode.ToString());
                writer.WriteElementString("UseHexInAdvancedMode", UseHexInAdvancedMode.ToString());
                writer.WriteElementString("PartSelectorTracking", PartSelectorTracking.ToString());
                writer.WriteElementString("ShowManufacturer", ShowManufacturer.ToString());
                writer.WriteElementString("UseColor", UseColor.ToString());
                writer.WriteElementString("ShowRarity", ShowRarity.ToString());
                writer.WriteElementString("ShowLevel", ShowLevel.ToString());
                writer.WriteElementString("lastLockerFile", GameData.OpenedLockerFilename());

                writer.WriteElementString("MaxCash", MaxCash.ToString());
                writer.WriteElementString("MaxLevel", MaxLevel.ToString());
                writer.WriteElementString("MaxExperience", MaxExperience.ToString());
                writer.WriteElementString("MaxInventorySlots", MaxBackpackSlots.ToString());
                writer.WriteElementString("MaxBankSlots", MaxBankSlots.ToString());
                writer.WriteElementString("MaxSkillPoints", MaxSkillPoints.ToString());

                writer.WriteElementString("RarityColor0", RarityColor[0].ToArgb().ToString("X"));
                writer.WriteElementString("RarityColor1", RarityColor[1].ToArgb().ToString("X"));
                writer.WriteElementString("RarityColor2", RarityColor[2].ToArgb().ToString("X"));
                writer.WriteElementString("RarityColor3", RarityColor[3].ToArgb().ToString("X"));
                writer.WriteElementString("RarityColor4", RarityColor[4].ToArgb().ToString("X"));
                writer.WriteElementString("RarityColor5", RarityColor[5].ToArgb().ToString("X"));
                writer.WriteElementString("RarityColor6", RarityColor[6].ToArgb().ToString("X"));
                writer.WriteElementString("RarityColor7", RarityColor[7].ToArgb().ToString("X"));
                writer.WriteElementString("RarityColor8", RarityColor[8].ToArgb().ToString("X"));
                writer.WriteElementString("RarityColor9", RarityColor[9].ToArgb().ToString("X"));
                writer.WriteElementString("RarityColor10", RarityColor[10].ToArgb().ToString("X"));
                writer.WriteElementString("RarityColor11", RarityColor[11].ToArgb().ToString("X"));

                writer.WriteEndElement();
                writer.WriteEndDocument();
            }
        }

        public static void Load(string filename)
        {
            if (!File.Exists(filename))
            {
                return;
            }

            using (XmlTextReader reader = new XmlTextReader(filename))
            {
                reader.XmlResolver = null;
                while (reader.Read())
                {
                    if (reader.NodeType != XmlNodeType.Element)
                    {
                        continue;
                    }

                    switch (reader.Name)
                    {
                        case "InputMode":
                            InputMode = GetInputMethod(reader.ReadElementContentAsString());
                            break;

                        case "UseHexInAdvancedMode": XmlReadBool(reader, ref useHexInAdvancedMode); break;
                        case "PartSelectorTracking": XmlReadBool(reader, ref partSelectorTracking); break;
                        case "ShowManufacturer": XmlReadBool(reader, ref showManufacturer); break;
                        case "ShowRarity": XmlReadBool(reader, ref showRarity); break;
                        case "ShowLevel": XmlReadBool(reader, ref showLevel); break;
                        case "UseColor": XmlReadBool(reader, ref useColor); break;
                        case "lastLockerFile":
                            {
                                GameData.OpenedLockerFilename(reader.ReadElementContentAsString());
                            }
                            break;

                        case "MaxCash": XmlReadInt(reader, ref maxCash); break;
                        case "MaxLevel": XmlReadInt(reader, ref maxLevel); break;
                        case "MaxExperience": XmlReadInt(reader, ref maxExperience); break;
                        case "MaxInventorySlots": XmlReadInt(reader, ref maxBackpackSlots); break;
                        case "MaxBankSlots": XmlReadInt(reader, ref maxBankSlots); break;
                        case "MaxSkillPoints": XmlReadInt(reader, ref maxSkillPoints); break;

                        case "RarityColor0":
                        case "RarityColor1":
                        case "RarityColor2":
                        case "RarityColor3":
                        case "RarityColor4":
                        case "RarityColor5":
                        case "RarityColor6":
                        case "RarityColor7":
                        case "RarityColor8":
                        case "RarityColor9":
                        case "RarityColor10":
                        case "RarityColor11":
                            try
                            {
                                int index = Parse.AsInt(reader.Name.After("RarityColor"));
                                string text = reader.ReadElementContentAsString();
                                if (uint.TryParse(text, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out uint colorval))
                                {
                                    rarityColor[index] = Color.FromArgb((int)colorval);
                                }
                            }
                            catch { }
                            break;
                    }
                }
            }
        }

        private static bool XmlReadBool(XmlTextReader reader, ref bool var)
        {
            if (bool.TryParse(reader.ReadElementContentAsString(), out bool value))
            {
                var = value;
                return true;
            }
            return false;
        }

        private static bool XmlReadInt(XmlTextReader reader, ref int var)
        {
            if (int.TryParse(reader.ReadElementContentAsString(), out int value))
            {
                var = value;
                return true;
            }
            return false;
        }

        private static InputMode GetInputMethod(string inputMode)
        {
            return string.Equals(inputMode, "Advanced", StringComparison.OrdinalIgnoreCase)
                ? InputMode.Advanced
                : InputMode.Standard;
        }
    }
}
