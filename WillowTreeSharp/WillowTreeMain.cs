using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using System.Windows.Forms;
using WillowTree.Controls;
using WillowTree.Inventory;
using WillowTree.Plugins;

namespace WillowTree
{
    public partial class WillowTreeMain : Form
    {
        private WillowSaveGame currentWsg;
        private readonly PluginComponentManager pluginManager;
        private Control selectedTabObject = null;
        private readonly IFile file;
        private readonly IDirectory directory;
        private readonly IGameData gameData;
        private readonly IGlobalSettings settings;
        private readonly AppThemes themes;

        public WillowTreeMain(
            IFile file,
            IDirectory directory,
            IGameData gameData,
            IGlobalSettings settings,
            PluginComponentManager pluginManager,
            AppThemes themes)
        {
            this.file = file;
            this.directory = directory;
            this.gameData = gameData;
            this.settings = settings;
            this.pluginManager = pluginManager;
            this.themes = themes;

            this.settings.Load(this.gameData.XmlPath + "options.xml");

            if (!this.directory.Exists(this.gameData.DataPath))
            {
                MessageBox.Show(
                    "Couldn't find the 'Data' folder! Please make sure that WillowTree# and its data folder are in the same directory.");
                Application.Exit();
                return;
            }

            if (!this.file.Exists($"{this.gameData.DataPath}default.xml"))
            {
                this.file.WriteAllText($"{this.gameData.DataPath}default.xml",
                    "<?xml version=\"1.0\" encoding=\"us-ascii\"?>\r\n<INI></INI>\r\n");
            }

            InitializeComponent();

            this.gameData.InitializeNameLookup();

            Save.Enabled = false;
            SaveAs.Enabled = false;
            SelectFormat.Enabled = false;

            CreatePluginAsTab("General", new ucGeneral());
            CreatePluginAsTab("Weapons", new ucGears());
            CreatePluginAsTab("Items", new ucGears());
            CreatePluginAsTab("Skills", new ucSkills());
            CreatePluginAsTab("Quest", new ucQuests());
            CreatePluginAsTab("Ammo Pools", new ucAmmo());
            CreatePluginAsTab("Echo Logs", new ucEchoes());
            CreatePluginAsTab("Bank", new ucGears());
            CreatePluginAsTab("Locker", new ucLocker());
            CreatePluginAsTab("Debug", new ucDebug());
            CreatePluginAsTab("About", new UcAbout());

            try
            {
                tabControl1.SelectTab("ucAbout");
            }
            catch
            {
            }

            SetUiTreeStyles(this.settings.UseColor);
        }

        public WillowSaveGame SaveData => currentWsg;

        public void CreatePluginAsTab(string tabTitle, Control control)
        {
            if (control is IPlugin plugin)
            {
                control.Text = tabTitle;
                control.BackColor = Color.Transparent;
                tabControl1.Controls.Add(control);
                pluginManager.InitializePlugin(plugin);
            }
        }

        private void AdvancedInputDecimal_Click(object sender, EventArgs e)
        {
            this.settings.UseHexInAdvancedMode = false;
            this.settings.InputMode = InputMode.Advanced;
        }

        private void AdvancedInputHexadecimal_Click(object sender, EventArgs e)
        {
            this.settings.UseHexInAdvancedMode = true;
            this.settings.InputMode = InputMode.Advanced;
        }

        private void colorizeListsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.settings.UseColor = !this.settings.UseColor;
            SetUiTreeStyles(this.settings.UseColor);
        }

        private void ConvertListForEditing<T>(InventoryList itmList, ref List<T> objs) where T : WillowSaveGame.Object
        {
            // Populate itmList with items created from the WillowSaveGame data lists
            itmList.ClearSilent();
            foreach (var obj in objs)
            {
                itmList.AddSilent(new InventoryEntry(itmList.invType, obj.Strings, obj.GetValues()));
            }

            itmList.OnListReload();

            objs = null;
        }

