using System.Windows.Forms;

namespace WillowTree.Controls
{
    public class MessageBoxWrapper : IMessageBox
    {
        public DialogResult Show(string text)
        {
            return MessageBox.Show(text);
        }

        public DialogResult Show(string text, string caption, MessageBoxButtons buttons, MessageBoxIcon icon)
        {
            return MessageBox.Show(text, caption, buttons, icon);
        }

        public DialogResult Show(string text, string caption, MessageBoxButtons buttons)
        {
            return MessageBox.Show(text, caption, buttons);
        }
    }
}
