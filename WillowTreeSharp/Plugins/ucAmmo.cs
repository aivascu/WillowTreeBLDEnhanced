using Aga.Controls.Tree;
using Microsoft.VisualBasic;
using System;
using System.Windows.Forms;
using WillowTree.Services.DataAccess;

namespace WillowTree.Plugins
{
    public partial class UcAmmo : UserControl, IPlugin
    {
        private WillowSaveGame currentWsg;

        public UcAmmo()
        {
            InitializeComponent();
        }

        private void DoAmmoTree()
        {
            AmmoTree.BeginUpdate();
            TreeModel model = new TreeModel();
            AmmoTree.Model = model;

            for (int build = 0; build < currentWsg.NumberOfPools; build++)
            {
                string ammoName = GetAmmoName(currentWsg.ResourcePools[build]);
                ColoredTextNode node = new ColoredTextNode(ammoName);
                model.Nodes.Add(node);
            }
            AmmoTree.EndUpdate();
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

            Enabled = false;
        }

        private void OnGameLoaded(object sender, PluginEventArgs e)
        {
            currentWsg = e.WillowTreeMain.SaveData;
            DoAmmoTree();
            Enabled = true;
        }

        public void ReleasePlugin()
        {
            currentWsg = null;
        }

        private void AmmoPoolRemaining_ValueChanged(object sender, EventArgs e)
        {
            TreeNodeAdv selectedNode = AmmoTree.SelectedNode;
            if (selectedNode != null)
            {
                currentWsg.RemainingPools[selectedNode.Index] = (float)AmmoPoolRemaining.Value;
            }
        }

        private void AmmoSDULevel_ValueChanged(object sender, EventArgs e)
        {
            TreeNodeAdv selectedNode = AmmoTree.SelectedNode;
            if (selectedNode != null)
            {
                currentWsg.PoolLevels[selectedNode.Index] = (int)AmmoSDULevel.Value;
            }
        }

        private void AmmoTree_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Delete)
            {
                DeleteAmmo_Click(this, EventArgs.Empty);
            }
        }

        private void AmmoTree_SelectionChanged(object sender, EventArgs e)
        {
            TreeNodeAdv selectedNode = AmmoTree.SelectedNode;
            if (selectedNode != null)
            {
                AmmoPoolRemaining.Value = (decimal)currentWsg.RemainingPools[selectedNode.Index];
                AmmoSDULevel.Value = currentWsg.PoolLevels[selectedNode.Index];
            }
        }

        private void DeleteAmmo_Click(object sender, EventArgs e)
        {
            if (AmmoTree.SelectedNode != null)
            {
                currentWsg.NumberOfPools--;
                ArrayHelper.ResizeArraySmaller(ref currentWsg.AmmoPools, currentWsg.NumberOfPools);
                ArrayHelper.ResizeArraySmaller(ref currentWsg.ResourcePools, currentWsg.NumberOfPools);
                ArrayHelper.ResizeArraySmaller(ref currentWsg.RemainingPools, currentWsg.NumberOfPools);
                ArrayHelper.ResizeArraySmaller(ref currentWsg.PoolLevels, currentWsg.NumberOfPools);
                DoAmmoTree();
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
                    currentWsg.NumberOfPools++;
                    ArrayHelper.ResizeArrayLarger(ref currentWsg.AmmoPools, currentWsg.NumberOfPools);
                    ArrayHelper.ResizeArrayLarger(ref currentWsg.ResourcePools, currentWsg.NumberOfPools);
                    ArrayHelper.ResizeArrayLarger(ref currentWsg.RemainingPools, currentWsg.NumberOfPools);
                    ArrayHelper.ResizeArrayLarger(ref currentWsg.PoolLevels, currentWsg.NumberOfPools);
                    currentWsg.AmmoPools[currentWsg.NumberOfPools - 1] = newDResourcepools;
                    currentWsg.ResourcePools[currentWsg.NumberOfPools - 1] = newDResources;
                    DoAmmoTree();
                }
            }
            catch
            {
                MessageBox.Show("Couldn't add new ammo pool.");
            }
        }
    }
}
