namespace WillowTreeSharp.Domain
{
    public class XBoxId
    {
        public XBoxId(long profileId, byte[] deviceId)
        {
            this.ProfileId = profileId;
            this.DeviceId = deviceId;
        }

        public long ProfileId { get; private set; }
        public byte[] DeviceId { get; private set; }
    }
}
