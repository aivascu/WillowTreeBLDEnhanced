/*  This file is part of WillowTree#
 * 
 *  Copyright (C) 2011 Matthew Carter <matt911@users.sf.net>
 *  Copyright (C) 2010, 2011 XanderChaos
 *  Copyright (C) 2011 Thomas Kaiser
 *  Copyright (C) 2010 JackSchitt
 * 
 *  WillowTree# is free software: you can redistribute it and/or modify
 *  it under the terms of the GNU General Public License as published by
 *  the Free Software Foundation, either version 3 of the License, or
 *  (at your option) any later version.
 *
 *  WillowTree# is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 *  GNU General Public License for more details.
 *
 *  You should have received a copy of the GNU General Public License
 *  along with WillowTree#.  If not, see <http://www.gnu.org/licenses/>.
 */
using Aga.Controls.Tree;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using System.Xml;
using WillowTree.Controls;
using WillowTree.Services.DataAccess;

namespace WillowTree.Plugins
{
    public partial class ucEchoes : UserControl, IPlugin
    {
        private WillowSaveGame CurrentWSG;
        private XmlFile EchoesXml;

        public ucEchoes()
        {
            this.InitializeComponent();
        }

        public void InitializePlugin(PluginComponentManager pm)
        {
            PluginEvents events = new PluginEvents
            {
                GameLoaded = OnGameLoaded
            };
            pm.RegisterPlugin(this, events);

            this.EchoesXml = GameData.EchoesXml;
            this.DoEchoList();
            this.Enabled = false;
        }

        public void ReleasePlugin()
        {
            this.CurrentWSG = null;
            this.EchoesXml = null;
        }

        public void OnGameLoaded(object sender, PluginEventArgs e)
        {
            this.CurrentWSG = e.WillowTreeMain.SaveData;
            this.DoEchoTree();
            this.Enabled = true;
        }

        public void DoEchoList()
        {
            foreach (string section in this.EchoesXml.StListSectionNames())
            {
                string name = this.EchoesXml.XmlReadValue(section, "Subject");
                if (name != "")
                {
                    this.EchoList.Items.Add(name);
                }
                else
                {
                    this.EchoList.Items.Add(section);
                }
            }
        }

        public void DoEchoTree()
        {
            this.EchoTree.BeginUpdate();
            TreeModel model = new TreeModel();
            this.EchoTree.Model = model;

            for (int i = 0; i < this.CurrentWSG.NumberOfEchoLists; i++)
            {
                // Category nodes
                //      Text = human readable category heading
                //      Tag = echo list index stored as a string (0 based)
                ColoredTextNode parent = new ColoredTextNode
                {
                    Tag = i.ToString(),
                    Text = $"Playthrough {(this.CurrentWSG.EchoLists[i].Index + 1)} Echo Logs"
                };
                model.Nodes.Add(parent);

                for (int build = 0; build < this.CurrentWSG.EchoLists[i].TotalEchoes; build++)
                {
                    string name = this.CurrentWSG.EchoLists[i].Echoes[build].Name;

                    // Echo nodes
                    //      Text = human readable echo name
                    //      Tag = internal echo name
                    ColoredTextNode node = new ColoredTextNode
                    {
                        Tag = name,
                        Text = this.EchoesXml.XmlReadValue(name, "Subject")
                    };
                    if (node.Text == "")
                    {
                        node.Text = $"({name})";
                    }

                    parent.Nodes.Add(node);
                }
            }
            this.EchoTree.EndUpdate();
        }

        public static string EchoSearchKey;
        public bool EchoSearchByName(EchoEntry ee)
        {
            return ee.Name == EchoSearchKey;
        }

