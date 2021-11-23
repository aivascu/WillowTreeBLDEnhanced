namespace WillowTree.Inventory
{
    public class InventoryComparisonIterator
    {
        public int ComparerIndex;
        public InventoryComparer[] Comparers;

        public InventoryComparisonIterator(InventoryComparer[] comparers)
        {
            Comparers = comparers;
        }

        public void NextComparer()
        {
            ComparerIndex++;
            if (ComparerIndex >= Comparers.Length)
                ComparerIndex = 0;
        }

        public void PreviousComparer()
        {
            ComparerIndex++;
            if (ComparerIndex >= Comparers.Length)
                ComparerIndex = 0;
        }

        public InventoryComparer CurrentComparer()
        {
            return Comparers[ComparerIndex];
        }
    }
}
