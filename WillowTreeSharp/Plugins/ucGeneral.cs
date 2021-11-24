using Aga.Controls.Tree;
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;
using System.Xml;
using WillowTree.Controls;
using WillowTree.Services.DataAccess;

namespace WillowTree.Plugins
{
    public partial class ucGeneral : UserControl, IPlugin
    {
        private WillowSaveGame currentWsg;
        private XmlFile locationsXml;

        public ucGeneral(IGameData gameData, IGlobalSettings globalSettings)
        {
            this.InitializeComponent();
            this.GameData = gameData;
            this.GlobalSettings = globalSettings;
        }

        public IGameData GameData { get; }
        public IGlobalSettings GlobalSettings { get; }

        public void DoWindowTitle()
        {
            this.ParentForm.Text =
                $"WillowTree# - {this.CharacterName.Text}  Level {this.Level.Value} {this.Class.Text} ({this.currentWsg.Platform})";
        }

        public void InitializePlugin(PluginComponentManager pluginManager)
        {
            PluginEvents events = new PluginEvents
            {
                GameLoaded = OnGameLoaded,
                GameSaving = OnGameSaving
            };
            pluginManager.RegisterPlugin(this, events);

            this.Enabled = false;
            this.locationsXml = this.GameData.LocationsXml;
            this.GameData.SetXPchart();
            this.DoLocationsList();
            this.Cash.Maximum = this.GlobalSettings.MaxCash;
            this.Experience.Maximum = this.GlobalSettings.MaxExperience;
            this.Level.Maximum = this.GlobalSettings.MaxLevel;
            this.BankSpace.Maximum = this.GlobalSettings.MaxBankSlots;
            this.BackpackSpace.Maximum = this.GlobalSettings.MaxBackpackSlots;
            this.SkillPoints.Maximum = this.GlobalSettings.MaxSkillPoints;
        }

        public void ReleasePlugin()
        {
            this.currentWsg = null;
            this.locationsXml = null;
        }

        private void CharacterName_TextChanged(object sender, EventArgs e)
        {
            this.DoWindowTitle();
        }

        private void Class_SelectedIndexChanged(object sender, EventArgs e)
        {
            this.UpdateClass();
            this.DoWindowTitle();
        }

        private void DeleteAllLocations()
        {
            this.currentWsg.TotalLocations = 0;
            this.currentWsg.LocationStrings = Array.Empty<string>();
            this.DoLocationTree();
        }

        private void DeleteAllLocations_Click(object sender, EventArgs e)
        {
            this.DeleteAllLocations();
        }

        private void DeleteLocation_Click(object sender, EventArgs e)
        {
            TreeNodeAdv nextSelection = null;
            while (this.LocationTree.SelectedNode != null)
            {
                int selected = this.LocationTree.SelectedNode.Index;

                this.currentWsg.TotalLocations--;
                for (int position = selected; position < this.currentWsg.TotalLocations; position++)
                {
                    this.currentWsg.LocationStrings[position] = this.currentWsg.LocationStrings[position + 1];
                }

                ArrayHelper.ResizeArraySmaller(ref this.currentWsg.LocationStrings, this.currentWsg.TotalLocations);

                nextSelection = this.LocationTree.SelectedNode.NextVisibleNode;
                this.LocationTree.SelectedNode.Remove();
            }

            if (nextSelection != null)
            {
                this.LocationTree.SelectedNode = nextSelection;
            }

            this.DoLocationTree();
        }

        private void DoLocationsList()
        {
            this.LocationsList.Items.Clear();

            foreach (string section in this.locationsXml.StListSectionNames())
            {
                string outpostName = this.locationsXml.XmlReadValue(section, "OutpostDisplayName");
                if (outpostName == "")
                {
                    outpostName = this.locationsXml.XmlReadValue(section, "OutpostName");
                }

                this.LocationsList.Items.Add(outpostName);
            }
        }