        public void DeleteAllEchoes(int index)
        {
            EchoTable et = this.CurrentWSG.EchoLists[index];
            et.Echoes.Clear();
            et.TotalEchoes = 0;

            TreeNodeAdv[] children = this.EchoTree.Root.Children[index].Children.ToArray();
            foreach (TreeNodeAdv child in children)
            {
                child.Remove();
            }
        }
        private int GetSelectedEchoList()
        {
            int index = -1;

            if (this.EchoTree.SelectedNode == null)
            {
                // Do nothing, fall through to the feedback message below
            }
            else if (this.EchoTree.SelectedNode.Parent != this.EchoTree.Root)
            {
                index = Parse.AsInt(this.EchoTree.SelectedNode.Parent.GetKey(), -1);
            }
            else
            {
                // This is a category node not an echo.  If there is exactly one
                // selected then choose it as the location for import, otherwise 
                // do nothing and let the feedback message below take effect.
                if (this.EchoTree.SelectedNodes.Count == 1)
                {
                    index = Parse.AsInt(this.EchoTree.SelectedNode.GetKey());
                }
            }
            return index;
        }
        public void LoadEchoes(string filename, int index)
        {
            XmlDocument doc = new XmlDocument();
            doc.Load(filename);

            if (doc.SelectSingleNode("WT/Echoes") == null)
            {
                throw new ApplicationException("NoEchoes");
            }

            XmlNodeList echonodes = doc.SelectNodes("WT/Echoes/Echo");

            int count = echonodes.Count;

            EchoTable et = this.CurrentWSG.EchoLists[index];
            et.Echoes.Clear();
            et.TotalEchoes = 0;

            this.EchoTree.BeginUpdate();

            for (int i = 0; i < count; i++)
            {
                // Create a new echo entry and populate it from the xml node
                EchoEntry ee = new EchoEntry();
                XmlNode node = echonodes[i];
                string name = node.GetElement("Name", "");
                ee.Name = name;
                ee.DlcValue1 = node.GetElementAsInt("DLCValue1", 0);
                ee.DlcValue2 = node.GetElementAsInt("DLCValue2", 0);

                // Add the echo to the list
                et.Echoes.Add(ee);
                et.TotalEchoes++;

                // Add the echo to the tree view
                ColoredTextNode treeNode = new ColoredTextNode
                {
                    Tag = name,
                    Text = this.EchoesXml.XmlReadValue(name, "Subject")
                };
                if (treeNode.Text == "")
                {
                    treeNode.Text = $"({name})";
                }

                this.EchoTree.Root.Children[index].AddNode(treeNode);
            }

            this.EchoTree.EndUpdate();
        }
        public void MergeFromSaveEchoes(string filename, int index)
        {
            WillowSaveGame OtherSave = new WillowSaveGame();
            OtherSave.LoadWsg(filename);

            if (OtherSave.NumberOfEchoLists - 1 < index)
            {
                return;
            }

            EchoTable etOther = OtherSave.EchoLists[index];
            EchoTable et = this.CurrentWSG.EchoLists[index];

            this.EchoTree.BeginUpdate();

            // Copy only the locations that are not duplicates from the other save
            foreach (EchoEntry ee in this.CurrentWSG.EchoLists[index].Echoes)
            {
                string name = ee.Name;

                // Make sure the echo is not already in the list
                EchoSearchKey = name;
                if (et.Echoes.FindIndex(this.EchoSearchByName) != -1)
                {
                    continue;
                }

                // Add the echo entry to the echo list
                et.Echoes.Add(ee);
                et.TotalEchoes++;

                // Add the echo to the tree view
                ColoredTextNode treeNode = new ColoredTextNode
                {
                    Tag = name,
                    Text = this.EchoesXml.XmlReadValue(name, "Subject")
                };
                if (treeNode.Text == "")
                {
                    treeNode.Text = $"({name})";
                }

                this.EchoTree.Root.Children[index].AddNode(treeNode);
            }
            this.EchoTree.EndUpdate();
        }
        public void MergeAllFromXmlEchoes(string filename, int index)
        {
            XmlDocument doc = new XmlDocument();
            doc.Load(filename);

            if (doc.SelectSingleNode("WT/Echoes") == null)
            {
                throw new ApplicationException("NoEchoes");
            }

            XmlNodeList echonodes = doc.SelectNodes("WT/Echoes/Echo");
            if (echonodes == null)
            {
                return;
            }

            EchoTable et = this.CurrentWSG.EchoLists[index];

            this.EchoTree.BeginUpdate();

            // Copy only the echos that are not duplicates from the XML file
            foreach (XmlNode node in echonodes)
            {
                string name = node.GetElement("Name", "");

                // Make sure the echo is not already in the list
                EchoSearchKey = name;
                if (et.Echoes.FindIndex(this.EchoSearchByName) != -1)
                {
                    continue;
                }

                // Create a new echo entry an populate it from the node
                EchoEntry ee = new EchoEntry
                {
                    Name = name,
                    DlcValue1 = node.GetElementAsInt("DLCValue1", 0),
                    DlcValue2 = node.GetElementAsInt("DLCValue2", 0)
                };

                // Add the echo entry to the echo list
                et.Echoes.Add(ee);
                et.TotalEchoes++;

                // Add the echo to the tree view
                ColoredTextNode treeNode = new ColoredTextNode
                {
                    Tag = name,
                    Text = this.EchoesXml.XmlReadValue(name, "Subject")
                };
                if (treeNode.Text == "")
                {
                    treeNode.Text = $"({name})";
                }

                this.EchoTree.Root.Children[index].AddNode(treeNode);
            }
            this.EchoTree.EndUpdate();
        }
        private void SaveToXmlEchoes(string filename, int index)
        {
            XmlTextWriter writer = new XmlTextWriter(filename, System.Text.Encoding.UTF8);
            writer.Formatting = Formatting.Indented;
            writer.Indentation = 2;
            writer.WriteStartDocument();
            writer.WriteComment("WillowTree Echo File");
            writer.WriteComment("Note: the XML tags are case sensitive");
            writer.WriteStartElement("WT");
            writer.WriteStartElement("Echoes");

            EchoTable et = this.CurrentWSG.EchoLists[index];

            int count = this.CurrentWSG.EchoLists[index].TotalEchoes;
            for (int i = 0; i < count; i++)
            {
                EchoEntry ee = et.Echoes[i];
                writer.WriteStartElement("Echo");
                writer.WriteElementString("Name", ee.Name);
                writer.WriteElementString("DLCValue1", ee.DlcValue1.ToString());
                writer.WriteElementString("DLCValue2", ee.DlcValue2.ToString());
                writer.WriteEndElement();
            }

            writer.WriteEndDocument();
            writer.Close();
        }
        private void SaveSelectedToXmlEchoes(string filename, int index)
        {
            TreeNodeAdv[] selected;

            // There are two valid ways a user can select nodes to save to xml.
            // He can choose exactly one category node or he can choose multiple
            // echo nodes.  Figure out which and create an array of the nodes.
            if (this.EchoTree.SelectedNode.Parent == this.EchoTree.Root && this.EchoTree.SelectedNodes.Count == 1)
            {
                selected = this.EchoTree.Root.Children[index].Children.ToArray();
            }
            else
            {
                selected = this.EchoTree.SelectedNodes.ToArray();
            }

            XmlTextWriter writer = new XmlTextWriter(filename, System.Text.Encoding.UTF8);
            writer.Formatting = Formatting.Indented;
            writer.Indentation = 2;
            writer.WriteStartDocument();
            writer.WriteComment("WillowTree Echo File");
            writer.WriteComment("Note: the XML tags are case sensitive");
            writer.WriteStartElement("WT");
            writer.WriteStartElement("Echoes");

            EchoTable et = this.CurrentWSG.EchoLists[index];

            foreach (TreeNodeAdv nodeAdv in selected)
            {
                string key = nodeAdv.GetKey();
                EchoSearchKey = nodeAdv.GetKey();

                int i = et.Echoes.FindIndex(this.EchoSearchByName);
                if (i == -1)
                {
                    continue;
                }

                EchoEntry ee = et.Echoes[i];
                writer.WriteStartElement("Echo");
                writer.WriteElementString("Name", ee.Name);
                writer.WriteElementString("DLCValue1", ee.DlcValue1.ToString());
                writer.WriteElementString("DLCValue2", ee.DlcValue2.ToString());
                writer.WriteEndElement();
            }

            writer.WriteEndDocument();
            writer.Close();
        }

