using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using System.Xml;
using WillowTree.Services.DataAccess;

namespace WillowTree.Inventory
{
    public class InventoryEntry
    {
        // Every weapon or item must have a unique key.  The key is used to
        // locate the it in the Weapons, Items, or Locker dictionaries where
        // there may be other items that have the same name but different stats.
        public string Key;

        // Base Data.  These values are needed to define an item.  All of them
        // must be saved to store the item.
        public byte Type;

        public List<string> Parts;
        public int Quantity;
        public int QualityIndex;
        public int EquippedSlot;
        public int LevelIndex;
        public int IsJunk;
        public int IsLocked;

        // User data.  These are user defined fields that are saved in the
        // locker for user notes.  They are not necessary to define an item
        // and are saved only so that the user can keep notes.
        // TODO: The rating has never worked since the conversion away from DotNetBar
        // since there's no control to display star ratings in Windows Forms.
        // I don't know if the description actually saves as it should or not.
        // It may not work either.  It might be better to just scrap these.
        public string Rating;

        public string Description;

        // Derived data - Stored.  These values can be calculated from base data if
        // if they are not saved, but they are saved to improve the load speed of
        // the locker when it gets large. It takes a long time to do calculations on
        // all the items otherwise.
        public string Name;

        public int Rarity;
        public int EffectiveLevel;
        public string Category;
        public string[] NameParts;

        // Derived data - Unstored.  These values are calculated from the base data and
        // do not get stored
        public Color Color;

        public bool UsesBigLevel;

        public InventoryEntry()
        { }

        public InventoryEntry(XmlNode node)
        {
            this.Key = node.GetElement("Key", "");
            this.Name = node.GetElement("Name", "");
            this.Rating = node.GetElement("Rating", "");
            this.Description = node.GetElement("Description", "");
            this.Parts = new List<string>();

            int i;
            for (i = 0; i < 14; i++)
            {
                string part = node.GetElement("Part" + (i + 1), "");
                if (part == "")
                {
                    break;
                }

                this.Parts.Add(part);
            }

            if (i == 14)
            {
                this.Type = InventoryType.Weapon;
            }
            else if (i == 9)
            {
                this.Type = InventoryType.Item;
            }
            else
            {
                this.Type = InventoryType.Unknown;
            }

            this.UsesBigLevel = ItemgradePartUsesBigLevel(this.Parts[0]);

            this.LevelIndex = node.GetElementAsInt("Level");
            this.QualityIndex = node.GetElementAsInt("Quality");
            this.Quantity = node.GetElementAsInt("RemAmmo_Quantity");
            this.IsJunk = node.GetElementAsInt("IsJunk", 0);
            this.IsLocked = node.GetElementAsInt("IsLocked", 0);
            this.EquippedSlot = 0;

            try
            {
                // If any of these values is missing they all need to be
                // recalculated.  An exception will be thrown if one does
                // not exist or the format is incorrect.
                this.Rarity = node.GetElementAsInt("Rarity");
                this.EffectiveLevel = node.GetElementAsInt("EffectiveLevel");
                this.Category = node.GetElement("Category");
                if (this.Category == "")
                {
                    this.Category = "none";
                }

                List<string> nameparts = new List<string>();
                string namepart;
                for (i = 0; true; i++)
                {
                    XmlNode namepartnode = node["NamePart" + (i + 1)];
                    if (namepartnode == null)
                    {
                        break;
                    }

                    namepart = namepartnode.InnerText;
                    if (namepart != null)
                    {
                        nameparts.Add(namepart);
                    }
                    else
                    {
                        nameparts.Add(string.Empty);
                    }
                }
                this.NameParts = nameparts.ToArray();
                if (this.NameParts.Length < 4)
                {
                    throw new FormatException();
                }

                if (this.Type == InventoryType.Weapon)
                {
                    this.Color = RarityToColorWeapon(this.Rarity);
                }
                else
                {
                    this.Color = RarityToColorItem(this.Rarity);
                }
            }
            catch
            {
                if (this.Type == InventoryType.Weapon)
                {
                    this.RecalculateDataWeapon();
                }
                else
                {
                    this.RecalculateDataItem();
                }

                this.BuildName();
            }
        }

