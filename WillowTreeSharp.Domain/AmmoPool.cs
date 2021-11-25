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

        public string Resource { get; private set; }
        public string Name { get; private set; }
        public float Remaining { get; private set; }
        public int Level { get; private set; }
    }
}