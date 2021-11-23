using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using WillowTree.CustomControls;
using WillowTree.Inventory;
using WillowTree.Plugins;

namespace WillowTree
{
    public partial class WillowTreeMain : Form
    {
        private PluginComponentManager PluginManager = Services.PluginManager;

        private WillowSaveGame CurrentWSG;

        public WillowSaveGame SaveData
        {
            get { return CurrentWSG; }
        }

        private void SetUITreeStyles(bool UseColor)
        {
            TreeViewTheme theme;

            if (UseColor)
                theme = Services.AppThemes.treeViewTheme1;
            else
                theme = null;

            db.ItemList.OnTreeThemeChanged(theme);
            db.WeaponList.OnTreeThemeChanged(theme);
            db.BankList.OnTreeThemeChanged(theme);
            db.LockerList.OnTreeThemeChanged(theme);
        }

        public WillowTreeMain()
        {
            GlobalSettings.Load();

            if (!Directory.Exists(db.DataPath))
            {
                MessageBox.Show("Couldn't find the 'Data' folder! Please make sure that WillowTree# and its data folder are in the same directory.");
                Application.Exit();
                return;
            }

            if (!File.Exists(db.DataPath + "default.xml"))
                File.WriteAllText(db.DataPath + "default.xml", "<?xml version=\"1.0\" encoding=\"us-ascii\"?>\r\n<INI></INI>\r\n");

            InitializeComponent();

            db.InitializeNameLookup();

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
            CreatePluginAsTab("About", new ucAbout());

            try
            {
                tabControl1.SelectTab("ucAbout");
            }
            catch
            {
            }

            SetUITreeStyles(GlobalSettings.UseColor);
        }

        public void CreatePluginAsTab(string tabTitle, Control plugin)
        {
            if (plugin is IPlugin)
            {
                plugin.Text = tabTitle;
                plugin.BackColor = Color.Transparent;
                tabControl1.Controls.Add(plugin);
                PluginManager.InitializePlugin(plugin as IPlugin);
            }
        }

        private void ConvertListForEditing<T>(InventoryList itmList, ref List<T> objs) where T : WillowSaveGame.Object
        {
            // Populate itmList with items created from the WillowSaveGame data lists
            itmList.ClearSilent();
            foreach (var obj in objs)
                itmList.AddSilent(new InventoryEntry(itmList.invType, obj.Strings, obj.GetValues()));
            itmList.OnListReload();

            objs = null;
        }

        private void ConvertListForEditing(InventoryList itmList, ref List<WillowSaveGame.BankEntry> itmBank)
        {
            // Populate itmList with items created from the WillowSaveGame data lists
            itmList.ClearSilent();
            for (int i = 0; i < itmBank.Count; i++)
            {
                List<int> itmBankValues = new List<int>() { itmBank[i].Quantity, itmBank[i].Quality, itmBank[i].EquipedSlot, itmBank[i].Level, itmBank[i].Junk, itmBank[i].Locked };

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

        private void RepopulateListForSaving<T>(InventoryList itmList, ref List<T> objs) where T : WillowSaveGame.Object, new()
        {
            objs = new List<T>();
            foreach (InventoryEntry item in itmList.Items.Values)
            {
                var obj = new T();
                obj.Strings = item.Parts;
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
                    itm.Strings = new List<string>() { oldParts[1], oldParts[0], oldParts[6], oldParts[2], oldParts[3],
                                                                  oldParts[4], oldParts[5], oldParts[7], oldParts[8] };
                }
                else
                    itm.Strings = new List<string>(item.Parts);

                //Item/Weapon in bank have their type increase by 1, we  increase TypeId by 1 to restore them to their natural value
                itm.TypeId = (byte)(item.Type + 1);

                List<int> values = item.GetValues();

                //if (Convert.ToBoolean(values[4])) //TODO RSM UsesBigLevel
                itm.Quantity = values[0];//Quantity;
                itm.Quality = (short)values[1];//QualityIndex;
                itm.EquipedSlot = (byte)values[2];//Equipped;
                itm.Level = (short)values[3];//LevelIndex;
                itm.Junk = (byte)values[4];
                itm.Locked = (byte)values[5];

                itmBank.Add(itm);
            }

            // The string lists stored in the bank entries are not not shared
            // after this method runs like they are for the backpack inventory.
            // Still they should be released after saving since this is duplicate
            // data that will be rebuilt on the next save attempt.
        }

        private void DoWindowTitle()
        {
            ucGeneral eGeneral = PluginManager.GetPlugin(typeof(ucGeneral)) as ucGeneral;
            if (eGeneral != null)
                eGeneral.DoWindowTitle();
        }

        private void Open_Click(object sender, EventArgs e)
        {
            string fileName = (CurrentWSG != null) ? CurrentWSG.OpenedWsg : "";

            Util.WTOpenFileDialog openDlg = new Util.WTOpenFileDialog("sav", fileName);
            if (openDlg.ShowDialog() == DialogResult.OK)
            {
                fileName = openDlg.FileName();

                PluginManager.OnGameLoading(new PluginEventArgs(this, fileName));
                Application.DoEvents();
                CurrentWSG = new WillowSaveGame();
                CurrentWSG.AutoRepair = true;
                CurrentWSG.LoadWsg(fileName);

                if (CurrentWSG.RequiredRepair == true)
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
                        throw new FileFormatException("Savegame file is corrupt.");
                }

                ConvertListForEditing(db.WeaponList, ref CurrentWSG.Weapons);
                ConvertListForEditing(db.ItemList, ref CurrentWSG.Items);
                ConvertListForEditing(db.BankList, ref CurrentWSG.Dlc.BankInventory);

                PluginManager.OnGameLoaded(new PluginEventArgs(this, fileName));

                Save.Enabled = true;
                SaveAs.Enabled = true;
                SelectFormat.Enabled = true;
            }
            else
                fileName = "";
        }

