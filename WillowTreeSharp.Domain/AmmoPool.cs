namespace WillowTreeSharp.Domain
{
    public class AmmoPool
    {
        public AmmoPool(string resource, string name, float remaining, int level)
        {
            this.Resource = resource;
            this.Name = name;
            this.Remaining = remaining;
            this.Level = level;
        }

        public string Resource { get; set; }
        public string Name { get; set; }
        public float Remaining { get; set; }
        public int Level { get; set; }
    }
}