        private void DoLocationTree()
        {
            // Clear the tree
            TreeModel model = new TreeModel();
            this.LocationTree.Model = model;

            this.LocationTree.BeginUpdate();
            for (int build = 0; build < this.currentWsg.TotalLocations; build++)
            {
                string key = this.currentWsg.LocationStrings[build];
                string name = this.locationsXml.XmlReadAssociatedValue("OutpostDisplayName", "OutpostName", key);
                if (name?.Length == 0)
                {
                    name = key;
                }

                model.Nodes.Add(new ColoredTextNode(name) { Tag = key });
            }

            this.LocationTree.EndUpdate();
        }

        private void ExportAllToFileLocations_Click(object sender, EventArgs e)
        {
            WTSaveFileDialog tempSave = new WTSaveFileDialog("locations", "Default.locations");
            try
            {
                if (tempSave.ShowDialog() == DialogResult.OK)
                {
                    this.SaveToXmlLocations(tempSave.FileName());
                    MessageBox.Show($"Locations saved to {tempSave.FileName()}");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error occurred while trying to save locations: {ex.ToString()}");
            }
        }

        private void ExportSelectedToFileLocations_Click(object sender, EventArgs e)
        {
            WTSaveFileDialog tempSave = new WTSaveFileDialog("locations", "Default.locations");

            try
            {
                if (tempSave.ShowDialog() == DialogResult.OK)
                {
                    this.SaveSelectedToXmlLocations(tempSave.FileName());
                    MessageBox.Show($"Locations saved to {tempSave.FileName()}");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error occurred while trying to save locations: {ex.ToString()}");
            }
        }

        private void ImportAllFromFileLocations_Click(object sender, EventArgs e)
        {
            WTOpenFileDialog tempOpen = new WTOpenFileDialog("locations", "Default.locations");

            if (tempOpen.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    this.LoadLocations(tempOpen.FileName());
                }
                catch (ApplicationException ex)
                {
                    if (ex.Message == "NoLocations")
                    {
                        MessageBox.Show("Couldn't find a location section in the file.  Action aborted.");
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error occurred while trying to load: {ex.ToString()}");
                }
            }
        }

        private void ImportAllFromSaveLocations_Click(object sender, EventArgs e)
        {
            WTOpenFileDialog tempOpen = new WTOpenFileDialog("sav", "");

            if (tempOpen.ShowDialog() != DialogResult.OK)
            {
                return;
            }

            WillowSaveGame otherSave = new WillowSaveGame();

            try
            {
                otherSave.LoadWsg(tempOpen.FileName());
            }
            catch
            {
                MessageBox.Show("Couldn't open the other save file.");
                return;
            }

            this.currentWsg.TotalLocations = otherSave.TotalLocations;
            this.currentWsg.LocationStrings = otherSave.LocationStrings;
            this.DoLocationTree();
        }

        private void Level_ValueChanged(object sender, EventArgs e)
        {
            if (this.Level.Value > 0 && this.Level.Value < 70)
            {
                this.Experience.Minimum = this.GameData.XPChart[(int)this.Level.Value];
            }
            else
            {
                this.Experience.Minimum = 0;
            }

            this.DoWindowTitle();
        }

        private void LoadLocations(string filename)
        {
            XmlDocument doc = new XmlDocument();
            doc.Load(filename);

            if (doc.SelectSingleNode("WT/Locations") == null)
            {
                throw new ApplicationException("NoLocations");
            }

            XmlNodeList locationnodes = doc.SelectNodes("WT/Locations/Location");

            int locationcount = locationnodes.Count;
            string[] location = new string[locationcount];
            for (int i = 0; i < locationcount; i++)
            {
                location[i] = locationnodes[i].InnerText;
            }

            this.currentWsg.LocationStrings = location;
            this.currentWsg.TotalLocations = locationcount;
            this.DoLocationTree();
        }

        private void LocationsList_SelectedIndexChanged(object sender, EventArgs e)
        {
            try
            {
                int selectedItem = this.LocationsList.SelectedIndex;
                this.currentWsg.TotalLocations++;
                ArrayHelper.ResizeArrayLarger(ref this.currentWsg.LocationStrings, this.currentWsg.TotalLocations);
                this.currentWsg.LocationStrings[this.currentWsg.TotalLocations - 1] =
                    this.locationsXml.StListSectionNames()[selectedItem];
                this.DoLocationTree();
            }
            catch
            {
                // ignore
            }
        }

        private void LocationTree_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Delete)
            {
                this.DeleteLocation_Click(this, EventArgs.Empty);
            }
        }