        private void Save_Click(object sender, EventArgs e)
        {
            try
            {
                File.Copy(CurrentWSG.OpenedWsg, CurrentWSG.OpenedWsg + ".bak0", true);
                if (File.Exists(CurrentWSG.OpenedWsg + ".bak10") == true)
                    File.Delete(CurrentWSG.OpenedWsg + ".bak10");
                for (int i = 9; i >= 0; i--)
                {
                    if (File.Exists(CurrentWSG.OpenedWsg + ".bak" + i) == true)
                        File.Move(CurrentWSG.OpenedWsg + ".bak" + i, CurrentWSG.OpenedWsg + ".bak" + (i + 1));
                }
            }
            catch { }

            //            try
            {
                SaveToFile(CurrentWSG.OpenedWsg);
                MessageBox.Show("Saved WSG to: " + CurrentWSG.OpenedWsg);
            }
            //            catch { MessageBox.Show("Couldn't save WSG"); }
        }

        private void SaveAs_Click(object sender, EventArgs e)
        {
            Util.WTSaveFileDialog tempSave = new Util.WTSaveFileDialog("sav", CurrentWSG.OpenedWsg);

            if (tempSave.ShowDialog() == DialogResult.OK)
            {
                SaveToFile(tempSave.FileName());
                MessageBox.Show("Saved WSG to: " + CurrentWSG.OpenedWsg);
                Save.Enabled = true;
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
            DialogResult result = MessageBox.Show("This savegame contains some unexpected or possibly corrupt DLC data that WillowTree# does not know how to parse, so it cannot be rewritten in a different machine byte order.  The extra data can be discarded to allow you to convert the savegame, but this will cause DLC data loss if the unknown DLC data is actually used by Borderlands.  It is typical for Steam PC savegames to have a data section like this and it must be removed to convert to Xbox 360 or PS3 format.  The data probably serves no gameplay purpose because it doesn't exist in the PC DVD version.\r\n\r\nDo you want to discard the raw data to allow the conversion?", "Unexpected Raw Data", MessageBoxButtons.YesNo);
            if (result == DialogResult.No)
                return false;

            CurrentWSG.DiscardRawData();
            return true;
        }

        private void PCFormat_Click(object sender, EventArgs e)
        {
            if (CurrentWSG.Platform == "PC")
                return;

            if ((CurrentWSG.ContainsRawData == true) && (CurrentWSG.EndianWsg != ByteOrder.LittleEndian))
            {
                if (!UIAction_RemoveRawData())
                    return;
            }

            CurrentWSG.Platform = "PC";
            CurrentWSG.EndianWsg = ByteOrder.LittleEndian;
            DoWindowTitle();
            CurrentWSG.OpenedWsg = "";
            Save.Enabled = false;
        }

        private void PS3Format_Click(object sender, EventArgs e)
        {
            if (CurrentWSG.Platform == "PS3")
                return;

            if ((CurrentWSG.ContainsRawData == true) && (CurrentWSG.EndianWsg != ByteOrder.BigEndian))
            {
                if (!UIAction_RemoveRawData())
                    return;
            }

            CurrentWSG.Platform = "PS3";
            CurrentWSG.EndianWsg = ByteOrder.BigEndian;
            DoWindowTitle();
            MessageBox.Show("This save data will be stored in the PS3 format. Please note that you will require \r\nproper SFO, PNG, and PFD files to be transfered back to the \r\nPS3. These can be acquired from another Borderlands save \r\nfor the same profile.");
            CurrentWSG.OpenedWsg = "";
            Save.Enabled = false;
        }

        private void XBoxFormat_Click(object sender, EventArgs e)
        {
            if (CurrentWSG.Platform == "X360")
                return;

            if ((CurrentWSG.ContainsRawData == true) && (CurrentWSG.EndianWsg != ByteOrder.BigEndian))
            {
                if (!UIAction_RemoveRawData())
                    return;
            }

            if (CurrentWSG.DeviceId == null)
            {
                XBoxIDDialog dlgXBoxID = new XBoxIDDialog();
                if (dlgXBoxID.ShowDialog() == DialogResult.OK)
                {
                    CurrentWSG.ProfileId = dlgXBoxID.ID.ProfileID;
                    int DeviceIDLength = dlgXBoxID.ID.DeviceID.Count();
                    CurrentWSG.DeviceId = new byte[DeviceIDLength];
                    Array.Copy(dlgXBoxID.ID.DeviceID, CurrentWSG.DeviceId, DeviceIDLength);
                }
                else
                    return;
            }
            CurrentWSG.Platform = "X360";
            CurrentWSG.EndianWsg = ByteOrder.BigEndian;
            DoWindowTitle();
            CurrentWSG.OpenedWsg = "";
            Save.Enabled = false;
        }

        private void XBoxJPFormat_Click(object sender, EventArgs e)
        {
            if (CurrentWSG.Platform == "X360JP")
                return;

            if ((CurrentWSG.ContainsRawData == true) && (CurrentWSG.EndianWsg != ByteOrder.BigEndian))
            {
                if (!UIAction_RemoveRawData())
                    return;
            }

            if (CurrentWSG.DeviceId == null)
            {
                XBoxIDDialog dlgXBoxID = new XBoxIDDialog();
                if (dlgXBoxID.ShowDialog() == DialogResult.OK)
                {
                    CurrentWSG.ProfileId = dlgXBoxID.ID.ProfileID;
                    int DeviceIDLength = dlgXBoxID.ID.DeviceID.Count();
                    CurrentWSG.DeviceId = new byte[DeviceIDLength];
                    Array.Copy(dlgXBoxID.ID.DeviceID, CurrentWSG.DeviceId, DeviceIDLength);
                }
                else
                    return;
            }
            CurrentWSG.Platform = "X360JP";
            CurrentWSG.EndianWsg = ByteOrder.BigEndian;
            DoWindowTitle();
            CurrentWSG.OpenedWsg = "";
            Save.Enabled = false;
        }

        private void ExitWT_Click(object sender, EventArgs e)
        {
            GlobalSettings.Save();
            Application.Exit();
        }

        private void WillowTreeMain_FormClosing(object sender, EventArgs e)
        {
            GlobalSettings.Save();
        }

        private void StandardInput_Click(object sender, EventArgs e)
        {
            GlobalSettings.InputMode = InputMode.Standard;
        }

        private void AdvancedInputDecimal_Click(object sender, EventArgs e)
        {
            GlobalSettings.UseHexInAdvancedMode = false;
            GlobalSettings.InputMode = InputMode.Advanced;
        }

        private void AdvancedInputHexadecimal_Click(object sender, EventArgs e)
        {
            GlobalSettings.UseHexInAdvancedMode = true;
            GlobalSettings.InputMode = InputMode.Advanced;
        }

        private void SaveToFile(string filename)
        {
            PluginManager.OnGameSaving(new PluginEventArgs(this, filename));
            Application.DoEvents();

            // Convert the weapons and items data from WeaponList/ItemList into
            // the format used by WillowSaveGame.
            RepopulateListForSaving(db.WeaponList, ref CurrentWSG.Weapons);
            RepopulateListForSaving(db.ItemList, ref CurrentWSG.Items);
            RepopulateListForSaving(db.BankList, ref CurrentWSG.Dlc.BankInventory);
            CurrentWSG.SaveWsg(filename);
            CurrentWSG.OpenedWsg = filename;

            // Release the WillowSaveGame inventory data now that saving is complete.  The
            // same data is still contained in db.WeaponList, db.ItemList, and db.BankList
            // in the format used by the WillowTree UI.
            CurrentWSG.Weapons = null;
            CurrentWSG.Items = null;
            CurrentWSG.Dlc.BankInventory = null;

            PluginManager.OnGameSaved(new PluginEventArgs(this, CurrentWSG.OpenedWsg));
        }

        private void NextSort_Click(object sender, EventArgs e)
        {
            IPlugin page = tabControl1.TabPages[tabControl1.SelectedIndex] as IPlugin;
            if (page != null)
                PluginManager.OnPluginCommand(page, new PluginCommandEventArgs(this, PluginCommand.ChangeSortMode));
        }

        private void IncreaseNavigationLayers_Click(object sender, EventArgs e)
        {
            IPlugin page = tabControl1.TabPages[tabControl1.SelectedIndex] as IPlugin;
            if (page != null)
                PluginManager.OnPluginCommand(page, new PluginCommandEventArgs(this, PluginCommand.IncreaseNavigationDepth));
        }

        private void UpdateNames()
        {
            db.WeaponList.OnNameFormatChanged();
            db.ItemList.OnNameFormatChanged();
            db.BankList.OnNameFormatChanged();
            db.LockerList.OnNameFormatChanged();
        }

        private void showRarityValueToolStripMenuItem_Click(object sender, EventArgs e)
        {
            GlobalSettings.ShowRarity = !GlobalSettings.ShowRarity;
            UpdateNames();
        }

        private void colorizeListsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            GlobalSettings.UseColor = !GlobalSettings.UseColor;
            SetUITreeStyles(GlobalSettings.UseColor);
        }