        private void ConvertListForEditing(InventoryList itmList, ref List<WillowSaveGame.BankEntry> itmBank)
        {
            // Populate itmList with items created from the WillowSaveGame data lists
            itmList.ClearSilent();
            for (int i = 0; i < itmBank.Count; i++)
            {
                List<int> itmBankValues = new List<int>()
                {
                    itmBank[i].Quantity, itmBank[i].Quality, itmBank[i].EquipedSlot, itmBank[i].Level, itmBank[i].Junk,
                    itmBank[i].Locked
                };

                // Store a reference to the parts list
                List<string> parts = itmBank[i].Strings;

                // Detach the parts list from the bank entry.
                itmBank[i].Strings = null;

                // Items have a different part order in the bank and in the backpack
                // Part                Index      Index
                //                   Inventory    Bank
                // Item Grade            0          1
                // Item Type             1          0
                // Body                  2          3
                // Left                  3          4
                // Right                 4          5
                // Material              5          6
                // Manufacturer          6          2
                // Prefix                7          7
                // Title                 8          8

                int itmType = itmBank[i].TypeId - 1;

                // Convert all items into the backpack part order.  Weapons use
                // the same format for both backpack and bank.

                if (itmType == InventoryType.Item)
                {
                    string temp = parts[1];
                    parts[1] = parts[0];
                    parts[0] = temp;
                    temp = parts[2];
                    parts[2] = parts[3];
                    parts[3] = parts[4];
                    parts[4] = parts[5];
                    parts[5] = parts[6];
                    parts[6] = temp;
                }

                // Create an inventory entry with the re-ordered parts list and add it
                itmList.AddSilent(new InventoryEntry((byte)(itmBank[i].TypeId - 1), parts, itmBankValues));
                //Item/Weapon in bank have their type increase by 1, we reduce TypeId by 1 to manipulate them like other list
            }

            itmList.OnListReload();

            // Release the WillowSaveGame bank data now that the data is converted
            // to the format that the WillowTree UI uses.  It gets recreated at save time.
            itmBank = null;
        }

        private void DoWindowTitle()
        {
            if (pluginManager.GetPlugin(typeof(ucGeneral)) is ucGeneral eGeneral)
            {
                eGeneral.DoWindowTitle();
            }
        }

        private void ExitWT_Click(object sender, EventArgs e)
        {
            this.settings.Save();
            Application.Exit();
        }

        private void IncreaseNavigationLayers_Click(object sender, EventArgs e)
        {
            if (tabControl1.TabPages[tabControl1.SelectedIndex] is IPlugin page)
            {
                pluginManager.OnPluginCommand(page,
                    new PluginCommandEventArgs(this, PluginCommand.IncreaseNavigationDepth));
            }
        }

        private void MenuItemPartSelectorTracking_Click(object sender, EventArgs e)
        {
            this.settings.PartSelectorTracking = !this.settings.PartSelectorTracking;
        }

        private void NextSort_Click(object sender, EventArgs e)
        {
            if (tabControl1.TabPages[tabControl1.SelectedIndex] is IPlugin page)
            {
                pluginManager.OnPluginCommand(page, new PluginCommandEventArgs(this, PluginCommand.ChangeSortMode));
            }
        }

        private void Open_Click(object sender, EventArgs e)
        {
            string fileName = (currentWsg != null) ? currentWsg.OpenedWsg : "";

            WTOpenFileDialog openDlg = new WTOpenFileDialog("sav", fileName);
            if (openDlg.ShowDialog() != DialogResult.OK) return;

            fileName = openDlg.FileName();

            pluginManager.OnGameLoading(new PluginEventArgs(this, fileName));
            Application.DoEvents();
            currentWsg = new WillowSaveGame
            {
                AutoRepair = true
            };
            currentWsg.LoadWsg(fileName);

            if (currentWsg.RequiredRepair)
            {
                DialogResult result = MessageBox.Show(
                    "Your savegame contains corrupted data so it cannot be loaded.  " +
                    "It is possible to discard the invalid data to repair your savegame " +
                    "so that it can be opened.  Repairing WILL CAUSE SOME DATA LOSS but " +
                    "should bring your savegame back to a working state.\r\n\r\nDo you want to " +
                    "repair the savegame?",
                    "Recoverable Corruption Detected",
                    MessageBoxButtons.YesNo, MessageBoxIcon.Exclamation);

                if (result == DialogResult.No)
                {
                    throw new FileFormatException("Savegame file is corrupt.");
                }
            }

            ConvertListForEditing(this.gameData.WeaponList, ref currentWsg.Weapons);
            ConvertListForEditing(this.gameData.ItemList, ref currentWsg.Items);
            ConvertListForEditing(this.gameData.BankList, ref currentWsg.Dlc.BankInventory);

            pluginManager.OnGameLoaded(new PluginEventArgs(this, fileName));

            Save.Enabled = true;
            SaveAs.Enabled = true;
            SelectFormat.Enabled = true;
        }

