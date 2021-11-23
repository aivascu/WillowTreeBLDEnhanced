namespace WillowTree.Inventory
{
    public static class InventoryComparers
    {
        public static InventoryComparer KeyComparer = new InventoryComparer(new int[] { 8 });
        public static InventoryComparer CategoryNameComparer = new InventoryComparer(new int[] { 2, 0, 8 });
        public static InventoryComparer ManufacturerNameComparer = new InventoryComparer(new int[] { 6, 0, 8 });
        public static InventoryComparer CategoryRarityLevelComparer = new InventoryComparer(new int[] { 2, 1, 7, 8 });
        public static InventoryComparer CategoryTitlePrefixModelComparer = new InventoryComparer(new int[] { 2, 3, 4, 5, 8 });
        public static InventoryComparer CategoryLevelNameComparer = new InventoryComparer(new int[] { 2, 7, 0, 8 });
        public static InventoryComparer NameComparer = new InventoryComparer(new int[] { 0, 8 });
        public static InventoryComparer RarityLevelComparer = new InventoryComparer(new int[] { 1, 7, 8 });
        public static InventoryComparer TitlePrefixModelComparer = new InventoryComparer(new int[] { 3, 4, 5, 8 });
        public static InventoryComparer LevelNameComparer = new InventoryComparer(new int[] { 7, 0, 8 });

        public static InventoryComparer[] DefaultComparerList =
                new InventoryComparer[]
                    {
                        KeyComparer,
                        CategoryNameComparer,
                        ManufacturerNameComparer,
                        CategoryRarityLevelComparer,
                        CategoryTitlePrefixModelComparer,
                        CategoryLevelNameComparer,
                        NameComparer,
                        RarityLevelComparer,
                        LevelNameComparer
                    };
    }
}