        private void AddListEchoes_Click(object sender, EventArgs e)
        {
            int index = this.CurrentWSG.NumberOfEchoLists;

            // Create an empty echo table
            EchoTable et = new EchoTable
            {
                Index = this.CurrentWSG.NumberOfEchoLists,
                Echoes = new List<EchoEntry>(),
                TotalEchoes = 0
            };

            // Add the new table to the list
            this.CurrentWSG.EchoLists.Add(et);
            this.CurrentWSG.NumberOfEchoLists++;

            this.EchoTree.BeginUpdate();

            //Add the new table to the tree view
            ColoredTextNode categoryNode = new ColoredTextNode
            {
                Tag = index.ToString(),
                Text = $"Playthrough {(this.CurrentWSG.EchoLists[index].Index + 1)} Echo Logs"
            };
            (this.EchoTree.Model as TreeModel).Nodes.Add(categoryNode);

            this.EchoTree.EndUpdate();
        }

        private void ClearEchoes_Click(object sender, EventArgs e)
        {
            int index = this.GetSelectedEchoList();
            if (index == -1)
            {
                MessageBox.Show("Select an echo list first.");
                return;
            }

            this.DeleteAllEchoes(index);
        }

        private void CloneFromSaveEchoes_Click(object sender, EventArgs e)
        {
            int index = this.GetSelectedEchoList();
            if (index == -1)
            {
                MessageBox.Show("Select a single echo list to import to first.");
                return;
            }
            
            WTOpenFileDialog tempOpen = new WTOpenFileDialog("sav", "");

            if (tempOpen.ShowDialog() == DialogResult.OK)
            {
                WillowSaveGame OtherSave = new WillowSaveGame();

                try
                {
                    OtherSave.LoadWsg(tempOpen.FileName());
                }
                catch { MessageBox.Show("Couldn't open the other save file."); return; }

                if (OtherSave.NumberOfEchoLists - 1 < index)
                {
                    MessageBox.Show("The echo list does not exist in the other savegame file.");
                    return;
                }

                // Replace the old entries in the echo table with the new ones
                this.CurrentWSG.EchoLists[index] = OtherSave.EchoLists[index];

                EchoTable et = this.CurrentWSG.EchoLists[index];

                this.EchoTree.BeginUpdate();
                TreeNodeAdv parent = this.EchoTree.Root.Children[index];

                // Remove the old entries from the tree view
                TreeNodeAdv[] children = parent.Children.ToArray();
                foreach (TreeNodeAdv child in children)
                {
                    child.Remove();
                }

                // Add the new entries to the tree view
                foreach (EchoEntry ee in et.Echoes)
                {
                    string name = ee.Name;

                    ColoredTextNode node = new ColoredTextNode
                    {
                        Tag = name,
                        Text = this.EchoesXml.XmlReadValue(name, "Subject")
                    };
                    if (node.Text == "")
                    {
                        node.Text = $"({name})";
                    }

                    parent.AddNode(node);
                }
                this.EchoTree.EndUpdate();                
            }
        }

