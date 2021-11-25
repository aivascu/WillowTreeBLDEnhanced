using System.Collections.Generic;
using System.IO;
using WillowTreeSharp.Domain;

namespace WillowTree.Services.DataAccess
{
    public abstract class ObjectReader : IObjectReader
    {
        public abstract IEnumerable<string> ReadStrings(BinaryReader reader, ByteOrder byteOrder);

        public virtual IEnumerable<int> ReadValues(BinaryReader reader, ByteOrder byteOrder, int revision)
            => WillowSaveGameBase.ReadObjectValues(reader, byteOrder, revision);
    }
}
