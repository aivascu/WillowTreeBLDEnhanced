using Aga.Controls.Tree;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using System.Xml;
using WillowTree.CustomControls;
using WillowTree.Services.DataAccess;

namespace WillowTree.Inventory
{
    public class InventoryTreeList
    {
        public Dictionary<string, string> CategoryLookup = new Dictionary<string, string>()
        {
            { "all", "ALL" },
            { "ammo", "AMMO" },
            { "ammoupgrade", "AMMO UPGRADES" },
            { "any", "ANY" },
            { "ar", "ASSAULT RIFLES" },
            { "comm", "CLASS MODS" },
            { "compare", "COMPARE" },
            { "elemental", "ELEMENTAL ARTIFACTS" },
            { "equipped", "EQUIPPED" },
            { "eridian", "ERIDIAN WEAPONS" },
            { "grenade", "GRENADES" },
            { "health", "MED KITS" },
            { "healthTab", "HEALTH" },
            { "instahealth", "INSTA-HEALTHS" },
            { "items", "ITEMS" },
            { "manufacturers", "BRANDS" },
            { "mod", "GRENADE MODS" },
            { "money", "MONEY" },
            { "none", "STOCK ITEMS" },
            { "personal", "PERSONAL" },
            { "repeater", "REPEATERS" },
            { "revolver", "REVOLVERS" },
            { "rocket", "ROCKET LAUNCHERS" },
            { "sdu", "UPGRADES" },
            { "shield", "SHIELDS" },
            { "shop", "SHOP" },
            { "shotgun", "SHOTGUNS" },
            { "smg", "SUB-MACHINE GUNS" },
            { "sniper", "SNIPER RIFLES" },
            { "types", "TYPES" },
            { "weapon", "WEAPONS" },
        };

        public InventoryComparisonIterator IEComparisonEngine =
            new InventoryComparisonIterator(InventoryComparers.DefaultComparerList);

        public int NavigationLayers;
        public List<InventoryEntry> Sorted;
        public WTTreeView Tree;
        public InventoryList Unsorted;
        public bool updateSelection;

        private int _lastNodeIndex = -1;

        private TreeNodeAdv _next;

        private ColoredTextNode _node;

        private TreeNodeAdv _parent;

        private readonly string[] itemParts = new string[]
        {
            "Item Grade",
            "Item Type",
            "Body",
            "Left Side",
            "Right Side",
            "Material",
            "Manufacturer",
            "Prefix",
            "Title"
        };

        private readonly string[] weaponParts = new string[]
        {
            "Item Grade",
            "Manufacturer",
            "Weapon Type",
            "Body",
            "Grip",
            "Mag",
            "Barrel",
            "Sight",
            "Stock",
            "Action",
            "Accessory",
            "Material",
            "Prefix",
            "Title"
        };

        public InventoryTreeList(WTTreeView tree, InventoryList ilist)
        {
            this.Unsorted = ilist;
            this.Sorted = new List<InventoryEntry>();
            this.Tree = tree;
            this.NavigationLayers = 1;

            Unsorted.EntryAdd += OnEntryAdd;
            Unsorted.EntryRemove += OnEntryRemove;
            Unsorted.EntryRemoveNode += OnEntryRemoveNode;
            Unsorted.ListReload += OnListReload;
            Unsorted.NameFormatChanged += OnNameFormatChanged;
            Unsorted.TreeThemeChanged += OnTreeThemeChanged;
            //Unsorted.NavigationDepthChanged += OnNavigationDepthChanged;
            //Unsorted.SortModeChanged += OnSortModeChanged;
        }

        public void Add(InventoryEntry entry)
        {
            Unsorted.Add(entry);
            // Implicit event call to OnEntryAdd occurs here
        }

        public void AddNew(byte invType)
        {
            List<int> values = null;
            List<string> parts = new List<string>();

            if (invType == InventoryType.Weapon)
            {
                values = new List<int>() { 0, 5, 0, 0 };
                parts.AddRange(weaponParts);
            }
            else if (invType == InventoryType.Item)
            {
                values = new List<int>() { 1, 5, 0, 0 };
                parts.AddRange(itemParts);
            }

            this.Add(new InventoryEntry(invType, parts, values));
        }

