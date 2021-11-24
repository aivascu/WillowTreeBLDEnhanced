using Aga.Controls.Tree;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using WillowTree.CustomControls;
using WillowTree.Inventory;

namespace WillowTree.Plugins
{
    public partial class ucLocker : UserControl, IPlugin
    {
        private PluginComponentManager pluginManager;
        private Font HighlightFont;

        public InventoryTreeList LockerTL;

        public ucLocker()
        {
            InitializeComponent();
        }

        public void InitializePlugin(PluginComponentManager pm)
        {
            PluginEvents events = new PluginEvents
            {
                GameLoaded = OnGameLoaded,
                GameSaving = OnGameSaving,
                PluginCommand = OnPluginCommand
            };
            pm.RegisterPlugin(this, events);

            pluginManager = pm;

            // The index translators control the caption that goes over the top of each
            // level or quality SlideSelector.  Attach each translator then signal the
            // value changed event to cause the translator to update the caption.
            this.QualityLocker.IndexTranslator += QualityTranslator;
            this.LevelIndexLocker.IndexTranslator += LevelTranslator;
            this.LevelIndexOverride.IndexTranslator += LevelTranslator;
            this.QualityOverride.IndexTranslator += QualityTranslator;
            this.LevelIndexLocker.OnValueChanged(EventArgs.Empty);
            this.QualityLocker.OnValueChanged(EventArgs.Empty);
            this.LevelIndexOverride.OnValueChanged(EventArgs.Empty);
            this.QualityOverride.OnValueChanged(EventArgs.Empty);

            HighlightFont = new Font(LockerTree.Font, FontStyle.Italic | FontStyle.Bold);

            ImportAllFromItems.Enabled = false;
            ImportAllFromWeapons.Enabled = false;

            LockerTL = new InventoryTreeList(LockerTree, GameData.LockerList);

            string lockerFilename = GameData.OpenedLockerFilename();
            if (!System.IO.File.Exists(lockerFilename))
                GameData.OpenedLockerFilename(GameData.DataPath + "default.xml");

            try
            {
                LoadLocker(GameData.OpenedLockerFilename());
                LockerTL.UpdateTree();
            }
            catch (ApplicationException)
            {
                MessageBox.Show("The locker file \"" + GameData.OpenedLockerFilename() + " could not be loaded.  It may be corrupt.  If you delete or rename it the program will make a new one and you may be able to start the program successfully.  Shutting down now.");
                Application.Exit();
            }
        }

        public void ReleasePlugin()
        {
            this.QualityLocker.IndexTranslator -= QualityTranslator;
            this.LevelIndexLocker.IndexTranslator -= LevelTranslator;
            this.LevelIndexOverride.IndexTranslator -= LevelTranslator;
            this.QualityOverride.IndexTranslator -= QualityTranslator;

            pluginManager = null;
            HighlightFont = null;

            LockerTL = null;
            GameData.OpenedLockerFilename(null);
        }

        public void OnGameLoaded(object sender, PluginEventArgs e)
        {
            ImportAllFromItems.Enabled = true;
            ImportAllFromWeapons.Enabled = true;
            LockerTL.UpdateTree();
        }

        public void OnGameSaving(object sender, PluginEventArgs e)
        {
            LockerTL.SaveToXml(GameData.OpenedLockerFilename());
        }

        public void OnPluginCommand(object sender, PluginCommandEventArgs e)
        {
            if (e.Command == PluginCommand.IncreaseNavigationDepth)
                LockerTL.IncreaseNavigationDepth();
            else if (e.Command == PluginCommand.ChangeSortMode)
                LockerTL.NextSort();
        }

        public void LoadLocker(string InputFile)
        {
            LockerTL.Tree.BeginUpdate();
            LockerTL.Clear();
            LockerTL.ImportFromXml(InputFile, InventoryType.Any);
            LockerTL.Tree.EndUpdate();
        }

        private void NewWeaponLocker_Click(object sender, EventArgs e)
        {
            LockerTL.AddNew(InventoryType.Weapon);
            LockerTL.AdjustSelectionAfterAdd();
            LockerTree.EnsureVisible(LockerTree.SelectedNode);
        }

