using System.IO;
using System.Reflection;

namespace WillowTree.Services.DataAccess
{
    internal static class Constants
    {
        public static readonly string DataPath = Path.Combine(GetAppPath(), "Data") + Path.DirectorySeparatorChar;

        private static string GetAppPath()
        {
            if (Assembly.GetEntryAssembly() == null)
            {
                return string.Empty;
            }

            return Path.GetDirectoryName(Assembly.GetEntryAssembly()?.Location);
        }
    }
}
