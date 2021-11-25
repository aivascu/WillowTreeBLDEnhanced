using Aga.Controls.Tree;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using WillowTree.Controls;
using WillowTree.CustomControls;
using WillowTree.Inventory;
using WillowTree.Services.DataAccess;
using WillowTreeSharp.Domain;

namespace WillowTree.Plugins
{
    public partial class ucLocker : UserControl, IPlugin
    {
        private PluginComponentManager pluginManager;
        private Font highlightFont;

        public InventoryTreeList lockerTl;

        public ucLocker()
        {
            this.InitializeComponent();
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

            this.pluginManager = pm;

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

            this.highlightFont = new Font(this.LockerTree.Font, FontStyle.Italic | FontStyle.Bold);

            this.ImportAllFromItems.Enabled = false;
            this.ImportAllFromWeapons.Enabled = false;

            this.lockerTl = new InventoryTreeList(this.LockerTree, GameData.LockerList);

            string lockerFilename = GameData.OpenedLockerFilename();
            if (!File.Exists(lockerFilename))
            {
                GameData.OpenedLockerFilename($"{GameData.DataPath}default.xml");
            }

            try
            {
                this.LoadLocker(GameData.OpenedLockerFilename());
                this.lockerTl.UpdateTree();
            }
            catch (ApplicationException)
            {
                MessageBox.Show(
                    $"The locker file \"{GameData.OpenedLockerFilename()} could not be loaded.  It may be corrupt.  If you delete or rename it the program will make a new one and you may be able to start the program successfully.  Shutting down now.");
                Application.Exit();
            }
        }

        public void ReleasePlugin()
        {
            this.QualityLocker.IndexTranslator -= QualityTranslator;
            this.LevelIndexLocker.IndexTranslator -= LevelTranslator;
            this.LevelIndexOverride.IndexTranslator -= LevelTranslator;
            this.QualityOverride.IndexTranslator -= QualityTranslator;

            this.pluginManager = null;
            this.highlightFont = null;

            this.lockerTl = null;
            GameData.OpenedLockerFilename(null);
        }

        public void OnGameLoaded(object sender, PluginEventArgs e)
        {
            this.ImportAllFromItems.Enabled = true;
            this.ImportAllFromWeapons.Enabled = true;
            this.lockerTl.UpdateTree();
        }

        public void OnGameSaving(object sender, PluginEventArgs e)
        {
            this.lockerTl.SaveToXml(GameData.OpenedLockerFilename());
        }

        public void OnPluginCommand(object sender, PluginCommandEventArgs e)
        {
            if (e.Command == PluginCommand.IncreaseNavigationDepth)
            {
                this.lockerTl.IncreaseNavigationDepth();
            }
            else if (e.Command == PluginCommand.ChangeSortMode)
            {
                this.lockerTl.NextSort();
            }
        }

        public void LoadLocker(string inputFile)
        {
            this.lockerTl.Tree.BeginUpdate();
            this.lockerTl.Clear();
            this.lockerTl.ImportFromXml(inputFile, InventoryType.Any);
            this.lockerTl.Tree.EndUpdate();
        }

        private void NewWeaponLocker_Click(object sender, EventArgs e)
        {
            this.lockerTl.AddNew(InventoryType.Weapon);
            this.lockerTl.AdjustSelectionAfterAdd();
            this.LockerTree.EnsureVisible(this.LockerTree.SelectedNode);
        }

        private void NewItemLocker_Click(object sender, EventArgs e)
        {
            this.lockerTl.AddNew(InventoryType.Item);
            this.lockerTl.AdjustSelectionAfterAdd();
            this.LockerTree.EnsureVisible(this.LockerTree.SelectedNode);
        }

        private void DeleteLocker_Click(object sender, EventArgs e)
        {
            this.lockerTl.DeleteSelected();
        }

        private void DuplicateLocker_Click(object sender, EventArgs e)
        {
            this.lockerTl.DuplicateSelected();
        }