        private void NewItemLocker_Click(object sender, EventArgs e)
        {
            LockerTL.AddNew(InventoryType.Item);
            LockerTL.AdjustSelectionAfterAdd();
            LockerTree.EnsureVisible(LockerTree.SelectedNode);
        }

        private void DeleteLocker_Click(object sender, EventArgs e)
        {
            LockerTL.DeleteSelected();
        }

        private void DuplicateLocker_Click(object sender, EventArgs e)
        {
            LockerTL.DuplicateSelected();
        }

        private void MoveLocker_Click(object sender, EventArgs e)
        {
            // Copy the selection because it will change as nodes are removed.
            TreeNodeAdv[] nodes = LockerTree.SelectedNodes.ToArray();

            foreach (TreeNodeAdv node in nodes)
                if (node.Children.Count == 0)
                {
                    InventoryEntry entry = node.GetEntry() as InventoryEntry;
                    if (entry.Type == InventoryType.Weapon)
                        GameData.WeaponList.Add(entry);
                    else if (entry.Type == InventoryType.Item)
                        GameData.ItemList.Add(entry);

                    LockerTL.Remove(node, false);
                }
        }

        private void CopyBackpack_Click(object sender, EventArgs e)
        {
            foreach (TreeNodeAdv node in LockerTree.SelectedNodes)
                if (node.Children.Count == 0)
                {
                    InventoryEntry entry = node.GetEntry() as InventoryEntry;
                    if (entry.Type == InventoryType.Weapon)
                        GameData.WeaponList.Duplicate(entry); //LockerTL.CopySelected(db.WeaponList, false);
                    else if (entry.Type == InventoryType.Item)
                        GameData.ItemList.Duplicate(entry); //LockerTL.CopySelected(db.ItemList, false);
                }
        }

        private void CopyBank_Click(object sender, EventArgs e)
        {
            LockerTL.CopySelected(GameData.BankList, false);
        }

        private void ClearAllLocker_Click(object sender, EventArgs e)
        {
            LockerTL.Clear();
        }

        private void PurgeDuplicatesLocker_Click(object sender, EventArgs e)
        {
            LockerTL.PurgeDuplicates();
        }

        private string ExportToTextLocker()
        {
            List<string> InOutParts = new List<string>();

            for (int Progress = 0; Progress < PartsLocker.Items.Count; Progress++)
                InOutParts.Add((string)PartsLocker.Items[Progress]);

            List<int> values = null;

            if (OverrideExportSettings.Checked == true)
                values = InventoryEntry.CalculateValues((int)RemAmmoOverride.Value,
                    (int)QualityOverride.Value, 0, (int)LevelIndexOverride.Value, (int)JunkLocker.Value, (int)LockedLocker.Value, ((string)PartsLocker.Items[0]));
            else
                values = InventoryEntry.CalculateValues((int)RemAmmoLocker.Value,
                    (int)QualityLocker.Value, 0, (int)LevelIndexLocker.Value, (int)JunkLocker.Value, (int)LockedLocker.Value, ((string)PartsLocker.Items[0]));

            for (int i = 0; i < WillowSaveGame.ExportValuesCount; i++)
                InOutParts.Add(values[i].ToString());

            return string.Join("\r\n", InOutParts.ToArray()) + "\r\n";
        }

        private void ExportToClipboardLocker_Click(object sender, EventArgs e)
        {
            try
            {
                Clipboard.SetText(ExportToTextLocker());
            }
            catch { MessageBox.Show("Export to clipboard failed."); }
        }

        private void ExportToFileLocker_Click(object sender, EventArgs e)
        {
            WTSaveFileDialog ToFile = new WTSaveFileDialog("txt", LockerPartsGroup.Text);
            if (ToFile.ShowDialog() == DialogResult.OK)
                System.IO.File.WriteAllText(ToFile.FileName(), ExportToTextLocker());
        }

        private void ExportToXmlLocker_Click(object sender, EventArgs e)
        {
            WTSaveFileDialog fileDlg = new WTSaveFileDialog("xml", GameData.OpenedLockerFilename());

            if (fileDlg.ShowDialog() == DialogResult.OK)
                LockerTL.SaveToXml(fileDlg.FileName());
        }