        // The last parent node to have a child added
        // The child that was added to the last parent node
        public void AddToTreeView(InventoryEntry entry)
        {
            _parent = CreateNavigationNodes(entry);

            _node = new ColoredTextNode
            {
                Tag = entry,
                ForeColor = entry.Color,
                Text = entry.Name
            };

            Collection<Node> nodes;
            if (_parent == null)
            {
                _parent = Tree.Root;
                nodes = (Tree.Model as TreeModel).Nodes;
            }
            else
            {
                nodes = (_parent.Tag as Node).Nodes;
            }

            IComparer<InventoryEntry> Comparer = IEComparisonEngine.CurrentComparer();

            for (int i = nodes.Count - 1; i >= 0; i--)
            {
                if (Comparer.Compare(nodes[i].Tag as InventoryEntry, entry) == -1)
                {
                    _lastNodeIndex = i + 1;
                    nodes.Insert(_lastNodeIndex, _node);
                    return;
                }
            }
            nodes.Insert(0, _node);
            _lastNodeIndex = 0;
        }

        public void AdjustSelectionAfterAdd()
        {
            Tree.SelectedNode = _parent.FindNodeAdvByTag(_node, false);
        }

        public void AdjustSelectionAfterRemove()
        {
            if (_next != null)
            {
                Tree.SelectedNode = _next;
            }
        }

        public void Clear()
        {
            Unsorted.Clear();
            // Implicit event call to OnListReload here
        }

        public void ClearTreeView()
        {
            Tree.Clear();
        }

        public void CopySelected(InventoryList dest, bool deleteSource)
        {
            TreeNodeAdv[] nodes = Tree.SelectedNodes.ToArray();

            Tree.BeginUpdate();
            foreach (TreeNodeAdv node in nodes)
            {
                InventoryEntry old = node.GetEntry() as InventoryEntry;
                // If the entry is null it is because the tag isn't an inventory
                // entry object.  That means it is a category node so don't duplicate
                // it.
                if (old == null)
                {
                    continue;
                }

                InventoryEntry entry = new InventoryEntry(old);
                if (deleteSource)
                {
                    Remove(node, false);
                }

                dest.Add(entry);
            }
            Tree.EndUpdate();
        }

        public TreeNodeAdv CreateNavigationNodes(InventoryEntry entry)
        {
            int[] sortmodes = IEComparisonEngine.CurrentComparer().comparisons;
            int loopcount = (NavigationLayers < sortmodes.Length) ? NavigationLayers : sortmodes.Length;

            TreeNodeAdv navnode = null;
            TreeNodeAdv newbranch = null;
            string currentcategory;
            string categorytext;
            for (int i = 0; i < loopcount; i++)
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

                switch (sortmodes[i])
                {
                    case 2:
                        currentcategory = entry.Category;
                        if (currentcategory == "")
                        {
                            currentcategory = "none";
                        }

                        if (!CategoryLookup.TryGetValue(currentcategory, out categorytext))
                        {
                            currentcategory = "(Unknown)";
                            categorytext = "(Unknown)";
                        }
                        break;

                    case 6:
                        currentcategory = entry.NameParts[0];
                        if (currentcategory == "")
                        {
                            currentcategory = "No Manufacturer";
                        }

                        categorytext = currentcategory;
                        break;

                    case 7:
                        currentcategory = "Level " + entry.EffectiveLevel.ToString();
                        categorytext = currentcategory;
                        break;

                    case 3:
                        currentcategory = entry.NameParts[3];
                        if (currentcategory == "")
                        {
                            currentcategory = "No Title";
                        }

                        categorytext = currentcategory;
                        break;

                    case 4:
                        currentcategory = entry.NameParts[2];
                        if (currentcategory == "")
                        {
                            currentcategory = "No Prefix";
                        }

                        categorytext = currentcategory;
                        break;

                    case 5:
                        currentcategory = entry.NameParts[1];
                        if (currentcategory == "")
                        {
                            currentcategory = "No Model";
                        }

                        categorytext = currentcategory;
                        break;

                    case 1:
                        currentcategory = entry.Name;
                        categorytext = currentcategory;
                        break;

                    default:
                        return navnode;
                }

                if (navnode != null)
                {
                    newbranch = navnode.FindFirstByTag(currentcategory, false);
                }
                else
                {
                    newbranch = Tree.Root.FindFirstByTag(currentcategory, false);
                }

                if (newbranch == null)
                {
                    // This category does not exist yet.  Create a node for it.
                    ColoredTextNode data = new ColoredTextNode
                    {
                        Tag = currentcategory,
                        ForeColor = Color.LightSkyBlue
                    };
                    if (GlobalSettings.UseColor)
                    {
                        data.Text = categorytext;
                    }
                    else
                    {
                        data.Text = "--- " + categorytext + " ---";
                    }

                    if (navnode == null)
                    {
                        (Tree.Model as TreeModel).Nodes.Add(data);
                        navnode = Tree.Root;
                    }
                    else
                    {
                        navnode.AddNode(data);
                    }

                    newbranch = navnode.Children[navnode.Children.Count - 1];
                }
                // Update the navnode then iterate again for the next tier of
                // category nodes until all category nodes are present
                navnode = newbranch;
            }
            return navnode;
        }