        private void optionsToolStripMenuItem_DropDownOpening(object sender, EventArgs e)
        {
            colorizeListsToolStripMenuItem.Checked = this.settings.UseColor;
            showRarityValueToolStripMenuItem.Checked = this.settings.ShowRarity;
            showEffectiveLevelsToolStripMenuItem.Checked = this.settings.ShowLevel;
            showManufacturerToolStripMenuItem.Checked = this.settings.ShowManufacturer;
            MenuItemPartSelectorTracking.Checked = this.settings.PartSelectorTracking;
        }

        private void PCFormat_Click(object sender, EventArgs e)
        {
            if (currentWsg.Platform == "PC")
            {
                return;
            }

            if (currentWsg.ContainsRawData && (currentWsg.EndianWsg != ByteOrder.LittleEndian) && !UIAction_RemoveRawData())
            {
                return;
            }

            currentWsg.Platform = "PC";
            currentWsg.EndianWsg = ByteOrder.LittleEndian;
            DoWindowTitle();
            currentWsg.OpenedWsg = "";
            Save.Enabled = false;
        }

        private void PS3Format_Click(object sender, EventArgs e)
        {
            if (currentWsg.Platform == "PS3")
            {
                return;
            }

            if (currentWsg.ContainsRawData && (currentWsg.EndianWsg != ByteOrder.BigEndian))
            {
                if (!UIAction_RemoveRawData())
                {
                    return;
                }
            }

            currentWsg.Platform = "PS3";
            currentWsg.EndianWsg = ByteOrder.BigEndian;
            DoWindowTitle();
            MessageBox.Show(
                "This save data will be stored in the PS3 format. Please note that you will require \r\nproper SFO, PNG, and PFD files to be transfered back to the \r\nPS3. These can be acquired from another Borderlands save \r\nfor the same profile.");
            currentWsg.OpenedWsg = "";
            Save.Enabled = false;
        }

        private void RepopulateListForSaving<T>(InventoryList itmList, ref List<T> objs)
            where T : WillowSaveGame.Object, new()
        {
            objs = new List<T>();
            foreach (InventoryEntry item in itmList.Items.Values)
            {
                var obj = new T
                {
                    Strings = item.Parts
                };
                obj.SetValues(item.GetValues());
                objs.Add(obj);
            }

            // itm may represent: item, weapon, bank
            // Note that the string lists that contain the parts are shared
            // between itmList and itmStrings after this method runs, so
            // cross-contamination can occur if they are modified.  They should
            // only be held in this state long enough to save, which does not modify
            // values, then itmStrings/itmValues should be released by setting them
            // to null since they will not be used again until the next save when they
            // will have to be recreated.
        }

        private void RepopulateListForSaving(InventoryList itmList, ref List<WillowSaveGame.BankEntry> itmBank)
        {
            itmBank = new List<WillowSaveGame.BankEntry>();
            WillowSaveGame.BankEntry itm;

            // Build the lists of string and value data needed by WillowSaveGame to store the
            // inventory from the data that is in itmList.
            foreach (InventoryEntry item in itmList.Items.Values)
            {
                itm = new WillowSaveGame.BankEntry();

                if (item.Type == InventoryType.Item)
                {
                    // Items must have their parts reordered because they are different in the bank.
                    List<string> oldParts = item.Parts;
                    itm.Strings = new List<string>()
                    {
                        oldParts[1], oldParts[0], oldParts[6], oldParts[2], oldParts[3],
                        oldParts[4], oldParts[5], oldParts[7], oldParts[8]
                    };
                }
                else
                {
                    itm.Strings = new List<string>(item.Parts);
                }

                //Item/Weapon in bank have their type increase by 1, we  increase TypeId by 1 to restore them to their natural value
                itm.TypeId = (byte)(item.Type + 1);

                List<int> values = item.GetValues();

                //if (Convert.ToBoolean(values[4])) //TODO RSM UsesBigLevel
                itm.Quantity = values[0]; //Quantity;
                itm.Quality = (short)values[1]; //QualityIndex;
                itm.EquipedSlot = (byte)values[2]; //Equipped;
                itm.Level = (short)values[3]; //LevelIndex;
                itm.Junk = (byte)values[4];
                itm.Locked = (byte)values[5];

                itmBank.Add(itm);
            }

            // The string lists stored in the bank entries are not not shared
            // after this method runs like they are for the backpack inventory.
            // Still they should be released after saving since this is duplicate
            // data that will be rebuilt on the next save attempt.
        }

