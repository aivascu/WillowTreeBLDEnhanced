namespace WillowTreeSharp.Domain
{
    public class DlcSection
    {
        public int Id;
        public byte[] RawData;
        public byte[] BaseData; // used temporarily in SaveWSG to store the base data for a section as a byte array
    }
}