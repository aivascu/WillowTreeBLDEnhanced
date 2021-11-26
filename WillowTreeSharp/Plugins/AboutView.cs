using System;
using System.Windows.Forms;

namespace WillowTree.Plugins
{
    public partial class AboutView : UserControl, IPlugin
    {
        public AboutView()
        {
            this.InitializeComponent();
        }

        public event EventHandler Tick;
        public event EventHandler UpdateRequest;
        public event EventHandler ViewSelect;

        public void InitializePlugin(PluginComponentManager pm)
        {
            var events = new PluginEvents
            {
                PluginSelected = this.OnPluginSelected,
                PluginUnselected = this.OnPluginUnselected
            };
            pm.RegisterPlugin(this, events);

            this.UpdateButton.Hide();
        }

        public void ReleasePlugin()
        {
        }

        private void OnPluginSelected(object sender, PluginEventArgs e)
        {
            this.ViewSelect?.Invoke(this, e);
        }

        private void OnPluginUnselected(object sender, PluginEventArgs e)
        {
            this.timer1.Enabled = false;
        }

        private void OnTimerTick(object sender, EventArgs e)
        {
            this.Tick?.Invoke(this, e);
        }

        private void UpdateButton_Click(object sender, EventArgs e)
        {
            this.UpdateRequest?.Invoke(this, e);
        }

        public void SetUpdateButtonText(string text)
        {
            this.UpdateButton.Text = text;
        }

        public void ShowUpdateButton()
        {
            this.UpdateButton.Show();
        }

        public void DisableTimer()
        {
            this.timer1.Enabled = false;
        }

        public void EnableTimer()
        {
            this.timer1.Enabled = false;
        }
    }
}
