/*  This file is part of WillowTree#
 *
 *  Copyright (C) 2011 Matthew Carter <matt911@users.sf.net>
 *  Copyright (C) 2010, 2011 XanderChaos
 *  Copyright (C) 2011 Thomas Kaiser
 *  Copyright (C) 2010 JackSchitt
 *
 *  WillowTree# is free software: you can redistribute it and/or modify
 *  it under the terms of the GNU General Public License as published by
 *  the Free Software Foundation, either version 3 of the License, or
 *  (at your option) any later version.
 *
 *  WillowTree# is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 *  GNU General Public License for more details.
 *
 *  You should have received a copy of the GNU General Public License
 *  along with WillowTree#.  If not, see <http://www.gnu.org/licenses/>.
 */

using Aga.Controls.Tree;
using Aga.Controls.Tree.NodeControls;
using System.Collections.Generic;
using WillowTree.CustomControls;

namespace WillowTree
{
    public static class AgaControlsExtensions
    {
        // Aga.Controls is basically just TreeViewAdv and all the utility stuff
        // needed to make it work.  These are extensions added to try to help do
        // normal things like insert and remove nodes because the way the
        // TreeViewAdv control works is very strange.
        //
        // TreeViewAdv uses two layers of nodes.  One set of nodes (class
        // TreeNodeAdv) is used to return user selection information and the other
        // set of nodes (class Node) is used to create the tree and store data in.
        // Both sets confusingly hold navigation information.  The Tag property
        // of each (TreeNodeAdv) points to one (Node) which holds the data, so
        // each time you check the selection of nodes you get a list of
        // (TreeNodeAdv)s but you can only add (Node)s to the tree.

        // This is not actually an extension method, but it is needed to work with
        // TreeViewAdv and is not form-specific so I placed it here.
        public static void ColoredTextNode_DrawText(object sender, DrawEventArgs e)
        {
            TreeViewTheme theme = (e.Node.Tree as WTTreeView).Theme;
            // When a text node is drawn, this callback is called to determine
            // the colors to use.  It is not necessary to set or modify any colors
            // in e (DrawEventArgs) unless custom colors are desired.  The color of
            // nodes usually depends on whether they are selected or active.  That
            // information is stored in e.Context.DrawSelectionMode.

            // e.Node is a TreeNodeAdv navigation node.  e.Node.Tag points to the
            // actual data node which must be of type Node or a descendant of
            // Node.  It is ColoredTextNode in this program.
            ColoredTextNode node = e.Node.Tag as ColoredTextNode;

            // The node is drawn with different colors depending on whether it is
            // selected and whether or not the control is active.  The state is
            // provided in e.Context.DrawSelection.
            //if (e.Context.DrawSelection == DrawSelectionMode.None)
            //{
            //    e.TextColor = theme.ForeColor;
            //    e.BackgroundBrush = theme.BackBrush;
            //}
            if (e.Context.DrawSelection == DrawSelectionMode.None)
            {
                e.TextColor = (e.Node.Tag as ColoredTextNode).ForeColor;
                e.BackgroundBrush = theme.BackBrush;
            }
            else if (e.Context.DrawSelection == DrawSelectionMode.Active)
            {
                e.TextColor = theme.HighlightForeColor;
                e.BackgroundBrush = theme.HighlightBackBrush;
            }
            else if (e.Context.DrawSelection == DrawSelectionMode.Inactive)
            {
                e.TextColor = theme.InactiveForeColor;
                e.BackgroundBrush = theme.InactiveBackBrush;
            }
            else if (e.Context.DrawSelection == DrawSelectionMode.FullRowSelect)
            {
                e.TextColor = theme.HighlightForeColor;
                e.BackgroundBrush = theme.BackBrush;
            }

            if (!e.Context.Enabled)
                e.TextColor = theme.DisabledForeColor;

            // Apply a custom font if the node has one
            if (node.Font != null)
                e.Font = node.Font;
        }

        public static TreeNodeAdv FindFirstNode(this WTTreeView tree, string searchText, bool searchChildren)
        {
            TreeNodeAdv root = tree.Root;
            return FindFirstNode(root, searchText, searchChildren);
        }

        public static TreeNodeAdv FindFirstNodeByTag(this WTTreeView tree, object searchTag, bool searchChildren)
        {
            TreeNodeAdv root = tree.Root;
            if (root.Children.Count == 0)
                return null;

            TreeNodeAdv node = root.Children[0];

            while (node != null)
            {
                Node data = node.Tag as Node;

                // Check this node
                if (data.Tag.Equals(searchTag))
                    return node;

                // Select the next node
                if ((searchChildren == true) && (node.Children.Count > 0))
                    node = node.Children[0];        // First child node
                else
                {
                    while (node.NextNode == null)
                    {
                        // No more siblings, so back to the parent
                        node = node.Parent;
                        if (node == root)
                            return null;
                    }
                    node = node.NextNode;           // Next sibling node
                }
            }
            return null;
        }

