using System;
using System.Collections.Generic;
using System.Windows.Forms;
using Aga.Controls.Tree;
using Microsoft.VisualBasic;
using WillowTree.Controls;
using WillowTreeSharp.Domain;

namespace WillowTree.Plugins
{
    public partial class UcAmmo : UserControl, IPlugin
    {
        private WillowSaveGame currentWsg;

        private Dictionary<string, string> namesLookup = new Dictionary<string, string>
        {
            { "d_resources.AmmoResources.Ammo_Sniper_Rifle",  "Sniper Rifle" },
            { "d_resources.AmmoResources.Ammo_Repeater_Pistol", "Repeater Pistol" },
            { "d_resources.AmmoResources.Ammo_Grenade_Protean", "Protean Grenades" },
            { "d_resources.AmmoResources.Ammo_Patrol_SMG", "Patrol SMG" },
            { "d_resources.AmmoResources.Ammo_Combat_Shotgun", "Combat Shotgun" },
            { "d_resources.AmmoResources.Ammo_Combat_Rifle", "Combat Rifle" },
            { "d_resources.AmmoResources.Ammo_Revolver_Pistol", "Revolver Pistol" },
            { "d_resources.AmmoResources.Ammo_Rocket_Launcher", "Rocket Launcher" },
        };

        private Dictionary<string, string> resourceLookup = new Dictionary<string, string>
        {
            {  "Sniper Rifle" , "d_resources.AmmoResources.Ammo_Sniper_Rifle"},
            { "Repeater Pistol" , "d_resources.AmmoResources.Ammo_Repeater_Pistol"},
            { "Protean Grenades" , "d_resources.AmmoResources.Ammo_Grenade_Protean"},
            { "Patrol SMG" , "d_resources.AmmoResources.Ammo_Patrol_SMG"},
            { "Combat Shotgun" , "d_resources.AmmoResources.Ammo_Combat_Shotgun"},
            { "Combat Rifle" , "d_resources.AmmoResources.Ammo_Combat_Rifle"},
            { "Revolver Pistol" , "d_resources.AmmoResources.Ammo_Revolver_Pistol"},
            { "Rocket Launcher" , "d_resources.AmmoResources.Ammo_Rocket_Launcher"},
        };

        public UcAmmo(IMessageBox messageBox)
        {
            this.MessageBox = messageBox;
            this.InitializeComponent();
        }

        public IMessageBox MessageBox { get; }

        private void DoAmmoTree()
        {
            this.AmmoTree.BeginUpdate();
            TreeModel model = new TreeModel();
            this.AmmoTree.Model = model;

            foreach (var item in this.currentWsg.AmmoPools)
            {
                string ammoName = this.GetAmmoName(item.Resource);
                ColoredTextNode node = new ColoredTextNode(ammoName);
                model.Nodes.Add(node);
            }
            this.AmmoTree.EndUpdate();
        }

        private string GetAmmoName(string resource)
        {
            return this.namesLookup.TryGetValue(resource, out var value)
                ? value
                : resource;
        }

        private string GetAmmoResource(string name)
        {
            return this.resourceLookup.TryGetValue(name, out var value)
                ? value
                : name;
        }

        public void InitializePlugin(PluginComponentManager pluginManager)
        {
            PluginEvents events = new PluginEvents
            {
                GameLoaded = OnGameLoaded
            };
            pluginManager.RegisterPlugin(this, events);

            this.Enabled = false;
        }

        private void OnGameLoaded(object sender, PluginEventArgs e)
        {
            this.currentWsg = e.WillowTreeMain.SaveData;
            this.DoAmmoTree();
            this.Enabled = true;
        }

        public void ReleasePlugin()
        {
            this.currentWsg = null;
        }

        private void AmmoPoolRemaining_ValueChanged(object sender, EventArgs e)
        {
            TreeNodeAdv selectedNode = this.AmmoTree.SelectedNode;
            if (selectedNode != null)
            {
                this.currentWsg.AmmoPools[selectedNode.Index].Remaining = (float)this.AmmoPoolRemaining.Value;
            }
        }

        private void AmmoSDULevel_ValueChanged(object sender, EventArgs e)
        {
            TreeNodeAdv selectedNode = this.AmmoTree.SelectedNode;
            if (selectedNode != null)
            {
                this.currentWsg.AmmoPools[selectedNode.Index].Level = (int)this.AmmoSDULevel.Value;
            }
        }

        private void AmmoTree_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Delete)
            {
                this.DeleteAmmo_Click(this, EventArgs.Empty);
            }
        }

        private void AmmoTree_SelectionChanged(object sender, EventArgs e)
        {
            TreeNodeAdv selectedNode = this.AmmoTree.SelectedNode;
            if (selectedNode != null)
            {
                this.AmmoPoolRemaining.Value = (decimal)this.currentWsg.AmmoPools[selectedNode.Index].Remaining;
                this.AmmoSDULevel.Value = this.currentWsg.AmmoPools[selectedNode.Index].Level;
            }
        }

        private void DeleteAmmo_Click(object sender, EventArgs e)
        {
            var selectedNode = this.AmmoTree.SelectedNode;
            if (selectedNode != null)
            {
                var resource = this.GetAmmoResource(selectedNode.Text());
                this.currentWsg.AmmoPools.RemoveAll(x => x.Resource == resource);
                this.DoAmmoTree();
            }
        }

        private void NewAmmo_Click(object sender, EventArgs e)
        {
            try
            {
                string resource = Interaction.InputBox("Enter the 'd_resources' for the new Ammo Pool", "New Ammo Pool", "", 10, 10);
                string resourcePool = Interaction.InputBox("Enter the 'd_resourcepools' for the new Ammo Pool", "New Ammo Pool", "", 10, 10);
                if (resourcePool != "" && resource != "")
                {
                    var newPool = new AmmoPool(resource, resourcePool, 0, 0);
                    this.currentWsg.AmmoPools.Add(newPool);
                    this.DoAmmoTree();
                }
            }
            catch
            {
                this.MessageBox.Show("Couldn't add new ammo pool.");
            }
        }
    }
}