        private void Save_Click(object sender, EventArgs e)
        {
            try
            {
                this.file.Copy(currentWsg.OpenedWsg, $"{currentWsg.OpenedWsg}.bak0", true);
                if (this.file.Exists($"{currentWsg.OpenedWsg}.bak10"))
                {
                    this.file.Delete($"{currentWsg.OpenedWsg}.bak10");
                }

                for (int i = 9; i >= 0; i--)
                {
                    if (this.file.Exists($"{currentWsg.OpenedWsg}.bak{i}"))
                    {
                        this.file.Move($"{currentWsg.OpenedWsg}.bak{i}", $"{currentWsg.OpenedWsg}.bak{(i + 1)}");
                    }
                }
            }
            catch
            {
            }

            SaveToFile(currentWsg.OpenedWsg);
            MessageBox.Show($"Saved WSG to: {currentWsg.OpenedWsg}");
        }

        private void SaveAs_Click(object sender, EventArgs e)
        {
            WTSaveFileDialog tempSave = new WTSaveFileDialog("sav", currentWsg.OpenedWsg);

            if (tempSave.ShowDialog() != DialogResult.OK) return;

            SaveToFile(tempSave.FileName());
            MessageBox.Show($"Saved WSG to: {currentWsg.OpenedWsg}");
            Save.Enabled = true;
        }

        private void SaveOptions_Click(object sender, EventArgs e)
        {
            this.settings.Save();
        }

        private void SaveToFile(string filename)
        {
            pluginManager.OnGameSaving(new PluginEventArgs(this, filename));
            Application.DoEvents();

            // Convert the weapons and items data from WeaponList/ItemList into
            // the format used by WillowSaveGame.
            RepopulateListForSaving(this.gameData.WeaponList, ref currentWsg.Weapons);
            RepopulateListForSaving(this.gameData.ItemList, ref currentWsg.Items);
            RepopulateListForSaving(this.gameData.BankList, ref currentWsg.Dlc.BankInventory);
            currentWsg.SaveWsg(filename);
            currentWsg.OpenedWsg = filename;

            // Release the WillowSaveGame inventory data now that saving is complete.  The
            // same data is still contained in db.WeaponList, db.ItemList, and db.BankList
            // in the format used by the WillowTree UI.
            currentWsg.Weapons = null;
            currentWsg.Items = null;
            currentWsg.Dlc.BankInventory = null;

            pluginManager.OnGameSaved(new PluginEventArgs(this, currentWsg.OpenedWsg));
        }

        private void SetUiTreeStyles(bool useColor)
        {
            var theme = useColor ? this.themes.treeViewTheme1 : null;

            this.gameData.ItemList.OnTreeThemeChanged(theme);
            this.gameData.WeaponList.OnTreeThemeChanged(theme);
            this.gameData.BankList.OnTreeThemeChanged(theme);
            this.gameData.LockerList.OnTreeThemeChanged(theme);
        }

