using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.Text;
using System.Xml;
using Aga.Controls.Tree;

namespace WillowTree.Services.DataAccess
{
    /// <summary>
    /// Create a New XML file to store or load data
    /// </summary>
    public class XmlFile
    {
        public string path;
        public XmlDocument xmlrdrdoc = null;
        private static readonly Dictionary<string, XmlFile> XmlCache = new Dictionary<string, XmlFile>();
        private readonly List<string> listListSectionNames = new List<string>();

        public IPath Path { get; set; } = new PathWrapper(new FileSystem());
        public IDirectory Directory { get; set; } = new DirectoryWrapper(new FileSystem());

        public XmlFile(string filePath)
            : this(GameData.DataPath, GameData.XmlPath, filePath)
        {
        }

        public XmlFile(string dataPath, string xmlPath, string filePath)
        {
            List<string> listfilePath = new List<string>();
            filePath = Path.GetFullPath(filePath);
            this.path = filePath;
            listfilePath.Add(filePath); //Contains all ini style filenames
            string fileext = Path.GetExtension(filePath);
            if ((fileext == ".ini") || (fileext == ".txt"))
            {
                string filename = Path.GetFileNameWithoutExtension(filePath);
                // Must add the directory separator character to the end of the folder because
                // it is stored that way in db.DataPath.
                string folder = Path.GetDirectoryName(filePath) + Path.DirectorySeparatorChar;

                if (dataPath.Length <= folder.Length && folder.Substring(0, dataPath.Length)
                    .Equals(dataPath, StringComparison.OrdinalIgnoreCase))
                {
                    // If the INI or TXT file is in the Data folder (db.DataPath) or its
                    // descendant then make a corresponding XML file in the Xml folder
                    // (db.XmlPath).  This is different from below because the Xml folder
                    // doesn't necessarily have to be db.DataPath + "Xml\".  The Xml path
                    // could be C:\Xml\ and the Data path could be C:\Data\ and a file named
                    // C:\Data\Quests\Part1.ini would produce an XML file named
                    // C:\Xml\Quests\Part1.xml.  The code below would produce a file named
                    // C:\Data\Quests\Xml\Part1.xml
                    folder = xmlPath + folder.Substring(dataPath.Length);
                }
                else
                {
                    // This handles cases where the file is not in the Data folder or
                    // one of its subfolders.
                    //
                    // A subfolder called Xml will be created if necessary in the folder that
                    // contains the INI file and the file folder will have "Xml\" appended.
                    // (matt911) I don't think there is any place that WillowTree# even
                    // attempts to use an ini or txt file as an XmlFile except when it is in
                    // the Data folder, so this code line is probably never executed.  It might
                    // have some use if an application other than WT# was to use the XML
                    // code though.
                    folder = folder + "Xml" + Path.DirectorySeparatorChar;
                }
                string targetfile = Path.Combine(folder, $"{filename}.xml");
                if (!Directory.Exists(Path.GetDirectoryName(targetfile)))
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(targetfile));
                }

