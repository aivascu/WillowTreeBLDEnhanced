using Aga.Controls.Tree;
using Microsoft.VisualBasic;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using System.Windows.Forms;
using WillowTree.Common;
using WillowTree.Controls;
using WillowTree.CustomControls;
using WillowTree.Inventory;
using WillowTree.Services.Configuration;
using WillowTree.Services.DataAccess;
using WillowTreeSharp.Domain;

namespace WillowTree.Plugins
{
    public partial class ucGears : UserControl, IPlugin
    {
        private readonly IInventoryData inventoryData;
        private PluginComponentManager pluginManager;
        private Font HighlightFont;
        private WillowSaveGame CurrentWSG;

        public InventoryTreeList GearTL;

        private string gearTextName;
        private string gearFileName;
        private int gearVisibleLine;

        public ucGears(IGameData gameData, IInventoryData inventoryData, IGlobalSettings settings, IXmlCache xmlCache, IFile file)
        {
            this.inventoryData = inventoryData;
            this.GameData = gameData;
            this.GlobalSettings = settings;
            this.XmlCache = xmlCache;
            this.File = file;
            this.InitializeComponent();
        }

        public IGameData GameData { get; }
        public IGlobalSettings GlobalSettings { get; }
        public IXmlCache XmlCache { get; }
        public IFile File { get; }

        public void InitializePlugin(PluginComponentManager pluginManager)
        {
            this.pluginManager = pluginManager;

            PluginEvents events = new PluginEvents
            {
                GameLoaded = OnGameLoaded,
                PluginCommand = OnPluginCommand
            };
            pluginManager.RegisterPlugin(this, events);

            switch (this.Text)
            {
                case "Weapons":
                    this.GearTL = new InventoryTreeList(this.GearTree, this.inventoryData.WeaponList);
                    this.gbGear.Text = "Weapon Backpack";
                    this.copyToBackpackToolStripMenuItem.Visible = false;
                    break;

                case "Items":
                    this.GearTL = new InventoryTreeList(this.GearTree, this.inventoryData.ItemList);
                    this.gbGear.Text = "Item Backpack";
                    this.copyToBackpackToolStripMenuItem.Visible = false;
                    break;

                case "Bank":
                    this.GearTL = new InventoryTreeList(this.GearTree, this.inventoryData.BankList);
                    this.gbGear.Text = "Bank";
                    this.copyToBankToolStripMenuItem.Visible = false;
                    break;
            }

            this.Init();

            // The index translators control the caption that goes over the top of each
            // level or quality SlideSelector.  Attach each translator then signal the
            // value changed event to cause the translator to update the caption.
            this.LevelIndexGear.IndexTranslator += this.LevelTranslator;
            this.QualityGear.IndexTranslator += this.QualityTranslator;
            this.LevelIndexGear.OnValueChanged(EventArgs.Empty);
            this.QualityGear.OnValueChanged(EventArgs.Empty);

            this.HighlightFont = new Font(this.GearTree.Font, FontStyle.Italic | FontStyle.Bold);

            this.Enabled = false;
        }

        private void Init()
        {
            string tabName = this.Text;

            //Section for Bank to change interface
            if (this.GearTree.SelectedNode?.GetEntry() is InventoryEntry entry)
            {
                if (entry.Type == InventoryType.Weapon)
                {
                    tabName = "Weapons";
                }
                else if (entry.Type == InventoryType.Item)
                {
                    tabName = "Items";
                }
            }

            //Config interface for Weapon, Item, Bank
            this.EquippedSlotGear.Items.Clear();
            switch (tabName)
            {
                case "Weapons":
                    this.DoGearTabs("weapon_tabs.txt");//TODO RSM Push both Weapon/Item in array (db.WpnFile) so we don't have to reload
                    this.gearTextName = "Weapon";
                    this.gearVisibleLine = 15;

                    //Change control label and text
                    this.exportGearToolStripMenuItem.Text = "Export Weapon";
                    this.GearPartsGroup.Text = "Weapon Parts";
                    this.labelGearEquipped.Text = "Equipped Slot";
                    this.labelGearQuantity.Text = "Remaining Ammo";

                    this.EquippedSlotGear.Items.AddRange(new object[] { "Unequipped", "Slot 1 (Up)", "Slot 2 (Down)", "Slot 3 (Left)", "Slot 4 (Right)" });
                    break;

                case "Items":
                    this.DoGearTabs("item_tabs.txt");
                    this.gearTextName = "Item";
                    this.gearVisibleLine = 17;

                    //Change control label and text
                    this.exportGearToolStripMenuItem.Text = "Export Item";
                    this.GearPartsGroup.Text = "Item Parts";
                    this.labelGearEquipped.Text = "Equipped";
                    this.labelGearQuantity.Text = "Quantity";

                    this.EquippedSlotGear.Items.AddRange(new object[] { "No", "Yes" });
                    break;
            }
        }

