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
            InitializeComponent();
            GameData = gameData;
            GlobalSettings = globalSettings;
        }

        public IGameData GameData { get; }
        public IGlobalSettings GlobalSettings { get; }

        public void DoWindowTitle()
        {
            ParentForm.Text =
                $"WillowTree# - {CharacterName.Text}  Level {Level.Value} {Class.Text} ({currentWsg.Platform})";
        }

        public void InitializePlugin(PluginComponentManager pluginManager)
        {
            PluginEvents events = new PluginEvents
            {
                GameLoaded = OnGameLoaded,
                GameSaving = OnGameSaving
            };
            pluginManager.RegisterPlugin(this, events);

            Enabled = false;
            locationsXml = GameData.LocationsXml;
            GameData.SetXPchart();
            DoLocationsList();
            Cash.Maximum = GlobalSettings.MaxCash;
            Experience.Maximum = GlobalSettings.MaxExperience;
            Level.Maximum = GlobalSettings.MaxLevel;
            BankSpace.Maximum = GlobalSettings.MaxBankSlots;
            BackpackSpace.Maximum = GlobalSettings.MaxBackpackSlots;
            SkillPoints.Maximum = GlobalSettings.MaxSkillPoints;
        }

        public void ReleasePlugin()
        {
            currentWsg = null;
            locationsXml = null;
        }

        private void CharacterName_TextChanged(object sender, EventArgs e)
        {
            DoWindowTitle();
        }

        private void Class_SelectedIndexChanged(object sender, EventArgs e)
        {
            UpdateClass();
            DoWindowTitle();
        }

        private void DeleteAllLocations()
        {
            currentWsg.TotalLocations = 0;
            currentWsg.LocationStrings = Array.Empty<string>();
            DoLocationTree();
        }

        private void DeleteAllLocations_Click(object sender, EventArgs e)
        {
            DeleteAllLocations();
        }

        private void DeleteLocation_Click(object sender, EventArgs e)
        {
            TreeNodeAdv nextSelection = null;
            while (LocationTree.SelectedNode != null)
            {
                int selected = LocationTree.SelectedNode.Index;

                currentWsg.TotalLocations--;
                for (int position = selected; position < currentWsg.TotalLocations; position++)
                {
                    currentWsg.LocationStrings[position] = currentWsg.LocationStrings[position + 1];
                }

                ArrayHelper.ResizeArraySmaller(ref currentWsg.LocationStrings, currentWsg.TotalLocations);

                nextSelection = LocationTree.SelectedNode.NextVisibleNode;
                LocationTree.SelectedNode.Remove();
            }

            if (nextSelection != null)
            {
                LocationTree.SelectedNode = nextSelection;
            }

            DoLocationTree();
        }

        private void DoLocationsList()
        {
            LocationsList.Items.Clear();

            foreach (string section in locationsXml.StListSectionNames())
            {
                string outpostName = locationsXml.XmlReadValue(section, "OutpostDisplayName");
                if (outpostName == "")
                {
                    outpostName = locationsXml.XmlReadValue(section, "OutpostName");
                }

                LocationsList.Items.Add(outpostName);
            }
        }

        private void DoLocationTree()
        {
            // Clear the tree
            TreeModel model = new TreeModel();
            LocationTree.Model = model;

            LocationTree.BeginUpdate();
            for (int build = 0; build < currentWsg.TotalLocations; build++)
            {
                string key = currentWsg.LocationStrings[build];
                string name = locationsXml.XmlReadAssociatedValue("OutpostDisplayName", "OutpostName", key);
                if (name?.Length == 0)
                {
                    name = key;
                }

                model.Nodes.Add(new ColoredTextNode(name) { Tag = key });
            }

            LocationTree.EndUpdate();
        }

        private void ExportAllToFileLocations_Click(object sender, EventArgs e)
        {
            WTSaveFileDialog tempSave = new WTSaveFileDialog("locations", "Default.locations");
            try
            {
                if (tempSave.ShowDialog() == DialogResult.OK)
                {
                    SaveToXmlLocations(tempSave.FileName());
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
                    SaveSelectedToXmlLocations(tempSave.FileName());
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
                    LoadLocations(tempOpen.FileName());
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

            currentWsg.TotalLocations = otherSave.TotalLocations;
            currentWsg.LocationStrings = otherSave.LocationStrings;
            DoLocationTree();
        }

        private void Level_ValueChanged(object sender, EventArgs e)
        {
            if (Level.Value > 0 && Level.Value < 70)
            {
                Experience.Minimum = GameData.XPChart[(int)Level.Value];
            }
            else
            {
                Experience.Minimum = 0;
            }

            DoWindowTitle();
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

            currentWsg.LocationStrings = location;
            currentWsg.TotalLocations = locationcount;
            DoLocationTree();
        }

        private void LocationsList_SelectedIndexChanged(object sender, EventArgs e)
        {
            try
            {
                int selectedItem = LocationsList.SelectedIndex;
                currentWsg.TotalLocations++;
                ArrayHelper.ResizeArrayLarger(ref currentWsg.LocationStrings, currentWsg.TotalLocations);
                currentWsg.LocationStrings[currentWsg.TotalLocations - 1] =
                    locationsXml.StListSectionNames()[selectedItem];
                DoLocationTree();
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
                DeleteLocation_Click(this, EventArgs.Empty);
            }
        }

        private void MergeAllFromFileLocations_Click(object sender, EventArgs e)
        {
            WTOpenFileDialog tempOpen = new WTOpenFileDialog("locations", "Default.locations");

            if (tempOpen.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    MergeAllFromXmlLocations(tempOpen.FileName());
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
            List<string> locations = new List<string>(currentWsg.LocationStrings);

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
            currentWsg.LocationStrings = locations.ToArray();
            currentWsg.TotalLocations = locations.Count;
            DoLocationTree();
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
            List<string> locations = new List<string>(currentWsg.LocationStrings);

            // Copy only the locations that are not duplicates from the other save
            foreach (string location in otherSave.LocationStrings)
            {
                if (!locations.Contains(location))
                {
                    locations.Add(location);
                }
            }

            // Update WSG data from the newly constructed list
            currentWsg.LocationStrings = locations.ToArray();
            currentWsg.TotalLocations = locations.Count;
            DoLocationTree();
        }

        private void MergeFromSaveLocations_Click(object sender, EventArgs e)
        {
            WTOpenFileDialog tempOpen = new WTOpenFileDialog("sav", "");

            if (tempOpen.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    MergeFromSaveLocations(tempOpen.FileName());
                }
                catch
                {
                    MessageBox.Show("Couldn't open the other save file.");
                    return;
                }

                DoLocationTree();
            }
        }

        private void OnGameLoaded(object sender, PluginEventArgs e)
        {
            // Warning: Setting numeric up/down controls to values that are outside their min/max
            // range will cause an exception and crash the program.  It is safest to use
            // Util.SetNumericUpDown() to set them since it will adjust the value to a valid value
            // if it is too high or low.
            currentWsg = e.WillowTreeMain.SaveData;

            CharacterName.Text = currentWsg.CharacterName;
            Level.Value = currentWsg.Level;
            if (Level.Value != currentWsg.Level)
            {
                MessageBox.Show(
                    $"The character's level was outside the acceptable range.  It has been adjusted.\n\nOld: {currentWsg.Level}\nNew: {(int)Level.Value}");
            }

            Experience.Value = currentWsg.Experience;
            if (Experience.Value != currentWsg.Experience)
            {
                MessageBox.Show(
                    $"The character's experience was outside the acceptable range.  It has been adjusted.\n\nOld: {currentWsg.Experience}\nNew: {(int)Experience.Value}");
            }

            SkillPoints.Value = currentWsg.SkillPoints;
            if (SkillPoints.Value != currentWsg.SkillPoints)
            {
                MessageBox.Show(
                    $"The character's skill point count was outside the acceptable range.  It has been adjusted.\n\nOld: {currentWsg.SkillPoints}\nNew: {(int)SkillPoints.Value}");
            }

            PT2Unlocked.SelectedIndex = currentWsg.FinishedPlaythrough1 == 0 ? 0 : 1;

            // No message when cash is adjusted because it will likely have to be changed on
            // every load for people who exceed the limit.  The spam would be annoying.
            Cash.Value = currentWsg.Cash < 0 ? int.MaxValue : currentWsg.Cash;

            BackpackSpace.Value = currentWsg.BackpackSize;
            if (BackpackSpace.Value != currentWsg.BackpackSize)
            {
                MessageBox.Show(
                    $"The character's backpack capacity was outside the acceptable range.  It has been adjusted.\n\nOld: {currentWsg.BackpackSize}\nNew: {(int)BackpackSpace.Value}");
            }

            EquipSlots.Value = currentWsg.EquipSlots;
            SaveNumber.Value = currentWsg.SaveNumber;

            UI_UpdateCurrentLocationComboBox(currentWsg.CurrentLocation);

            switch (currentWsg.Class)
            {
                case "gd_Roland.Character.CharacterClass_Roland":
                    Class.SelectedIndex = 0;
                    break;

                case "gd_lilith.Character.CharacterClass_Lilith":
                    Class.SelectedIndex = 1;
                    break;

                case "gd_mordecai.Character.CharacterClass_Mordecai":
                    Class.SelectedIndex = 2;
                    break;

                case "gd_Brick.Character.CharacterClass_Brick":
                    Class.SelectedIndex = 3;
                    break;
            }

            // If DLC section 1 is not present then the bank does not exist, so disable the
            // control to prevent the user from editing its size.
            labelGeneralBankSpace.Enabled = currentWsg.Dlc.HasSection1;
            BankSpace.Enabled = currentWsg.Dlc.HasSection1;
            if (currentWsg.Dlc.HasSection1)
            {
                BankSpace.Value = currentWsg.Dlc.BankSize;
                if (BankSpace.Value != currentWsg.Dlc.BankSize)
                {
                    MessageBox.Show(
                        $"The character's bank capacity was outside the acceptable range.  It has been adjusted.\n\nOld: {currentWsg.BackpackSize}\nNew: {(int)BackpackSpace.Value}");
                }
            }
            else
            {
                BankSpace.Value = 0;
            }

            DoWindowTitle();
            Application.DoEvents();
            DoLocationTree();
            Enabled = true;
        }

        private void OnGameSaving(object sender, PluginEventArgs e)
        {
            if (BankSpace.Enabled)
            {
                currentWsg.Dlc.BankSize = (int)BankSpace.Value;
            }

            // TODO: Most of these values that are being set in GameSaving should
            // be set right away with events when the values change in order to
            // play nicely with other plugins.  There is the potential for this
            // plugin to change a value and another plugin may not be aware of the
            // change because it only gets applied at save time the way it works now.
            currentWsg.CharacterName = CharacterName.Text;
            currentWsg.Level = (int)Level.Value;
            currentWsg.Experience = (int)Experience.Value;
            currentWsg.SkillPoints = (int)SkillPoints.Value;
            currentWsg.FinishedPlaythrough1 = PT2Unlocked.SelectedIndex;
            currentWsg.Cash = (int)Cash.Value;
            currentWsg.BackpackSize = (int)BackpackSpace.Value;
            currentWsg.EquipSlots = (int)EquipSlots.Value;
            currentWsg.SaveNumber = (int)SaveNumber.Value;

            // Try to look up the outpost name from the text that is displayed in the combo box.
            string currentLocation = locationsXml.XmlReadAssociatedValue(
                "OutpostName",
                "OutpostDisplayName",
                (string)CurrentLocation.SelectedItem);

            // If the outpost name is not found then this location is not in the data file
            // so the string stored in CurrentLocation is already the outpost name.
            if (currentLocation == "")
            {
                currentLocation = (string)CurrentLocation.SelectedItem;
            }

            currentWsg.CurrentLocation = currentLocation;
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

            foreach (TreeNodeAdv nodeAdv in LocationTree.SelectedNodes)
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
            for (int i = 0; i < currentWsg.TotalLocations; i++)
            {
                writer.WriteElementString("Location", currentWsg.LocationStrings[i]);
            }

            writer.WriteEndDocument();
            writer.Close();
        }

        private void UI_UpdateCurrentLocationComboBox(string locationToSelect)
        {
            CurrentLocation.Items.Clear();
            CurrentLocation.Items.Add("None");

            // See if the selected location can be found in the WT# data file.
            string loc = locationsXml.XmlReadAssociatedValue("OutpostDisplayName", "OutpostName", locationToSelect);
            if (loc == "")
            {
                // Not in the data file, so an entry must be added or the combo
                // box won't even be able to display the selected location.
                if (locationToSelect != "None")
                {
                    CurrentLocation.Items.Add(locationToSelect);
                }

                loc = locationToSelect;
            }

            // Add all the location entries that were in the WT# location file
            foreach (string location in LocationsList.Items)
            {
                CurrentLocation.Items.Add(location);
            }

            CurrentLocation.SelectedItem = loc;
        }

        private void UpdateClass()
        {
            switch (Class.SelectedIndex)
            {
                case 0:
                    currentWsg.Class = "gd_Roland.Character.CharacterClass_Roland";
                    break;

                case 1:
                    currentWsg.Class = "gd_lilith.Character.CharacterClass_Lilith";
                    break;

                case 2:
                    currentWsg.Class = "gd_mordecai.Character.CharacterClass_Mordecai";
                    break;

                case 3:
                    currentWsg.Class = "gd_Brick.Character.CharacterClass_Brick";
                    break;
            }
        }
    }
}
