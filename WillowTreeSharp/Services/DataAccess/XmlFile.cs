using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.Text;
using System.Xml;

namespace WillowTree.Services.DataAccess
{
    /// <summary>
    /// Create a New XML file to store or load data
    /// </summary>
    public class XmlFile
    {
        public string path;
        public XmlDocument xmlrdrdoc = null;
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

        public List<string> StListSectionNames()
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

        public static void ConvertIni2Xml(List<string> iniNames, string xmlName)
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
