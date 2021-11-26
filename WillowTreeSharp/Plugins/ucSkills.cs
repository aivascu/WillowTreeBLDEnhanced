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
using System.Text;
using System.Windows.Forms;
using System.Xml;
using WillowTree.Common;
using WillowTree.Controls;
using WillowTree.Services.DataAccess;
using WillowTreeSharp.Domain;

namespace WillowTree.Plugins
{
    public partial class UcSkills : UserControl, IPlugin
    {
        private WillowSaveGame currentWsg;
        private string lastClass;
        private PluginComponentManager pluginManager;
        private XmlFile skillsAllXml;
        private XmlFile skillsBerserkerXml;
        private XmlFile skillsCommonXml;
        private XmlFile skillsHunterXml;
        private XmlFile skillsSirenXml;
        private XmlFile skillsSoldierXml;

        public UcSkills(IGameData gameData)
        {
            this.InitializeComponent();
            this.GameData = gameData;
        }

        public IGameData GameData { get; }

        public void DoSkillList()
        {
            XmlFile xml;

            this.SkillList.Items.Clear();

            this.lastClass = this.currentWsg.Class;
            xml = this.GetClassSkillXml(this.lastClass);
            foreach (string section in xml.StListSectionNames())
            {
                this.SkillList.Items.Add(xml.XmlReadValue(section, "SkillName"));
            }

            xml = this.skillsCommonXml;
            foreach (string section in xml.StListSectionNames())
            {
                this.SkillList.Items.Add(xml.XmlReadValue(section, "SkillName"));
            }
        }

        public void InitializePlugin(PluginComponentManager pm)
        {
            PluginEvents events = new PluginEvents
            {
                GameLoaded = OnGameLoaded
            };
            pm.RegisterPlugin(this, events);

            this.pluginManager = pm;
            this.skillsAllXml = this.GameData.SkillsAllXml;
            this.skillsCommonXml = this.GameData.SkillsCommonXml;
            this.skillsBerserkerXml = this.GameData.SkillsBerserkerXml;
            this.skillsSoldierXml = this.GameData.SkillsSoldierXml;
            this.skillsSirenXml = this.GameData.SkillsSirenXml;
            this.skillsHunterXml = this.GameData.SkillsHunterXml;

            this.Enabled = false;
        }

        public void OnGameLoaded(object sender, PluginEventArgs e)
        {
            this.currentWsg = e.WillowTreeMain.SaveData;
            this.DoSkillList();
            this.Enabled = true;
            this.DoSkillTree();
        }

        public void ReleasePlugin()
        {
            this.pluginManager = null;
            this.currentWsg = null;
            this.skillsAllXml = null;
            this.skillsCommonXml = null;
            this.skillsBerserkerXml = null;
            this.skillsSoldierXml = null;
            this.skillsSirenXml = null;
            this.skillsHunterXml = null;
        }

        private void DeleteSkill_Click(object sender, EventArgs e)
        {
            TreeNodeAdv[] selectedNodes = this.SkillTree.SelectedNodes.ToArray();

            int count = selectedNodes.Length;
            for (int i = 0; i < count; i++)
            {
                string skillName = selectedNodes[i].GetKey();

                // Remove the skill from the WillowSaveGame data
                int selected = this.currentWsg.SkillNames.IndexOf(skillName);
                if (selected != -1)
                {
                    for (int position = selected; position < this.currentWsg.NumberOfSkills - 1; position++)
                    {
                        this.currentWsg.SkillNames[position] = this.currentWsg.SkillNames[position + 1];
                        this.currentWsg.InUse[position] = this.currentWsg.InUse[position + 1];
                        this.currentWsg.ExpOfSkills[position] = this.currentWsg.ExpOfSkills[position + 1];
                        this.currentWsg.LevelOfSkills[position] = this.currentWsg.LevelOfSkills[position + 1];
                    }

                    ArrayHelper.ResizeArraySmaller(ref this.currentWsg.SkillNames, this.currentWsg.NumberOfSkills);
                    ArrayHelper.ResizeArraySmaller(ref this.currentWsg.InUse, this.currentWsg.NumberOfSkills);
                    ArrayHelper.ResizeArraySmaller(ref this.currentWsg.ExpOfSkills, this.currentWsg.NumberOfSkills);
                    ArrayHelper.ResizeArraySmaller(ref this.currentWsg.LevelOfSkills, this.currentWsg.NumberOfSkills);

                    this.currentWsg.NumberOfSkills--;
                }

                // Remove the skill from the skill tree
                if (selectedNodes[i] == this.SkillTree.SelectedNode)
                {
                    this.SkillTree.SelectedNode = this.SkillTree.SelectedNode.NextVisibleNode;
                }

                selectedNodes[i].Remove();
            }

            if (this.SkillTree.SelectedNode == null && this.SkillTree.Root.Children.Count > 0)
            {
                this.SkillTree.SelectedNode = this.SkillTree.Root.Children.Last();
            }
        }

