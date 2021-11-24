/*  This file is part of WillowTree#
 * 
 *  Copyright (C) 2011 Matthew Carter <matt911@users.sf.net>
 *  Copyright (C) 2011 XanderChaos
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
using System.IO;
using System.Text;
using System.Windows.Forms;
using WillowTree.Controls;
using X360.IO;
using X360.STFS;

namespace WillowTree
{
    public partial class XBoxIDDialog : Form
    {
        public XBoxUniqueID ID;

        public XBoxIDDialog()
        {
            this.InitializeComponent();
        }
   
        private void button1_Click(object sender, EventArgs e)
        {
            WTOpenFileDialog tempOpen = new WTOpenFileDialog("sav", "");

            if (tempOpen.ShowDialog() == DialogResult.OK)
            {
                this.XBoxIDFilePath.Text = tempOpen.FileName();
                try
                {
                    this.ID = new XBoxUniqueID(tempOpen.FileName());
                    this.ProfileBox.Text = this.ID.ProfileID.ToString("X");
                    this.DeviceBox.Text = BitConverter.ToString(this.ID.DeviceID);
                    this.DeviceBox.Text = this.DeviceBox.Text.Replace("-", "");
                }
                catch
                {
                    this.ID = null;
                    MessageBox.Show("The file is not a valid Xbox 360 savegame file.");
                }
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            if (this.ID != null)
            {
                this.DialogResult = DialogResult.OK;
            }
            else
            {
                MessageBox.Show("Please select a valid Xbox 360 save to use first.");
            }
        }

        private void button3_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
        }

    }
    public class XBoxUniqueID
    {
        public long ProfileID { get; private set; }
        public byte[] DeviceID { get; private set; }

        public XBoxUniqueID(string FileName)
        {
            BinaryReader br = new BinaryReader(File.Open(FileName, FileMode.Open), Encoding.ASCII);
            string Magic = new string(br.ReadChars(3));
            if (Magic != "CON")
            {
                throw new FileFormatException();
            }
            br.Close();
            br = null;

            STFSPackage CON = new STFSPackage(new DJsIO(FileName, DJFileMode.Open, true), new X360.Other.LogRecord());
            this.ProfileID = CON.Header.ProfileID;
            this.DeviceID = CON.Header.DeviceID;
            CON.CloseIO();
        }
    }
}