        public static IEnumerable<TreeNodeAdv> FindNodes(this WTTreeView tree, string searchText, bool searchChildren)
        {
            TreeNodeAdv root = tree.Root;
            if (root.Children.Count == 0)
                yield break;

            TreeNodeAdv node = root.Children[0];

            while (node != null)
            {
                Node data = node.Tag as Node;

                // Check this node
                if (data.Text == searchText)
                    yield return node;

                // Select the next node
                if ((searchChildren == true) && (node.Children.Count > 0))
                    node = node.Children[0];        // First child node
                else
                {
                    while (node.NextNode == null)
                    {
                        // No more siblings, so back to the parent
                        node = node.Parent;
                        if (node == root)
                            yield break;
                    }
                    node = node.NextNode;           // Next sibling node
                }
            }
        }

        public static IEnumerable<TreeNodeAdv> FindNodesByTag(this WTTreeView tree, object searchTag, bool searchChildren)
        {
            TreeNodeAdv root = tree.Root;
            if (root.Children.Count == 0)
                yield break;

            TreeNodeAdv node = root.Children[0];

            while (node != null)
            {
                Node data = node.Tag as Node;

                // Check this node
                if (data.Tag.Equals(searchTag))
                    yield return node;

                // Select the next node
                if ((searchChildren == true) && (node.Children.Count > 0))
                    node = node.Children[0];        // First child node
                else
                {
                    while (node.NextNode == null)
                    {
                        // No more siblings, so back to the parent
                        node = node.Parent;
                        if (node == root)
                            yield break;
                    }
                    node = node.NextNode;           // Next sibling node
                }
            }
        }

        public static void Clear(this WTTreeView tree)
        {
            (tree.Model as TreeModel).Nodes.Clear();
        }

        public static TreeNodeAdv FindFirstNode(this TreeNodeAdv root, string searchText, bool searchChildren)
        {
            if (root.Children.Count == 0)
                return null;

            TreeNodeAdv node = root.Children[0];

            while (node != null)
            {
                Node data = node.Tag as Node;

                // Check this node
                if (data.Text == searchText)
                    return node;

                // Select the next node
                if ((searchChildren == true) && (node.Children.Count > 0))
                    node = node.Children[0];        // First child node
                else
                {
                    while (node.NextNode == null)
                    {
                        // No more siblings, so back to the parent
                        node = node.Parent;
                        if (node == root)
                            return null;
                    }
                    node = node.NextNode;           // Next sibling node
                }
            }
            return null;
        }

        public static TreeNodeAdv FindFirstByTag(this TreeNodeAdv root, object searchTag, bool searchChildren)
        {
            if (root.Children.Count == 0)
                return null;

            TreeNodeAdv node = root.Children[0];
            while (node != null)
            {
                Node data = node.Tag as Node;

                // Check this node
                if (data.Tag.Equals(searchTag))
                    return node;

                // Select the next node
                if ((searchChildren == true) && (node.Children.Count > 0))
                    node = node.Children[0];        // First child node
                else
                {
                    while (node.NextNode == null)
                    {
                        // No more siblings, so back to the parent
                        node = node.Parent;
                        if (node == root)
                            return null;
                    }
                    node = node.NextNode;           // Next sibling node
                }
            }
            return null;
        }

        public static TreeNodeAdv FindNodeAdvByTag(this TreeNodeAdv root, object searchTag, bool searchChildren)
        {
            if (root.Children.Count == 0)
                return null;

            TreeNodeAdv node = root.Children[0];
            while (node != null)
            {
                // Check this node
                if (node.Tag.Equals(searchTag))
                    return node;

                // Select the next node
                if ((searchChildren == true) && (node.Children.Count > 0))
                    node = node.Children[0];        // First child node
                else
                {
                    while (node.NextNode == null)
                    {
                        // No more siblings, so back to the parent
                        node = node.Parent;
                        if (node == root)
                            return null;
                    }
                    node = node.NextNode;           // Next sibling node
                }
            }
            return null;
        }

        public static IEnumerable<TreeNodeAdv> FindNodes(this TreeNodeAdv root, string searchText, bool searchChildren)
        {
            if (root.Children.Count == 0)
                yield break;

            TreeNodeAdv node = root.Children[0];

            while (node != null)
            {
                Node data = node.Tag as Node;

                // Check this node
                if (data.Text == searchText)
                    yield return node;

                // Select the next node
                if ((searchChildren == true) && (node.Children.Count > 0))
                    node = node.Children[0];        // First child node
                else
                {
                    while (node.NextNode == null)
                    {
                        // No more siblings, so back to the parent
                        node = node.Parent;
                        if (node == root)
                            yield break;
                    }
                    node = node.NextNode;           // Next sibling node
                }
            }
        }

