namespace WillowTree.Inventory
{
    public class InventoryDataWrapper : IInventoryData
    {
        public InventoryList BankList => InventoryData.BankList;

        public InventoryList ItemList => InventoryData.ItemList;

        public InventoryList LockerList => InventoryData.LockerList;

        public InventoryList WeaponList => InventoryData.WeaponList;
    }
}
