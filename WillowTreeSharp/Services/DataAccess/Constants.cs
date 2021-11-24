using System.IO;
using System.Reflection;

namespace WillowTree.Services.DataAccess
{
    static internal class Constants
    {
        public static readonly string AppPath = (Assembly.GetEntryAssembly() != null ? Path.GetDirectoryName(Assembly.GetEntryAssembly()?.Location) : string.Empty) +
                                                Path.DirectorySeparatorChar;

        public static readonly string DataPath = Constants.AppPath + "Data" + Path.DirectorySeparatorChar;
    }
}