        public static IEnumerable<TreeNodeAdv> FindNodesByTag(this TreeNodeAdv root, object searchTag, bool searchChildren)
        {
            if (root.Children.Count == 0)
                yield break;

            TreeNodeAdv node = root.Children[0];

            while (node != null)
            {
                Node data = node.Tag as Node;

                // Check this node
                if (data.Tag.Equals(searchTag))
                    yield return node;

                // Select the next node
                if ((searchChildren == true) && (node.Children.Count > 0))
                    node = node.Children[0];        // First child node
                else
                {
                    while (node.NextNode == null)
                    {
                        // No more siblings, so back to the parent
                        node = node.Parent;
                        if (node == root)
                            yield break;
                    }
                    node = node.NextNode;           // Next sibling node
                }
            }
        }

        public static Node FindFirstNode(this Node root, string searchText, bool searchNodes)
        {
            if (root.Nodes.Count == 0)
                return null;

            Node node = root.Nodes[0];

            while (node != null)
            {
                // Check this node
                if (node.Text == searchText)
                    return node;

                // Select the next node
                if ((searchNodes == true) && (node.Nodes.Count > 0))
                    node = node.Nodes[0];        // First child node
                else
                {
                    while (node.NextNode == null)
                    {
                        // No more siblings, so back to the parent
                        node = node.Parent;
                        if (node == root)
                            return null;
                    }
                    node = node.NextNode;           // Next sibling node
                }
            }
            return null;
        }

        public static Node FindFirstByTag(this Node root, string searchTag, bool searchNodes)
        {
            if (root.Nodes.Count == 0)
                return null;

            Node node = root.Nodes[0];

            while (node != null)
            {
                // Check this node
                if (node.Tag as string == searchTag)
                    return node;

                // Select the next node
                if ((searchNodes == true) && (node.Nodes.Count > 0))
                    node = node.Nodes[0];        // First child node
                else
                {
                    while (node.NextNode == null)
                    {
                        // No more siblings, so back to the parent
                        node = node.Parent;
                        if (node == root)
                            return null;
                    }
                    node = node.NextNode;           // Next sibling node
                }
            }
            return null;
        }

        public static IEnumerable<Node> FindNodes(this Node root, string searchText, bool searchNodes)
        {
            if (root.Nodes.Count == 0)
                yield break;

            Node node = root.Nodes[0];

            while (node != null)
            {
                // Check this node
                if (node.Text == searchText)
                    yield return node;

                // Select the next node
                if ((searchNodes == true) && (node.Nodes.Count > 0))
                    node = node.Nodes[0];        // First child node
                else
                {
                    while (node.NextNode == null)
                    {
                        // No more siblings, so back to the parent
                        node = node.Parent;
                        if (node == root)
                            yield break;
                    }
                    node = node.NextNode;           // Next sibling node
                }
            }
        }

        public static IEnumerable<Node> FindNodesByTag(this Node root, string searchTag, bool searchNodes)
        {
            if (root.Nodes.Count == 0)
                yield break;

            Node node = root.Nodes[0];

            while (node != null)
            {
                // Check this node
                if (node.Tag as string == searchTag)
                    yield return node;

                // Select the next node
                if ((searchNodes == true) && (node.Nodes.Count > 0))
                    node = node.Nodes[0];        // First child node
                else
                {
                    while (node.NextNode == null)
                    {
                        // No more siblings, so back to the parent
                        node = node.Parent;
                        if (node == root)
                            yield break;
                    }
                    node = node.NextNode;           // Next sibling node
                }
            }
        }

        public static void InsertNode(this TreeNodeAdv parent, int index, Node dataNode)
        {
            (parent.Tag as Node).Nodes.Insert(index, dataNode);
        }

        public static void AddNode(this TreeNodeAdv parent, Node dataNode)
        {
            (parent.Tag as Node).Nodes.Add(dataNode);
        }

        public static void Remove(this TreeNodeAdv node)
        {
            (node.Tag as Node).Parent = null;
        }

        public static object GetEntry(this TreeNodeAdv nodeAdv)
        {
            return (nodeAdv.Tag as Node).Tag;
        }

        public static void SetEntry(this TreeNodeAdv nodeAdv, object entry)
        {
            (nodeAdv.Tag as Node).Tag = entry;
        }

        /// <summary>
        /// Assumes that the Tag in the data Node pointed to by this TreeNodeAdv
        /// is a string value and returns it.  Shorthand for (Tag as Node).Tag.
        /// </summary>
        public static string GetKey(this TreeNodeAdv nodeAdv)
        {
            return (nodeAdv.Tag as Node).Tag as string;
        }

        /// <summary>
        /// Sets the Tag in the data Node pointed to by this TreeNodeAdv to a string
        /// value.
        /// </summary>
        public static void SetKey(this TreeNodeAdv nodeAdv, string key)
        {
            (nodeAdv.Tag as Node).Tag = key;
        }

        public static string GetText(this TreeNodeAdv nodeAdv)
        {
            return (nodeAdv.Tag as Node).Text;
        }

        public static void SetText(this TreeNodeAdv nodeAdv, string text)
        {
            (nodeAdv.Tag as Node).Text = text;
        }

        public static ColoredTextNode Data(this TreeNodeAdv node)
        {
            return node.Tag as ColoredTextNode;
        }

        public static string Text(this TreeNodeAdv node)
        {
            return (node.Tag as Node).Text;
        }
    }
}