        private void MoveLocker_Click(object sender, EventArgs e)
        {
            // Copy the selection because it will change as nodes are removed.
            TreeNodeAdv[] nodes = this.LockerTree.SelectedNodes.ToArray();

            foreach (TreeNodeAdv node in nodes)
            {
                if (node.Children.Count == 0)
                {
                    InventoryEntry entry = node.GetEntry() as InventoryEntry;
                    if (entry.Type == InventoryType.Weapon)
                    {
                        GameData.WeaponList.Add(entry);
                    }
                    else if (entry.Type == InventoryType.Item)
                    {
                        GameData.ItemList.Add(entry);
                    }

                    this.lockerTl.Remove(node, false);
                }
            }
        }

        private void CopyBackpack_Click(object sender, EventArgs e)
        {
            foreach (TreeNodeAdv node in this.LockerTree.SelectedNodes)
            {
                if (node.Children.Count == 0)
                {
                    InventoryEntry entry = node.GetEntry() as InventoryEntry;
                    if (entry.Type == InventoryType.Weapon)
                    {
                        GameData.WeaponList.Duplicate(entry);
                    }
                    else if (entry.Type == InventoryType.Item)
                    {
                        GameData.ItemList.Duplicate(entry);
                    }
                }
            }
        }

        private void CopyBank_Click(object sender, EventArgs e)
        {
            this.lockerTl.CopySelected(GameData.BankList, false);
        }

        private void ClearAllLocker_Click(object sender, EventArgs e)
        {
            this.lockerTl.Clear();
        }

        private void PurgeDuplicatesLocker_Click(object sender, EventArgs e)
        {
            this.lockerTl.PurgeDuplicates();
        }

        private string ExportToTextLocker()
        {
            List<string> inOutParts = this.PartsLocker.Items.Cast<string>().ToList();

            List<int> values = null;

            if (this.OverrideExportSettings.Checked)
            {
                values = InventoryEntry.CalculateValues((int)this.RemAmmoOverride.Value,
                    this.QualityOverride.Value, 0, this.LevelIndexOverride.Value, (int)this.JunkLocker.Value, (int)this.LockedLocker.Value, ((string)this.PartsLocker.Items[0]));
            }
            else
            {
                values = InventoryEntry.CalculateValues((int)this.RemAmmoLocker.Value,
                    this.QualityLocker.Value, 0, this.LevelIndexLocker.Value, (int)this.JunkLocker.Value, (int)this.LockedLocker.Value, ((string)this.PartsLocker.Items[0]));
            }

            for (int i = 0; i < WillowSaveGame.ExportValuesCount; i++)
            {
                inOutParts.Add(values[i].ToString());
            }

            return $"{string.Join("\r\n", inOutParts.ToArray())}\r\n";
        }

        private void ExportToClipboardLocker_Click(object sender, EventArgs e)
        {
            try
            {
                Clipboard.SetText(this.ExportToTextLocker());
            }
            catch
            {
                MessageBox.Show("Export to clipboard failed.");
            }
        }

        private void ExportToFileLocker_Click(object sender, EventArgs e)
        {
            WTSaveFileDialog toFile = new WTSaveFileDialog("txt", this.LockerPartsGroup.Text);
            if (toFile.ShowDialog() == DialogResult.OK)
            {
                File.WriteAllText(toFile.FileName(), this.ExportToTextLocker());
            }
        }

        private void ExportToXmlLocker_Click(object sender, EventArgs e)
        {
            WTSaveFileDialog fileDlg = new WTSaveFileDialog("xml", GameData.OpenedLockerFilename());

            if (fileDlg.ShowDialog() == DialogResult.OK)
            {
                this.lockerTl.SaveToXml(fileDlg.FileName());
            }
        }

        private bool ImportFromTextLocker(string text)
        {
            InventoryEntry gear = InventoryEntry.ImportFromText(text, InventoryType.Unknown);

            if (gear == null)
            {
                return false;
            }

            this.lockerTl.Add(gear);
            return true;
        }

