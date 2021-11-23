using System;
using System.Windows.Forms;

namespace WillowTree
{
    public static partial class Util
    {
        public static void SetNumericUpDown(NumericUpDown updown, decimal value)
        {
            if (value > updown.Maximum)
            {
                value = updown.Maximum;
            }
            else if (value < updown.Minimum)
            {
                value = updown.Minimum;
            }

            updown.Value = value;
        }

        public class WTOpenFileDialog
        {
            private OpenFileDialog fDlg = null;

            public WTOpenFileDialog(String fileExt, String fileName)
            {
                if (string.IsNullOrEmpty(fileName))
                    fileName = "";

                fDlg = new OpenFileDialog();
                fDlg.DefaultExt = "*." + fileExt;
                fDlg.Filter = "WillowTree (*." + fileExt + ")|*." + fileExt + "|All Files (*.*)|*.*";
                fDlg.FileName = fileName;
            }

            public DialogResult ShowDialog()
            { return fDlg.ShowDialog(); }

            public String FileName()
            { return fDlg.FileName; }

            public String[] FileNames()
            { return fDlg.FileNames; }

            public void Multiselect(bool multiselect)
            { fDlg.Multiselect = multiselect; }
        }

        public class WTSaveFileDialog
        {
            private SaveFileDialog fDlg = null;

            public WTSaveFileDialog(String fileExt, String fileName)
            {
                fDlg = new SaveFileDialog();
                fDlg.DefaultExt = "*." + fileExt;
                fDlg.Filter = "WillowTree (*." + fileExt + ")|*." + fileExt + "|All Files (*.*)|*.*";
                fDlg.FileName = fileName;
            }

            public DialogResult ShowDialog()
            { return fDlg.ShowDialog(); }

            public String FileName()
            { return fDlg.FileName; }
        }
    }
}
