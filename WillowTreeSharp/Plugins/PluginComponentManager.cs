using System;
using System.Collections.Generic;
using System.Linq;

namespace WillowTree.Plugins
{
    public class PluginComponentManager
    {
        private event EventHandler<PluginEventArgs> GameLoading;
        private event EventHandler<PluginEventArgs> GameLoaded;
        private event EventHandler<PluginEventArgs> GameSaving;
        private event EventHandler<PluginEventArgs> GameSaved;

        private readonly Dictionary<IPlugin, PluginEvents> pluginEventTable = new Dictionary<IPlugin, PluginEvents>();

        /// <summary>
        /// The application calls this to execute the GameLoading event handler 
        /// of all registered plugins
        /// </summary>
        public void OnGameLoading(PluginEventArgs e)
        {
            GameLoading?.Invoke(this, e);
        }

        /// <summary>
        /// The application calls this to execute the GameLoaded event handler 
        /// of all registered plugins
        /// </summary>
        public void OnGameLoaded(PluginEventArgs e)
        {
            GameLoaded?.Invoke(this, e);
        }

        /// <summary>
        /// The application calls this to execute the GameSaving event handler 
        /// of all registered plugins
        /// </summary>
        public void OnGameSaving(PluginEventArgs e)
        {
            GameSaving?.Invoke(this, e);
        }

        /// <summary>
        /// The application calls this to execute the GameSaved event handler 
        /// of all registered plugins
        /// </summary>
        public void OnGameSaved(PluginEventArgs e)
        {
            GameSaved?.Invoke(this, e);
        }

        /// <summary>
        /// The application calls this to signal a plugin when it is selected
        /// or otherwise being displayed to the user.  This could be called when 
        /// the plugin's tab page is selected so that it can update its controls
        /// to reflect data that may have changed while the plugin was hidden.
        /// </summary>
        public void OnPluginSelected(IPlugin plugin, PluginEventArgs e)
        {
            if (pluginEventTable.TryGetValue(plugin, out var functions))
            {
                functions.PluginSelected?.Invoke(this, e);
            }
        }

        /// <summary>
        /// The application calls this to tell a plugin when it is no longer
        /// being displayed or otherwise selected for input.  If a plugin
        /// uses GUI events like animation timers, then it can
        /// detach them until it is selected again.
        /// </summary>
        public void OnPluginUnselected(IPlugin plugin, PluginEventArgs e)
        {
            if (pluginEventTable.TryGetValue(plugin, out var functions))
            {
                functions.PluginUnselected?.Invoke(this, e);
            }
        }

        public void OnPluginCommand(IPlugin plugin, PluginCommandEventArgs e)
        {
            if (pluginEventTable.TryGetValue(plugin, out var functions))
            {
                functions.PluginCommand?.Invoke(this, e);
            }
        }

        /// <summary>
        /// This calls a plugin's InitializePlugin method to let 
        /// it know that the application is ready and it can initialize.
        /// When the plugin initializes it should call RegisterPlugin 
        /// to subscribe to plugin events.
        /// </summary>
        public void InitializePlugin(IPlugin plugin)
        {
            plugin.InitializePlugin(this);
        }

        /// <summary>
        /// Plugins call this to register for plugin events.  Typically it will
        /// be called in the plugin's InitializePlugin method.  If a particular
        /// event is not needed it can be left null in the PluginEvents structure.
        /// </summary>
        public void RegisterPlugin(IPlugin plugin, PluginEvents eventHandlers)
        {
            // Store a list of the event handlers so they can be detached later
            pluginEventTable.Add(plugin, eventHandlers);

            if (eventHandlers.GameLoading != null)
                GameLoading += eventHandlers.GameLoading;
            if (eventHandlers.GameLoaded != null)
                GameLoaded += eventHandlers.GameLoaded;
            if (eventHandlers.GameSaving != null)
                GameSaving += eventHandlers.GameSaving;
            if (eventHandlers.GameSaved != null)
                GameSaved += eventHandlers.GameSaved;
        }

        private void DetachEvents(PluginEvents eventHandlers)
        {
            if (eventHandlers.GameLoading != null)
                GameLoading -= eventHandlers.GameLoading;
            if (eventHandlers.GameLoaded != null)
                GameLoaded -= eventHandlers.GameLoaded;
            if (eventHandlers.GameSaving != null)
                GameSaving -= eventHandlers.GameSaving;
            if (eventHandlers.GameSaved != null)
                GameSaved -= eventHandlers.GameSaved;
        }

        public IPlugin GetPlugin(Type pluginType)
        {
            return pluginEventTable.Keys.FirstOrDefault(plugin => plugin.GetType() == pluginType);
        }

        public void UnregisterPlugin(IPlugin plugin)
        {
            // Retrieve the list of event handlers to detach
            if (pluginEventTable.TryGetValue(plugin, out var eventHandlers) != true)
            {
                return;
            }

            DetachEvents(eventHandlers);
            pluginEventTable.Remove(plugin);
            plugin.ReleasePlugin();
        }

        public void UnregisterAllPlugins()
        {
            foreach (KeyValuePair<IPlugin, PluginEvents> kvp in pluginEventTable)
            {
                DetachEvents(kvp.Value);
                kvp.Key.ReleasePlugin();
            }

            pluginEventTable.Clear();
        }
    }
}