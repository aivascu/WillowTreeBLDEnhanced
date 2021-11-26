using System;
using System.Diagnostics;
using System.Net;
using WillowTree.Plugins;

namespace WillowTree.Presenters
{
    public class AboutViewPresenter
    {
        private string downloadUrlFromServer;
        private string versionFromServer;

        private readonly IAboutView aboutView;

        public AboutViewPresenter(IAboutView aboutView)
        {
            this.aboutView = aboutView ?? throw new ArgumentNullException(nameof(aboutView));

            this.aboutView.Tick += this.OnTick;
            this.aboutView.UpdateRequest += this.OnUpdateRequested;
            this.aboutView.ViewSelect += this.OnViewSelected;
        }

        private void OnViewSelected(object sender, EventArgs e)
        {
            if (this.versionFromServer == null)
            {
                this.aboutView.EnableTimer();
            }
        }

        private void OnUpdateRequested(object sender, EventArgs e)
        {
            Process.Start($"http://{this.downloadUrlFromServer}");
        }

        private void OnTick(object sender, EventArgs e)
        {
            if (this.versionFromServer == null)
            {
                return;
            }

            this.aboutView.DisableTimer();
            this.CheckVerPopup();
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
                        webClient.DownloadString("https://willowtree.sourceforge.net/version.txt");
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

            this.aboutView.SetUpdateButtonText(
                $"Version {this.versionFromServer} is now available! Click here to download.");
            this.aboutView.ShowUpdateButton();
        }
    }
}
