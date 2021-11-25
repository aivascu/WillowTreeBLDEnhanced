namespace WillowTreeSharp.Domain
{
    public class Location
    {
        public Location(string id)
        {
            this.Id = id;
        }

        public string Id { get; }

        public static implicit operator string(Location location)
        {
            return location.Id;
        }

        public static implicit operator Location(string value)
        {
            return new Location(value);
        }
    }
}