        private void MergeAllFromFileLocations_Click(object sender, EventArgs e)
        {
            WTOpenFileDialog tempOpen = new WTOpenFileDialog("locations", "Default.locations");

            if (tempOpen.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    this.MergeAllFromXmlLocations(tempOpen.FileName());
                }
                catch (ApplicationException ex)
                {
                    if (ex.Message == "NoLocations")
                    {
                        MessageBox.Show("Couldn't find a location section in the file.  Action aborted.");
                    }
                }
                catch
                {
                    MessageBox.Show("Couldn't load the file.  Action aborted.");
                }
            }
        }

        private void MergeAllFromXmlLocations(string filename)
        {
            XmlDocument doc = new XmlDocument();
            doc.Load(filename);

            if (doc.SelectSingleNode("WT/Locations") == null)
            {
                throw new ApplicationException("NoLocations");
            }

            XmlNodeList locationNodes = doc.SelectNodes("WT/Locations/Location");
            if (locationNodes == null)
            {
                return;
            }

            // Construct a list structure with the current locations
            List<string> locations = new List<string>(this.currentWsg.LocationStrings);

            // Copy only the locations that are not duplicates from the XML file
            foreach (XmlNode node in locationNodes)
            {
                string location = node.InnerText;
                if (!locations.Contains(location))
                {
                    locations.Add(location);
                }
            }

            // Update WSG data from the newly constructed list
            this.currentWsg.LocationStrings = locations.ToArray();
            this.currentWsg.TotalLocations = locations.Count;
            this.DoLocationTree();
        }

        private void MergeFromSaveLocations(string filename)
        {
            WillowSaveGame otherSave = new WillowSaveGame();
            otherSave.LoadWsg(filename);

            if (otherSave.LocationStrings.Length == 0)
            {
                return;
            }

            // Construct a list structure with the current locations
            List<string> locations = new List<string>(this.currentWsg.LocationStrings);

            // Copy only the locations that are not duplicates from the other save
            foreach (string location in otherSave.LocationStrings)
            {
                if (!locations.Contains(location))
                {
                    locations.Add(location);
                }
            }

            // Update WSG data from the newly constructed list
            this.currentWsg.LocationStrings = locations.ToArray();
            this.currentWsg.TotalLocations = locations.Count;
            this.DoLocationTree();
        }