        private bool ImportFromTextLocker(string text)
        {
            InventoryEntry gear = InventoryEntry.ImportFromText(text, InventoryType.Unknown);

            if (gear == null) return false;

            LockerTL.Add(gear);
            return true;
        }

        private void ImportFromClipboardLocker_Click(object sender, EventArgs e)
        {
            try
            {
                if (ImportFromTextLocker(Clipboard.GetText()))
                    LockerTree.SelectedNode = LockerTree.AllNodes.Last();
            }
            catch
            {
                MessageBox.Show("Invalid clipboard data.  Import failed.");
            }
        }

        private void ImportFromFilesLocker_Click(object sender, EventArgs e)
        {
            WTOpenFileDialog FromFile = new WTOpenFileDialog("txt", "");
            FromFile.Multiselect(true);

            if (FromFile.ShowDialog() == DialogResult.OK)
            {
                foreach (String file in FromFile.FileNames())
                {
                    try
                    {
                        if (ImportFromTextLocker(System.IO.File.ReadAllText(file)))
                            LockerTree.SelectedNode = LockerTree.AllNodes.Last();
                    }
                    catch (System.IO.IOException)
                    {
                        MessageBox.Show("Unable to read file \"" + file + "\".");
                    }
                }
            }
        }

        private void ImportAllFromXmlLocker_Click(object sender, EventArgs e)
        {
            WTOpenFileDialog fileDlg = new WTOpenFileDialog("xml", "");
            if (fileDlg.ShowDialog() == DialogResult.OK)
            {
                LockerTL.ImportFromXml(fileDlg.FileName(), InventoryType.Any);
                LockerTL.SaveToXml(fileDlg.FileName());
            }
        }

        private void ImportAllFromItemsLocker_Click(object sender, EventArgs e)
        {
            LockerTree.BeginUpdate();
            foreach (InventoryEntry item in GameData.ItemList.Items.Values)
                LockerTL.Duplicate(item);
            LockerTree.EndUpdate();
            LockerTL.SaveToXml(GameData.OpenedLockerFilename());
        }

        private void ImportAllFromWeaponsLocker_Click(object sender, EventArgs e)
        {
            LockerTree.BeginUpdate();
            foreach (InventoryEntry weapon in GameData.WeaponList.Items.Values)
                LockerTL.Duplicate(weapon);
            LockerTree.EndUpdate();
            LockerTL.SaveToXml(GameData.OpenedLockerFilename());
        }

        private void LockerTree_SelectionChanged(object sender, EventArgs e)
        {
            PartsLocker.Items.Clear();

            if (LockerTree.SelectedNode == null || LockerTree.SelectedNode.Children.Count > 0)
                return;

            InventoryEntry entry = LockerTree.SelectedNode.GetEntry() as InventoryEntry;

            if (LockerTree.SelectedNode.Children.Count == 0)
            {   // Tree nodes with no children are items or weapons.  Entries with children would be section headers.
                string SelectedItem = LockerTree.SelectedNode.Data().Text;

                LockerPartsGroup.Text = entry.Name;
                RatingLocker.Text = entry.Rating;
                DescriptionLocker.Text = entry.Description.Replace("$LINE$", "\r\n");

                int partcount = entry.GetPartCount();

                for (int Progress = 0; Progress < partcount; Progress++)
                    PartsLocker.Items.Add(entry.Parts[Progress]);

                WTSlideSelector.MinMaxAdvanced(entry.UsesBigLevel, ref LevelIndexLocker);

                Util.SetNumericUpDown(RemAmmoLocker, entry.Quantity);
                QualityLocker.Value = entry.QualityIndex;
                LevelIndexLocker.Value = entry.LevelIndex;
                JunkLocker.Value = entry.IsJunk;
                LockedLocker.Value = entry.IsLocked;
            }
        }

