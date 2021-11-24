using System;
using System.Diagnostics;
using System.Net;
using System.Windows.Forms;

namespace WillowTree.Plugins
{
    public partial class UcAbout : UserControl, IPlugin
    {
        private string downloadUrlFromServer;
        private string versionFromServer;

        public UcAbout()
        {
            this.InitializeComponent();
        }

        /// <summary>
        /// Recovers the latest version from the sourceforge server.
        /// </summary>
        public void CheckVersion(object state)
        {
            try
            {
                using (var webClient = new WebClient())
                {
                    var versionTextFromServer =
                        webClient.DownloadString("http://willowtree.sourceforge.net/version.txt");
                    var remoteVersionInfo = versionTextFromServer.Replace("\r\n", "\n").Split('\n');
                    if (remoteVersionInfo.Length <= 1 && remoteVersionInfo.Length > 3)
                    {
                        return;
                    }

                    this.versionFromServer = remoteVersionInfo[0];
                    this.downloadUrlFromServer = remoteVersionInfo[1];
                }
            }
            catch
            {
                // ignored
            }
        }

        public void InitializePlugin(PluginComponentManager pm)
        {
            var events = new PluginEvents
            {
                PluginSelected = OnPluginSelected,
                PluginUnselected = OnPluginUnselected
            };
            pm.RegisterPlugin(this, events);

            // Only check for new version if it's not a debug build.
            //ThreadPool.QueueUserWorkItem(CheckVersion);
            this.UpdateButton.Hide();
        }

        public void ReleasePlugin()
        {
        }

        private static string GetVersion()
        {
            return "2.2.1";
        }

        private void CheckVerPopup()
        {
            if (this.versionFromServer == GetVersion() || string.IsNullOrEmpty(this.versionFromServer))
            {
                return;
            }

            this.UpdateButton.Text = $"Version {this.versionFromServer} is now available! Click here to download.";
            this.UpdateButton.Show();
        }

        private void OnPluginSelected(object sender, PluginEventArgs e)
        {
            if (this.versionFromServer == null)
            {
                this.timer1.Enabled = true;
            }
        }

        private void OnPluginUnselected(object sender, PluginEventArgs e)
        {
            this.timer1.Enabled = false;
        }

        private void OnTimerTick(object sender, EventArgs e)
        {
            if (this.versionFromServer == null)
            {
                return;
            }

            this.timer1.Enabled = false;
            this.CheckVerPopup();
        }

        private void UpdateButton_Click(object sender, EventArgs e)
        {
            Process.Start($"http://{this.downloadUrlFromServer}");
        }
    }
}
