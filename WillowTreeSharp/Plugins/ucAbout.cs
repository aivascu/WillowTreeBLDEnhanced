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

using System;
using System.Linq;
using System.Net;
using System.Windows.Forms;
#if !DEBUG
using System.Threading;
#endif

namespace WillowTree.Plugins
{
    public partial class UcAbout : UserControl, IPlugin
    {
        private string downloadUrlFromServer;
        private string versionFromServer;

        public void InitializePlugin(PluginComponentManager pm)
        {
            var events = new PluginEvents
            {
                PluginSelected = OnPluginSelected,
                PluginUnselected = OnPluginUnselected
            };
            pm.RegisterPlugin(this, events);

#if !DEBUG
            // Only check for new version if it's not a debug build.
            ThreadPool.QueueUserWorkItem(CheckVersion);
#endif
            UpdateButton.Hide();
        }

        public void ReleasePlugin()
        {
        }

        public UcAbout()
        {
            InitializeComponent();
        }

        private void OnPluginSelected(object sender, PluginEventArgs e)
        {
            if (versionFromServer == null)
                timer1.Enabled = true;
        }

        private void OnPluginUnselected(object sender, PluginEventArgs e)
        {
            timer1.Enabled = false;
        }

        private void CheckVerPopup()
        {
            if (versionFromServer == GetVersion() || string.IsNullOrEmpty(versionFromServer)) return;

            UpdateButton.Text = $"Version {versionFromServer} is now available! Click here to download.";
            UpdateButton.Show();
        }

        private static string GetVersion()
        {
            return "2.2.1";
        }

        /// Recovers the latest version from the sourceforge server.
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

        private void UpdateButton_Click(object sender, EventArgs e)
        {
            System.Diagnostics.Process.Start($"http://{downloadUrlFromServer}");
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
    }
}