namespace WillowTree
{
    /// <summary>
    /// WillowTree.Services contains shared services or data that need to be
    /// made available to all components or controls in WillowTree independently
    /// of the WillowTreeMain form.  Classes that require instancing cannot be
    /// instanced directly in a static class so I define properties that will
    /// create an instance when they are first used.  This prevents every plugin
    /// or other control that might use the service while not hosted on
    /// the WillowTreeMain form from having to know how to create and initialize
    /// it.  For example, when user controls are designed there will be no
    /// currently running instance of WillowTreeMain to ask for the themes to
    /// render the control colors, so those have to be shared here instead of as
    /// variables of WillowTreeMain.
    /// </summary>
    public static class ServiceLocator
    {
        private static AppThemes _AppThemes;

        /// <summary>
        /// Single instance of the application themes to be shared by any objects
        /// that need it in the application. This will not change or be released
        /// until the application terminates.
        /// </summary>
        public static AppThemes AppThemes
        {
            get
            {
                if (_AppThemes == null)
                    _AppThemes = new AppThemes();

                return _AppThemes;
            }
            set
            {
                if (_AppThemes == null)
                    _AppThemes = value;
            }
        }
    }
}