        private void SaveChangesLocker_Click(object sender, EventArgs e)
        {
            if (LockerTree.SelectedNode == null)
                return;

            if (LockerTree.SelectedNode.Children.Count > 0)
                return;

            InventoryEntry entry = LockerTree.SelectedNode.GetEntry() as InventoryEntry;

            int partcount = entry.GetPartCount();

            for (int Progress = 0; Progress < partcount; Progress++)
                entry.Parts[Progress] = (string)PartsLocker.Items[Progress];

            entry.UsesBigLevel = InventoryEntry.ItemgradePartUsesBigLevel((string)PartsLocker.Items[0]);

            entry.Quantity = (int)RemAmmoLocker.Value;
            entry.QualityIndex = QualityLocker.Value;
            entry.EquippedSlot = 0;
            entry.LevelIndex = LevelIndexLocker.Value;
            entry.IsJunk = (int)JunkLocker.Value;
            entry.IsLocked = (int)LockedLocker.Value;

            if (entry.Type == InventoryType.Weapon)
            {
                entry.RecalculateDataWeapon();
                entry.BuildName();
            }
            else if (entry.Type == InventoryType.Item)
            {
                entry.RecalculateDataItem();
                entry.BuildName();
            }
            else
            {
                System.Diagnostics.Debug.Assert(true, "Invalid item type in locker");
                entry.Name = "Invalid ItemType (" + entry.Type + ")";
            }

            // When the item changes, it may not belong in the same location in
            // in the sorted tree because the name, level, or other sort key
            // has changed.  Remove the node then place it back into the tree to
            // make sure it is relocated to the proper location, then select the
            // node and make sure it is visible so the user is focused on the new
            // location after the changes.
            LockerTL.RemoveFromTreeView(LockerTree.SelectedNode, false);
            LockerTL.AddToTreeView(entry);
            LockerTL.AdjustSelectionAfterAdd();
            LockerTree.EnsureVisible(LockerTree.SelectedNode);

            LockerPartsGroup.Text = entry.Name;
            LockerTree.Focus();
        }

        //TODO Should be less dependant from "Colorize Lists" menu
        private void btnlockerSearch_Click(object sender, EventArgs e)
        {
            string searchText = lockerSearch.Text.ToUpper();
            string text = "";

            foreach (TreeNodeAdv node in LockerTL.Tree.AllNodes)
            {
                if (node.Children.Count == 0)
                {
                    text = (node.GetEntry() as InventoryEntry).ToXmlText().ToUpper();

                    if (searchText != "" && text.Contains(searchText))
                        (node.Tag as ColoredTextNode).Font = HighlightFont;
                    else
                        (node.Tag as ColoredTextNode).Font = LockerTree.Font;
                }
            }
            this.Refresh(); //LockerTree_SelectionChanged is not needed for visual update
        }

        private void lockerSearch_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                btnlockerSearch_Click(this, KeyEventArgs.Empty);
            }
        }

        private void OpenLocker_Click(object sender, EventArgs e)
        {
            WTOpenFileDialog FromFile = new WTOpenFileDialog("xml", GameData.OpenedLockerFilename());

            try
            {
                if (FromFile.ShowDialog() == DialogResult.OK)
                {
                    LoadLocker(FromFile.FileName());
                    GameData.OpenedLockerFilename(FromFile.FileName());
                }
            }
            catch { MessageBox.Show("Could not load the selected WillowTree Locker."); }
        }

        private void LockerTree_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Delete)
                DeleteLocker_Click(this, EventArgs.Empty);
            else if (e.KeyCode == Keys.Insert)
                DuplicateLocker_Click(this, EventArgs.Empty);
            else if (e.KeyData == (Keys.Control | Keys.B))
                CopyBackpack_Click(this, EventArgs.Empty);
            else if (e.KeyData == (Keys.Control | Keys.N))
                CopyBank_Click(this, EventArgs.Empty);
        }

        public static string LevelTranslator(object obj)
        {
            WTSlideSelector levelindex = (WTSlideSelector)obj;

            return levelindex.InputMode == InputMode.Advanced
                ? GlobalSettings.UseHexInAdvancedMode
                    ? "Level Index (hexadecimal)"
                    : "Level Index (decimal)"
                : levelindex.Value == 0
                    ? "Level: Default"
                    : $"Level: {levelindex.Value - 2}";
        }

        public static string QualityTranslator(object obj)
        {
            WTSlideSelector qualityindex = (WTSlideSelector)obj;

            return qualityindex.InputMode == InputMode.Advanced
                ? GlobalSettings.UseHexInAdvancedMode
                    ? "Quality Index (hexadecimal)"
                    : "Quality Index (decimal)"
                : $"Quality: {qualityindex.Value}";
        }
    }
}
