using System;

namespace WillowTree.Plugins
{
    public class PluginEventArgs : EventArgs
    {
        public PluginEventArgs(WillowTreeMain willowTreeMain, string fileName)
        {
            this.FileName = fileName;
            this.WillowTreeMain = willowTreeMain;
        }

        public WillowTreeMain WillowTreeMain { get; }

        public string FileName { get; }
    }
}