        public static bool ItemgradePartUsesBigLevel(string itemgradePart)
        {
            return itemgradePart == "gd_itemgrades.Weapons.ItemGrade_Weapon_Scorpio";
        }

        public InventoryEntry(byte inType, List<string> inParts, List<int> inValues)
        {
            // This makes a shallow copy of elements.
            // Parts and Values still link to the input structures
            this.Type = inType;
            this.Parts = inParts;

            ConvertValues(
                inValues,
                inParts[0],
                out this.UsesBigLevel,
                out this.Quantity,
                out this.QualityIndex,
                out this.EquippedSlot,
                out this.LevelIndex,
                out this.IsJunk,
                out this.IsLocked);

            this.Rating = "";
            this.Description = "";

            if (this.Type == InventoryType.Weapon)
            {
                this.RecalculateDataWeapon();
            }
            else
            {
                this.RecalculateDataItem();
            }

            this.BuildName();
        }

        public InventoryEntry(InventoryEntry copyFrom)
        {   // This constructor makes a deep copy of all elements.
            // Parts are new copies of the input structures
            this.Key = copyFrom.Key;
            this.Type = copyFrom.Type;
            this.Parts = new List<string>(copyFrom.Parts);

            this.Quantity = copyFrom.Quantity;
            this.QualityIndex = copyFrom.QualityIndex;
            this.EquippedSlot = copyFrom.EquippedSlot;
            this.LevelIndex = copyFrom.LevelIndex;
            this.IsJunk = copyFrom.IsJunk;
            this.IsLocked = copyFrom.IsLocked;

            this.Rating = copyFrom.Rating;
            this.Description = copyFrom.Description;

            this.Name = copyFrom.Name;
            this.Rarity = copyFrom.Rarity;
            this.EffectiveLevel = copyFrom.EffectiveLevel;
            this.Category = copyFrom.Category;
            this.NameParts = (string[])copyFrom.NameParts.Clone();

            this.Color = copyFrom.Color;
            this.UsesBigLevel = copyFrom.UsesBigLevel;
        }

        /// <summary>
        /// Update the name of the gear
        /// </summary>
        public void BuildName()
        {
            // Build a new display name for the item based on the user preferences
            // from all the previously calculated name parts.
            // This is the name the user will see in the weapon or item tree.
            // The name parts in the input string list are:
            //      Manufacturer string
            //      Model string
            //      Title prefix string
            //      Title string
            //      Rarity string
            //      Level string
            System.Text.StringBuilder sb = new System.Text.StringBuilder();

            bool showManufacturer = GlobalSettings.ShowManufacturer;
            bool showRarity = GlobalSettings.ShowRarity;
            bool showLevel = GlobalSettings.ShowLevel;
            bool nameIsEmpty = true;

            int namePartCount = this.NameParts.Length;
            for (int i = 0; i < namePartCount; i++)
            {
                string namePart = this.NameParts[i];
                if (namePart.Length == 0)
                {
                    continue;
                }
                else if ((i == 0) && (!showManufacturer))
                {
                    continue;
                }
                else if ((i == 4) && (!showRarity))
                {
                    continue;
                }
                else if ((i == 5) && (!showLevel))
                {
                    continue;
                }
                else if (nameIsEmpty)
                {
                    // If the name is still empty on the fourth part then it has no title, prefix,
                    // manufacturer, or model number in its parts so there's no way to name it.
                    // Choose a default name and don't show either the level or quality because
                    // they make little sense on an otherwise blank or invalid item.
                    if (i >= 4)
                    {
                        break;
                    }

                    nameIsEmpty = false;
                }
                else
                {
                    sb.Append(' ');
                }

                sb.Append(namePart);
            }
            if (nameIsEmpty)
            {
                if (this.Type == InventoryType.Weapon)
                {
                    this.Name = "(New Weapon)";
                }
                else if (this.Type == InventoryType.Item)
                {
                    this.Name = "(New Item)";
                }
                else
                {
                    this.Name = "(Unknown)";
                }
            }
            else
            {
                this.Name = sb.ToString();
            }
        }

