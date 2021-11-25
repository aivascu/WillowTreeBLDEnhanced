using Aga.Controls.Tree;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Xml;
using WillowTree.Controls;
using WillowTree.Services.DataAccess;
using WillowTreeSharp;
using WillowTreeSharp.Domain;

namespace WillowTree.Plugins
{
    public partial class UcQuests : UserControl, IPlugin
    {
        public static string questSearchKey;
        private bool clicked = false;
        private WillowSaveGame currentWsg;
        private XmlFile questsXml;

        public UcQuests()
        {
            this.InitializeComponent();
        }

        public void DeleteAllQuests(int index)
        {
            QuestTable qt = this.currentWsg.QuestLists[index];
            qt.Quests.Clear();
            qt.TotalQuests = 0;

            this.QuestTree.BeginUpdate();
            foreach (TreeNodeAdv child in this.QuestTree.Root.Children[index].Children.ToArray())
            {
                child.Remove();
            }

            string startQuest = "Z0_Missions.Missions.M_IntroStateSaver";
            this.AddQuestByName(startQuest, index);
            qt.CurrentQuest = startQuest;

            this.QuestTree.EndUpdate();
        }

        public void DoQuestList()
        {
            foreach (string section in this.questsXml.StListSectionNames())
            {
                this.QuestList.Items.Add(this.questsXml.XmlReadValue(section, "MissionName"));
            }
        }

        public void DoQuestTree()
        {
            this.QuestTree.BeginUpdate();

            // Make a new quest tree or clear the old one
            if (this.QuestTree.Model == null)
            {
                this.QuestTree.Model = new TreeModel();
            }
            else
            {
                this.QuestTree.Clear();
            }

            TreeModel model = this.QuestTree.Model as TreeModel;

            for (int listIndex = 0; listIndex < this.currentWsg.NumberOfQuestLists; listIndex++)
            {
                // Create the category node for this playthrough
                // Quest tree category nodes:
                //     Text = human readable quest category ("Playthrough 1 Quests", etc)
                //     Tag = quest list index as string (0 based)
                ColoredTextNode parent = new ColoredTextNode
                {
                    Text = $"Playthrough {(listIndex + 1)} Quests",
                    Tag = listIndex.ToString()
                };
                model.Nodes.Add(parent);

                QuestTable qt = this.currentWsg.QuestLists[listIndex];
                // Create all the actual quest nodes for this playthrough
                //     Text = human readable quest name
                //     Tag = internal quest name
                for (int questIndex = 0; questIndex < qt.TotalQuests; questIndex++)
                {
                    string nodeName = qt.Quests[questIndex].Name;

                    ColoredTextNode node = new ColoredTextNode
                    {
                        Tag = nodeName,
                        Text = this.questsXml.XmlReadValue(nodeName, "MissionName")
                    };
                    if (node.Text == "")
                    {
                        node.Text = $"({nodeName})";
                    }

                    parent.Nodes.Add(node);
                }
            }
            this.QuestTree.EndUpdate();
        }

        public void InitializePlugin(PluginComponentManager pm)
        {
            PluginEvents events = new PluginEvents
            {
                GameLoaded = OnGameLoaded
            };
            pm.RegisterPlugin(this, events);

            this.questsXml = GameData.QuestsXml;

            this.Enabled = false;
            this.DoQuestList();
        }