        private void DoSkillTree()
        {
            // Skill tree
            //     Key = name of the skill as stored in CurrentWSG.SkillNames
            //     Text = human readable display name of the skill
            this.SkillTree.BeginUpdate();
            TreeModel model = new TreeModel();
            this.SkillTree.Model = model;

            this.SkillLevel.Value = 0;
            this.SkillExp.Value = 0;
            this.SkillActive.SelectedItem = "No";
            for (int build = 0; build < this.currentWsg.NumberOfSkills; build++)
            {
                ColoredTextNode node = new ColoredTextNode();

                string key = this.currentWsg.SkillNames[build];
                node.Key = key;

                string name = this.skillsAllXml.XmlReadValue(key, "SkillName");
                node.Text = name != "" ? name : this.currentWsg.SkillNames[build];

                model.Nodes.Add(node);
            }

            this.SkillTree.EndUpdate();
        }

        private void ExportToFileSkills_Click(object sender, EventArgs e)
        {
            WTSaveFileDialog tempExport = new WTSaveFileDialog("skills", $"{this.currentWsg.CharacterName}.skills");

            if (tempExport.ShowDialog() != DialogResult.OK)
            {
                return;
            }

            // Create empty xml file
            XmlTextWriter writer = new XmlTextWriter(tempExport.FileName(), new ASCIIEncoding())
            {
                Formatting = Formatting.Indented,
                Indentation = 2
            };
            writer.WriteStartDocument();
            writer.WriteStartElement("INI");
            writer.WriteEndElement();
            writer.WriteEndDocument();
            writer.Flush();
            writer.Close();

            XmlFile skills = new XmlFile(tempExport.FileName());
            List<string> subsectionnames = new List<string>();
            List<string> subsectionvalues = new List<string>();

            for (int progress = 0; progress < this.currentWsg.NumberOfSkills; progress++)
            {
                subsectionnames.Clear();
                subsectionvalues.Clear();

                subsectionnames.Add("Level");
                subsectionnames.Add("Experience");
                subsectionnames.Add("InUse");
                subsectionvalues.Add(this.currentWsg.LevelOfSkills[progress].ToString());
                subsectionvalues.Add(this.currentWsg.ExpOfSkills[progress].ToString());
                subsectionvalues.Add(this.currentWsg.InUse[progress].ToString());

                skills.AddSection(this.currentWsg.SkillNames[progress], subsectionnames, subsectionvalues);
            }
        }

        private XmlFile GetClassSkillXml(string classString)
        {
            switch (classString)
            {
                case "gd_Roland.Character.CharacterClass_Roland":
                    return this.skillsSoldierXml;

                case "gd_lilith.Character.CharacterClass_Lilith":
                    return this.skillsSirenXml;

                case "gd_mordecai.Character.CharacterClass_Mordecai":
                    return this.skillsHunterXml;

                case "gd_Brick.Character.CharacterClass_Brick":
                    return this.skillsBerserkerXml;

                default:
                    return this.skillsCommonXml;
            }
        }

        private void ImportFromFileSkills_Click(object sender, EventArgs e)
        {
            WTOpenFileDialog tempImport = new WTOpenFileDialog("skills", $"{this.currentWsg.CharacterName}.skills");

            if (tempImport.ShowDialog() != DialogResult.OK)
            {
                return;
            }

            XmlFile importSkills = new XmlFile(tempImport.FileName());
            List<string> sectionNames = importSkills.StListSectionNames();

            int sectionCount = sectionNames.Count;

            string[] tempSkillNames = new string[sectionCount];
            int[] tempSkillLevels = new int[sectionCount];
            int[] tempSkillExp = new int[sectionCount];
            int[] tempSkillInUse = new int[sectionCount];
            for (int progress = 0; progress < sectionCount; progress++)
            {
                string name = sectionNames[progress];

                tempSkillNames[progress] = name;
                tempSkillLevels[progress] = Parse.AsInt(importSkills.XmlReadValue(name, "Level"));
                tempSkillExp[progress] = Parse.AsInt(importSkills.XmlReadValue(name, "Experience"));
                tempSkillInUse[progress] = Parse.AsInt(importSkills.XmlReadValue(name, "InUse"));
            }

            this.currentWsg.SkillNames = tempSkillNames;
            this.currentWsg.LevelOfSkills = tempSkillLevels;
            this.currentWsg.ExpOfSkills = tempSkillExp;
            this.currentWsg.InUse = tempSkillInUse;
            this.currentWsg.NumberOfSkills = sectionCount;
            this.DoSkillTree();
        }

