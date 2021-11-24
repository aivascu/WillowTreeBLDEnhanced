using System;

namespace WillowTree.Plugins
{
    public struct PluginEvents
    {
        /// <summary>
        /// This event handler will be called when the user has chosen a savegame
        /// file and is about to load it into WillowTree.
        /// </summary>
        public EventHandler<PluginEventArgs> GameLoading;

        /// <summary>
        /// This event handler will be called when once WillowTree has loaded 
        /// a new savegame file.
        /// </summary>
        public EventHandler<PluginEventArgs> GameLoaded;

        /// <summary>
        /// This event handler will be called when the user has chosen to save
        /// the savegame file back to disk, before the file is saved.
        /// </summary>
        public EventHandler<PluginEventArgs> GameSaving;

        /// <summary>
        /// This event handler will be called when a savegame file is finished
        /// saving to disk.
        /// </summary>
        public EventHandler<PluginEventArgs> GameSaved;

        /// <summary>
        /// This event handler will be called each time the plugin becomes
        /// selected for display, typically when its tab page comes into view.
        /// </summary>
        public EventHandler<PluginEventArgs> PluginSelected;

        /// <summary>
        ///  This event handler will be called to let the plugin know it is
        ///  is no longer in view, typically when a different tab page becomes
        ///  selected.
        /// </summary>
        public EventHandler<PluginEventArgs> PluginUnselected;

        public EventHandler<PluginCommandEventArgs> PluginCommand;
    }
}