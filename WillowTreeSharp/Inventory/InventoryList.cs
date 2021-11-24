using Aga.Controls.Tree;
using System.Collections.Generic;
using WillowTree.CustomControls;
using WillowTree.Services.DataAccess;

namespace WillowTree.Inventory
{
    public class InventoryList
    {
        public byte invType;

        public Dictionary<string, InventoryEntry> Items;

        public InventoryList(byte inventoryType)
        {
            this.Items = new Dictionary<string, InventoryEntry>();
            this.invType = inventoryType;
        }

        public delegate void EntryChangeByKeyEventHandler(InventoryList ilist, string key);

        public delegate void EntryChangeEventHandler(InventoryList ilist, InventoryEntry entry);

        public delegate void EntryChangeNodeEventHandler(InventoryList ilist, TreeNodeAdv node);

        public delegate void InventoryListEventHandler(InventoryList ilist);

        public delegate void TreeThemeChangedEventHandler(InventoryList ilist, TreeViewTheme theme);

        public event EntryChangeEventHandler EntryAdd;

        public event EntryChangeEventHandler EntryRemove;

        public event EntryChangeNodeEventHandler EntryRemoveNode;

        public event InventoryListEventHandler ListReload;

        public event InventoryListEventHandler NameFormatChanged;

        public event TreeThemeChangedEventHandler TreeThemeChanged;

        public void Add(InventoryEntry entry)
        {
            entry.Key = GameData.CreateUniqueKey();
            this.Items.Add(entry.Key, entry);
            this.OnEntryAdd(entry);
        }

        public void AddSilent(InventoryEntry entry)
        {
            entry.Key = GameData.CreateUniqueKey();
            this.Items.Add(entry.Key, entry);
        }

        public void Clear()
        {
            this.Items.Clear();
            this.OnListReload();
        }

        public void ClearSilent()
        {
            this.Items.Clear();
        }

        public void Duplicate(InventoryEntry entry)
        {
            InventoryEntry copy = new InventoryEntry(entry)
            {
                Key = GameData.CreateUniqueKey()
            };
            this.Items.Add(copy.Key, copy);
            this.OnEntryAdd(copy);
        }

        public void OnEntryAdd(InventoryEntry entry)
        {
            if (EntryAdd != null)
            {
                EntryAdd(this, entry);
            }
        }

        public void OnEntryRemoveNode(TreeNodeAdv node)
        {
            if (EntryRemoveNode != null)
            {
                EntryRemoveNode(this, node);
            }
        }

        public void OnListReload()
        {
            if (ListReload != null)
            {
                ListReload(this);
            }
        }

        public void OnNameFormatChanged()
        {
            if (NameFormatChanged != null)
            {
                NameFormatChanged(this);
            }
        }

        public void OnTreeThemeChanged(TreeViewTheme theme)
        {
            if (TreeThemeChanged != null)
            {
                TreeThemeChanged(this, theme);
            }
        }

        public void Remove(TreeNodeAdv node)
        {
            InventoryEntry entry = node.GetEntry() as InventoryEntry;
            this.Items.Remove(entry.Key);
            this.OnEntryRemoveNode(node);
        }
    }
}
