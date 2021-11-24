namespace WillowTree.Services.DataAccess
{
    public class XmlCacheWrapper : IXmlCache
    {
        public XmlFile XmlFileFromCache(string filename)
        {
            return XmlCache.XmlFileFromCache(filename);
        }
    }
}
