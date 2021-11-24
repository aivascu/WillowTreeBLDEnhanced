using System;

namespace WillowTree.Plugins
{
    public class PluginCommandEventArgs : EventArgs
    {
        public PluginCommandEventArgs(WillowTreeMain willowTreeMain, PluginCommand command)
        {
            Command = command;
            WillowTreeMain = willowTreeMain;
        }

        public WillowTreeMain WillowTreeMain { get; }

        public PluginCommand Command { get; }
    }
}