        public void ReleasePlugin()
        {
            this.LevelIndexGear.IndexTranslator -= this.LevelTranslator;
            this.QualityGear.IndexTranslator -= this.QualityTranslator;

            this.pluginManager = null;
            this.HighlightFont = null;
            this.CurrentWSG = null;

            this.GearTL = null;
        }

        public void OnGameLoaded(object sender, PluginEventArgs e)
        {
            this.CurrentWSG = e.WillowTreeMain.SaveData;
            this.GearTL.UpdateTree();

            //Config interface with WTM for Weapon, Item
            switch (this.Text)
            {
                case "Weapons":
                case "Weapons Bank":
                case "Items":
                case "Items Bank":
                    this.gearFileName = $"{this.CurrentWSG.CharacterName}'s {this.gearTextName}s";
                    break;
            }

            this.Enabled = true;
        }

        public void OnPluginCommand(object sender, PluginCommandEventArgs e)
        {
            switch (e.Command)
            {
                case PluginCommand.IncreaseNavigationDepth:
                    this.GearTL.IncreaseNavigationDepth();
                    break;

                case PluginCommand.ChangeSortMode:
                    this.GearTL.NextSort();
                    break;
            }
        }

        private void DoGearTabs(string textFile)
        {
            this.PartSelectorGear.Clear();

            string TabsLine = this.File.ReadAllText(Path.Combine(this.GameData.DataPath, textFile));
            string[] TabsList = TabsLine.Split(';');
            for (int Progress = 0; Progress < TabsList.Length; Progress++)
            {
                this.DoPartsCategory(TabsList[Progress], this.PartSelectorGear);
            }
        }

        private void NewGear_Click(object sender, EventArgs e)
        {
            this.GearTL.AddNew(this.GearTL.Unsorted.invType);//Bank AddNewWeapon/Item
            this.GearTL.AdjustSelectionAfterAdd();
            this.GearTree.EnsureVisible(this.GearTree.SelectedNode);
        }

        private void DeletePartGear_Click(object sender, EventArgs e)
        {
            if (this.PartsGear.SelectedIndex != -1)
            {
                this.PartsGear.Items[this.PartsGear.SelectedIndex] = "None";
            }
        }

        private void DeleteGear_Click(object sender, EventArgs e)
        {
            this.GearTL.DeleteSelected();
        }

        private void DuplicateGear_Click(object sender, EventArgs e)
        {
            this.GearTL.DuplicateSelected();
        }

        private void MoveGear_Click(object sender, EventArgs e)
        {
            this.GearTL.CopySelected(this.inventoryData.LockerList, true);
        }

        private void CopyLocker_Click(object sender, EventArgs e)
        {
            this.GearTL.CopySelected(this.inventoryData.LockerList, false);
        }

        private void CopyBackpack_Click(object sender, EventArgs e)
        {
            var entries = this.GearTree.SelectedNodes
                .Where(node => node.Children.Count == 0)
                .Select(x => x.GetEntry())
                .OfType<InventoryEntry>();

            foreach (var entry in entries)
            {
                if (entry.Type == InventoryType.Weapon)
                {
                    this.inventoryData.WeaponList.Duplicate(entry);
                }
                else if (entry.Type == InventoryType.Item)
                {
                    this.inventoryData.ItemList.Duplicate(entry);
                }
            }
        }

        private void CopyBank_Click(object sender, EventArgs e)
        {
            this.GearTL.CopySelected(this.inventoryData.BankList, false);
        }

        private void ClearAllGear_Click(object sender, EventArgs e)
        {
            this.GearTL.Clear();
        }

        private void PurgeDuplicatesGear_Click(object sender, EventArgs e)
        {
            this.GearTL.PurgeDuplicates();
        }

