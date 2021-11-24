using Aga.Controls.Tree;
using System.Text;
using System.Xml;

namespace WillowTree.Services.DataAccess
{
    /// <summary>
    /// Clear duplicate items from an xml item/weapon file
    /// TODO: This is an ugly method that probably could be done in an easier
    /// to understand, more correct, or faster way.  It does seem to work
    /// though, so I'm not going to try to rewrite it right now.
    /// </summary>
    public class PurgeDuplicateNodesCommand
    {
        public PurgeDuplicateNodesCommand(string filePath)
        {
            this.FilePath = filePath;
        }

        public string FilePath { get; }

        
        public void Execute()
        {
            // A tree model to store the nodes in
            TreeModel model = new TreeModel();

            // rootnode
            Node ndroot = new Node("INI");
            model.Nodes.Add(ndroot);

            XmlDocument xmlrdrdoc = new XmlDocument();

            xmlrdrdoc.Load(this.FilePath);

            // get a list of all items
            foreach (XmlNode xn in xmlrdrdoc.SelectNodes("/INI/Section"))
            {
                Node ndparent = ndroot;
                bool bFound;

                string[] strParts =
                {
                    xn.GetElement("Type", ""),
                    xn.GetElement("Part1", ""),
                    xn.GetElement("Part2", ""),
                    xn.GetElement("Part3", ""),
                    xn.GetElement("Part4", ""),
                    xn.GetElement("Part5", ""),
                    xn.GetElement("Part6", ""),
                    xn.GetElement("Part7", ""),
                    xn.GetElement("Part8", ""),
                    xn.GetElement("Part9", ""),
                    xn.GetElement("Part10", ""),
                    xn.GetElement("Part11", ""),
                    xn.GetElement("Part12", ""),
                    xn.GetElement("Part13", ""),
                    xn.GetElement("Part14", ""),
                    xn.GetElement("Name", ""),
                    xn.GetElement("Rating", ""),
                    xn.GetElement("Description", ""),
                    xn.GetElement("RemAmmo_Quantity", ""),
                    xn.GetElement("Quality", ""),
                    xn.GetElement("Level", ""),
                };

                for (int partIndex = 0; partIndex < 21; partIndex++)
                {
                    // All sections
                    // read the xml values
                    bFound = false;

                    for (int ndcnt = 0; ndcnt < ndparent.Nodes.Count; ndcnt++)
                    {
                        if (ndparent.Nodes[ndcnt].Text == strParts[partIndex])
                        {
                            bFound = true;
                            ndparent = ndparent.Nodes[ndcnt];
                            break;
                        }
                    }

                    if (!bFound)
                    {
                        Node ndchild = new ColoredTextNode
                        {
                            Text = strParts[partIndex]
                        };
                        ndparent.Nodes.Add(ndchild);
                        ndparent = ndchild;
                    }
                }
            }

            XmlTextWriter writer = new XmlTextWriter(this.FilePath, new ASCIIEncoding())
            {
                Formatting = Formatting.Indented,
                Indentation = 2
            };
            writer.WriteStartDocument();
            writer.WriteComment("Comment");
            writer.WriteStartElement("INI");

            for (int ndcntrt = 0; ndcntrt < ndroot.Nodes.Count; ndcntrt++)
            {
                Node ndtype = ndroot.Nodes[ndcntrt];
                for (int ndcnttype = 0; ndcnttype < ndtype.Nodes.Count; ndcnttype++)
                {
                    Node ndpart1 = ndtype.Nodes[ndcnttype];

                    for (int ndcntpart1 = 0; ndcntpart1 < ndpart1.Nodes.Count; ndcntpart1++)
                    {
                        Node ndpart2 = ndpart1.Nodes[ndcntpart1];

                        for (int ndcntpart2 = 0; ndcntpart2 < ndpart2.Nodes.Count; ndcntpart2++)
                        {
                            Node ndpart3 = ndpart2.Nodes[ndcntpart2];
                            for (int ndcntpart3 = 0; ndcntpart3 < ndpart3.Nodes.Count; ndcntpart3++)
                            {
                                Node ndpart4 = ndpart3.Nodes[ndcntpart3];

                                for (int ndcntpart4 = 0; ndcntpart4 < ndpart4.Nodes.Count; ndcntpart4++)
                                {
                                    Node ndpart5 = ndpart4.Nodes[ndcntpart4];

                                    for (int ndcntpart5 = 0; ndcntpart5 < ndpart5.Nodes.Count; ndcntpart5++)
                                    {
                                        Node ndpart6 = ndpart5.Nodes[ndcntpart5];

                                        for (int ndcntpart6 = 0; ndcntpart6 < ndpart6.Nodes.Count; ndcntpart6++)
                                        {
                                            Node ndpart7 = ndpart6.Nodes[ndcntpart6];

                                            for (int ndcntpart7 = 0; ndcntpart7 < ndpart7.Nodes.Count; ndcntpart7++)
                                            {
                                                Node ndpart8 = ndpart7.Nodes[ndcntpart7];

                                                for (int ndcntpart8 = 0; ndcntpart8 < ndpart8.Nodes.Count; ndcntpart8++)
                                                {
                                                    Node ndpart9 = ndpart8.Nodes[ndcntpart8];

                                                    for (int ndcntpart9 = 0;
                                                        ndcntpart9 < ndpart9.Nodes.Count;
                                                        ndcntpart9++)
                                                    {
                                                        Node ndpart10 = ndpart9.Nodes[ndcntpart9];

                                                        for (int ndcntpart10 = 0;
                                                            ndcntpart10 < ndpart10.Nodes.Count;
                                                            ndcntpart10++)
                                                        {
                                                            Node ndpart11 = ndpart10.Nodes[ndcntpart10];

                                                            for (int ndcntpart11 = 0;
                                                                ndcntpart11 < ndpart11.Nodes.Count;
                                                                ndcntpart11++)
                                                            {
                                                                Node ndpart12 = ndpart11.Nodes[ndcntpart11];

                                                                for (int ndcntpart12 = 0;
                                                                    ndcntpart12 < ndpart12.Nodes.Count;
                                                                    ndcntpart12++)
                                                                {
                                                                    Node ndpart13 = ndpart12.Nodes[ndcntpart12];

                                                                    for (int ndcntpart13 = 0;
                                                                        ndcntpart13 < ndpart13.Nodes.Count;
                                                                        ndcntpart13++)
                                                                    {
                                                                        Node ndpart14 = ndpart13.Nodes[ndcntpart13];

                                                                        for (int ndcntpart14 = 0;
                                                                            ndcntpart14 < ndpart14.Nodes.Count;
                                                                            ndcntpart14++)
                                                                        {
                                                                            Node ndpart15 = ndpart14.Nodes[ndcntpart14];
                                                                            for (int ndcntpart15 = 0;
                                                                                ndcntpart15 < ndpart15.Nodes.Count;
                                                                                ndcntpart15++)
                                                                            {
                                                                                Node ndpart16 =
                                                                                    ndpart15.Nodes[ndcntpart15];
                                                                                for (int ndcntpart16 = 0;
                                                                                    ndcntpart16 < ndpart16.Nodes.Count;
                                                                                    ndcntpart16++)
                                                                                {
                                                                                    Node ndpart17 =
                                                                                        ndpart16.Nodes[ndcntpart16];
                                                                                    for (int ndcntpart17 = 0;
                                                                                        ndcntpart17 <
                                                                                        ndpart17.Nodes.Count;
                                                                                        ndcntpart17++)
                                                                                    {
                                                                                        Node ndpart18 =
                                                                                            ndpart17.Nodes[ndcntpart17];
                                                                                        for (int ndcntpart18 = 0;
                                                                                            ndcntpart18 <
                                                                                            ndpart18.Nodes.Count;
                                                                                            ndcntpart18++)
                                                                                        {
                                                                                            Node ndpart19 =
                                                                                                ndpart18.Nodes[
                                                                                                    ndcntpart18];
                                                                                            for (int ndcntpart19 = 0;
                                                                                                ndcntpart19 <
                                                                                                ndpart19.Nodes.Count;
                                                                                                ndcntpart19++)
                                                                                            {
                                                                                                Node ndpart20 =
                                                                                                    ndpart19.Nodes[
                                                                                                        ndcntpart19];

                                                                                                writer
                                                                                                    .WriteStartElement(
                                                                                                        "Section");
                                                                                                writer
                                                                                                    .WriteElementString(
                                                                                                        "Name",
                                                                                                        ndpart15.Text);
                                                                                                writer
                                                                                                    .WriteElementString(
                                                                                                        "Type",
                                                                                                        ndtype.Text);
                                                                                                writer
                                                                                                    .WriteElementString(
                                                                                                        "Rating",
                                                                                                        ndpart16.Text);
                                                                                                writer
                                                                                                    .WriteElementString(
                                                                                                        "Description",
                                                                                                        ndpart17.Text
                                                                                                            .Replace(
                                                                                                                '\"',
                                                                                                                ' ')
                                                                                                            .Trim());

                                                                                                writer
                                                                                                    .WriteElementString(
                                                                                                        "Part1",
                                                                                                        ndpart1.Text);
                                                                                                writer
                                                                                                    .WriteElementString(
                                                                                                        "Part2",
                                                                                                        ndpart2.Text);
                                                                                                writer
                                                                                                    .WriteElementString(
                                                                                                        "Part3",
                                                                                                        ndpart3.Text);
                                                                                                writer
                                                                                                    .WriteElementString(
                                                                                                        "Part4",
                                                                                                        ndpart4.Text);
                                                                                                writer
                                                                                                    .WriteElementString(
                                                                                                        "Part5",
                                                                                                        ndpart5.Text);
                                                                                                writer
                                                                                                    .WriteElementString(
                                                                                                        "Part6",
                                                                                                        ndpart6.Text);
                                                                                                writer
                                                                                                    .WriteElementString(
                                                                                                        "Part7",
                                                                                                        ndpart7.Text);
                                                                                                writer
                                                                                                    .WriteElementString(
                                                                                                        "Part8",
                                                                                                        ndpart8.Text);
                                                                                                writer
                                                                                                    .WriteElementString(
                                                                                                        "Part9",
                                                                                                        ndpart9.Text);
                                                                                                writer
                                                                                                    .WriteElementString(
                                                                                                        "Part10",
                                                                                                        ndpart10.Text);
                                                                                                writer
                                                                                                    .WriteElementString(
                                                                                                        "Part11",
                                                                                                        ndpart11.Text);
                                                                                                writer
                                                                                                    .WriteElementString(
                                                                                                        "Part12",
                                                                                                        ndpart12.Text);
                                                                                                writer
                                                                                                    .WriteElementString(
                                                                                                        "Part13",
                                                                                                        ndpart13.Text);
                                                                                                writer
                                                                                                    .WriteElementString(
                                                                                                        "Part14",
                                                                                                        ndpart14.Text);
                                                                                                writer
                                                                                                    .WriteElementString(
                                                                                                        "RemAmmo_Quantity",
                                                                                                        ndpart18.Text);
                                                                                                writer
                                                                                                    .WriteElementString(
                                                                                                        "Quality",
                                                                                                        ndpart19.Text);
                                                                                                writer
                                                                                                    .WriteElementString(
                                                                                                        "Level",
                                                                                                        ndpart20.Text);
                                                                                                writer
                                                                                                    .WriteEndElement();
                                                                                            }
                                                                                        }
                                                                                    }
                                                                                }
                                                                            }
                                                                        }
                                                                    }
                                                                }
                                                            }
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }

            writer.WriteEndElement();
            writer.WriteEndDocument();
            writer.Flush();
            writer.Close();
        }
    }
}
