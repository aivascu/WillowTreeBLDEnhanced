using System.Collections.Generic;
using System.IO;

namespace WillowTree.Services.DataAccess
{
    public class WeaponsReader : ObjectReader
    {
        public override IEnumerable<string> ReadStrings(BinaryReader reader, ByteOrder byteOrder)
            => WillowSaveGameBase.ReadWeaponStrings(reader, byteOrder);
    }
}