                ConvertIni2Xml(listfilePath, targetfile);
                this.path = targetfile;
            }
            xmlrdrdoc = null;
            listListSectionNames.Clear();
        }

        public XmlFile(List<string> filePaths, string targetfile)
        {
            ConvertIni2Xml(filePaths, targetfile);
            path = targetfile;

            xmlrdrdoc = null;
        }

        // Clear duplicate items from an xml item/weapon file
        // TODO: This is an ugly method that probably could be done in an easier
        // to understand, more correct, or faster way.  It does seem to work
        // though, so I'm not going to try to rewrite it right now.
        public static void PurgeDuplicates(string InputFile)
        {
            // A tree model to store the nodes in
            TreeModel model = new TreeModel();

            // rootnode
            Node ndroot = new Node("INI");
            model.Nodes.Add(ndroot);

            XmlDocument xmlrdrdoc = new XmlDocument();

            xmlrdrdoc.Load(InputFile);

            // get a list of all items
            foreach (XmlNode xn in xmlrdrdoc.SelectNodes("/INI/Section"))
            {
                Node ndparent = ndroot;
                bool bFound;

                string[] strParts = new string[]
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

                for (int partindex = 0; partindex < 21; partindex++)
                {
                    // All sections
                    // read the xml values
                    bFound = false;

                    for (int ndcnt = 0; ndcnt < ndparent.Nodes.Count; ndcnt++)
                    {
                        if (ndparent.Nodes[ndcnt].Text == strParts[partindex])
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
                            Text = strParts[partindex]
                        };
                        ndparent.Nodes.Add(ndchild);
                        ndparent = ndchild;
                    }
                }
            }

            XmlTextWriter writer = new XmlTextWriter(InputFile, new ASCIIEncoding())
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

                                                    for (int ndcntpart9 = 0; ndcntpart9 < ndpart9.Nodes.Count; ndcntpart9++)
                                                    {
                                                        Node ndpart10 = ndpart9.Nodes[ndcntpart9];

                                                        for (int ndcntpart10 = 0; ndcntpart10 < ndpart10.Nodes.Count; ndcntpart10++)
                                                        {
                                                            Node ndpart11 = ndpart10.Nodes[ndcntpart10];

                                                            for (int ndcntpart11 = 0; ndcntpart11 < ndpart11.Nodes.Count; ndcntpart11++)
                                                            {
                                                                Node ndpart12 = ndpart11.Nodes[ndcntpart11];

                                                                for (int ndcntpart12 = 0; ndcntpart12 < ndpart12.Nodes.Count; ndcntpart12++)
                                                                {
                                                                    Node ndpart13 = ndpart12.Nodes[ndcntpart12];

                                                                    for (int ndcntpart13 = 0; ndcntpart13 < ndpart13.Nodes.Count; ndcntpart13++)
                                                                    {
                                                                        Node ndpart14 = ndpart13.Nodes[ndcntpart13];

                                                                        for (int ndcntpart14 = 0; ndcntpart14 < ndpart14.Nodes.Count; ndcntpart14++)
                                                                        {
                                                                            Node ndpart15 = ndpart14.Nodes[ndcntpart14];
                                                                            for (int ndcntpart15 = 0; ndcntpart15 < ndpart15.Nodes.Count; ndcntpart15++)
                                                                            {
                                                                                Node ndpart16 = ndpart15.Nodes[ndcntpart15];
                                                                                for (int ndcntpart16 = 0; ndcntpart16 < ndpart16.Nodes.Count; ndcntpart16++)
                                                                                {
                                                                                    Node ndpart17 = ndpart16.Nodes[ndcntpart16];
                                                                                    for (int ndcntpart17 = 0; ndcntpart17 < ndpart17.Nodes.Count; ndcntpart17++)
                                                                                    {
                                                                                        Node ndpart18 = ndpart17.Nodes[ndcntpart17];
                                                                                        for (int ndcntpart18 = 0; ndcntpart18 < ndpart18.Nodes.Count; ndcntpart18++)
                                                                                        {
                                                                                            Node ndpart19 = ndpart18.Nodes[ndcntpart18];
                                                                                            for (int ndcntpart19 = 0; ndcntpart19 < ndpart19.Nodes.Count; ndcntpart19++)
                                                                                            {
                                                                                                Node ndpart20 = ndpart19.Nodes[ndcntpart19];

                                                                                                writer.WriteStartElement("Section");
                                                                                                writer.WriteElementString("Name", ndpart15.Text);
                                                                                                writer.WriteElementString("Type", ndtype.Text);
                                                                                                writer.WriteElementString("Rating", ndpart16.Text);
                                                                                                writer.WriteElementString("Description", ndpart17.Text.Replace('\"', ' ').Trim());

                                                                                                writer.WriteElementString("Part1", ndpart1.Text);
                                                                                                writer.WriteElementString("Part2", ndpart2.Text);
                                                                                                writer.WriteElementString("Part3", ndpart3.Text);
                                                                                                writer.WriteElementString("Part4", ndpart4.Text);
                                                                                                writer.WriteElementString("Part5", ndpart5.Text);
                                                                                                writer.WriteElementString("Part6", ndpart6.Text);
                                                                                                writer.WriteElementString("Part7", ndpart7.Text);
                                                                                                writer.WriteElementString("Part8", ndpart8.Text);
                                                                                                writer.WriteElementString("Part9", ndpart9.Text);
                                                                                                writer.WriteElementString("Part10", ndpart10.Text);
                                                                                                writer.WriteElementString("Part11", ndpart11.Text);
                                                                                                writer.WriteElementString("Part12", ndpart12.Text);
                                                                                                writer.WriteElementString("Part13", ndpart13.Text);
                                                                                                writer.WriteElementString("Part14", ndpart14.Text);
                                                                                                writer.WriteElementString("RemAmmo_Quantity", ndpart18.Text);
                                                                                                writer.WriteElementString("Quality", ndpart19.Text);
                                                                                                writer.WriteElementString("Level", ndpart20.Text);
                                                                                                writer.WriteEndElement();
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

        public static XmlFile XmlFileFromCache(string filename)
        {
            if (string.IsNullOrEmpty(filename))
            {
                return null;
            }

            // Cache all XML files opened with XmlFileFromCache in a
            // dictionary then reuse them.  This prevents having to re-read
            // the file data constantly by creating a new XmlFile then
            // releasing it.
            if (XmlCache.TryGetValue(filename, out XmlFile xml))
            {
                return xml;
            }

            // If its not in the cache then open from disk and add to cache
            xml = new XmlFile(filename);
            XmlCache.Add(filename, xml);
            return xml;
        }

        public void AddSection(string sectionname, List<string> subsectionnames, List<string> subsectionvalues)
        {
            XmlTextReader reader = new XmlTextReader(path);
            XmlDocument doc = new XmlDocument();
            doc.Load(reader);
            reader.Close();

            XmlElement root = doc.DocumentElement;
            XmlElement newSection = doc.CreateElement("Section");
            string innerxml = $"<Name>{sectionname}</Name>";

            for (int Progress = 0; Progress < subsectionnames.Count; Progress++)
            {
                innerxml = $"{innerxml}<{subsectionnames[Progress]}>{subsectionvalues[Progress]}</{subsectionnames[Progress]}>";
            }

            newSection.InnerXml = innerxml;
            root.AppendChild(newSection);

            doc.Save(path);

            listListSectionNames.Add(sectionname);

            // Read in new
            xmlrdrdoc = null;
        }

        public List<string> stListSectionNames()
        {
            if (xmlrdrdoc == null)
            {
                xmlrdrdoc = new XmlDocument();
                xmlrdrdoc.Load(path);
            }

            if (listListSectionNames.Count == 0)
            {
                foreach (XmlNode node in xmlrdrdoc.SelectNodes("/INI/Section/Name"))
                {
                    listListSectionNames.Add(node.InnerText);
                }
            }
            return listListSectionNames;
        }

        /// <summary>
        /// Looks for the first section that has a Key/Value combination matching
        /// AssociatedKey/AssociatedValue and returns the Value of the requested Key.
        /// </summary>
        /// <param name="Key"></param>
        /// <param name="AssociatedKey"></param>
        /// <param name="AssociatedValue"></param>
        /// <returns></returns>
        public string XmlReadAssociatedValue(string Key, string AssociatedKey, string AssociatedValue)
        {
            if (xmlrdrdoc == null)
            {
                xmlrdrdoc = new XmlDocument();
                xmlrdrdoc.Load(path);
            }

            string temp = "";

            foreach (XmlNode xn in xmlrdrdoc.SelectNodes($"/INI/Section[{AssociatedKey}=\"{AssociatedValue}\"]"))
            {
                XmlNode node = xn[Key];
                if (node != null)
                {
                    temp = node.InnerText;
                    break;
                }
            }

            return temp;
        }

        public XmlNode XmlReadNode(string Section)
        {
            if (xmlrdrdoc == null)
            {
                xmlrdrdoc = new XmlDocument();
                xmlrdrdoc.Load(path);
            }

            return xmlrdrdoc.SelectSingleNode($"/INI/Section[Name=\"{Section}\"]");
        }

        public List<string> XmlReadSection(string Section)
        {
            if (xmlrdrdoc == null)
            {
                xmlrdrdoc = new XmlDocument();
                xmlrdrdoc.Load(path);
            }

            List<string> temp = new List<string>();

            foreach (XmlNode xn in xmlrdrdoc.SelectNodes($"/INI/Section[Name=\"{Section}\"]"))
            {
                foreach (XmlNode cnd in xn.ChildNodes)
                {
                    if (cnd.Name != "Name")
                    {
                        temp.Add($"{cnd.Name}:{cnd.InnerText}");
                    }
                }
            }

            return temp;
        }

        /// <summary>
        /// Read Data Value From the Ini File
        /// </summary>
        /// <PARAM name="Section"></PARAM>
        /// <PARAM name="Key"></PARAM>
        /// <returns></returns>
        public string XmlReadValue(string Section, string Key)
        {
            if (xmlrdrdoc == null)
            {
                xmlrdrdoc = new XmlDocument();
                xmlrdrdoc.Load(path);
            }

            foreach (XmlNode xn in xmlrdrdoc.SelectNodes($"/INI/Section[Name=\"{Section}\"]"))
            {
                XmlNode node = xn[Key];
                if (node != null)
                {
                    return node.InnerText;
                }
            }

            return string.Empty;
        }

        private static void ConvertIni2Xml(List<string> iniNames, string xmlName)
        {
            bool xmlNeedsUpdate;

            if (File.Exists(xmlName))
            {
                // If any of the INI files used to create the XML file is newer than
                // the XML file then the XML file needs to be rebuilt.
                DateTime xmlWriteTime = File.GetLastWriteTimeUtc(xmlName);
                xmlNeedsUpdate = false;

                foreach (string iniName in iniNames)
                {
                    if (File.GetLastWriteTime(iniName) >= xmlWriteTime)
                    {
                        xmlNeedsUpdate = true;
                    }
                }
            }
            else
            {
                xmlNeedsUpdate = true;
            }

            if (xmlNeedsUpdate)
            {
                XmlTextWriter writer = new XmlTextWriter(xmlName, new ASCIIEncoding());
                try
                {
                    writer.Formatting = Formatting.Indented;
                    writer.Indentation = 2;
                    writer.WriteStartDocument();
                    writer.WriteComment("Comment");
                    writer.WriteStartElement("INI");

                    string line;
                    bool sectionIsOpen = false;

                    // Read each INI file and write its data to the XML file
                    foreach (string iniName in iniNames)
                    {
                        StreamReader file = new StreamReader(iniName);
                        int lineNumber = 0;

                        // Keep reading lines until the end of the file
                        while ((line = file.ReadLine()) != null)
                        {
                            lineNumber++;
                            // Ignore any empty lines
                            if (line.Length > 0)
                            {
                                line = line.TrimEnd();
                                // Section headers in INI files look like:
                                // [Section]
                                if (line.StartsWith("[") && line.EndsWith("]"))
                                {
                                    // --- This line is a section header ---
                                    // Terminate the previous Xml element if there was
                                    // already a section open.
                                    if (sectionIsOpen)
                                    {
                                        writer.WriteEndElement();
                                    }

                                    // Write a new XML element to hold this INI section
                                    writer.WriteStartElement("Section");
                                    sectionIsOpen = true;
                                    string sectionName = line.Substring(1, line.Length - 2);
                                    writer.WriteElementString("Name", sectionName);
                                }
                                else if (line.Contains("="))
                                {
                                    // --- This line is a property assignment line ---
                                    string propName = line.Substring(0, line.IndexOf("="));
                                    propName = propName.Replace("[", "");
                                    propName = propName.Replace("]", "");
                                    propName = propName.Replace("(", "");
                                    propName = propName.Replace(")", "");
                                    string propValue = line.Substring(line.IndexOf("=") + 1);

                                    if (propValue.StartsWith("\""))
                                    {
                                        propValue = propValue.Substring(1, propValue.Length - 2);
                                    }
                                    writer.WriteElementString(propName, propValue);
                                }
                                else if (line[0] == ';')
                                {
                                    // Comment lines start with a semicolon, ignore them.
                                }
                                else
                                {
                                    throw new FileFormatException("File format is invalid on line " + lineNumber + "\r\nFile: " + iniName);
                                }
                            }
                        }
                        file.Close();
                    }
                    if (sectionIsOpen)
                    {
                        writer.WriteEndElement();
                    }

                    writer.WriteEndElement();
                    writer.WriteEndDocument();
                    writer.Flush();
                    writer.Close();
                }
                catch (Exception)
                {
                    writer.Close();
                    File.Delete(xmlName);

                    // Re-throw the exception.  This code does not consume the exception.
                    // It just to makes sure any incomplete XML file is deleted so next
                    // time the application runs it will attempt to build it again.
                    throw;
                }
            }
        }
    }
}
