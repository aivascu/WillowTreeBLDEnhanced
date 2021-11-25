using System.Diagnostics;
using System.Text;

namespace WillowTree.Services.DataAccess
{
    public static class SaveEncoding
    {
        private static Encoding singleByteEncoding; // DO NOT REFERENCE THIS DIRECTLY!

        public static Encoding SingleByteEncoding
        {
            get
            {
                // Not really thread safe, but it doesn't matter (Encoding should be effectively sealed).
                if (!(singleByteEncoding is null))
                    return singleByteEncoding;

                // Use ISO 8859-1 (Windows 1252) encoding for all single-byte strings.
                singleByteEncoding = Encoding.GetEncoding(1252);
                Debug.Assert(singleByteEncoding != null, "singleByteEncoding != null");
                Debug.Assert(singleByteEncoding.IsSingleByte, "Given string encoding is not a single-byte encoding.");

                return singleByteEncoding;
            }
        }
    }
}