        private void optionsToolStripMenuItem_DropDownOpening(object sender, EventArgs e)
        {
            colorizeListsToolStripMenuItem.Checked = GlobalSettings.UseColor;
            showRarityValueToolStripMenuItem.Checked = GlobalSettings.ShowRarity;
            showEffectiveLevelsToolStripMenuItem.Checked = GlobalSettings.ShowLevel;
            showManufacturerToolStripMenuItem.Checked = GlobalSettings.ShowManufacturer;
            MenuItemPartSelectorTracking.Checked = GlobalSettings.PartSelectorTracking;
        }

        private void showManufacturerToolStripMenuItem_Click(object sender, EventArgs e)
        {
            GlobalSettings.ShowManufacturer = !GlobalSettings.ShowManufacturer;
            UpdateNames();
        }

        private void showEffectiveLevelsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            GlobalSettings.ShowLevel = !GlobalSettings.ShowLevel;
            UpdateNames();
        }

        private void SaveOptions_Click(object sender, EventArgs e)
        {
            GlobalSettings.Save();
        }

        public Font treeViewFont = new Font("Microsoft Sans Serif", 8.25F, FontStyle.Regular, GraphicsUnit.Point, ((byte)(0)));

        private Control SelectedTabObject = null;

        private void tabControl1_SelectedIndexChanged(object sender, EventArgs e)
        {
            if ((SelectedTabObject != null) && (SelectedTabObject is IPlugin))
                PluginManager.OnPluginUnselected(SelectedTabObject as IPlugin, new PluginEventArgs(this, null));

            int selected = tabControl1.SelectedIndex;
            if (selected >= 0)
            {
                SelectedTabObject = tabControl1.Controls[selected];
                if ((SelectedTabObject != null) && (SelectedTabObject is IPlugin))
                    PluginManager.OnPluginSelected(SelectedTabObject as IPlugin, new PluginEventArgs(this, null));
            }
            else
                SelectedTabObject = null;

            //if (tabControl1.SelectedIndex == tabControl1.GetTabIndex(DebugTab))
            //{
            //    DumpTreeDebugInfo(Weapon.Tree);
            //}
        }

        private void MenuItemPartSelectorTracking_Click(object sender, EventArgs e)
        {
            GlobalSettings.PartSelectorTracking = !GlobalSettings.PartSelectorTracking;
        }
    }
}