        private string ExportToTextGear()
        {
            List<string> InOutParts = new List<string>();

            for (int Progress = 0; Progress < this.PartsGear.Items.Count; Progress++)
            {
                InOutParts.Add((string)this.PartsGear.Items[Progress]);
            }

            List<int> values = InventoryEntry.CalculateValues(
                (int)this.QuantityGear.Value,
                this.QualityGear.Value,
                this.EquippedSlotGear.SelectedIndex,
                this.LevelIndexGear.Value,
                (int)this.JunkGear.Value,
                (int)this.LockedGear.Value,
                (string)this.PartsGear.Items[0]);

            int valueCount = WillowSaveGame.ExportValuesCount;
            for (int i = 0; i < valueCount; i++)
            {
                InOutParts.Add(values[i].ToString());
            }

            return $"{string.Join("\r\n", InOutParts.ToArray())}\r\n";
        }

        private void ExportToClipboardGear_Click(object sender, EventArgs e)
        {
            try
            {
                Clipboard.SetText(this.ExportToTextGear());
            }
            catch
            {
                MessageBox.Show("Export to clipboard failed.");
            }
        }

        private void ExportToFileGear_Click(object sender, EventArgs e)
        {
            WTSaveFileDialog ToFile = new WTSaveFileDialog("txt", this.GearPartsGroup.Text);
            if (ToFile.ShowDialog() == DialogResult.OK)
            {
                this.File.WriteAllText(ToFile.FileName(), this.ExportToTextGear());
            }
        }

        private void ExportToXmlGears_Click(object sender, EventArgs e)
        {
            WTSaveFileDialog fileDlg = new WTSaveFileDialog("xml", this.gearFileName);

            if (fileDlg.ShowDialog() == DialogResult.OK)
            {
                this.GearTL.SaveToXml(fileDlg.FileName());
            }
        }

        private bool ImportFromTextGear(string text)
        {
            InventoryEntry gear = InventoryEntry.ImportFromText(text, this.GearTL.Unsorted.invType);

            if (gear == null)
            {
                return false;
            }

            this.GearTL.Add(gear);
            return true;
        }

        private void ImportFromClipboardGear_Click(object sender, EventArgs e)
        {
            try
            {
                if (this.ImportFromTextGear(Clipboard.GetText()))
                {
                    this.GearTree.SelectedNode = this.GearTree.AllNodes.Last();
                    InventoryEntry gear = this.GearTree.SelectedNode.GetEntry() as InventoryEntry;
                    this.RefreshGearTree(gear);
                }
            }
            catch
            {
                MessageBox.Show($"Invalid clipboard data, {this.gearTextName} not inserted.");
            }
        }

        private void ImportFromFilesGears_Click(object sender, EventArgs e)
        {
            WTOpenFileDialog FromFile = new WTOpenFileDialog("txt", this.gearFileName);
            FromFile.Multiselect(true);

            if (FromFile.ShowDialog() == DialogResult.OK)
            {
                foreach (string file in FromFile.FileNames())
                {
                    try
                    {
                        if (this.ImportFromTextGear(this.File.ReadAllText(file)))
                        {
                            this.GearTree.SelectedNode = this.GearTree.AllNodes.Last();
                        }
                    }
                    catch (IOException)
                    {
                        MessageBox.Show($"Unable to read file \"{file}\".");
                    }
                }
            }
        }

        private void ImportAllFromXmlGears_Click(object sender, EventArgs e)
        {
            WTOpenFileDialog fileDlg = new WTOpenFileDialog("xml", this.gearFileName);

            if (fileDlg.ShowDialog() == DialogResult.OK)
            {
                this.GearTL.ImportFromXml(fileDlg.FileName(), this.GearTL.Unsorted.invType);
            }
        }

        private void GearTree_SelectionChanged(object sender, EventArgs e)
        {
            int OldPartIndex = this.PartsGear.SelectedIndex;

            this.PartsGear.Items.Clear();

            // If the node has children it not an item. It is a category label.
            if (this.GearTree.SelectedNode == null || this.GearTree.SelectedNode.Children.Count > 0)
            {
                this.GearPartsGroup.Text = $"No {this.gearTextName} Selected";
                return;
            }

            if (!(this.GearTree.SelectedNode.GetEntry() is InventoryEntry gear))
            {
                return;
            }

            this.GearPartsGroup.Text = gear.Name;

            this.Init();
            for (int i = 0; i < gear.Parts.Count; i++)
            {
                this.PartsGear.Items.Add(gear.Parts[i]);
            }

            WTSlideSelector.MinMaxAdvanced(gear.UsesBigLevel, ref this.LevelIndexGear);

            this.QuantityGear.Value = gear.Quantity;
            this.QualityGear.Value = gear.QualityIndex;
            this.EquippedSlotGear.SelectedIndex = gear.EquippedSlot;
            this.LevelIndexGear.Value = gear.LevelIndex;
            this.JunkGear.Value = gear.IsJunk;
            this.LockedGear.Value = gear.IsLocked;

            if (this.PartsGear.Items.Count > OldPartIndex)
            {
                this.PartsGear.SelectedIndex = OldPartIndex;
            }

            this.GearInformationUpdate();
        }

