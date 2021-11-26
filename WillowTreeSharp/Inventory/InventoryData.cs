namespace WillowTree.Inventory
{
    public static class InventoryData
    {
        public static InventoryList WeaponList { get; } = new InventoryList(InventoryType.Weapon);
        public static InventoryList ItemList { get; } = new InventoryList(InventoryType.Item);
        public static InventoryList BankList { get; } = new InventoryList(InventoryType.Any);
        public static InventoryList LockerList { get; } = new InventoryList(InventoryType.Any);
    }
}