        private void DeleteEcho_Click(object sender, EventArgs e)
        {
            // Get out if no node is selected
            int index = this.GetSelectedEchoList();
            if (index == -1 || this.EchoTree.SelectedNode.Parent == this.EchoTree.Root)
            {
                return;
            }

            TreeNodeAdv NextSelection = null;

            TreeNodeAdv[] selected = this.EchoTree.SelectedNodes.ToArray();
            foreach (TreeNodeAdv nodeAdv in selected)
            {
                // Just remove the node from the selection if it is a
                // category node
                if (nodeAdv.Parent == this.EchoTree.Root)
                {
                    NextSelection = nodeAdv;
                    nodeAdv.IsSelected = false;
                    continue;
                }

                EchoTable et = this.CurrentWSG.EchoLists[index];

                // Remove the echo from the echo list
                et.TotalEchoes--;
                et.Echoes.RemoveAt(nodeAdv.Index);
                
                // Remove the echo from the tree view
                NextSelection = nodeAdv.NextVisibleNode;
                nodeAdv.Remove();
            }

            // Select a new selected node if the selected node was removed
            if (this.EchoTree.SelectedNode == null)
            {
                this.EchoTree.SelectedNode = NextSelection;
            }
        }

        private void EchoList_Click(object sender, EventArgs e)
        {
            this.newToolStripMenuItem1.HideDropDown();

            if (this.EchoList.SelectedIndex == -1)
            {
                return;
            }

            int index = this.GetSelectedEchoList();
            if (index == -1)
            {
                MessageBox.Show("Select an echo list first.");
                return;
            }

            string name = this.EchoesXml.StListSectionNames()[this.EchoList.SelectedIndex];

            // Create a new echo entry and populate it
            EchoEntry ee = new EchoEntry
            {
                Name = name,
                // file then it needs to be looked up here.
                // exist in the data files yet.  When the proper data is in the data
                // TODO: These values shouldn't always be zero, but the data doesn't
                DlcValue1 = 0,
                DlcValue2 = 0
            };

            // Add the new echo to the echo list
            EchoTable et = this.CurrentWSG.EchoLists[index];
            et.Echoes.Add(ee);
            et.TotalEchoes++;

            // Add the new echo to the echo tree view
            ColoredTextNode treeNode = new ColoredTextNode
            {
                Tag = name,
                Text = this.EchoesXml.XmlReadValue(name, "Subject")
            };
            if (treeNode.Text == "")
            {
                treeNode.Text = $"{$"({treeNode.Tag}"})";
            }

            TreeNodeAdv parent = this.EchoTree.Root.Children[index];
            parent.AddNode(treeNode);

            // Select the newly added node so the user will know it was added
            this.EchoTree.SelectedNode = parent.Children[parent.Children.Count - 1];
            this.EchoTree.EnsureVisible(this.EchoTree.SelectedNode);
        }

        private void ExportEchoes_Click(object sender, EventArgs e)
        {
            int index = this.GetSelectedEchoList();
            if (index == -1)
            {
                MessageBox.Show("Select a playthrough to export first.");
                return;
            }

            try
            {
                WTSaveFileDialog tempExport = new WTSaveFileDialog("echologs",
                    $"{this.CurrentWSG.CharacterName}.PT{(index + 1)}.echologs");

                if (tempExport.ShowDialog() == DialogResult.OK)
                {
                    this.SaveToXmlEchoes(tempExport.FileName(), index);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Export failed:\r\n{ex}");
            }
        }

        private void ExportSelectedEchoes_Click(object sender, EventArgs e)
        {
            int index = this.GetSelectedEchoList();
            if (index == -1)
            {
                MessageBox.Show("Select a single echo list or select the echoes to export first.");
                return;
            }

            WTSaveFileDialog tempSave = new WTSaveFileDialog("echologs",
                $"{this.CurrentWSG.CharacterName}.PT{(index + 1)}.echologs");

            try
            {
                if (tempSave.ShowDialog() == DialogResult.OK)
                {
                    this.SaveSelectedToXmlEchoes(tempSave.FileName(), index);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error occurred while trying to save locations: {ex}");
                return;
            }

            MessageBox.Show($"Echoes saved to {tempSave.FileName()}");
        }

        private void ImportEchoes_Click(object sender, EventArgs e)
        {
            int index = this.GetSelectedEchoList();
            if (index == -1)
            {
                MessageBox.Show("Select a playthrough to import first.");
                return;
            }

            WTOpenFileDialog tempOpen = new WTOpenFileDialog("echologs", "Default.echologs");

            if (tempOpen.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    this.LoadEchoes(tempOpen.FileName(), index);
                }
                catch (ApplicationException ex)
                {
                    if (ex.Message == "NoEchoes")
                    {
                        MessageBox.Show("Couldn't find an echoes section in the file.  Action aborted.");
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error occurred while trying to load: {ex}");
                }
            }
        }

        private void EchoDLCValue1_ValueChanged(object sender, EventArgs e)
        {
            int index = this.GetSelectedEchoList();
            if (index == -1 || this.EchoTree.SelectedNode.Parent == this.EchoTree.Root)
            {
                return;
            }

            this.CurrentWSG.EchoLists[index].Echoes[this.EchoTree.SelectedNode.Index].DlcValue1 = (int)this.EchoDLCValue1.Value;
        }

        private void EchoDLCValue2_ValueChanged(object sender, EventArgs e)
        {
            int index = this.GetSelectedEchoList();
            if (index == -1 || this.EchoTree.SelectedNode.Parent == this.EchoTree.Root)
            {
                return;
            }

            this.CurrentWSG.EchoLists[index].Echoes[this.EchoTree.SelectedNode.Index].DlcValue2 = (int)this.EchoDLCValue2.Value;
        }

        private void UIClearEchoPanel()
        {
            this.EchoDLCValue1.Value = 0;
            this.EchoDLCValue2.Value = 0;
            this.EchoString.Text = "";
        }

        private void EchoTree_SelectionChanged(object sender, EventArgs e)
        {
            int index = this.GetSelectedEchoList();

            // If a echo node is not selected reset the UI elements and exit
            if (index == -1 || this.EchoTree.SelectedNode.Parent == this.EchoTree.Root)
            {
                this.UIClearEchoPanel();
                return;
            }

            EchoEntry ee = this.CurrentWSG.EchoLists[index].Echoes[this.EchoTree.SelectedNode.Index];

            this.EchoDLCValue1.Value = ee.DlcValue1;
            this.EchoDLCValue2.Value = ee.DlcValue2;
            this.EchoString.Text = ee.Name;
        }

        private void EchoTree_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Delete)
            {
                this.DeleteEcho_Click(this, EventArgs.Empty);
            }
        }

        private void MergeAllFromFileEchoes_Click(object sender, EventArgs e)
        {
            int index = this.GetSelectedEchoList();
            if (index == -1)
            {
                MessageBox.Show("Select a single echo list to import to first.");
                return;
            }

            WTOpenFileDialog tempOpen = new WTOpenFileDialog("echologs", "Default.echologs");

            if (tempOpen.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    this.MergeAllFromXmlEchoes(tempOpen.FileName(), index);
                }
                catch (ApplicationException ex)
                {
                    if (ex.Message == "NoEchoes")
                    {
                        MessageBox.Show("Couldn't find a location section in the file.  Action aborted.");
                    }
                }
                catch { MessageBox.Show("Couldn't load the file.  Action aborted."); }
            }
        }

