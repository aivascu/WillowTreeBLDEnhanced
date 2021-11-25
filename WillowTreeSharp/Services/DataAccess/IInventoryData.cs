using WillowTree.Inventory;

namespace WillowTree.Services.DataAccess
{
    public interface IInventoryData
    {
        InventoryList BankList { get; }
        InventoryList ItemList { get; }
        InventoryList LockerList { get; }
        InventoryList WeaponList { get; }

    }
}