        public void RecalculateDataItem()
        {   // There is a variety of data used for various methods of sorting
            // and naming items that is determined from the parts and values.
            // This function is used to fetch them all.

            string BodyText = GameData.GetName(this.Parts[5]); // Body text
            string MaterialText = GameData.GetName(this.Parts[2]); // Material text

            int Model = Parse.AsInt(GameData.GetPartAttribute(this.Parts[4], "PartNumberAddend"), 0);        // Number from stock
            Model += Parse.AsInt(GameData.GetPartAttribute(this.Parts[3], "PartNumberAddend"), 0);           // Number from mag

            this.Rarity = Parse.AsInt(GameData.GetPartAttribute(this.Parts[1], "BaseRarity"), 0);
            for (int i = 2; i < 9; i++)
            {
                int partrarity = GameData.GetPartRarity(this.Parts[i]);

                this.Rarity += partrarity;
            }
            this.Color = RarityToColorItem(this.Rarity);

            if ((GameData.GetPartRarity(this.Parts[4]) != 0) && (GameData.GetPartRarity(this.Parts[3]) != 0))
            {
                Model = Model * 10;
            }

            string ModelText = (Model != 0 ? Model.ToString() : "");

            string MfgText = GameData.GetPartAttribute(this.Parts[6], "Manufacturer"); // Mfg Name
            if (MfgText == "")
            {
                MfgText = GameData.GetPartAttribute(this.Parts[1], "NoManufacturerName");
            }

            string ModelName = BodyText + ModelText + MaterialText;

            string Prefix = GameData.GetName(this.Parts[7]);
            string Title = GameData.GetName(this.Parts[8]);

            if ((Title == "") && (Prefix == ""))
            {
                Title = GameData.GetPartAttribute(this.Parts[1], "ItemName");
                if (this.Name == "")
                {
                    this.Name = "Unknown Item";
                }
            }

            this.Category = GameData.GetPartAttribute(this.Parts[1], "Presentation");
            if (this.Category == "")
            {
                this.Category = "none";
            }

            this.EffectiveLevel = GameData.GetEffectiveLevelItem(this.Parts.ToArray(), this.QualityIndex, this.LevelIndex);

            this.NameParts = new string[]
                    {
                        MfgText,
                        ModelName,
                        Prefix,
                        Title,
                        "(R" + this.Rarity + ")",
                        "(L" + this.EffectiveLevel + ")"
                    };
        }

        public void RecalculateDataWeapon()
        {   // There is a variety of data used for various methods of sorting
            // and naming items that is determined from the parts and values.
            // This function is used to fetch them all.

            string BodyText = GameData.GetName(this.Parts[3]); // Body text
            string MaterialText = GameData.GetName(this.Parts[11]); // Material text

            int Model = Parse.AsInt(GameData.GetPartAttribute(this.Parts[8], "PartNumberAddend"), 0);        // Number from stock
            Model += Parse.AsInt(GameData.GetPartAttribute(this.Parts[5], "PartNumberAddend"), 0);           // Number from mag

            this.Rarity = Parse.AsInt(GameData.GetPartAttribute(this.Parts[2], "BaseRarity"), 0);
            for (int i = 3; i < 14; i++)
            {
                int partrarity = GameData.GetPartRarity(this.Parts[i]);
                // There are several parts that dont use the part rarity that I found
                // in the data.  This attempts to detect them and deal with them.
                if ((partrarity == 50) && (i < 10) && (!this.Parts[i].StartsWith("dlc3")))
                {
                    partrarity = 5;
                }

                this.Rarity += partrarity;
            }
            this.Color = RarityToColorWeapon(this.Rarity);

            if ((GameData.GetPartRarity(this.Parts[8]) != 0) && (GameData.GetPartRarity(this.Parts[5]) != 0))
            {
                Model = Model * 10;
            }

            string ModelText = (Model != 0 ? Model.ToString() : "");

            string MfgText = GameData.GetName(this.Parts[1]); // Mfg Name
            string ModelName = BodyText + ModelText + MaterialText;
            string Prefix = GameData.GetName(this.Parts[12]);
            string Title = GameData.GetName(this.Parts[13]);

            this.Category = GameData.GetPartAttribute(this.Parts[2], "Presentation");
            if (this.Category == "")
            {
                this.Category = "none";
            }

            this.EffectiveLevel = GameData.GetEffectiveLevelWeapon(this.Parts.ToArray(), this.QualityIndex, this.LevelIndex);
            // If the order or format of the level string changes, be sure to change it
            // in EditLevelAllWeapons_Click as well.
            this.NameParts = new string[]
                    {
                        MfgText,
                        ModelName,
                        Prefix,
                        Title,
                        "(R" + this.Rarity + ")",
                        "(L" + this.EffectiveLevel + ")"
                    };
        }

