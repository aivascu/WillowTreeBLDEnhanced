using System.Collections.Generic;

namespace WillowTree.Services.DataAccess
{
    public static class XmlCache
    {
        private static readonly Dictionary<string, XmlFile> xmlCache = new Dictionary<string, XmlFile>();

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
            if (xmlCache.TryGetValue(filename, out XmlFile xml))
            {
                return xml;
            }

            // If its not in the cache then open from disk and add to cache
            xml = new XmlFile(filename);
            xmlCache.Add(filename, xml);
            return xml;
        }
    }
}
