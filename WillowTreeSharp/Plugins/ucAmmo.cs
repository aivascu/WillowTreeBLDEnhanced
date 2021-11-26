using Aga.Controls.Tree;
using Microsoft.VisualBasic;
using System;
using System.Windows.Forms;
using WillowTree.Common;
using WillowTreeSharp.Domain;

namespace WillowTree.Plugins
{
    public partial class UcAmmo : UserControl, IPlugin
    {
        private WillowSaveGame currentWsg;

        public UcAmmo()
        {
            this.InitializeComponent();
        }

        private void DoAmmoTree()
        {
            this.AmmoTree.BeginUpdate();
            TreeModel model = new TreeModel();
            this.AmmoTree.Model = model;

            for (int build = 0; build < this.currentWsg.NumberOfPools; build++)
            {
                string ammoName = this.GetAmmoName(this.currentWsg.ResourcePools[build]);
                ColoredTextNode node = new ColoredTextNode(ammoName);
                model.Nodes.Add(node);
            }
            this.AmmoTree.EndUpdate();
        }

        private string GetAmmoName(string resource)
        {
            switch (resource)
            {
                case "d_resources.AmmoResources.Ammo_Sniper_Rifle":
                    return "Sniper Rifle";


                case "d_resources.AmmoResources.Ammo_Repeater_Pistol":
                    return "Repeater Pistol";


                case "d_resources.AmmoResources.Ammo_Grenade_Protean":
                    return "Protean Grenades";


                case "d_resources.AmmoResources.Ammo_Patrol_SMG":
                    return "Patrol SMG";


                case "d_resources.AmmoResources.Ammo_Combat_Shotgun":
                    return "Combat Shotgun";


                case "d_resources.AmmoResources.Ammo_Combat_Rifle":
                    return "Combat Rifle";


                case "d_resources.AmmoResources.Ammo_Revolver_Pistol":
                    return "Revolver Pistol";


                case "d_resources.AmmoResources.Ammo_Rocket_Launcher":
                    return "Rocket Launcher";


                default:
                    return resource;
            }
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
                this.currentWsg.RemainingPools[selectedNode.Index] = (float)this.AmmoPoolRemaining.Value;
            }
        }

        private void AmmoSDULevel_ValueChanged(object sender, EventArgs e)
        {
            TreeNodeAdv selectedNode = this.AmmoTree.SelectedNode;
            if (selectedNode != null)
            {
                this.currentWsg.PoolLevels[selectedNode.Index] = (int)this.AmmoSDULevel.Value;
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
                this.AmmoPoolRemaining.Value = (decimal)this.currentWsg.RemainingPools[selectedNode.Index];
                this.AmmoSDULevel.Value = this.currentWsg.PoolLevels[selectedNode.Index];
            }
        }

        private void DeleteAmmo_Click(object sender, EventArgs e)
        {
            if (this.AmmoTree.SelectedNode != null)
            {
                this.currentWsg.NumberOfPools--;
                ArrayHelper.ResizeArraySmaller(ref this.currentWsg.AmmoPools, this.currentWsg.NumberOfPools);
                ArrayHelper.ResizeArraySmaller(ref this.currentWsg.ResourcePools, this.currentWsg.NumberOfPools);
                ArrayHelper.ResizeArraySmaller(ref this.currentWsg.RemainingPools, this.currentWsg.NumberOfPools);
                ArrayHelper.ResizeArraySmaller(ref this.currentWsg.PoolLevels, this.currentWsg.NumberOfPools);
                this.DoAmmoTree();
            }
        }

        private void NewAmmo_Click(object sender, EventArgs e)
        {
            try
            {
                string newDResources = Interaction.InputBox("Enter the 'd_resources' for the new Ammo Pool", "New Ammo Pool", "", 10, 10);
                string newDResourcepools = Interaction.InputBox("Enter the 'd_resourcepools' for the new Ammo Pool", "New Ammo Pool", "", 10, 10);
                if (newDResourcepools != "" && newDResources != "")
                {
                    this.currentWsg.NumberOfPools++;
                    ArrayHelper.ResizeArrayLarger(ref this.currentWsg.AmmoPools, this.currentWsg.NumberOfPools);
                    ArrayHelper.ResizeArrayLarger(ref this.currentWsg.ResourcePools, this.currentWsg.NumberOfPools);
                    ArrayHelper.ResizeArrayLarger(ref this.currentWsg.RemainingPools, this.currentWsg.NumberOfPools);
                    ArrayHelper.ResizeArrayLarger(ref this.currentWsg.PoolLevels, this.currentWsg.NumberOfPools);
                    this.currentWsg.AmmoPools[this.currentWsg.NumberOfPools - 1] = newDResourcepools;
                    this.currentWsg.ResourcePools[this.currentWsg.NumberOfPools - 1] = newDResources;
                    this.DoAmmoTree();
                }
            }
            catch
            {
                MessageBox.Show("Couldn't add new ammo pool.");
            }
        }
    }
}
