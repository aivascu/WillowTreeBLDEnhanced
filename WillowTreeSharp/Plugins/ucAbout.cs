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
            InitializeComponent();
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

                    versionFromServer = remoteVersionInfo[0];
                    downloadUrlFromServer = remoteVersionInfo[1];
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
            UpdateButton.Hide();
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
            if (versionFromServer == GetVersion() || string.IsNullOrEmpty(versionFromServer))
            {
                return;
            }

            UpdateButton.Text = $"Version {versionFromServer} is now available! Click here to download.";
            UpdateButton.Show();
        }

        private void OnPluginSelected(object sender, PluginEventArgs e)
        {
            if (versionFromServer == null)
            {
                timer1.Enabled = true;
            }
        }

        private void OnPluginUnselected(object sender, PluginEventArgs e)
        {
            timer1.Enabled = false;
        }

        private void OnTimerTick(object sender, EventArgs e)
        {
            if (versionFromServer == null)
            {
                return;
            }

            timer1.Enabled = false;
            CheckVerPopup();
        }

        private void UpdateButton_Click(object sender, EventArgs e)
        {
            Process.Start($"http://{downloadUrlFromServer}");
        }
    }
}