        public int GetPartCount()
        {
            int partcount;
            if (this.Type == InventoryType.Weapon)
            {
                partcount = 14;
            }
            else if (this.Type == InventoryType.Item)
            {
                partcount = 9;
            }
            else if ((this.Parts.Count == 10) && (this.Parts[9] == ""))
            {
                partcount = 9;
            }
            else if ((this.Parts.Count == 15) && (this.Parts[14] == ""))
            {
                partcount = 14;
            }
            else
            {
                throw new InvalidDataException("Entry type is invalid in GetPartCount.  name = " + this.Name + ", type = " + this.Type);
            }

            return partcount;
        }

        public static void ConvertValues(List<int> values, string itemgradePart, out bool usesBigLevel, out int quantity, out int qualityIndex, out int equippedSlot, out int levelIndex, out int isJunk, out int isLocked)
        {
            usesBigLevel = ItemgradePartUsesBigLevel(itemgradePart);
            if (usesBigLevel)
            {
                qualityIndex = 0;
                uint val = (ushort)(short)values[1];
                levelIndex = (int)((ushort)(short)values[3] * (uint)65536 + (ushort)(short)values[1]);
            }
            else
            {
                qualityIndex = values[1];
                levelIndex = values[3];
            }

            quantity = values[0];
            equippedSlot = values[2];

            if (values.Count > 4)
            {
                isJunk = values[4];
                isLocked = values[5];
            }
            else
            {
                isJunk = 0;
                isLocked = 0;
            }
        }

        public static List<int> CalculateValues(int quantity, int qualityIndex, int equippedSlot, int levelIndex, int isJunk, int isLocked, string itemgradePart)
        {
            bool usesBigLevel = ItemgradePartUsesBigLevel(itemgradePart);
            return CalculateValues(quantity, qualityIndex, equippedSlot, levelIndex, usesBigLevel, isJunk, isLocked);
        }

        public static List<int> CalculateValues(int quantity, int qualityIndex, int equippedSlot, int levelIndex, bool usesBigLevel, int isJunk, int isLocked)
        {
            if (usesBigLevel)
            {
                return new List<int>() {
                    quantity,
                    (short)((uint)levelIndex % 65536),
                    equippedSlot,
                    (short)((uint)levelIndex / 65536),
                    isJunk,
                    isLocked
                };
            }
            else
            {
                return new List<int>() {
                    quantity,
                    qualityIndex,
                    equippedSlot,
                    levelIndex,
                    isJunk,
                    isLocked
                };
            }
        }

        public List<int> GetValues()
        {
            return CalculateValues(
                this.Quantity,
                this.QualityIndex,
                this.EquippedSlot,
                this.LevelIndex,
                this.UsesBigLevel,
                this.IsJunk,
                this.IsLocked);
        }