        private void SaveChangesGear_Click(object sender, EventArgs e)
        {
            if (this.GearTree.SelectedNode == null)
            {
                return;
            }

            // Do nothing if it is a category not an item.
            if (this.GearTree.SelectedNode.Children.Count > 0)
            {
                return;
            }

            if (this.GearTree.SelectedNode.GetEntry() is InventoryEntry gear)
            {
                this.RefreshGearTree(gear);
            }
        }

        private void RefreshGearTree(InventoryEntry gear)
        {
            for (int Progress = 0; Progress < this.PartsGear.Items.Count; Progress++)
            {
                gear.Parts[Progress] = (string)this.PartsGear.Items[Progress];
            }

            gear.UsesBigLevel = InventoryEntry.ItemgradePartUsesBigLevel((string)this.PartsGear.Items[0]);
            gear.Quantity = (int)this.QuantityGear.Value;
            gear.QualityIndex = this.QualityGear.Value;
            gear.EquippedSlot = this.EquippedSlotGear.SelectedIndex;
            gear.LevelIndex = this.LevelIndexGear.Value;
            gear.IsJunk = (int)this.JunkGear.Value;
            gear.IsLocked = (int)this.LockedGear.Value;

            // Recalculate the gear stats
            if ((this.GearTree.SelectedNode.GetEntry() as InventoryEntry).Type == InventoryType.Weapon)
            {
                gear.RecalculateDataWeapon();
            }
            else if ((this.GearTree.SelectedNode.GetEntry() as InventoryEntry).Type == InventoryType.Item)
            {
                gear.RecalculateDataItem();
            }

            gear.BuildName();

            // When the item changes, it may not belong in the same location in
            // in the sorted tree because the name, level, or other sort key
            // has changed.  Remove the node then place it back into the tree to
            // make sure it is relocated to the proper location, then select the
            // node and make sure it is visible so the user is focused on the new
            // location after the changes.
            this.GearTL.RemoveFromTreeView(this.GearTree.SelectedNode, false);
            this.GearTL.AddToTreeView(gear);
            this.GearTL.AdjustSelectionAfterAdd();
            this.GearTree.EnsureVisible(this.GearTree.SelectedNode);

            // Set the parts group page header to display the new name
            this.GearPartsGroup.Text = gear.Name;
        }

        //TODO Should be less dependant from "Colorize Lists" menu
        private void btnGearSearch_Click(object sender, EventArgs e)
        {
            string searchText = this.GearSearch.Text.ToUpper();
            IEnumerable<TreeNodeAdv> nodes = this.GearTL.Tree.AllNodes
                .Where(node => node.Children.Count == 0);
            foreach (var node in nodes)
            {
                if (node.Tag is ColoredTextNode coloredNode && node.GetEntry() is InventoryEntry entry)
                {
                    var text = entry.ToXmlText().ToUpper();
                    coloredNode.Font = searchText != "" && text.Contains(searchText)
                        ? this.HighlightFont
                        : this.GearTree.Font;
                }
            }

            this.Refresh(); //LockerTree_SelectionChanged is not needed for visual update
        }

