using System.Windows.Forms;

namespace WillowTree.Controls
{
    public class WTOpenFileDialog
    {
        private readonly OpenFileDialog dialog;

        public WTOpenFileDialog(string fileExt, string fileName)
        {
            if (string.IsNullOrEmpty(fileName))
            {
                fileName = "";
            }

            this.dialog = new OpenFileDialog
            {
                DefaultExt = $"*.{fileExt}",
                Filter = $"WillowTree (*.{fileExt})|*.{fileExt}|All Files (*.*)|*.*",
                FileName = fileName
            };
        }

        public DialogResult ShowDialog()
        { return this.dialog.ShowDialog(); }

        public string FileName()
        { return this.dialog.FileName; }

        public string[] FileNames()
        { return this.dialog.FileNames; }

        public void Multiselect(bool multiselect)
        { this.dialog.Multiselect = multiselect; }
    }
}
