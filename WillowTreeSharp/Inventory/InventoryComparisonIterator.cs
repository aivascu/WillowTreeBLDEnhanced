namespace WillowTree.Inventory
{
    public class InventoryComparisonIterator
    {
        public int ComparerIndex;
        public InventoryComparer[] Comparers;

        public InventoryComparisonIterator(InventoryComparer[] comparers)
        {
            this.Comparers = comparers;
        }

        public void NextComparer()
        {
            this.ComparerIndex++;
            if (this.ComparerIndex >= this.Comparers.Length)
            {
                this.ComparerIndex = 0;
            }
        }

        public void PreviousComparer()
        {
            this.ComparerIndex++;
            if (this.ComparerIndex >= this.Comparers.Length)
            {
                this.ComparerIndex = 0;
            }
        }

        public InventoryComparer CurrentComparer()
        {
            return this.Comparers[this.ComparerIndex];
        }
    }
}
