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
using WillowTree.Controls;
using WillowTree.Services.DataAccess;

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
            InitializeComponent();
            GameData = gameData;
        }

        public IGameData GameData { get; }

        public void DoSkillList()
        {
            XmlFile xml;

            SkillList.Items.Clear();

            lastClass = currentWsg.Class;
            xml = GetClassSkillXml(lastClass);
            foreach (string section in xml.StListSectionNames())
            {
                SkillList.Items.Add(xml.XmlReadValue(section, "SkillName"));
            }

            xml = skillsCommonXml;
            foreach (string section in xml.StListSectionNames())
            {
                SkillList.Items.Add(xml.XmlReadValue(section, "SkillName"));
            }
        }

        public void InitializePlugin(PluginComponentManager pm)
        {
            PluginEvents events = new PluginEvents
            {
                GameLoaded = OnGameLoaded
            };
            pm.RegisterPlugin(this, events);

            pluginManager = pm;
            skillsAllXml = GameData.SkillsAllXml;
            skillsCommonXml = GameData.SkillsCommonXml;
            skillsBerserkerXml = GameData.SkillsBerserkerXml;
            skillsSoldierXml = GameData.SkillsSoldierXml;
            skillsSirenXml = GameData.SkillsSirenXml;
            skillsHunterXml = GameData.SkillsHunterXml;

            Enabled = false;
        }

        public void OnGameLoaded(object sender, PluginEventArgs e)
        {
            currentWsg = e.WillowTreeMain.SaveData;
            DoSkillList();
            Enabled = true;
            DoSkillTree();
        }

        public void ReleasePlugin()
        {
            pluginManager = null;
            currentWsg = null;
            skillsAllXml = null;
            skillsCommonXml = null;
            skillsBerserkerXml = null;
            skillsSoldierXml = null;
            skillsSirenXml = null;
            skillsHunterXml = null;
        }

        private void DeleteSkill_Click(object sender, EventArgs e)
        {
            TreeNodeAdv[] selectedNodes = SkillTree.SelectedNodes.ToArray();

            int count = selectedNodes.Length;
            for (int i = 0; i < count; i++)
            {
                string skillName = selectedNodes[i].GetKey();

                // Remove the skill from the WillowSaveGame data
                int selected = currentWsg.SkillNames.IndexOf(skillName);
                if (selected != -1)
                {
                    for (int position = selected; position < currentWsg.NumberOfSkills - 1; position++)
                    {
                        currentWsg.SkillNames[position] = currentWsg.SkillNames[position + 1];
                        currentWsg.InUse[position] = currentWsg.InUse[position + 1];
                        currentWsg.ExpOfSkills[position] = currentWsg.ExpOfSkills[position + 1];
                        currentWsg.LevelOfSkills[position] = currentWsg.LevelOfSkills[position + 1];
                    }

                    ArrayHelper.ResizeArraySmaller(ref currentWsg.SkillNames, currentWsg.NumberOfSkills);
                    ArrayHelper.ResizeArraySmaller(ref currentWsg.InUse, currentWsg.NumberOfSkills);
                    ArrayHelper.ResizeArraySmaller(ref currentWsg.ExpOfSkills, currentWsg.NumberOfSkills);
                    ArrayHelper.ResizeArraySmaller(ref currentWsg.LevelOfSkills, currentWsg.NumberOfSkills);

                    currentWsg.NumberOfSkills--;
                }

                // Remove the skill from the skill tree
                if (selectedNodes[i] == SkillTree.SelectedNode)
                {
                    SkillTree.SelectedNode = SkillTree.SelectedNode.NextVisibleNode;
                }

                selectedNodes[i].Remove();
            }

            if (SkillTree.SelectedNode == null && SkillTree.Root.Children.Count > 0)
            {
                SkillTree.SelectedNode = SkillTree.Root.Children.Last();
            }
        }

        private void DoSkillTree()
        {
            // Skill tree
            //     Key = name of the skill as stored in CurrentWSG.SkillNames
            //     Text = human readable display name of the skill
            SkillTree.BeginUpdate();
            TreeModel model = new TreeModel();
            SkillTree.Model = model;

            SkillLevel.Value = 0;
            SkillExp.Value = 0;
            SkillActive.SelectedItem = "No";
            for (int build = 0; build < currentWsg.NumberOfSkills; build++)
            {
                ColoredTextNode node = new ColoredTextNode();

                string key = currentWsg.SkillNames[build];
                node.Key = key;

                string name = skillsAllXml.XmlReadValue(key, "SkillName");
                node.Text = name != "" ? name : currentWsg.SkillNames[build];

                model.Nodes.Add(node);
            }

            SkillTree.EndUpdate();
        }

        private void ExportToFileSkills_Click(object sender, EventArgs e)
        {
            WTSaveFileDialog tempExport = new WTSaveFileDialog("skills", $"{currentWsg.CharacterName}.skills");

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

            for (int progress = 0; progress < currentWsg.NumberOfSkills; progress++)
            {
                subsectionnames.Clear();
                subsectionvalues.Clear();

                subsectionnames.Add("Level");
                subsectionnames.Add("Experience");
                subsectionnames.Add("InUse");
                subsectionvalues.Add(currentWsg.LevelOfSkills[progress].ToString());
                subsectionvalues.Add(currentWsg.ExpOfSkills[progress].ToString());
                subsectionvalues.Add(currentWsg.InUse[progress].ToString());

                skills.AddSection(currentWsg.SkillNames[progress], subsectionnames, subsectionvalues);
            }
        }

        private XmlFile GetClassSkillXml(string classString)
        {
            switch (classString)
            {
                case "gd_Roland.Character.CharacterClass_Roland":
                    return skillsSoldierXml;

                case "gd_lilith.Character.CharacterClass_Lilith":
                    return skillsSirenXml;

                case "gd_mordecai.Character.CharacterClass_Mordecai":
                    return skillsHunterXml;

                case "gd_Brick.Character.CharacterClass_Brick":
                    return skillsBerserkerXml;

                default:
                    return skillsCommonXml;
            }
        }

        private void ImportFromFileSkills_Click(object sender, EventArgs e)
        {
            WTOpenFileDialog tempImport = new WTOpenFileDialog("skills", $"{currentWsg.CharacterName}.skills");

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

            currentWsg.SkillNames = tempSkillNames;
            currentWsg.LevelOfSkills = tempSkillLevels;
            currentWsg.ExpOfSkills = tempSkillExp;
            currentWsg.InUse = tempSkillInUse;
            currentWsg.NumberOfSkills = sectionCount;
            DoSkillTree();
        }

        private void SkillActive_SelectionChangeCommitted(object sender, EventArgs e)
        {
            if (SkillTree.SelectedNode == null)
            {
                return;
            }

            int index = currentWsg.SkillNames.IndexOf((SkillTree.SelectedNode.Tag as ColoredTextNode).Key);
            if (index != -1)
            {
                currentWsg.InUse[index] = SkillActive.SelectedIndex == 1 ? 1 : -1;
            }
        }

        private void SkillExp_ValueChanged(object sender, EventArgs e)
        {
            if (SkillTree.SelectedNode == null)
            {
                return;
            }

            int index = currentWsg.SkillNames.IndexOf((SkillTree.SelectedNode.Tag as ColoredTextNode).Key);
            if (index != -1)
            {
                currentWsg.ExpOfSkills[index] = (int)SkillExp.Value;
            }
        }

        private void SkillLevel_ValueChanged(object sender, EventArgs e)
        {
            if (SkillTree.SelectedNode == null)
            {
                return;
            }

            int index = currentWsg.SkillNames.IndexOf((SkillTree.SelectedNode.Tag as ColoredTextNode).Key);
            if (index != -1)
            {
                currentWsg.LevelOfSkills[index] = (int)SkillLevel.Value;
            }
        }

        private void SkillList_Click(object sender, EventArgs e)
        {
            newToolStripMenuItem4.HideDropDown();
            try
            {
                // Make sure that if the class was changed, the new skill list is loaded
                if (lastClass != currentWsg.Class)
                {
                    DoSkillList();
                }

                // Look up the name of the selected skill from its display text
                string skillName =
                    skillsAllXml.XmlReadAssociatedValue("Name", "SkillName", (string)SkillList.SelectedItem);
                if (skillName == "")
                {
                    skillName = (string)SkillList.SelectedItem;
                }

                // If the skill is already in the tree, just select it and do nothing
                TreeNodeAdv skillNode = SkillTree.FindFirstNodeByTag(skillName, false);
                if (skillNode != null)
                {
                    SkillTree.SelectedNode = skillNode;
                    return;
                }

                // Add enough room for the new skill in each of the skill arrays
                currentWsg.NumberOfSkills++;
                ArrayHelper.ResizeArrayLarger(ref currentWsg.SkillNames, currentWsg.NumberOfSkills);
                ArrayHelper.ResizeArrayLarger(ref currentWsg.LevelOfSkills, currentWsg.NumberOfSkills);
                ArrayHelper.ResizeArrayLarger(ref currentWsg.ExpOfSkills, currentWsg.NumberOfSkills);
                ArrayHelper.ResizeArrayLarger(ref currentWsg.InUse, currentWsg.NumberOfSkills);

                // Set the data for the new skill.
                int index = currentWsg.NumberOfSkills - 1;
                currentWsg.InUse[index] = -1;
                currentWsg.LevelOfSkills[index] = 0;
                currentWsg.ExpOfSkills[index] = 01;
                currentWsg.SkillNames[currentWsg.NumberOfSkills - 1] = skillName;
                DoSkillTree();
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
                DeleteSkill_Click(this, EventArgs.Empty);
            }
        }

        private void SkillTree_SelectionChanged(object sender, EventArgs e)
        {
            if (SkillTree.SelectedNode == null)
            {
                SkillName.Text = "";
                SkillLevel.Value = 0;
                SkillExp.Value = 0;
                return;
            }

            int index = currentWsg.SkillNames.IndexOf((SkillTree.SelectedNode.Tag as ColoredTextNode).Key);
            if (index == -1)
            {
                SkillName.Text = "";
                SkillLevel.Value = 0;
                SkillExp.Value = 0;
            }
            else
            {
                SkillName.Text = currentWsg.SkillNames[index];
                SkillLevel.Value = currentWsg.LevelOfSkills[index];
                SkillExp.Value = currentWsg.ExpOfSkills[index];
                SkillActive.SelectedItem = currentWsg.InUse[index] == -1 ? "No" : "Yes";
            }
        }
    }
}