        private void SkillActive_SelectionChangeCommitted(object sender, EventArgs e)
        {
            if (this.SkillTree.SelectedNode == null)
            {
                return;
            }

            int index = this.currentWsg.SkillNames.IndexOf((this.SkillTree.SelectedNode.Tag as ColoredTextNode).Key);
            if (index != -1)
            {
                this.currentWsg.InUse[index] = this.SkillActive.SelectedIndex == 1 ? 1 : -1;
            }
        }

        private void SkillExp_ValueChanged(object sender, EventArgs e)
        {
            if (this.SkillTree.SelectedNode == null)
            {
                return;
            }

            int index = this.currentWsg.SkillNames.IndexOf((this.SkillTree.SelectedNode.Tag as ColoredTextNode).Key);
            if (index != -1)
            {
                this.currentWsg.ExpOfSkills[index] = (int)this.SkillExp.Value;
            }
        }

        private void SkillLevel_ValueChanged(object sender, EventArgs e)
        {
            if (this.SkillTree.SelectedNode == null)
            {
                return;
            }

            int index = this.currentWsg.SkillNames.IndexOf((this.SkillTree.SelectedNode.Tag as ColoredTextNode).Key);
            if (index != -1)
            {
                this.currentWsg.LevelOfSkills[index] = (int)this.SkillLevel.Value;
            }
        }

        private void SkillList_Click(object sender, EventArgs e)
        {
            this.newToolStripMenuItem4.HideDropDown();
            try
            {
                // Make sure that if the class was changed, the new skill list is loaded
                if (this.lastClass != this.currentWsg.Class)
                {
                    this.DoSkillList();
                }

                // Look up the name of the selected skill from its display text
                string skillName =
                    this.skillsAllXml.XmlReadAssociatedValue("Name", "SkillName", (string)this.SkillList.SelectedItem);
                if (skillName == "")
                {
                    skillName = (string)this.SkillList.SelectedItem;
                }

                // If the skill is already in the tree, just select it and do nothing
                TreeNodeAdv skillNode = this.SkillTree.FindFirstNodeByTag(skillName, false);
                if (skillNode != null)
                {
                    this.SkillTree.SelectedNode = skillNode;
                    return;
                }

                // Add enough room for the new skill in each of the skill arrays
                this.currentWsg.NumberOfSkills++;
                ArrayHelper.ResizeArrayLarger(ref this.currentWsg.SkillNames, this.currentWsg.NumberOfSkills);
                ArrayHelper.ResizeArrayLarger(ref this.currentWsg.LevelOfSkills, this.currentWsg.NumberOfSkills);
                ArrayHelper.ResizeArrayLarger(ref this.currentWsg.ExpOfSkills, this.currentWsg.NumberOfSkills);
                ArrayHelper.ResizeArrayLarger(ref this.currentWsg.InUse, this.currentWsg.NumberOfSkills);

                // Set the data for the new skill.
                int index = this.currentWsg.NumberOfSkills - 1;
                this.currentWsg.InUse[index] = -1;
                this.currentWsg.LevelOfSkills[index] = 0;
                this.currentWsg.ExpOfSkills[index] = 01;
                this.currentWsg.SkillNames[this.currentWsg.NumberOfSkills - 1] = skillName;
                this.DoSkillTree();
            }
            catch
            {
                MessageBox.Show("Could not add new Skill.");
            }
        }

        private void SkillTree_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Delete)
            {
                this.DeleteSkill_Click(this, EventArgs.Empty);
            }
        }

        private void SkillTree_SelectionChanged(object sender, EventArgs e)
        {
            if (this.SkillTree.SelectedNode == null)
            {
                this.SkillName.Text = "";
                this.SkillLevel.Value = 0;
                this.SkillExp.Value = 0;
                return;
            }

            int index = this.currentWsg.SkillNames.IndexOf((this.SkillTree.SelectedNode.Tag as ColoredTextNode).Key);
            if (index == -1)
            {
                this.SkillName.Text = "";
                this.SkillLevel.Value = 0;
                this.SkillExp.Value = 0;
            }
            else
            {
                this.SkillName.Text = this.currentWsg.SkillNames[index];
                this.SkillLevel.Value = this.currentWsg.LevelOfSkills[index];
                this.SkillExp.Value = this.currentWsg.ExpOfSkills[index];
                this.SkillActive.SelectedItem = this.currentWsg.InUse[index] == -1 ? "No" : "Yes";
            }
        }
    }
}
