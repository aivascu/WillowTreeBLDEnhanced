using System;
using System.Windows.Forms;

namespace WillowTree
{
    internal static class Program
    {
        [STAThread]
        private static void Main()
        {
            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.ThrowException);
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new WillowTreeMain());
        }
    }
}