        public void DeleteSelected()
        {
            TreeNodeAdv[] nodes = Tree.SelectedNodes.ToArray();

            this.Remove(nodes);
            AdjustSelectionAfterRemove();
        }

        public void Duplicate(InventoryEntry entry)
        {
            InventoryEntry copy = new InventoryEntry(entry);
            Unsorted.Add(copy);
            // Implicit event call to OnEntryAdd occurs here
        }

        public void DuplicateSelected()
        {
            foreach (TreeNodeAdv node in Tree.SelectedNodes.ToArray())
            {
                InventoryEntry old = node.GetEntry() as InventoryEntry;
                // If the entry is null it is because the tag isn't an inventory
                // entry object.  That means it is a category node so don't duplicate
                // it.
                if (old == null)
                {
                    continue;
                }

                InventoryEntry entry = new InventoryEntry(old);
                Add(entry);
            }
        }

        public void ImportFromXml(string InputFile, int EntryType)
        {
            XmlDocument doc = new XmlDocument();

            try
            {
                doc.Load(InputFile);
            }
            catch
            {
                MessageBox.Show("Error loading the XML file.  Nothing imported.");
                return;
            }

            XmlNodeList nodes = doc.SelectNodes("/INI/Section");
            Tree.BeginUpdate();
            foreach (XmlNode node in nodes)
            {
                InventoryEntry entry = new InventoryEntry(node);
                if (entry.Type == InventoryType.Unknown)
                {
                    MessageBox.Show("Invalid entry skipped in XML file");
                    continue;
                }

                // Check to make sure the item is the right type then add it
                // ItemType -1 = unknown, 0 = weapons, 1 = items, 2 = any type
                if ((EntryType == InventoryType.Any) || (EntryType == entry.Type))
                {
                    this.Add(entry);
                }
            }
            Tree.EndUpdate();
        }

        public void IncreaseNavigationDepth()
        {
            NavigationLayers++;
            if (NavigationLayers > 3)
            {
                NavigationLayers = 0;
            }

            UpdateTree();
        }

        public void LoadFromXml(string InputFile, int EntryType)
        {
            Tree.BeginUpdate();
            this.Clear();
            ImportFromXml(InputFile, EntryType);
            Tree.EndUpdate();
        }

        public void NextSort()
        {
            IEComparisonEngine.NextComparer();
            SortByCustom();
        }

        public void OnEntryAdd(InventoryList ilist, InventoryEntry entry)
        {
            Sorted.Add(entry);
            AddToTreeView(entry);
        }

        public void OnEntryRemove(InventoryList ilist, InventoryEntry entry)
        {
            Sorted.Remove(entry);
            RemoveFromTreeView(entry, false);
        }

        public void OnEntryRemoveNode(InventoryList ilist, TreeNodeAdv node)
        {
            InventoryEntry entry = node.GetEntry() as InventoryEntry;
            Sorted.Remove(entry);
            RemoveFromTreeView(node, false);
        }

        public void OnListReload(InventoryList ilist)
        {
            Sorted.Clear();
            ClearTreeView();

            Sorted.AddRange(Unsorted.Items.Values);
            SortByCustom();
        }

        public void OnNameFormatChanged(InventoryList ilist)
        {
            UpdateNames();
        }

        public void OnTreeThemeChanged(InventoryList ilist, TreeViewTheme theme)
        {
            Tree.Theme = theme;
        }

        public void PurgeDuplicates()
        {
            string lastGoodFile = GameData.OpenedLockerFilename();    //Keep last valid locker path file
            string tempfile = GameData.DataPath + "purgeduplicates.temp";
            SaveToXml(tempfile);
            XmlFile.PurgeDuplicates(tempfile);
            LoadFromXml(tempfile, InventoryType.Any);
            File.Delete(tempfile);

            GameData.OpenedLockerFilename(lastGoodFile);  //Restore last valid locker path file
        }

        public void Remove(TreeNodeAdv nodeAdv, bool updateSelection)
        {
            this.updateSelection = updateSelection;
            Unsorted.Remove(nodeAdv);
        }

