using System.Collections.Generic;
using System.Linq;

namespace WillowTreeSharp.Domain
{
    public abstract class WillowObject
    {
        protected int[] values = new int[0x6];

        protected WillowObject()
        {
        }

        public List<string> Strings { get; set; } = new List<string>();

        public void SetValues(List<int> values)
        {
            this.values = values.ToArray();
        }

        public List<int> GetValues()
        {
            return this.values.ToList();
        }

        public int Quality
        {
            get => this.values[0x1];
            set => this.values[0x1] = value;
        }

        public int EquippedSlot
        {
            get => this.values[0x2];
            set => this.values[0x2] = value;
        }

        public int Level
        {
            get => this.values[0x3];
            set => this.values[0x3] = value;
        }

        public int Junk
        {
            get => this.values[0x4];
            set => this.values[0x4] = value;
        }

        public int Locked
        {
            get => this.values[0x5];
            set => this.values[0x5] = value;
        }
    }
}
