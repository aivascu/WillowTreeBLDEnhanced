using System.Collections.Generic;

namespace WillowTree.Inventory
{
    public class InventoryComparer : Comparer<InventoryEntry>
    {
        public int[] comparisons;

        public InventoryComparer(int[] comparisonarray)
        {
            this.comparisons = comparisonarray;
        }

        public override int Compare(InventoryEntry x, InventoryEntry y)
        {
            int result = 0;
            foreach (int comparison in this.comparisons)
            {
                switch (comparison)
                {
                    // 0: Name
                    // 1: Rarity
                    // 2: Category
                    // 3: Title
                    // 4: Prefix
                    // 5: Model
                    // 6: Manufacturer
                    // 7: Level
                    // 8: Key
                    case 0: result = string.Compare(x.Name, y.Name); break;
                    case 1:
                        if (x.Rarity > y.Rarity)
                        {
                            result = -1;
                        }
                        else if (x.Rarity < y.Rarity)
                        {
                            result = 1;
                        }
                        else
                        {
                            result = 0;
                        }

                        break;

                    case 2: result = string.Compare(x.Category, y.Category); break;
                    case 3: result = string.Compare(x.NameParts[3], y.NameParts[3]); break;
                    case 4: result = string.Compare(x.NameParts[2], y.NameParts[2]); break;
                    case 5: result = string.Compare(x.NameParts[1], y.NameParts[1]); break;
                    case 6: result = string.Compare(x.NameParts[0], y.NameParts[0]); break;
                    case 7:
                        if (x.EffectiveLevel > y.EffectiveLevel)
                        {
                            result = -1;
                        }
                        else if (x.EffectiveLevel < y.EffectiveLevel)
                        {
                            result = 1;
                        }
                        else
                        {
                            result = 0;
                        }

                        break;

                    case 8:
                        int xkeyval = int.Parse(x.Key);
                        int ykeyval = int.Parse(y.Key);
                        if (xkeyval < ykeyval)
                        {
                            result = -1;
                        }
                        else if (xkeyval > ykeyval)
                        {
                            result = 1;
                        }
                        else
                        {
                            result = 0;
                        }

                        break;
                }
                if (result != 0)
                {
                    return result;
                }
            }
            return result;
        }
    }
}
