using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.IO.Abstractions;
using System.Windows.Forms;
using WillowTree.Controls;
using WillowTree.Inventory;
using WillowTree.Plugins;
using WillowTree.Services.DataAccess;
using WillowTreeSharp.Domain;

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
        private readonly IInventoryData inventoryData;

        public WillowTreeMain(
            IFile file,
            IDirectory directory,
            IGameData gameData,
            IInventoryData inventoryData,
            IGlobalSettings settings,
            IXmlCache xmlCache,
            IMessageBox messageBox,
            PluginComponentManager pluginManager,
            AppThemes themes)
        {
            this.file = file;
            this.directory = directory;
            this.gameData = gameData;
            this.inventoryData = inventoryData;
            this.settings = settings;
            this.pluginManager = pluginManager;
            this.themes = themes;
            this.MessageBox = messageBox;

            this.settings.Load(Path.Combine(this.gameData.XmlPath, "options.xml"));

            if (!this.directory.Exists(this.gameData.DataPath))
            {
                this.MessageBox.Show(
                    "Couldn't find the 'Data' folder! Please make sure that WillowTree# and its data folder are in the same directory.");
                Application.Exit();
                return;
            }

            var filePath = Path.Combine(this.gameData.DataPath, "default.xml");
            if (!this.file.Exists(filePath))
            {
                this.file.WriteAllText(filePath, "<?xml version=\"1.0\" encoding=\"us-ascii\"?>\r\n<INI></INI>\r\n");
            }

            this.InitializeComponent();

            this.Save.Enabled = false;
            this.SaveAs.Enabled = false;
            this.SelectFormat.Enabled = false;

            this.CreatePluginAsTab("General", new ucGeneral(this.gameData, this.settings));
            this.CreatePluginAsTab("Weapons", new ucGears(this.gameData, this.inventoryData, this.settings, xmlCache, this.file));
            this.CreatePluginAsTab("Items", new ucGears(this.gameData, this.inventoryData, this.settings, xmlCache, this.file));
            this.CreatePluginAsTab("Skills", new UcSkills(this.gameData));
            this.CreatePluginAsTab("Quest", new UcQuests());
            this.CreatePluginAsTab("Ammo Pools", new UcAmmo());
            this.CreatePluginAsTab("Echo Logs", new ucEchoes());
            this.CreatePluginAsTab("Bank", new ucGears(this.gameData, this.inventoryData, this.settings, xmlCache, this.file));
            this.CreatePluginAsTab("Locker", new ucLocker());
            this.CreatePluginAsTab("About", new UcAbout());

            try
            {
                this.tabControl1.SelectTab("ucAbout");
            }
            catch
            {
            }

            this.SetUiTreeStyles(this.settings.UseColor);
        }

        public WillowSaveGame SaveData => this.currentWsg;

        public IMessageBox MessageBox { get; }

        public void CreatePluginAsTab(string tabTitle, Control control)
        {
            if (control is IPlugin plugin)
            {
                control.Text = tabTitle;
                control.BackColor = Color.Transparent;
                this.tabControl1.Controls.Add(control);
                this.pluginManager.InitializePlugin(plugin);
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
            this.SetUiTreeStyles(this.settings.UseColor);
        }

        private void ConvertListForEditing<T>(InventoryList itmList, ref List<T> objs) where T : WillowObject
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

        private void ConvertListForEditing(InventoryList itmList, ref List<BankEntry> itmBank)
        {
            // Populate itmList with items created from the WillowSaveGame data lists
            itmList.ClearSilent();
            for (int i = 0; i < itmBank.Count; i++)
            {
                List<int> itmBankValues = new List<int>()
                {
                    itmBank[i].Quantity, itmBank[i].Quality, itmBank[i].EquippedSlot, itmBank[i].Level, itmBank[i].Junk,
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
            if (this.pluginManager.GetPlugin(typeof(ucGeneral)) is ucGeneral eGeneral)
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
            if (this.tabControl1.TabPages[this.tabControl1.SelectedIndex] is IPlugin page)
            {
                this.pluginManager.OnPluginCommand(page,
                    new PluginCommandEventArgs(this, PluginCommand.IncreaseNavigationDepth));
            }
        }

        private void MenuItemPartSelectorTracking_Click(object sender, EventArgs e)
        {
            this.settings.PartSelectorTracking = !this.settings.PartSelectorTracking;
        }

        private void NextSort_Click(object sender, EventArgs e)
        {
            if (this.tabControl1.TabPages[this.tabControl1.SelectedIndex] is IPlugin page)
            {
                this.pluginManager.OnPluginCommand(page, new PluginCommandEventArgs(this, PluginCommand.ChangeSortMode));
            }
        }

        private void Open_Click(object sender, EventArgs e)
        {
            string fileName = (this.currentWsg != null) ? this.currentWsg.OpenedWsg : "";

            WTOpenFileDialog openDlg = new WTOpenFileDialog("sav", fileName);
            if (openDlg.ShowDialog() != DialogResult.OK)
            {
                return;
            }

            fileName = openDlg.FileName();

            this.pluginManager.OnGameLoading(new PluginEventArgs(this, fileName));
            Application.DoEvents();
            this.currentWsg = WillowSaveGameSerializer.ReadFile(fileName, true);

            if (this.currentWsg.RequiredRepair)
            {
                DialogResult result = this.MessageBox.Show(
                    "Your savegame contains corrupted data so it cannot be loaded.  It is possible to discard the invalid data to repair your savegame so that it can be opened.  Repairing WILL CAUSE SOME DATA LOSS but should bring your savegame back to a working state.\r\n\r\nDo you want to repair the savegame?",
                    "Recoverable Corruption Detected",
                    MessageBoxButtons.YesNo, MessageBoxIcon.Exclamation);

                if (result == DialogResult.No)
                {
                    throw new FileFormatException("Savegame file is corrupt.");
                }
            }

            this.ConvertListForEditing(this.inventoryData.WeaponList, ref this.currentWsg.Weapons);
            this.ConvertListForEditing(this.inventoryData.ItemList, ref this.currentWsg.Items);
            this.ConvertListForEditing(this.inventoryData.BankList, ref this.currentWsg.Dlc.BankInventory);

            this.pluginManager.OnGameLoaded(new PluginEventArgs(this, fileName));

            this.Save.Enabled = true;
            this.SaveAs.Enabled = true;
            this.SelectFormat.Enabled = true;
        }

        private void optionsToolStripMenuItem_DropDownOpening(object sender, EventArgs e)
        {
            this.colorizeListsToolStripMenuItem.Checked = this.settings.UseColor;
            this.showRarityValueToolStripMenuItem.Checked = this.settings.ShowRarity;
            this.showEffectiveLevelsToolStripMenuItem.Checked = this.settings.ShowLevel;
            this.showManufacturerToolStripMenuItem.Checked = this.settings.ShowManufacturer;
            this.MenuItemPartSelectorTracking.Checked = this.settings.PartSelectorTracking;
        }

        private void PCFormat_Click(object sender, EventArgs e)
        {
            if (this.currentWsg.Platform == "PC")
            {
                return;
            }

            if (this.currentWsg.ContainsRawData && (this.currentWsg.EndianWsg != ByteOrder.LittleEndian) && !this.UIAction_RemoveRawData())
            {
                return;
            }

            this.currentWsg.Platform = "PC";
            this.currentWsg.EndianWsg = ByteOrder.LittleEndian;
            this.DoWindowTitle();
            this.currentWsg.OpenedWsg = "";
            this.Save.Enabled = false;
        }

        private void PS3Format_Click(object sender, EventArgs e)
        {
            if (this.currentWsg.Platform == "PS3")
            {
                return;
            }

            if (this.currentWsg.ContainsRawData && (this.currentWsg.EndianWsg != ByteOrder.BigEndian) && !this.UIAction_RemoveRawData())
            {
                return;
            }

            this.currentWsg.Platform = "PS3";
            this.currentWsg.EndianWsg = ByteOrder.BigEndian;
            this.DoWindowTitle();
            this.MessageBox.Show(
                "This save data will be stored in the PS3 format. Please note that you will require \r\nproper SFO, PNG, and PFD files to be transfered back to the \r\nPS3. These can be acquired from another Borderlands save \r\nfor the same profile.");
            this.currentWsg.OpenedWsg = "";
            this.Save.Enabled = false;
        }

        private void RepopulateListForSaving<T>(InventoryList itmList, ref List<T> objs)
            where T : WillowObject, new()
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

        private void RepopulateListForSaving(InventoryList itmList, ref List<BankEntry> itmBank)
        {
            itmBank = new List<BankEntry>();
            BankEntry itm;

            // Build the lists of string and value data needed by WillowSaveGame to store the
            // inventory from the data that is in itmList.
            foreach (InventoryEntry item in itmList.Items.Values)
            {
                itm = new BankEntry();

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

                itm.Quantity = values[0]; //Quantity;
                itm.Quality = (short)values[1]; //QualityIndex;
                itm.EquippedSlot = (byte)values[2]; //Equipped;
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
                this.file.Copy(this.currentWsg.OpenedWsg, $"{this.currentWsg.OpenedWsg}.bak0", true);
                if (this.file.Exists($"{this.currentWsg.OpenedWsg}.bak10"))
                {
                    this.file.Delete($"{this.currentWsg.OpenedWsg}.bak10");
                }

                for (int i = 9; i >= 0; i--)
                {
                    if (this.file.Exists($"{this.currentWsg.OpenedWsg}.bak{i}"))
                    {
                        this.file.Move($"{this.currentWsg.OpenedWsg}.bak{i}", $"{this.currentWsg.OpenedWsg}.bak{(i + 1)}");
                    }
                }
            }
            catch
            {
            }

            this.SaveToFile(this.currentWsg.OpenedWsg);
            this.MessageBox.Show($"Saved WSG to: {this.currentWsg.OpenedWsg}");
        }

        private void SaveAs_Click(object sender, EventArgs e)
        {
            WTSaveFileDialog tempSave = new WTSaveFileDialog("sav", this.currentWsg.OpenedWsg);

            if (tempSave.ShowDialog() != DialogResult.OK)
            {
                return;
            }

            this.SaveToFile(tempSave.FileName());
            this.MessageBox.Show($"Saved WSG to: {this.currentWsg.OpenedWsg}");
            this.Save.Enabled = true;
        }

        private void SaveOptions_Click(object sender, EventArgs e)
        {
            this.settings.Save();
        }

        private void SaveToFile(string filename)
        {
            this.pluginManager.OnGameSaving(new PluginEventArgs(this, filename));
            Application.DoEvents();

            // Convert the weapons and items data from WeaponList/ItemList into
            // the format used by WillowSaveGame.
            this.RepopulateListForSaving(this.inventoryData.WeaponList, ref this.currentWsg.Weapons);
            this.RepopulateListForSaving(this.inventoryData.ItemList, ref this.currentWsg.Items);
            this.RepopulateListForSaving(this.inventoryData.BankList, ref this.currentWsg.Dlc.BankInventory);
            WillowSaveGameSerializer.WriteToFile(this.currentWsg, filename);
            this.currentWsg.OpenedWsg = filename;

            // Release the WillowSaveGame inventory data now that saving is complete.  The
            // same data is still contained in db.WeaponList, db.ItemList, and db.BankList
            // in the format used by the WillowTree UI.
            this.currentWsg.Weapons = null;
            this.currentWsg.Items = null;
            this.currentWsg.Dlc.BankInventory = null;

            this.pluginManager.OnGameSaved(new PluginEventArgs(this, this.currentWsg.OpenedWsg));
        }

        private void SetUiTreeStyles(bool useColor)
        {
            var theme = useColor ? this.themes.treeViewTheme1 : null;

            this.inventoryData.ItemList.OnTreeThemeChanged(theme);
            this.inventoryData.WeaponList.OnTreeThemeChanged(theme);
            this.inventoryData.BankList.OnTreeThemeChanged(theme);
            this.inventoryData.LockerList.OnTreeThemeChanged(theme);
        }

        private void showEffectiveLevelsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.settings.ShowLevel = !this.settings.ShowLevel;
            this.UpdateNames();
        }

        private void showManufacturerToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.settings.ShowManufacturer = !this.settings.ShowManufacturer;
            this.UpdateNames();
        }

        private void showRarityValueToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.settings.ShowRarity = !this.settings.ShowRarity;
            this.UpdateNames();
        }

        private void StandardInput_Click(object sender, EventArgs e)
        {
            this.settings.InputMode = InputMode.Standard;
        }

        private void tabControl1_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (this.selectedTabObject is IPlugin tabObject)
            {
                this.pluginManager.OnPluginUnselected(tabObject, new PluginEventArgs(this, null));
            }

            int selected = this.tabControl1.SelectedIndex;
            if (selected >= 0)
            {
                this.selectedTabObject = this.tabControl1.Controls[selected];
                if (this.selectedTabObject is IPlugin plugin)
                {
                    this.pluginManager.OnPluginSelected(plugin, new PluginEventArgs(this, null));
                }
            }
            else
            {
                this.selectedTabObject = null;
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
            DialogResult result = this.MessageBox.Show(
                "This savegame contains some unexpected or possibly corrupt DLC data that WillowTree# does not know how to parse, so it cannot be rewritten in a different machine byte order.  The extra data can be discarded to allow you to convert the savegame, but this will cause DLC data loss if the unknown DLC data is actually used by Borderlands.  It is typical for Steam PC savegames to have a data section like this and it must be removed to convert to Xbox 360 or PS3 format.  The data probably serves no gameplay purpose because it doesn't exist in the PC DVD version.\r\n\r\nDo you want to discard the raw data to allow the conversion?",
                "Unexpected Raw Data", MessageBoxButtons.YesNo);
            if (result == DialogResult.No)
            {
                return false;
            }

            WillowSaveGameSerializer.DiscardRawData(this.currentWsg);
            return true;
        }

        private void UpdateNames()
        {
            this.inventoryData.WeaponList.OnNameFormatChanged();
            this.inventoryData.ItemList.OnNameFormatChanged();
            this.inventoryData.BankList.OnNameFormatChanged();
            this.inventoryData.LockerList.OnNameFormatChanged();
        }

        private void WillowTreeMain_FormClosing(object sender, EventArgs e)
        {
            this.settings.Save();
        }

        private void XBoxFormat_Click(object sender, EventArgs e)
        {
            if (this.currentWsg.Platform == "X360")
            {
                return;
            }

            if (this.currentWsg.ContainsRawData && (this.currentWsg.EndianWsg != ByteOrder.BigEndian) && !this.UIAction_RemoveRawData())
            {
                return;
            }

            if (this.currentWsg.DeviceId == null)
            {
                XBoxIDDialog dlgXBoxId = new XBoxIDDialog();
                if (dlgXBoxId.ShowDialog() != DialogResult.OK)
                {
                    return;
                }

                this.currentWsg.ProfileId = dlgXBoxId.ID.ProfileID;
                int deviceIdLength = dlgXBoxId.ID.DeviceID.Length;
                this.currentWsg.DeviceId = new byte[deviceIdLength];
                Array.Copy(dlgXBoxId.ID.DeviceID, this.currentWsg.DeviceId, deviceIdLength);
            }

            this.currentWsg.Platform = "X360";
            this.currentWsg.EndianWsg = ByteOrder.BigEndian;
            this.DoWindowTitle();
            this.currentWsg.OpenedWsg = "";
            this.Save.Enabled = false;
        }

        private void XBoxJPFormat_Click(object sender, EventArgs e)
        {
            if (this.currentWsg.Platform == "X360JP")
            {
                return;
            }

            if (this.currentWsg.ContainsRawData && (this.currentWsg.EndianWsg != ByteOrder.BigEndian) && !this.UIAction_RemoveRawData())
            {
                return;
            }

            if (this.currentWsg.DeviceId == null)
            {
                XBoxIDDialog dlgXBoxId = new XBoxIDDialog();
                if (dlgXBoxId.ShowDialog() != DialogResult.OK)
                {
                    return;
                }

                this.currentWsg.ProfileId = dlgXBoxId.ID.ProfileID;
                int deviceIdLength = dlgXBoxId.ID.DeviceID.Length;
                this.currentWsg.DeviceId = new byte[deviceIdLength];
                Array.Copy(dlgXBoxId.ID.DeviceID, this.currentWsg.DeviceId, deviceIdLength);
            }

            this.currentWsg.Platform = "X360JP";
            this.currentWsg.EndianWsg = ByteOrder.BigEndian;
            this.DoWindowTitle();
            this.currentWsg.OpenedWsg = "";
            this.Save.Enabled = false;
        }
    }
}
