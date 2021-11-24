namespace WillowTree.Plugins
{
    public interface IPlugin
    {
        string Name { get; set; }
        void InitializePlugin(PluginComponentManager pm);
        void ReleasePlugin();
    }
}