        private void MergeFromSaveLocations_Click(object sender, EventArgs e)
        {
            WTOpenFileDialog tempOpen = new WTOpenFileDialog("sav", "");

            if (tempOpen.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    this.MergeFromSaveLocations(tempOpen.FileName());
                }
                catch
                {
                    MessageBox.Show("Couldn't open the other save file.");
                    return;
                }

                this.DoLocationTree();
            }
        }

        private void OnGameLoaded(object sender, PluginEventArgs e)
        {
            // Warning: Setting numeric up/down controls to values that are outside their min/max
            // range will cause an exception and crash the program.  It is safest to use
            // Util.SetNumericUpDown() to set them since it will adjust the value to a valid value
            // if it is too high or low.
            this.currentWsg = e.WillowTreeMain.SaveData;

            this.CharacterName.Text = this.currentWsg.CharacterName;
            this.Level.Value = this.currentWsg.Level;
            if (this.Level.Value != this.currentWsg.Level)
            {
                MessageBox.Show(
                    $"The character's level was outside the acceptable range.  It has been adjusted.\n\nOld: {this.currentWsg.Level}\nNew: {(int)this.Level.Value}");
            }

            this.Experience.Value = this.currentWsg.Experience;
            if (this.Experience.Value != this.currentWsg.Experience)
            {
                MessageBox.Show(
                    $"The character's experience was outside the acceptable range.  It has been adjusted.\n\nOld: {this.currentWsg.Experience}\nNew: {(int)this.Experience.Value}");
            }

            this.SkillPoints.Value = this.currentWsg.SkillPoints;
            if (this.SkillPoints.Value != this.currentWsg.SkillPoints)
            {
                MessageBox.Show(
                    $"The character's skill point count was outside the acceptable range.  It has been adjusted.\n\nOld: {this.currentWsg.SkillPoints}\nNew: {(int)this.SkillPoints.Value}");
            }

            this.PT2Unlocked.SelectedIndex = this.currentWsg.FinishedPlaythrough1 == 0 ? 0 : 1;

            // No message when cash is adjusted because it will likely have to be changed on
            // every load for people who exceed the limit.  The spam would be annoying.
            this.Cash.Value = this.currentWsg.Cash < 0 ? int.MaxValue : this.currentWsg.Cash;

            this.BackpackSpace.Value = this.currentWsg.BackpackSize;
            if (this.BackpackSpace.Value != this.currentWsg.BackpackSize)
            {
                MessageBox.Show(
                    $"The character's backpack capacity was outside the acceptable range.  It has been adjusted.\n\nOld: {this.currentWsg.BackpackSize}\nNew: {(int)this.BackpackSpace.Value}");
            }

            this.EquipSlots.Value = this.currentWsg.EquipSlots;
            this.SaveNumber.Value = this.currentWsg.SaveNumber;

            this.UI_UpdateCurrentLocationComboBox(this.currentWsg.CurrentLocation);

            switch (this.currentWsg.Class)
            {
                case "gd_Roland.Character.CharacterClass_Roland":
                    this.Class.SelectedIndex = 0;
                    break;

                case "gd_lilith.Character.CharacterClass_Lilith":
                    this.Class.SelectedIndex = 1;
                    break;

                case "gd_mordecai.Character.CharacterClass_Mordecai":
                    this.Class.SelectedIndex = 2;
                    break;

                case "gd_Brick.Character.CharacterClass_Brick":
                    this.Class.SelectedIndex = 3;
                    break;
            }

            // If DLC section 1 is not present then the bank does not exist, so disable the
            // control to prevent the user from editing its size.
            this.labelGeneralBankSpace.Enabled = this.currentWsg.Dlc.HasSection1;
            this.BankSpace.Enabled = this.currentWsg.Dlc.HasSection1;
            if (this.currentWsg.Dlc.HasSection1)
            {
                this.BankSpace.Value = this.currentWsg.Dlc.BankSize;
                if (this.BankSpace.Value != this.currentWsg.Dlc.BankSize)
                {
                    MessageBox.Show(
                        $"The character's bank capacity was outside the acceptable range.  It has been adjusted.\n\nOld: {this.currentWsg.BackpackSize}\nNew: {(int)this.BackpackSpace.Value}");
                }
            }
            else
            {
                this.BankSpace.Value = 0;
            }

            this.DoWindowTitle();
            Application.DoEvents();
            this.DoLocationTree();
            this.Enabled = true;
        }

        private void OnGameSaving(object sender, PluginEventArgs e)
        {
            if (this.BankSpace.Enabled)
            {
                this.currentWsg.Dlc.BankSize = (int)this.BankSpace.Value;
            }

            // TODO: Most of these values that are being set in GameSaving should
            // be set right away with events when the values change in order to
            // play nicely with other plugins.  There is the potential for this
            // plugin to change a value and another plugin may not be aware of the
            // change because it only gets applied at save time the way it works now.
            this.currentWsg.CharacterName = this.CharacterName.Text;
            this.currentWsg.Level = (int)this.Level.Value;
            this.currentWsg.Experience = (int)this.Experience.Value;
            this.currentWsg.SkillPoints = (int)this.SkillPoints.Value;
            this.currentWsg.FinishedPlaythrough1 = this.PT2Unlocked.SelectedIndex;
            this.currentWsg.Cash = (int)this.Cash.Value;
            this.currentWsg.BackpackSize = (int)this.BackpackSpace.Value;
            this.currentWsg.EquipSlots = (int)this.EquipSlots.Value;
            this.currentWsg.SaveNumber = (int)this.SaveNumber.Value;

            // Try to look up the outpost name from the text that is displayed in the combo box.
            string currentLocation = this.locationsXml.XmlReadAssociatedValue(
                "OutpostName",
                "OutpostDisplayName",
                (string)this.CurrentLocation.SelectedItem);

            // If the outpost name is not found then this location is not in the data file
            // so the string stored in CurrentLocation is already the outpost name.
            if (currentLocation == "")
            {
                currentLocation = (string)this.CurrentLocation.SelectedItem;
            }

            this.currentWsg.CurrentLocation = currentLocation;
        }

        private void SaveSelectedToXmlLocations(string filename)
        {
            XmlTextWriter writer = new XmlTextWriter(filename, Encoding.UTF8)
            {
                Formatting = Formatting.Indented,
                Indentation = 2
            };
            writer.WriteStartDocument();
            writer.WriteComment("WillowTree Location File");
            writer.WriteComment("Note: the XML tags are case sensitive");
            writer.WriteStartElement("WT");
            writer.WriteStartElement("Locations");

            foreach (TreeNodeAdv nodeAdv in this.LocationTree.SelectedNodes)
            {
                string name = nodeAdv.GetKey();
                if (!string.IsNullOrEmpty(name))
                {
                    writer.WriteElementString("Location", name);
                }
            }

            writer.WriteEndDocument();
            writer.Close();
        }

        private void SaveToXmlLocations(string filename)
        {
            XmlTextWriter writer = new XmlTextWriter(filename, Encoding.UTF8)
            {
                Formatting = Formatting.Indented,
                Indentation = 2
            };
            writer.WriteStartDocument();
            writer.WriteComment("WillowTree Location File");
            writer.WriteComment("Note: the XML tags are case sensitive");
            writer.WriteStartElement("WT");
            writer.WriteStartElement("Locations");
            for (int i = 0; i < this.currentWsg.TotalLocations; i++)
            {
                writer.WriteElementString("Location", this.currentWsg.LocationStrings[i]);
            }

            writer.WriteEndDocument();
            writer.Close();
        }

        private void UI_UpdateCurrentLocationComboBox(string locationToSelect)
        {
            this.CurrentLocation.Items.Clear();
            this.CurrentLocation.Items.Add("None");

            // See if the selected location can be found in the WT# data file.
            string loc = this.locationsXml.XmlReadAssociatedValue("OutpostDisplayName", "OutpostName", locationToSelect);
            if (loc == "")
            {
                // Not in the data file, so an entry must be added or the combo
                // box won't even be able to display the selected location.
                if (locationToSelect != "None")
                {
                    this.CurrentLocation.Items.Add(locationToSelect);
                }

                loc = locationToSelect;
            }

            // Add all the location entries that were in the WT# location file
            foreach (string location in this.LocationsList.Items)
            {
                this.CurrentLocation.Items.Add(location);
            }

            this.CurrentLocation.SelectedItem = loc;
        }

        private void UpdateClass()
        {
            switch (this.Class.SelectedIndex)
            {
                case 0:
                    this.currentWsg.Class = "gd_Roland.Character.CharacterClass_Roland";
                    break;

                case 1:
                    this.currentWsg.Class = "gd_lilith.Character.CharacterClass_Lilith";
                    break;

                case 2:
                    this.currentWsg.Class = "gd_mordecai.Character.CharacterClass_Mordecai";
                    break;

                case 3:
                    this.currentWsg.Class = "gd_Brick.Character.CharacterClass_Brick";
                    break;
            }
        }
    }
}