        private void ImportFromClipboardLocker_Click(object sender, EventArgs e)
        {
            try
            {
                if (this.ImportFromTextLocker(Clipboard.GetText()))
                {
                    this.LockerTree.SelectedNode = this.LockerTree.AllNodes.Last();
                }
            }
            catch
            {
                MessageBox.Show("Invalid clipboard data.  Import failed.");
            }
        }

        private void ImportFromFilesLocker_Click(object sender, EventArgs e)
        {
            WTOpenFileDialog fromFile = new WTOpenFileDialog("txt", "");
            fromFile.Multiselect(true);

            if (fromFile.ShowDialog() == DialogResult.OK)
            {
                foreach (string file in fromFile.FileNames())
                {
                    try
                    {
                        if (this.ImportFromTextLocker(File.ReadAllText(file)))
                        {
                            this.LockerTree.SelectedNode = this.LockerTree.AllNodes.Last();
                        }
                    }
                    catch (IOException)
                    {
                        MessageBox.Show($"Unable to read file \"{file}\".");
                    }
                }
            }
        }

        private void ImportAllFromXmlLocker_Click(object sender, EventArgs e)
        {
            WTOpenFileDialog fileDlg = new WTOpenFileDialog("xml", "");
            if (fileDlg.ShowDialog() == DialogResult.OK)
            {
                this.lockerTl.ImportFromXml(fileDlg.FileName(), InventoryType.Any);
                this.lockerTl.SaveToXml(fileDlg.FileName());
            }
        }

        private void ImportAllFromItemsLocker_Click(object sender, EventArgs e)
        {
            this.LockerTree.BeginUpdate();
            foreach (InventoryEntry item in GameData.ItemList.Items.Values)
            {
                this.lockerTl.Duplicate(item);
            }

            this.LockerTree.EndUpdate();
            this.lockerTl.SaveToXml(GameData.OpenedLockerFilename());
        }

        private void ImportAllFromWeaponsLocker_Click(object sender, EventArgs e)
        {
            this.LockerTree.BeginUpdate();
            foreach (InventoryEntry weapon in GameData.WeaponList.Items.Values)
            {
                this.lockerTl.Duplicate(weapon);
            }

            this.LockerTree.EndUpdate();
            this.lockerTl.SaveToXml(GameData.OpenedLockerFilename());
        }

        private void LockerTree_SelectionChanged(object sender, EventArgs e)
        {
            this.PartsLocker.Items.Clear();

            if (this.LockerTree.SelectedNode == null || this.LockerTree.SelectedNode.Children.Count > 0)
            {
                return;
            }

            InventoryEntry entry = this.LockerTree.SelectedNode.GetEntry() as InventoryEntry;

            if (this.LockerTree.SelectedNode.Children.Count == 0)
            {   // Tree nodes with no children are items or weapons.  Entries with children would be section headers.
                string selectedItem = this.LockerTree.SelectedNode.Data().Text;

                this.LockerPartsGroup.Text = entry.Name;
                this.RatingLocker.Text = entry.Rating;
                this.DescriptionLocker.Text = entry.Description.Replace("$LINE$", "\r\n");

                int partcount = entry.GetPartCount();

                for (int progress = 0; progress < partcount; progress++)
                {
                    this.PartsLocker.Items.Add(entry.Parts[progress]);
                }

                WTSlideSelector.MinMaxAdvanced(entry.UsesBigLevel, ref this.LevelIndexLocker);

                this.RemAmmoLocker.Value = entry.Quantity;
                this.QualityLocker.Value = entry.QualityIndex;
                this.LevelIndexLocker.Value = entry.LevelIndex;
                this.JunkLocker.Value = entry.IsJunk;
                this.LockedLocker.Value = entry.IsLocked;
            }
        }

