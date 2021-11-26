namespace WillowTree.Inventory
{
    public interface IInventoryData
    {
        InventoryList BankList { get; }
        InventoryList ItemList { get; }
        InventoryList LockerList { get; }
        InventoryList WeaponList { get; }

    }
}