        private void GearSearch_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                this.btnGearSearch_Click(this, EventArgs.Empty);
            }
        }

        private void EditQualityAllGears_Click(object sender, EventArgs e)
        {
            int quality;
            string qualityText = Interaction.InputBox($"All of the {this.gearTextName}s in your backpack will be adjusted to the following level:", "Edit All Qualitys", "", 10, 10);

            try
            {
                quality = Parse.AsInt(qualityText);
            }
            catch (FormatException)
            {
                return;
            }

            foreach (InventoryEntry gear in this.GearTL.Sorted)
            {
                gear.QualityIndex = quality;
            }

            this.QualityGear.Value = quality;
        }

        private void EditLevelAllGears_Click(object sender, EventArgs e)
        {
            int level;
            int levelindex;

            string levelText = Interaction.InputBox($"All of the {this.gearTextName}s in your backpack will be adjusted to the following level:", "Edit All Levels", "", 10, 10);
            try
            {
                level = Parse.AsInt(levelText);
                levelindex = level + 2;
            }
            catch (FormatException)
            {
                return;
            }

            foreach (InventoryEntry gear in this.GearTL.Sorted)
            {
                gear.EffectiveLevel = level;
                gear.NameParts[5] = $"(L{level})";
                gear.LevelIndex = levelindex;
            }

            this.GearTL.UpdateNames();

            this.LevelIndexGear.Value = levelindex;
        }

        private void PartSelectorGear_SelectionChanged(object sender, EventArgs e)
        {
            this.PartInfoGear.Clear();
            try
            {
                // Read ALL subsections of a given XML section
                var fileName = this.PartSelectorGear.SelectedNode.Parent.GetKey();
                var filePath = Path.Combine(this.GameData.DataPath, $"{fileName}.txt");
                XmlFile category = this.XmlCache.XmlFileFromCache(filePath);

                // XML Section: PartCategories.SelectedNode.Text
                List<string> xmlSection = category.XmlReadSection(this.PartSelectorGear.SelectedNode.GetText());

                this.PartInfoGear.Lines = xmlSection.ToArray();
            }
            catch { }
        }

        private void PartSelectorGear_NodeDoubleClick(object sender, MouseEventArgs e)
        {
            if (this.PartsGear.SelectedItem != null && this.PartSelectorGear.SelectedNode.Children.Count == 0)
            {
                // If part selector tracking is active it'll reposition the node
                // when the selected item is changed, so temporarily disable it.
                bool tracking = this.GlobalSettings.PartSelectorTracking;
                this.GlobalSettings.PartSelectorTracking = false;
                this.PartsGear.Items[this.PartsGear.SelectedIndex] =
                    $"{this.PartSelectorGear.SelectedNode.Parent.GetKey()}.{this.PartSelectorGear.SelectedNode.GetText()}";
                this.GlobalSettings.PartSelectorTracking = true;
            }
        }

        private void PartsGear_DoubleClick(object sender, EventArgs e)
        {
            string tempManualPart = Interaction.InputBox("Enter a new part", "Manual Edit", (string)this.PartsGear.SelectedItem, 10, 10);
            if (tempManualPart != "")
            {
                this.PartsGear.Items[this.PartsGear.SelectedIndex] = tempManualPart;
            }
        }

        private void PartsGear_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (this.PartsGear.SelectedIndex == -1)
            {
                return;
            }

            string part = this.PartsGear.SelectedItem.ToString();

            if (this.GlobalSettings.PartSelectorTracking)
            {
                this.PartSelectorGear.BeginUpdate();
                TreeNodeAdv categoryNode = this.PartSelectorGear.FindFirstNodeByTag(part.Before('.'), false);
                if (categoryNode != null)
                {
                    TreeNodeAdv partNode = categoryNode.FindFirstByTag(part.After('.'), false);
                    this.PartSelectorGear.CollapseAll();
                    this.PartSelectorGear.SelectedNode = partNode;
                    if (partNode != null)
                    {
                        CenterNode(this.PartSelectorGear.SelectedNode, this.gearVisibleLine);
                    }
                }
                this.PartSelectorGear.EndUpdate();
            }
            else
            {
                this.PartInfoGear.Clear();

                List<string> xmlSection = this.GameData.GetPartSection(part);

                if (xmlSection != null)
                {
                    this.PartInfoGear.Lines = xmlSection.ToArray();
                }
            }
        }

        private void PartsGear_SelectedValueChanged(object sender, EventArgs e)
        {
            if (this.PartsGear.SelectedIndex == 0)
            {
                if (InventoryEntry.ItemgradePartUsesBigLevel((string)this.PartsGear.Items[0]))
                {
                    this.LevelIndexGear.MaximumAdvanced = int.MaxValue;
                    this.LevelIndexGear.MinimumAdvanced = int.MinValue;
                }
                else
                {
                    this.LevelIndexGear.MaximumAdvanced = short.MaxValue;
                    this.LevelIndexGear.MinimumAdvanced = short.MinValue;
                }
            }
        }

        private void GearTree_KeyDown(object sender, KeyEventArgs e)
        {
            switch (e.KeyCode)
            {
                case Keys.Delete:
                    this.DeleteGear_Click(this, EventArgs.Empty);
                    break;

                case Keys.Insert:
                    this.DuplicateGear_Click(this, EventArgs.Empty);
                    break;

                default:
                    switch (e.KeyData)
                    {
                        case Keys.Control | Keys.B:
                            this.CopyBackpack_Click(this, EventArgs.Empty);
                            break;

                        case Keys.Control | Keys.N:
                            this.CopyBank_Click(this, EventArgs.Empty);
                            break;

                        case Keys.Control | Keys.L:
                            this.CopyLocker_Click(this, EventArgs.Empty);
                            break;
                    }

                    break;
            }
        }

        private void PartsGear_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Delete)
            {
                this.DeletePartGear_Click(this, EventArgs.Empty);
            }
        }

        private void GearInformationUpdate()
        {
            this.txtGearInformation.Clear();

            if (this.GearTree.SelectedNode?.GetEntry() is InventoryEntry entry && entry.Type == InventoryType.Weapon)
            {
                this.txtGearInformation.Text = this.GameData.WeaponInfo(entry.Parts.ToArray(), entry.QualityIndex, entry.LevelIndex);
            }
        }

        private static void CenterNode(TreeNodeAdv node, int visibleLines)
        {
            // This function will only work properly if:
            // 1) All TreeNodes have a fixed vertical height
            // 2) The tree is fully collapsed except the ancestors
            //    of this node.
            // 3) visibleLines is the actual number of nodes that can be
            //    displayed in the window vertically.
            int paddingabove = (visibleLines - 1) / 2;
            int paddingbelow = visibleLines - 1 - paddingabove;

            TreeViewAdv tree = node.Tree;
            TreeNodeAdv viewtop = node;
            while (paddingabove > 0)
            {
                if (viewtop.PreviousNode != null)
                {
                    viewtop = viewtop.PreviousNode;
                }
                else if (viewtop.Parent != null)
                {
                    viewtop = viewtop.Parent;
                }
                else
                {
                    break;
                }

                paddingabove--;
            }
            tree.EnsureVisible(viewtop);

            TreeNodeAdv viewbottom = node;
            node.Tree.EnsureVisible(node);
            while (paddingbelow > 0)
            {
                if (viewbottom.NextNode != null)
                {
                    viewbottom = viewbottom.NextNode;
                }
                else if (viewbottom.Parent?.NextNode != null)
                {
                    viewbottom = viewbottom.Parent.NextNode;
                }
                else
                {
                    break;
                }

                paddingbelow--;
            }
            tree.EnsureVisible(viewbottom);

            tree.EnsureVisible(node);
        }

        private void DoPartsCategory(string category, TreeViewAdv tree)
        {
            var filePath = Path.Combine(this.GameData.DataPath, $"{category}.txt");
            XmlFile PartList = this.XmlCache.XmlFileFromCache(filePath);
            TreeModel model = tree.Model as TreeModel;

            tree.BeginUpdate();
            tree.Model = model;

            ColoredTextNode parent = new ColoredTextNode(category)
            {
                Tag = category
            };
            model.Nodes.Add(parent);

            foreach (string section in PartList.StListSectionNames())
            {
                ColoredTextNode child = new ColoredTextNode
                {
                    Text = section,
                    Tag = section
                };
                parent.Nodes.Add(child);
            }
            tree.EndUpdate();
        }

        private string LevelTranslator(object obj)
        {
            WTSlideSelector levelindex = (WTSlideSelector)obj;

            return levelindex.InputMode == InputMode.Advanced
                ? this.GlobalSettings.UseHexInAdvancedMode
                    ? "Level Index (hexadecimal)"
                    : "Level Index (decimal)"
                : levelindex.Value == 0
                    ? "Level: Default"
                    : $"Level: {levelindex.Value - 2}";
        }

        private string QualityTranslator(object obj)
        {
            WTSlideSelector qualityindex = (WTSlideSelector)obj;

            return qualityindex.InputMode == InputMode.Advanced
                ? this.GlobalSettings.UseHexInAdvancedMode
                    ? "Quality Index (hexadecimal)"
                    : "Quality Index (decimal)"
                : $"Quality: {qualityindex.Value}";
        }
    }
}