        private void SaveChangesLocker_Click(object sender, EventArgs e)
        {
            if (this.LockerTree.SelectedNode == null)
            {
                return;
            }

            if (this.LockerTree.SelectedNode.Children.Count > 0)
            {
                return;
            }

            InventoryEntry entry = this.LockerTree.SelectedNode.GetEntry() as InventoryEntry;

            int partcount = entry.GetPartCount();

            for (int progress = 0; progress < partcount; progress++)
            {
                entry.Parts[progress] = (string)this.PartsLocker.Items[progress];
            }

            entry.UsesBigLevel = InventoryEntry.ItemgradePartUsesBigLevel((string)this.PartsLocker.Items[0]);

            entry.Quantity = (int)this.RemAmmoLocker.Value;
            entry.QualityIndex = this.QualityLocker.Value;
            entry.EquippedSlot = 0;
            entry.LevelIndex = this.LevelIndexLocker.Value;
            entry.IsJunk = (int)this.JunkLocker.Value;
            entry.IsLocked = (int)this.LockedLocker.Value;

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
                entry.Name = $"Invalid ItemType ({entry.Type})";
            }

            // When the item changes, it may not belong in the same location in
            // in the sorted tree because the name, level, or other sort key
            // has changed.  Remove the node then place it back into the tree to
            // make sure it is relocated to the proper location, then select the
            // node and make sure it is visible so the user is focused on the new
            // location after the changes.
            this.lockerTl.RemoveFromTreeView(this.LockerTree.SelectedNode, false);
            this.lockerTl.AddToTreeView(entry);
            this.lockerTl.AdjustSelectionAfterAdd();
            this.LockerTree.EnsureVisible(this.LockerTree.SelectedNode);

            this.LockerPartsGroup.Text = entry.Name;
            this.LockerTree.Focus();
        }

        //TODO Should be less dependant from "Colorize Lists" menu
        private void btnlockerSearch_Click(object sender, EventArgs e)
        {
            string searchText = this.lockerSearch.Text.ToUpper();
            string text = "";

            foreach (TreeNodeAdv node in this.lockerTl.Tree.AllNodes)
            {
                if (node.Children.Count == 0)
                {
                    text = (node.GetEntry() as InventoryEntry).ToXmlText().ToUpper();

                    if (searchText != "" && text.Contains(searchText))
                    {
                        (node.Tag as ColoredTextNode).Font = this.highlightFont;
                    }
                    else
                    {
                        (node.Tag as ColoredTextNode).Font = this.LockerTree.Font;
                    }
                }
            }
            this.Refresh(); //LockerTree_SelectionChanged is not needed for visual update
        }

        private void lockerSearch_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                this.btnlockerSearch_Click(this, EventArgs.Empty);
            }
        }

        private void OpenLocker_Click(object sender, EventArgs e)
        {
            WTOpenFileDialog fromFile = new WTOpenFileDialog("xml", GameData.OpenedLockerFilename());

            try
            {
                if (fromFile.ShowDialog() != DialogResult.OK)
                {
                    return;
                }
                this.LoadLocker(fromFile.FileName());
                GameData.OpenedLockerFilename(fromFile.FileName());
            }
            catch
            {
                MessageBox.Show("Could not load the selected WillowTree Locker.");
            }
        }

        private void LockerTree_KeyDown(object sender, KeyEventArgs e)
        {
            switch (e.KeyCode)
            {
                case Keys.Delete:
                    this.DeleteLocker_Click(this, EventArgs.Empty);
                    break;
                case Keys.Insert:
                    this.DuplicateLocker_Click(this, EventArgs.Empty);
                    break;
                default:
                {
                    switch (e.KeyData)
                    {
                        case Keys.Control | Keys.B:
                                this.CopyBackpack_Click(this, EventArgs.Empty);
                            break;
                        case Keys.Control | Keys.N:
                                this.CopyBank_Click(this, EventArgs.Empty);
                            break;
                    }

                    break;
                }
            }
        }

        private static string LevelTranslator(object obj)
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

        private static string QualityTranslator(object obj)
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