        private void showEffectiveLevelsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.settings.ShowLevel = !this.settings.ShowLevel;
            UpdateNames();
        }

        private void showManufacturerToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.settings.ShowManufacturer = !this.settings.ShowManufacturer;
            UpdateNames();
        }

        private void showRarityValueToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.settings.ShowRarity = !this.settings.ShowRarity;
            UpdateNames();
        }

        private void StandardInput_Click(object sender, EventArgs e)
        {
            this.settings.InputMode = InputMode.Standard;
        }

        private void tabControl1_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (selectedTabObject is IPlugin tabObject)
            {
                pluginManager.OnPluginUnselected(tabObject, new PluginEventArgs(this, null));
            }

            int selected = tabControl1.SelectedIndex;
            if (selected >= 0)
            {
                selectedTabObject = tabControl1.Controls[selected];
                if (selectedTabObject is IPlugin plugin)
                {
                    pluginManager.OnPluginSelected(plugin, new PluginEventArgs(this, null));
                }
            }
            else
            {
                selectedTabObject = null;
            }
        }

        /// <summary>
        /// Checks to make sure a savegame contains no raw data.  If it does it asks if it is ok
        /// to remove it and performs removal of the raw data.
        /// Return value is true if the result is a savegame with no raw data or false if the
        /// savegame still contains raw data.
        /// </summary>
        /// <returns></returns>
        private bool UIAction_RemoveRawData()
        {
            DialogResult result = MessageBox.Show(
                "This savegame contains some unexpected or possibly corrupt DLC data that WillowTree# does not know how to parse, so it cannot be rewritten in a different machine byte order.  The extra data can be discarded to allow you to convert the savegame, but this will cause DLC data loss if the unknown DLC data is actually used by Borderlands.  It is typical for Steam PC savegames to have a data section like this and it must be removed to convert to Xbox 360 or PS3 format.  The data probably serves no gameplay purpose because it doesn't exist in the PC DVD version.\r\n\r\nDo you want to discard the raw data to allow the conversion?",
                "Unexpected Raw Data", MessageBoxButtons.YesNo);
            if (result == DialogResult.No)
            {
                return false;
            }

            currentWsg.DiscardRawData();
            return true;
        }

        private void UpdateNames()
        {
            this.gameData.WeaponList.OnNameFormatChanged();
            this.gameData.ItemList.OnNameFormatChanged();
            this.gameData.BankList.OnNameFormatChanged();
            this.gameData.LockerList.OnNameFormatChanged();
        }

        private void WillowTreeMain_FormClosing(object sender, EventArgs e)
        {
            this.settings.Save();
        }

        private void XBoxFormat_Click(object sender, EventArgs e)
        {
            if (currentWsg.Platform == "X360")
            {
                return;
            }

            if (currentWsg.ContainsRawData && (currentWsg.EndianWsg != ByteOrder.BigEndian))
            {
                if (!UIAction_RemoveRawData())
                {
                    return;
                }
            }

            if (currentWsg.DeviceId == null)
            {
                XBoxIDDialog dlgXBoxId = new XBoxIDDialog();
                if (dlgXBoxId.ShowDialog() != DialogResult.OK)
                {
                    return;
                }

                currentWsg.ProfileId = dlgXBoxId.ID.ProfileID;
                int deviceIdLength = dlgXBoxId.ID.DeviceID.Count();
                currentWsg.DeviceId = new byte[deviceIdLength];
                Array.Copy(dlgXBoxId.ID.DeviceID, currentWsg.DeviceId, deviceIdLength);
            }

            currentWsg.Platform = "X360";
            currentWsg.EndianWsg = ByteOrder.BigEndian;
            DoWindowTitle();
            currentWsg.OpenedWsg = "";
            Save.Enabled = false;
        }

        private void XBoxJPFormat_Click(object sender, EventArgs e)
        {
            if (currentWsg.Platform == "X360JP")
            {
                return;
            }

            if (currentWsg.ContainsRawData && (currentWsg.EndianWsg != ByteOrder.BigEndian))
            {
                if (!UIAction_RemoveRawData())
                {
                    return;
                }
            }

            if (currentWsg.DeviceId == null)
            {
                XBoxIDDialog dlgXBoxId = new XBoxIDDialog();
                if (dlgXBoxId.ShowDialog() != DialogResult.OK)
                {
                    return;
                }

                currentWsg.ProfileId = dlgXBoxId.ID.ProfileID;
                int deviceIdLength = dlgXBoxId.ID.DeviceID.Count();
                currentWsg.DeviceId = new byte[deviceIdLength];
                Array.Copy(dlgXBoxId.ID.DeviceID, currentWsg.DeviceId, deviceIdLength);
            }

            currentWsg.Platform = "X360JP";
            currentWsg.EndianWsg = ByteOrder.BigEndian;
            DoWindowTitle();
            currentWsg.OpenedWsg = "";
            Save.Enabled = false;
        }
    }
}
