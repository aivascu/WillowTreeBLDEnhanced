using System.Windows.Forms;

namespace WillowTree.Controls
{
    public interface IMessageBox
    {
        DialogResult Show(string text);

        DialogResult Show(string text, string caption, MessageBoxButtons buttons, MessageBoxIcon icon);

        DialogResult Show(string text, string caption, MessageBoxButtons buttons);
    }
}