        public void LoadQuests(string filename, int index)
        {
            XmlDocument doc = new XmlDocument();
            doc.Load(filename);

            if (doc.SelectSingleNode("WT/Quests") == null)
            {
                throw new ApplicationException("NoQuests");
            }

            XmlNodeList questnodes = doc.SelectNodes("WT/Quests/Quest");

            int count = questnodes.Count;

            QuestTable questTable = this.currentWsg.QuestLists[index];
            questTable.Quests.Clear();
            questTable.TotalQuests = count;

            this.QuestTree.BeginUpdate();

            TreeNodeAdv parent = this.QuestTree.Root.Children[index];

            // Remove the old entries from the tree view
            foreach (TreeNodeAdv child in parent.Children.ToArray())
            {
                child.Remove();
            }

            for (int nodeIndex = 0; nodeIndex < count; nodeIndex++)
            {
                XmlNode node = questnodes[nodeIndex];
                QuestEntry questEntry = new QuestEntry
                {
                    Name = node.GetElement("Name", ""),
                    Progress = node.GetElementAsInt("Progress", 0),
                    DlcValue1 = node.GetElementAsInt("DLCValue1", 0),
                    DlcValue2 = node.GetElementAsInt("DLCValue2", 0)
                };

                int objectiveCount = node.GetElementAsInt("Objectives", 0);
                questEntry.NumberOfObjectives = objectiveCount;
                questEntry.Objectives = new QuestObjective[objectiveCount];

                for (int objectiveIndex = 0; objectiveIndex < objectiveCount; objectiveIndex++)
                {
                    questEntry.Objectives[objectiveIndex].Description = node.GetElement($"FolderName{objectiveIndex}", "");
                    questEntry.Objectives[objectiveIndex].Progress = node.GetElementAsInt($"FolderValue{objectiveIndex}", 0);
                }
                questTable.Quests.Add(questEntry);

                // Add the quest to the tree view
                ColoredTextNode treeNode = new ColoredTextNode
                {
                    Tag = questEntry.Name,
                    Text = this.questsXml.XmlReadValue(questEntry.Name, "MissionName")
                };
                if (treeNode.Text == "")
                {
                    treeNode.Text = $"{$"({treeNode.Tag}"})";
                }

                this.QuestTree.Root.Children[index].AddNode(treeNode);
            }

            // TODO: The current quest is not currently stored in a quest file.
            // It should be stored when the entire list is stored and restored
            // when the list is loaded here.
            questTable.CurrentQuest = "";

            this.QuestTree.EndUpdate();
        }

        public void MergeAllFromXmlQuests(string filename, int index)
        {
            XmlDocument doc = new XmlDocument();
            doc.Load(filename);

            if (doc.SelectSingleNode("/WT/Quests") == null)
            {
                throw new ApplicationException("NoQuests");
            }

            XmlNodeList questnodes = doc.SelectNodes("/WT/Quests/Quest");
            if (questnodes == null)
            {
                return;
            }

            QuestTable qt = this.currentWsg.QuestLists[index];

            this.QuestTree.BeginUpdate();
            // Copy only the quests that are not duplicates from the XML file
            foreach (XmlNode node in questnodes)
            {
                string name = node.GetElement("Name", "");
                int progress = node.GetElementAsInt("Progress", 0);

                // Check to see if the quest is already in the list
                questSearchKey = name;
                int prevIndex = qt.Quests.FindIndex(this.QuestSearchByName);
                if (prevIndex != -1)
                {
                    // This quest entry exists in both lists.  If the progress is
                    // not greater then don't do anything with it.
                    QuestEntry old = qt.Quests[prevIndex];
                    if (progress <= old.Progress)
                    {
                        continue;
                    }

                    // This quest progress is further advanced than the existing one
                    // so copy all its values.
                    old.Progress = progress;
                    old.DlcValue1 = node.GetElementAsInt("DLCValue1", 0);
                    old.DlcValue2 = node.GetElementAsInt("DLCValue2", 0);

                    int newObjectiveCount = node.GetElementAsInt("Objectives", 0);
                    old.NumberOfObjectives = newObjectiveCount;
                    old.Objectives = new QuestObjective[newObjectiveCount];

                    for (int objectiveIndex = 0; objectiveIndex < newObjectiveCount; objectiveIndex++)
                    {
                        old.Objectives[objectiveIndex].Description = node.GetElement($"FolderName{objectiveIndex}", "");
                        old.Objectives[objectiveIndex].Progress = node.GetElementAsInt($"FolderValue{objectiveIndex}", 0);
                    }

                    // The quest doesn't need to be added to the quest list since we
                    // modified an existing entry.  The tree view doesn't need to be
                    // changed because the name and text should still be the same.
                    continue;
                }

                // Create a new quest entry from the quest's xml node data
                QuestEntry qe = new QuestEntry
                {
                    Name = name,
                    Progress = progress,
                    DlcValue1 = node.GetElementAsInt("DLCValue1", 0),
                    DlcValue2 = node.GetElementAsInt("DLCValue2", 0)
                };

                int objectiveCount = node.GetElementAsInt("Objectives", 0);
                qe.NumberOfObjectives = objectiveCount;
                qe.Objectives = new QuestObjective[objectiveCount];

                for (int objectiveIndex = 0; objectiveIndex < objectiveCount; objectiveIndex++)
                {
                    qe.Objectives[objectiveIndex].Description = node.GetElement($"FolderName{objectiveIndex}", "");
                    qe.Objectives[objectiveIndex].Progress = node.GetElementAsInt($"FolderValue{objectiveIndex}", 0);
                }

                // Add the quest entry to the quest list
                qt.Quests.Add(qe);
                qt.TotalQuests++;

                // Add the quest to the tree view
                ColoredTextNode treeNode = new ColoredTextNode
                {
                    Tag = name,
                    Text = this.questsXml.XmlReadValue(name, "MissionName")
                };
                if (treeNode.Text == "")
                {
                    treeNode.Text = $"({name})";
                }

                this.QuestTree.Root.Children[index].AddNode(treeNode);
            }
            this.QuestTree.EndUpdate();

            // In case the operation modified the currently selected quest, refresh
            // the quest group panel by signalizing the selection changed event.
            this.QuestTree_SelectionChanged(null, null);
        }

        public void MergeFromSaveQuests(string filename, int index)
        {
            var otherSave = WillowSaveGameBase.ReadFile(filename);

            if (otherSave.NumberOfQuestLists - 1 < index)
            {
                return;
            }

            QuestTable qtOther = otherSave.QuestLists[index];
            QuestTable qt = this.currentWsg.QuestLists[index];

            this.QuestTree.BeginUpdate();
            foreach (QuestEntry qe in otherSave.QuestLists[index].Quests)
            {
                string name = qe.Name;
                int progress = qe.Progress;

                // Check to see if the quest is already in the list
                questSearchKey = name;
                int prevIndex = qt.Quests.FindIndex(this.QuestSearchByName);
                if (prevIndex != -1)
                {
                    // This quest entry exists in both lists.  If the progress is
                    // not greater then don't do anything with it.
                    QuestEntry old = qt.Quests[prevIndex];
                    if (progress < old.Progress)
                    {
                        continue;
                    }

                    if (progress == old.Progress)
                    {
                        // If the progress of the quest is the same, there may be
                        // individual objectives that don't have the same progress
                        // so check and update them.
                        int objectiveCount = qe.NumberOfObjectives;

                        // The number of objectives should be the same with the same
                        // level of progress.  If they aren't then there's something
                        // wrong so just ignore the new quest and keep the old.
                        if (objectiveCount != old.NumberOfObjectives)
                        {
                            continue;
                        }

                        for (int i = 0; i < objectiveCount; i++)
                        {
                            int objectiveProgress = old.Objectives[i].Progress;
                            if (qe.Objectives[i].Progress < objectiveProgress)
                            {
                                qe.Objectives[i].Progress = objectiveProgress;
                            }
                        }
                    }

                    // This quest progress is further advanced than the existing one
                    // so replace the existing one in the list.
                    qt.Quests[prevIndex] = qe;

                    // The quest doesn't need to be added to the quest list since we
                    // modified an existing entry.  The tree view doesn't need to be
                    // changed because the name and text should still be the same.
                    continue;
                }

                // Add the quest entry to the quest list
                qt.Quests.Add(qe);
                qt.TotalQuests++;

                // Add the quest to the tree view
                ColoredTextNode treeNode = new ColoredTextNode
                {
                    Tag = qe.Name,
                    Text = this.questsXml.XmlReadValue(qe.Name, "MissionName")
                };
                if (treeNode.Text == "")
                {
                    treeNode.Text = $"{$"({treeNode.Tag}"})";
                }

                this.QuestTree.Root.Children[index].AddNode(treeNode);
            }
            this.QuestTree.EndUpdate();

            // In case the operation modified the currently selected quest, refresh
            // the quest group panel by signalizing the selection changed event.
            this.QuestTree_SelectionChanged(null, null);
        }

        public bool MultipleIntroStateSaver(int playthroughIndex)
        {
            QuestTable questTable = this.currentWsg.QuestLists[playthroughIndex];
            int questCount = questTable.TotalQuests;

            int totalFound = 0;
            for (int questIndex = 0; questIndex < questCount; questIndex++)
            {
                if (questTable.Quests[questIndex].Name == "Z0_Missions.Missions.M_IntroStateSaver")
                {
                    totalFound++;
                }
            }

            return totalFound > 1;
        }

        public void OnGameLoaded(object sender, PluginEventArgs e)
        {
            this.currentWsg = e.WillowTreeMain.SaveData;
            this.DoQuestTree();

            int index = this.GetSelectedQuestList();
            if (index != -1)
            {
                this.ActiveQuest.Text = this.currentWsg.QuestLists[index].CurrentQuest;
            }

            this.Enabled = true;
        }

        public bool QuestSearchByName(QuestEntry qe)
        {
            return qe.Name == questSearchKey;
        }

        public void ReleasePlugin()
        {
            this.currentWsg = null;
            this.questsXml = null;
        }

        private void ActiveQuest_TextChanged(object sender, EventArgs e)
        {
            int index = this.GetSelectedQuestList();
            if (index != -1)
            {
                this.currentWsg.QuestLists[index].CurrentQuest = this.ActiveQuest.Text;
            }
        }

        private void AddListQuests_Click(object sender, EventArgs e)
        {
            int index = this.currentWsg.NumberOfQuestLists;

            // Create an empty quest table
            QuestTable qt = new QuestTable
            {
                Index = index,
                TotalQuests = 0,
                Quests = new List<QuestEntry>()
            };

            // Add the new table to the list
            this.currentWsg.QuestLists.Add(qt);
            this.currentWsg.NumberOfQuestLists++;

            this.QuestTree.BeginUpdate();

            //Add the new table to the tree view
            ColoredTextNode categoryNode = new ColoredTextNode
            {
                Text = $"Playthrough {(index + 1)} Quests",
                Tag = index.ToString()
            };
            (this.QuestTree.Model as TreeModel).Nodes.Add(categoryNode);

            // Add Fresh Off the Bus (the first quest) to the table
            string startQuest = "Z0_Missions.Missions.M_IntroStateSaver";
            this.AddQuestByName(startQuest, index);
            qt.CurrentQuest = startQuest;
            this.QuestTree.EndUpdate();
        }

        private void AddQuestByName(string name, int index)
        {
            QuestEntry qe = new QuestEntry
            {
                Name = name,
                // is added to the data files these should be changed.
                // contain the values they should be yet, so once that data
                // missions that are from the DLCs.  The data files dont
                // TODO: These should not always be zero.  They are non-zero for
                DlcValue1 = 0,
                DlcValue2 = 0
            };

            List<QuestObjective> objectives = new List<QuestObjective>();

            int objectiveCount;

            for (objectiveCount = 0; ; objectiveCount++)
            {
                QuestObjective objective;
                string desc = this.questsXml.XmlReadValue(qe.Name, $"Objectives{objectiveCount}");
                if (desc == "")
                {
                    break;
                }

                objective.Description = desc;
                objective.Progress = 0;
                objectives.Add(objective);
            }

            qe.NumberOfObjectives = objectiveCount;
            qe.Objectives = objectives.ToArray();
            if (objectiveCount > 0)
            {
                qe.Progress = 1;
            }
            else
            {
                qe.Progress = 2;
            }

            // Add the quest entry to the quest list
            QuestTable qt = this.currentWsg.QuestLists[index];
            qt.Quests.Add(qe);
            qt.TotalQuests++;

            // Add the quest entry to the tree view
            ColoredTextNode treeNode = new ColoredTextNode
            {
                Tag = name,
                Text = this.questsXml.XmlReadValue(name, "MissionName")
            };
            if (treeNode.Text == "")
            {
                treeNode.Text = $"({name})";
            }

            this.QuestTree.Root.Children[index].AddNode(treeNode);
        }

        private void ClearQuests_Click(object sender, EventArgs e)
        {
            int index = this.GetSelectedQuestList();
            if (index == -1)
            {
                MessageBox.Show("Select a quest list first.");
                return;
            }

            this.DeleteAllQuests(index);
        }

        private void DeleteQuest_Click(object sender, EventArgs e)
        {
            TreeNodeAdv nextSelection = null;

            int index = this.GetSelectedQuestList();

            // Get out if it is a category node or doesn't have a valid quest list index.
            if (index == -1 || this.QuestTree.SelectedNode.Parent == this.QuestTree.Root)
            {
                MessageBox.Show("Select one or more quests to delete first.");
                return;
            }

            // If there's only one node selected, it is the first quest,
            // and there's no other copy of the first quest in that quest
            // list then give the user a message letting him know he can't
            // remove it.  If he selects it in a group of quests then it will
            // just be silently ignored in the removal loop.
            if (this.QuestTree.SelectedNodes.Count == 1 && this.QuestTree.SelectedNode.GetText() == "Fresh Off The Bus" && !this.MultipleIntroStateSaver(index))
            {
                MessageBox.Show("You must have the default quest.");
                return;
            }

            foreach (TreeNodeAdv nodeAdv in this.QuestTree.SelectedNodes.ToArray())
            {
                if (nodeAdv.GetText() == "Fresh Off The Bus" && !this.MultipleIntroStateSaver(index))
                {
                    nodeAdv.IsSelected = false;
                    nextSelection = nodeAdv;
                    continue;
                }

                nextSelection = nodeAdv.NextVisibleNode;
                this.DeleteQuestEntry(index, nodeAdv.Index);
                nodeAdv.Remove();
            }
            if (nextSelection != null)
            {
                this.QuestTree.SelectedNode = nextSelection;
            }
        }

        private void DeleteQuestEntry(int listIndex, int entryIndex)
        {
            QuestTable qt = this.currentWsg.QuestLists[listIndex];
            qt.TotalQuests--;
            qt.Quests.RemoveAt(entryIndex);
        }

        private void ExportSelectedQuests_Click(object sender, EventArgs e)
        {
            int index = this.GetSelectedQuestList();
            if (index == -1)
            {
                MessageBox.Show("Select a single quest list or select the quests to export first.");
                return;
            }

            WTSaveFileDialog tempSave = new WTSaveFileDialog("quests",
                $"{this.currentWsg.CharacterName}.PT{(index + 1)}.quests");

            try
            {
                if (tempSave.ShowDialog() == DialogResult.OK)
                {
                    this.SaveSelectedToXmlQuests(tempSave.FileName(), index);
                    MessageBox.Show($"Quests saved to {tempSave.FileName()}");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error occurred while trying to save locations: {ex}");
            }
        }

        private void ExportToFileQuests_Click(object sender, EventArgs e)
        {
            int index = this.GetSelectedQuestList();
            if (index == -1)
            {
                MessageBox.Show("Select a playthrough to export first.");
                return;
            }

            try
            {
                WTSaveFileDialog tempExport = new WTSaveFileDialog("quests",
                    $"{this.currentWsg.CharacterName}.PT{(index + 1)}.quests");

                if (tempExport.ShowDialog() == DialogResult.OK)
                {
                    this.SaveToXmlQuests(tempExport.FileName(), index);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Export failed:\r\n{ex}");
            }
        }

        private int GetSelectedQuestList()
        {
            int index = -1;

            if (this.QuestTree.SelectedNode == null)
            {
                // Do nothing, fall through to return -1 for failure.
            }
            else if (this.QuestTree.SelectedNode.Parent != this.QuestTree.Root)
            {
                index = Parse.AsInt(this.QuestTree.SelectedNode.Parent.GetKey(), -1);
            }
            else
            {
                // This is a category node not a quest.  If there is exactly one
                // selected then choose it as the location for import, otherwise
                // fall through and return -1 for failure.
                if (this.QuestTree.SelectedNodes.Count == 1)
                {
                    index = Parse.AsInt(this.QuestTree.SelectedNode.GetKey());
                }
            }
            return index;
        }

        private void ImportFromFileQuests_Click(object sender, EventArgs e)
        {
            int index = this.GetSelectedQuestList();
            if (index == -1)
            {
                MessageBox.Show("Select a playthrough to import to first.");
                return;
            }

            WTOpenFileDialog tempImport = new WTOpenFileDialog("quests",
                $"{this.currentWsg.CharacterName}.PT{(index + 1)}.quests");

            if (tempImport.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    this.LoadQuests(tempImport.FileName(), index);
                }
                catch (ApplicationException ex)
                {
                    if (ex.Message == "NoQuests")
                    {
                        MessageBox.Show("Couldn't find a quests section in the file.  Action aborted.");
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error occurred while trying to load: {ex}");
                }
            }
        }

        private void ImportFromSaveQuests_Click(object sender, EventArgs e)
        {
            int index = this.GetSelectedQuestList();
            if (index == -1)
            {
                MessageBox.Show("Select a playthrough to import to first.");
                return;
            }

            WTOpenFileDialog tempOpen = new WTOpenFileDialog("sav", "");

            if (tempOpen.ShowDialog() == DialogResult.OK)
            {
                WillowSaveGame otherSave;

                try
                {
                    otherSave = WillowSaveGameBase.ReadFile(tempOpen.FileName());
                }
                catch { return; }

                if (otherSave.NumberOfQuestLists - 1 < index)
                {
                    MessageBox.Show("This quest list does not exist in the other savegame file.");
                    return;
                }

                // Note that when you set lists equal to one another like this it doesn't copy
                // the elements, only the pointer to the list.  This is only safe here because
                // OtherSave will be disposed of right away and not modify the values.  If OtherSave
                // was being used actively then a new copy of all the elements in the quest list
                // would have to be made or else changes to one would affect the quest
                // list of the other.

                // Replace the old entries in the quest table with the new ones
                this.currentWsg.QuestLists[index] = otherSave.QuestLists[index];

                QuestTable qt = this.currentWsg.QuestLists[index];

                this.QuestTree.BeginUpdate();
                TreeNodeAdv parent = this.QuestTree.Root.Children[index];

                // Remove the old entries from the tree view
                foreach (TreeNodeAdv child in parent.Children.ToArray())
                {
                    child.Remove();
                }

                // Add the new entries to the tree view
                foreach (QuestEntry qe in qt.Quests)
                {
                    string nodeName = qe.Name;

                    ColoredTextNode node = new ColoredTextNode
                    {
                        Tag = nodeName,
                        Text = this.questsXml.XmlReadValue(nodeName, "MissionName")
                    };
                    if (node.Text == "")
                    {
                        node.Text = $"{$"({node.Tag}"})";
                    }

                    parent.AddNode(node);
                }
                this.QuestTree.EndUpdate();
            }
        }

        private void MergeAllFromFileQuests_Click(object sender, EventArgs e)
        {
            int index = this.GetSelectedQuestList();
            if (index == -1)
            {
                MessageBox.Show("Select a single quest list to import to first.");
                return;
            }

            WTOpenFileDialog tempOpen = new WTOpenFileDialog("quests", "Default.quests");
            if (tempOpen.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    this.MergeAllFromXmlQuests(tempOpen.FileName(), index);
                }
                catch (ApplicationException ex)
                {
                    if (ex.Message == "NoQuests")
                    {
                        MessageBox.Show("Couldn't find a quests section in the file.  Action aborted.");
                    }
                }
                catch
                {
                    MessageBox.Show("Couldn't load the file.  Action aborted.");
                }
            }
        }

        private void MergeFromSaveQuests_Click(object sender, EventArgs e)
        {
            int index = this.GetSelectedQuestList();
            if (index == -1)
            {
                MessageBox.Show("Select a single quest list to import to first.");
                return;
            }

            WTOpenFileDialog tempOpen = new WTOpenFileDialog("sav", "");

            if (tempOpen.ShowDialog() == DialogResult.OK)
            {
                WillowSaveGame otherSave = new WillowSaveGame();

                try
                {
                    this.MergeFromSaveQuests(tempOpen.FileName(), index);
                }
                catch
                {
                    MessageBox.Show("Couldn't open the other save file.");
                }
            }
        }

        private void Objectives_SelectedIndexChanged(object sender, EventArgs e)
        {
            int index = this.GetSelectedQuestList();

            // Do nothing if the quest list index is invalid or it is a
            // category node not a quest node.
            if (index == -1 || this.QuestTree.SelectedNode.Parent == this.QuestTree.Root)
            {
                return;
            }

            if (this.clicked)
            {
                this.ObjectiveValue.Value = this.currentWsg.QuestLists[index].Quests[this.QuestTree.SelectedNode.Index].Objectives[this.Objectives.SelectedIndex].Progress;
            }
        }

        private void ObjectiveValue_ValueChanged(object sender, EventArgs e)
        {
            int index = this.GetSelectedQuestList();

            // Do nothing if the quest list index is invalid or it is a
            // category node not a quest node.
            if (index == -1 || this.QuestTree.SelectedNode.Parent == this.QuestTree.Root)
            {
                return;
            }

            if (this.Objectives.Items.Count > 0 && this.clicked)
            {
                this.currentWsg.QuestLists[index].Quests[this.QuestTree.SelectedNode.Index].Objectives[this.Objectives.SelectedIndex].Progress = (int)this.ObjectiveValue.Value;
            }
        }

        private void QuestDLCValue1_ValueChanged(object sender, EventArgs e)
        {
            int index = this.GetSelectedQuestList();
            if (index == -1 || this.QuestTree.SelectedNode.Parent == this.QuestTree.Root)
            {
                return;
            }

            this.currentWsg.QuestLists[index].Quests[this.QuestTree.SelectedNode.Index].DlcValue1 = (int)this.QuestDLCValue1.Value;
        }

        private void QuestDLCValue2_ValueChanged(object sender, EventArgs e)
        {
            int index = this.GetSelectedQuestList();
            if (index == -1 || this.QuestTree.SelectedNode.Parent == this.QuestTree.Root)
            {
                return;
            }

            this.currentWsg.QuestLists[index].Quests[this.QuestTree.SelectedNode.Index].DlcValue2 = (int)this.QuestDLCValue2.Value;
        }

        private void QuestList_SelectedIndexChanged(object sender, EventArgs e)
        {
            int index = this.GetSelectedQuestList();
            if (index == -1)
            {
                MessageBox.Show("Select a playthrough to add to first.");
                return;
            }

            int selectedItem = this.QuestList.SelectedIndex;
            this.NewQuest.HideDropDown();
            try
            {
                if (selectedItem != -1)
                {
                    QuestTable questTable = this.currentWsg.QuestLists[index];

                    List<string> sectionNames = this.questsXml.StListSectionNames();

                    QuestEntry questEntry = new QuestEntry();
                    string name = sectionNames[selectedItem];
                    questEntry.Name = name;
                    questEntry.Progress = 1;
                    // TODO: These should not always be zero.  They are non-zero for
                    // missions that are from the DLCs.  The data files dont
                    // contain the values they should be yet, so once that data
                    // is added to the data files these should be changed.
                    questEntry.DlcValue1 = 0;
                    questEntry.DlcValue2 = 0;

                    List<QuestObjective> objectives = new List<QuestObjective>();

                    int objectiveCount;

                    XmlNode questXmlNode = this.questsXml.XmlReadNode(name);
                    System.Diagnostics.Debug.Assert(questXmlNode != null);

                    for (objectiveCount = 0; ; objectiveCount++)
                    {
                        QuestObjective objective;

                        string desc = questXmlNode.GetElement($"Objectives{objectiveCount}", "");
                        if (desc == "")
                        {
                            break;
                        }

                        objective.Description = desc;
                        objective.Progress = 0;
                        objectives.Add(objective);
                    }

                    questEntry.NumberOfObjectives = objectiveCount;
                    questEntry.Objectives = objectives.ToArray();

                    questTable.Quests.Add(questEntry);
                    questTable.TotalQuests++;

                    ColoredTextNode treeNode = new ColoredTextNode
                    {
                        Tag = name,
                        Text = questXmlNode.GetElement("MissionName", "")
                    };

                    if (treeNode.Text == "")
                    {
                        treeNode.Text = $"({name})";
                    }

                    TreeNodeAdv parent = this.QuestTree.Root.Children[index];
                    parent.AddNode(treeNode);
                    this.QuestTree.SelectedNode = parent.Children[parent.Children.Count - 1];
                }
            }
            catch { }
        }

        private void QuestProgress_Click(object sender, EventArgs e)
        {
            this.clicked = true;
        }

        private void QuestProgress_SelectedIndexChanged(object sender, EventArgs e)
        {
            int index = this.GetSelectedQuestList();
            if (index == -1 || this.QuestTree.SelectedNode.Parent == this.QuestTree.Root)
            {
                return;
            }

            try
            {
                QuestTable qt = this.currentWsg.QuestLists[index];
                if (this.clicked)
                {
                    QuestEntry qe = qt.Quests[this.QuestTree.SelectedNode.Index];

                    if (qe.Progress == 4 && this.QuestProgress.SelectedIndex < 3)
                    {
                        // The quest was marked as turned in before and now will not
                        // be complete.  The objective list has to be re-added since it is
                        // removed when the quest is turned in.
                        this.Objectives.Items.Clear();

                        List<QuestObjective> objectives = new List<QuestObjective>();

                        XmlNode questXmlNode = this.questsXml.XmlReadNode(qe.Name);

                        int objectiveCount;
                        for (objectiveCount = 0; ; objectiveCount++)
                        {
                            QuestObjective objective;
                            string desc = questXmlNode.GetElement($"Objectives{objectiveCount}", "");
                            if (desc == "")
                            {
                                break;
                            }

                            objective.Description = desc;
                            objective.Progress = 0;
                            objectives.Add(objective);
                        }

                        qe.NumberOfObjectives = objectiveCount;
                        qe.Objectives = objectives.ToArray();
                        qe.Progress = this.QuestProgress.SelectedIndex;

                        if (objectiveCount > 0)
                        {
                            for (int objectiveIndex = 0; objectiveIndex < objectiveCount; objectiveIndex++)
                            {
                                this.Objectives.Items.Add(qe.Objectives[objectiveIndex].Description);
                            }
                        }

                        // Update the UI elements
                        this.NumberOfObjectives.Value = objectiveCount;
                        this.ObjectiveValue.Value = 0;
                    }
                    else if (qe.Progress != 4 && this.QuestProgress.SelectedIndex == 3)
                    {
                        // The quest was not marked as turned in but now it will be.  Clear the
                        // objective list since turned in quests no longer should have one.
                        this.Objectives.Items.Clear();
                        qe.Objectives = Array.Empty<QuestObjective>();
                        qe.Progress = 4;
                        qe.NumberOfObjectives = 0;
                    }
                    else
                    {
                        qe.Progress = this.QuestProgress.SelectedIndex;
                    }
                }
            }
            catch { }
        }

        private void QuestTree_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Delete)
            {
                this.DeleteQuest_Click(this, EventArgs.Empty);
            }
        }

        private void QuestTree_SelectionChanged(object sender, EventArgs e)
        {
            this.clicked = false;

            int index = this.GetSelectedQuestList();
            this.UpdateActiveQuestList(index);

            // If a quest node is not selected reset the UI elements and exit
            if (index == -1 || this.QuestTree.SelectedNode.Parent == this.QuestTree.Root)
            {
                this.UiClearQuestPanel();
                return;
            }

            try
            {
                QuestEntry qe = this.currentWsg.QuestLists[index].Quests[this.QuestTree.SelectedNode.Index];

                this.SelectedQuestGroup.Text = this.QuestTree.SelectedNode.GetText();
                string key = this.QuestTree.SelectedNode.GetKey();
                this.QuestString.Text = key;

                if (qe.Progress > 2)
                {
                    this.QuestProgress.SelectedIndex = 3;
                }
                else
                {
                    this.QuestProgress.SelectedIndex = qe.Progress;
                }

                int objectiveCount = qe.NumberOfObjectives;
                this.NumberOfObjectives.Value = objectiveCount;

                XmlNode questData = this.questsXml.XmlReadNode(key);
                this.Objectives.Items.Clear();
                for (int objectiveIndex = 0; objectiveIndex < objectiveCount; objectiveIndex++)
                {
                    this.Objectives.Items.Add(questData.GetElement($"Objectives{objectiveIndex}", ""));
                }

                this.ObjectiveValue.Value = 0;
                this.QuestSummary.Text = questData.GetElement("MissionSummary", "");
                this.QuestDescription.Text = questData.GetElement("MissionDescription", "");
                this.QuestDLCValue1.Value = qe.DlcValue1;
                this.QuestDLCValue2.Value = qe.DlcValue2;
            }
            catch
            {
                // Blank out all the user elements if there is any kind of exception
                // while trying to set them.
                this.UiClearQuestPanel();
            }
        }

        private void RemoveListQuests_Click(object sender, EventArgs e)
        {
            TreeNodeAdv[] selection = this.QuestTree.SelectedNodes.ToArray();

            this.QuestTree.BeginUpdate();
            foreach (TreeNodeAdv nodeAdv in selection)
            {
                if (nodeAdv.Parent == this.QuestTree.Root)
                {
                    this.currentWsg.NumberOfQuestLists--;
                    this.currentWsg.QuestLists.RemoveAt(nodeAdv.Index);
                    this.QuestTree.Root.Children[nodeAdv.Index].Remove();
                }
            }

            // The indexes will be messed up if a list that is not the last one is
            // removed, so update the tree text, tree indexes, and quest list indices
            int count = this.currentWsg.NumberOfQuestLists;
            for (int index = 0; index < count; index++)
            {
                TreeNodeAdv nodeAdv = this.QuestTree.Root.Children[index];

                // Adjust the category node's text and tag to reflect its new position
                ColoredTextNode parent = nodeAdv.Data();
                parent.Text = $"Playthrough {(index + 1)} Quests";
                parent.Tag = index.ToString();

                // Adjust the quest list index to reflect its new position
                this.currentWsg.QuestLists[index].Index = index;
            }
            this.QuestTree.EndUpdate();
        }

        private void SaveSelectedToXmlQuests(string filename, int index)
        {
            TreeNodeAdv[] selected;

            // There are two valid ways a user can select nodes to save to xml.
            // He can choose exactly one category node or he can choose multiple
            // quest nodes.  Figure out which and create an array of the nodes.
            if (this.QuestTree.SelectedNode.Parent == this.QuestTree.Root && this.QuestTree.SelectedNodes.Count == 1)
            {
                selected = this.QuestTree.Root.Children[index].Children.ToArray();
            }
            else
            {
                selected = this.QuestTree.SelectedNodes.ToArray();
            }

            XmlTextWriter writer = new XmlTextWriter(filename, Encoding.UTF8)
            {
                Formatting = Formatting.Indented,
                Indentation = 2
            };
            writer.WriteStartDocument();
            writer.WriteComment("WillowTree Quest File");
            writer.WriteComment("Note: the XML tags are case sensitive");
            writer.WriteStartElement("WT");
            writer.WriteStartElement("Quests");

            QuestTable qt = this.currentWsg.QuestLists[index];

            foreach (TreeNodeAdv nodeAdv in selected)
            {
                string key = nodeAdv.GetKey();
                questSearchKey = nodeAdv.GetKey();

                int i = qt.Quests.FindIndex(this.QuestSearchByName);
                if (i == -1)
                {
                    continue;
                }

                QuestEntry qe = qt.Quests[i];
                writer.WriteStartElement("Quest");
                writer.WriteElementString("Name", qe.Name);
                writer.WriteElementString("Progress", qe.Progress.ToString());
                writer.WriteElementString("DLCValue1", qe.DlcValue1.ToString());
                writer.WriteElementString("DLCValue2", qe.DlcValue2.ToString());
                writer.WriteElementString("Objectives", qe.NumberOfObjectives.ToString());

                int objectiveCount = qe.NumberOfObjectives;
                for (int objectiveIndex = 0; objectiveIndex < objectiveCount; objectiveIndex++)
                {
                    writer.WriteElementString($"FolderName{objectiveIndex}", qe.Objectives[objectiveIndex].Description);
                    writer.WriteElementString($"FolderValue{objectiveIndex}", qe.Objectives[objectiveIndex].Progress.ToString());
                }
                writer.WriteEndElement();
            }

            writer.WriteEndDocument();
            writer.Close();
        }

        private void SaveToXmlQuests(string filename, int index)
        {
            XmlTextWriter writer = new XmlTextWriter(filename, Encoding.UTF8)
            {
                Formatting = Formatting.Indented,
                Indentation = 2
            };
            writer.WriteStartDocument();
            writer.WriteComment("WillowTree Quest File");
            writer.WriteComment("Note: the XML tags are case sensitive");
            writer.WriteStartElement("WT");
            writer.WriteStartElement("Quests");

            QuestTable qt = this.currentWsg.QuestLists[index];

            int count = this.currentWsg.QuestLists[index].TotalQuests;
            for (int i = 0; i < count; i++)
            {
                QuestEntry qe = qt.Quests[i];
                writer.WriteStartElement("Quest");
                writer.WriteElementString("Name", qe.Name);
                writer.WriteElementString("Progress", qe.Progress.ToString());
                writer.WriteElementString("DLCValue1", qe.DlcValue1.ToString());
                writer.WriteElementString("DLCValue2", qe.DlcValue2.ToString());
                writer.WriteElementString("Objectives", qe.NumberOfObjectives.ToString());

                int objectiveCount = qe.NumberOfObjectives;
                for (int objectiveIndex = 0; objectiveIndex < objectiveCount; objectiveIndex++)
                {
                    writer.WriteElementString($"FolderName{objectiveIndex}", qe.Objectives[objectiveIndex].Description);
                    writer.WriteElementString($"FolderValue{objectiveIndex}", qe.Objectives[objectiveIndex].Progress.ToString());
                }
                writer.WriteEndElement();
            }

            writer.WriteEndDocument();
            writer.Close();
        }

        private void SetActiveQuest_Click(object sender, EventArgs e)
        {
            int index = this.GetSelectedQuestList();
            if (index == -1 || this.QuestTree.SelectedNodes.Count != 1 ||
                this.QuestTree.SelectedNode.Parent == this.QuestTree.Root)
            {
                MessageBox.Show("Select a single quest from the quest list first.");
                return;
            }

            QuestTable qt = this.currentWsg.QuestLists[index];
            string currentQuest = this.QuestTree.SelectedNode.GetKey();
            qt.CurrentQuest = currentQuest;

            this.ActiveQuest.Text = currentQuest;
        }

        private void UiClearQuestPanel()
        {
            this.QuestString.Text = "";
            this.Objectives.Items.Clear();
            this.NumberOfObjectives.Value = 0;
            this.ObjectiveValue.Value = 0;
            this.QuestProgress.SelectedIndex = 0;
            this.QuestDescription.Text = "";
            this.QuestSummary.Text = "";
            this.SelectedQuestGroup.Text = "No Quest Selected";
            this.QuestDLCValue1.Value = 0;
            this.QuestDLCValue2.Value = 0;
        }

        private void UpdateActiveQuestList(int index)
        {
            if (index == -1)
            {
                this.ActivePT1QuestGroup.Text = "No Playthrough Selected";
                this.ActiveQuest.Text = "";
            }
            else
            {
                this.ActivePT1QuestGroup.Text = $"Active Playthrough {(index + 1)} Quest";
                this.ActiveQuest.Text = this.currentWsg.QuestLists[index].CurrentQuest;
            }
        }
    }
}
