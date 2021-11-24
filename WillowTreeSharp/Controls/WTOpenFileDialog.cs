using System.Windows.Forms;

namespace WillowTree.Controls
{
    public class WTOpenFileDialog
    {
        private readonly OpenFileDialog dialog;

        public WTOpenFileDialog(string fileExt, string fileName)
        {
            if (string.IsNullOrEmpty(fileName))
                fileName = "";

            dialog = new OpenFileDialog
            {
                DefaultExt = $"*.{fileExt}",
                Filter = $"WillowTree (*.{fileExt})|*.{fileExt}|All Files (*.*)|*.*",
                FileName = fileName
            };
        }

        public DialogResult ShowDialog()
        { return dialog.ShowDialog(); }

        public string FileName()
        { return dialog.FileName; }

        public string[] FileNames()
        { return dialog.FileNames; }

        public void Multiselect(bool multiselect)
        { dialog.Multiselect = multiselect; }
    }
}
