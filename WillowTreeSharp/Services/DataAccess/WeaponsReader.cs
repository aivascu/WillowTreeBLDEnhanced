using System.Collections.Generic;
using System.IO;
using WillowTreeSharp.Domain;

namespace WillowTree.Services.DataAccess
{
    public class WeaponsReader : ObjectReader
    {
        public override IEnumerable<string> ReadStrings(BinaryReader reader, ByteOrder byteOrder)
            => WillowSaveGameBase.ReadWeaponStrings(reader, byteOrder);
    }
}
