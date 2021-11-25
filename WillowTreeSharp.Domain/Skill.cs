namespace WillowTreeSharp.Domain
{
    public class Skill
    {
        public Skill(string name, int level, int experience, int inUse)
        {
            this.Name = name;
            this.Level = level;
            this.Experience = experience;
            this.InUse = inUse;
        }

        public string Name { get; private set; }
        public int Level { get; private set; }
        public int Experience { get; private set; }
        public int InUse { get; private set; }
    }
}
