using System.Collections.Generic;
using System.IO;
using WillowTreeSharp.Domain;

namespace WillowTree.Services.DataAccess
{
    public interface IObjectReader
    {
        IEnumerable<string> ReadStrings(BinaryReader reader, ByteOrder byteOrder);

        IEnumerable<int> ReadValues(BinaryReader reader, ByteOrder byteOrder, int revision);
    }
}