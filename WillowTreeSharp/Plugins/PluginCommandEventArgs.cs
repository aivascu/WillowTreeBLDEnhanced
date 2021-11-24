using System;

namespace WillowTree.Plugins
{
    public class PluginCommandEventArgs : EventArgs
    {
        public PluginCommandEventArgs(WillowTreeMain willowTreeMain, PluginCommand command)
        {
            this.Command = command;
            this.WillowTreeMain = willowTreeMain;
        }

        public WillowTreeMain WillowTreeMain { get; }

        public PluginCommand Command { get; }
    }
}