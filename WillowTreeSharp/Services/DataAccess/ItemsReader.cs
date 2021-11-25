using System.Collections.Generic;
using System.IO;
using WillowTreeSharp.Domain;

namespace WillowTree.Services.DataAccess
{
    public class ItemsReader : ObjectReader
    {
        public override IEnumerable<string> ReadStrings(BinaryReader reader, ByteOrder byteOrder)
            => WillowSaveGameSerializer.ReadItemStrings(reader, byteOrder);
    }
}
