using System.Windows.Forms;

namespace WillowTree.Controls
{
    public class WTSaveFileDialog
    {
        private readonly SaveFileDialog dialog;

        public WTSaveFileDialog(string fileExt, string fileName)
        {
            dialog = new SaveFileDialog
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
    }
}