        private void MergeFromSaveEchoes_Click(object sender, EventArgs e)
        {
            int index = this.GetSelectedEchoList();
            if (index == -1)
            {
                MessageBox.Show("Select a single echo list to import to first.");
                return;
            }
            
            WTOpenFileDialog tempOpen = new WTOpenFileDialog("sav", "");

            if (tempOpen.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    this.MergeFromSaveEchoes(tempOpen.FileName(), index);
                }
                catch { MessageBox.Show("Couldn't open the other save file."); }
            }
        }
        
        private void RemoveListEchoes_Click(object sender, EventArgs e)
        {
            TreeNodeAdv[] selection = this.EchoTree.SelectedNodes.ToArray();

            foreach (TreeNodeAdv nodeAdv in selection)
            {
                if (nodeAdv.Parent == this.EchoTree.Root)
                {
                    this.CurrentWSG.NumberOfEchoLists--;
                    this.CurrentWSG.EchoLists.RemoveAt(nodeAdv.Index);
                    this.EchoTree.Root.Children[nodeAdv.Index].Remove();
                }
            }

            // The indexes will be messed up if a list that is not the last one is
            // removed, so update the tree text, tree indexes, and echo list indices
            int count = this.CurrentWSG.NumberOfEchoLists;
            for (int index = 0; index < count; index++)
            {
                TreeNodeAdv nodeAdv = this.EchoTree.Root.Children[index];

                // Adjust the category node's text and tag to reflect its new position
                ColoredTextNode parent = nodeAdv.Data();
                parent.Text = $"Playthrough {(index + 1)} Echo Logs";
                parent.Tag = index.ToString();

                // Adjust the echo list index to reflect its new position 
                this.CurrentWSG.EchoLists[index].Index = index;
            }
            this.EchoTree.EndUpdate();
        }

        private void EchoString_TextChanged(object sender, EventArgs e)
        {
            int index = this.GetSelectedEchoList();
            if (index == -1 || this.EchoTree.SelectedNode.Parent == this.EchoTree.Root)
            {
                return;
            }

            string name = this.EchoString.Text;
            this.CurrentWSG.EchoLists[index].Echoes[this.EchoTree.SelectedNode.Index].Name = name;

            string text = this.EchoesXml.XmlReadValue(name, "Subject");
            if (text == "")
            {
                text = $"({name})";
            }

            this.EchoTree.SelectedNode.SetKey(name);
            this.EchoTree.SelectedNode.SetText(text);
        }
    }
}