        public static InventoryEntry ImportFromText(string text, byte inType)
        {
            List<string> InOutParts = new List<string>();
            InOutParts.AddRange(text.Split(new string[] { "\r\n" }, StringSplitOptions.None));

            // Create new lists
            List<string> parts = new List<string>();
            List<int> values = new List<int>();

            try
            {
                int tempInt;
                int Progress;
                for (Progress = 0; Progress < InOutParts.Count; Progress++)
                {
                    if (int.TryParse(InOutParts[Progress], out tempInt))
                    {
                        break;
                    }

                    parts.Add(InOutParts[Progress]);
                }

                if (Progress != 9 && Progress != 14)
                {
                    throw new FormatException();
                }

                byte t = Progress == 9 ? InventoryType.Item : InventoryType.Weapon;

                if (t != inType)
                {
                    MessageBox.Show(inType == InventoryType.Item
                        ? "You are try to import weapon into item's backpack!"
                        : "You are try to import item into weapon's backpack!");
                    return null;
                }

                if (InOutParts.Count == Progress + WillowSaveGame.ExportValuesCount)
                {
                    for (int i = 0; i < WillowSaveGame.ExportValuesCount; i++)
                    {
                        values.Add(Parse.AsInt(InOutParts[Progress + i]));
                    }

                    if (values.Count > 4)
                    {
                        values[4] = (values[4] != 0 && values[4] != 1) ? 0 : values[4];
                        values[5] = (values[5] != 0 && values[5] != 1) ? 0 : values[5];
                    }
                }
                else if (InOutParts.Count < Progress + 4)
                {
                    throw new FormatException();
                }
                else
                {
                    string message = "Item is in old format, do you want to convert it to the new format?";
                    string caption = "Ignoring imported item";
                    MessageBoxButtons buttons = MessageBoxButtons.YesNo;
                    DialogResult result;

                    // Displays the MessageBox.
                    result = MessageBox.Show(message, caption, buttons);
                    if (result == DialogResult.Yes)
                    {
                        for (int i = 0; i < 4; i++)
                        {
                            values.Add(Parse.AsInt(InOutParts[Progress + i]));
                        }

                        values.Add(0);
                        values.Add(0);
                    }
                    else
                    {
                        return null;
                    }
                }

                values[2] = 0;  // set equipped slot to 0
            }
            catch (FormatException)
            {
                MessageBox.Show("Imported data is invalid.  Not inserted.");
                return null;
            }

            return new InventoryEntry(inType, parts, values);
        }

        // TODO: This doesn't indent properly in most cases.  It should have the
        // indentation sent as a parameter so it is not fixed.
        public string ToXmlText()
        {
            StringWriter sw = new StringWriter();
            XmlTextWriter writer = new XmlTextWriter(sw)
            {
                Formatting = Formatting.Indented,
                Indentation = 2
            };
            writer.WriteElementString("Key", this.Key);
            writer.WriteElementString("Name", this.Name);
            writer.WriteElementString("Type", this.Type.ToString());
            writer.WriteElementString("Rating", this.Rating);
            writer.WriteElementString("Description", this.Description);

            int partcount = this.GetPartCount();
            for (int i = 0; i < partcount; i++)
            {
                writer.WriteElementString("Part" + (i + 1), this.Parts[i]);
            }

            writer.WriteElementString("RemAmmo_Quantity", this.Quantity.ToString());
            writer.WriteElementString("Quality", this.QualityIndex.ToString());
            writer.WriteElementString("Level", this.LevelIndex.ToString());
            writer.WriteElementString("IsJunk", this.IsJunk.ToString());
            writer.WriteElementString("IsLocked", this.IsLocked.ToString());

            writer.WriteElementString("Rarity", this.Rarity.ToString());
            writer.WriteElementString("EffectiveLevel", this.EffectiveLevel.ToString());
            writer.WriteElementString("Category", this.Category);

            int namepartcount = this.NameParts.Length;
            for (int i = 0; i < namepartcount; i++)
            {
                writer.WriteElementString("NamePart" + (i + 1), this.NameParts[i]);
            }

            writer.Close();
            return sw.ToString();
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

    }
}
