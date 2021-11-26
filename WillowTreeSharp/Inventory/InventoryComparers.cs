namespace WillowTree.Inventory
{
    public static class InventoryComparers
    {
        public static InventoryComparer KeyComparer = new InventoryComparer(new[] { 8 });
        public static InventoryComparer CategoryNameComparer = new InventoryComparer(new[] { 2, 0, 8 });
        public static InventoryComparer ManufacturerNameComparer = new InventoryComparer(new[] { 6, 0, 8 });
        public static InventoryComparer CategoryRarityLevelComparer = new InventoryComparer(new[] { 2, 1, 7, 8 });

        public static InventoryComparer CategoryTitlePrefixModelComparer =
            new InventoryComparer(new[] { 2, 3, 4, 5, 8 });

        public static InventoryComparer CategoryLevelNameComparer = new InventoryComparer(new[] { 2, 7, 0, 8 });
        public static InventoryComparer NameComparer = new InventoryComparer(new[] { 0, 8 });
        public static InventoryComparer RarityLevelComparer = new InventoryComparer(new[] { 1, 7, 8 });
        public static InventoryComparer TitlePrefixModelComparer = new InventoryComparer(new[] { 3, 4, 5, 8 });
        public static InventoryComparer LevelNameComparer = new InventoryComparer(new[] { 7, 0, 8 });

        public static InventoryComparer[] DefaultComparerList =
        {
            KeyComparer, CategoryNameComparer, ManufacturerNameComparer, CategoryRarityLevelComparer,
            CategoryTitlePrefixModelComparer, CategoryLevelNameComparer, NameComparer, RarityLevelComparer,
            LevelNameComparer
        };
    }
}
