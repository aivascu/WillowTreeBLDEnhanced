using System;
using System.Xml;

namespace WillowTree.Services.DataAccess
{
    public static class XmlNodeExtensions
    {
        // These XmlNode extensions are used to handle some repetitive error
        // handling and data type conversion related to getting values which may
        // or may not exist from an XmlNode.
        public static string GetElement(this XmlNode source, string elementname)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source), "xml node was null");
            }

            XmlNode child = source[elementname];
            return child != null
                ? child.InnerText
                : throw new FormatException($"Element {elementname} not found in xml node");
        }

        public static string GetElement(this XmlNode source, string elementname, string defaultvalue)
        {
            if (source == null)
            {
                return defaultvalue;
            }

            XmlNode child = source[elementname];
            return child == null
                ? defaultvalue
                : child.InnerText;
        }

        public static int GetElementAsInt(this XmlNode source, string elementname)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source), "xml node was null");
            }

            string elementtext = source.GetElement(elementname);
            return int.TryParse(elementtext, out int outvalue)
                ? outvalue
                : throw new FormatException();
        }

        public static int GetElementAsInt(this XmlNode source, string elementname, int defaultvalue)
        {
            if (source == null)
            {
                return defaultvalue;
            }

            string elementtext = source.GetElement(elementname, "");
            return int.TryParse(elementtext, out int outvalue)
                ? outvalue
                : defaultvalue;
        }
    }
}