        public void Remove(IEnumerable<TreeNodeAdv> nodesToRemove)
        {
            TreeNodeAdv[] nodes = nodesToRemove.ToArray();
            Tree.BeginUpdate();

            foreach (TreeNodeAdv node in nodes)
            {
                // Nodes with children are not items they are categories
                if (node.Children.Count == 0)
                {
                    Remove(node, false);
                }
            }
            Tree.EndUpdate();
        }

        public void RemoveFromTreeView(TreeNodeAdv node, bool selectNextNode)
        {
            _next = null;

            if (node == null)
            {
                return;
            }

            // If the node being removed is the selected node in the tree
            // a new node will have to be selected.
            if (node == Tree.SelectedNode)
            {
                _next = node.NextVisibleNode;
                // Navigate through children until an actual item node is
                // found if the new node is a navigation node.
                if (_next != null)
                {
                    while (_next.Children.Count > 0)
                    {
                        _next = _next.Children[0];
                    }
                    //while (newnode.Children.Count > 0)
                    //    newnode = newnode.Children[0];
                }
            }

            // Remove the item node and any parent navigation nodes that are
            // empty.
            if (node != null)
            {
                TreeNodeAdv parent = node.Parent;
                node.Remove();
                while ((parent != Tree.Root) && (parent.Children.Count == 0))
                {
                    node = parent;
                    parent = node.Parent;
                    node.Remove();
                }
            }

            if (selectNextNode)
            {
                Tree.SelectedNode = _next;
            }
        }

        public void RemoveFromTreeView(InventoryEntry entry, bool selectNextNode)
        {
            // This is much slower than removing an entry by its node because it
            // has to search the whole tree for the entry first.  Use
            // RemoveFromTreeView(TreeNodeAdv node, bool selectNextNode) when
            // possible.
            _next = null;

            // First find the node being removed in the tree
            TreeNodeAdv node = Tree.FindFirstNodeByTag(entry, true);
            if (node == null)
            {
                return;
            }

            // If the node being removed is the selected node in the tree
            // a new node will have to be selected.
            if (node == Tree.SelectedNode)
            {
                _next = node.NextVisibleNode;
                // Navigate through children until an actual item node is
                // found if the new node is a navigation node.
                if (_next != null)
                {
                    while (_next.Children.Count > 0)
                    {
                        _next = _next.Children[0];
                    }
                    //while (newnode.Children.Count > 0)
                    //    newnode = newnode.Children[0];
                }
            }

            // Remove the item node and any parent navigation nodes that are
            // empty.
            if (node != null)
            {
                TreeNodeAdv parent = node.Parent;
                node.Remove();
                while ((parent != Tree.Root) && (parent.Children.Count == 0))
                {
                    node = parent;
                    parent = node.Parent;
                    node.Remove();
                }
            }

            if (selectNextNode)
            {
                Tree.SelectedNode = _next;
            }
        }

        public void SaveToXml(string InputFile)
        {
            try
            {
                XmlDocument doc = new XmlDocument();
                using (XmlTextWriter writer = new XmlTextWriter(InputFile, System.Text.Encoding.UTF8))
                {
                    writer.Formatting = Formatting.Indented;
                    writer.Indentation = 2;
                    writer.WriteStartDocument();
                    writer.WriteStartElement("INI");
                    foreach (InventoryEntry entry in this.Sorted)
                    {
                        writer.WriteStartElement("Section");
                        writer.WriteRaw(entry.ToXmlText());
                        writer.WriteEndElement();
                    }
                    writer.WriteEndElement();
                    writer.WriteEndDocument();

                    GameData.OpenedLockerFilename(InputFile);
                }
            }
            catch (Exception e)
            {
                MessageBox.Show("Failure while writing to XML file \"" + InputFile + "\".  " + e.Message);
            }
        }

        public void SortByCustom()
        {
            Sorted.Sort(IEComparisonEngine.CurrentComparer().Compare);
            UpdateTree();
        }

        public void UpdateNames()
        {
            foreach (InventoryEntry entry in Sorted)
            {
                entry.BuildName();
            }

            UpdateTree();
        }

        public void UpdateTree()
        {
            // This procedure clears the GUI tree and rebuilds it.  It
            // adds navigation nodes as needed to show item categories
            Tree.BeginUpdate();
            Tree.Clear();
            foreach (InventoryEntry entry in Sorted)
            {
                AddToTreeView(entry);
            }

            Tree.EndUpdate();
        }
    }
}
