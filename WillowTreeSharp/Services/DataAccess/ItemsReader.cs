using System.Collections.Generic;
using System.IO;

namespace WillowTree.Services.DataAccess
{
    public class ItemsReader : ObjectReader
    {
        public override IEnumerable<string> ReadStrings(BinaryReader reader, ByteOrder byteOrder)
            => WillowSaveGameBase.ReadItemStrings(reader, byteOrder);
    }
}
