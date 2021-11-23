using System.Drawing;

namespace WillowTree
{
    public class GlobalSettingsWrapper : IGlobalSettings
    {
        public Color BackgroundColor
        {
            get => GlobalSettings.BackgroundColor;
            set => GlobalSettings.BackgroundColor = value;
        }

        public InputMode InputMode
        {
            get => GlobalSettings.InputMode;
            set => GlobalSettings.InputMode = value;
        }

        public string LastLockerFile
        {
            get => GlobalSettings.LastLockerFile;
            set => GlobalSettings.LastLockerFile = value;
        }

        public int MaxBackpackSlots
        {
            get => GlobalSettings.MaxBackpackSlots;
            set => GlobalSettings.MaxBackpackSlots = value;
        }

        public int MaxBankSlots
        {
            get => GlobalSettings.MaxBankSlots;
            set => GlobalSettings.MaxBankSlots = value;
        }

        public int MaxCash
        {
            get => GlobalSettings.MaxCash;
            set => GlobalSettings.MaxCash = value;
        }

        public int MaxExperience
        {
            get => GlobalSettings.MaxExperience;
            set => GlobalSettings.MaxExperience = value;
        }

        public int MaxLevel
        {
            get => GlobalSettings.MaxLevel;
            set => GlobalSettings.MaxLevel = value;
        }

        public int MaxSkillPoints
        {
            get => GlobalSettings.MaxSkillPoints;
            set => GlobalSettings.MaxSkillPoints = value;
        }

        public bool PartSelectorTracking
        {
            get => GlobalSettings.PartSelectorTracking;
            set => GlobalSettings.PartSelectorTracking = value;
        }

        public Color[] RarityColor
        {
            get => GlobalSettings.RarityColor;
            set => GlobalSettings.RarityColor = value;
        }

        public bool ShowLevel
        {
            get => GlobalSettings.ShowLevel;
            set => GlobalSettings.ShowLevel = value;
        }

        public bool ShowManufacturer
        {
            get => GlobalSettings.ShowManufacturer;
            set => GlobalSettings.ShowManufacturer = value;
        }

        public bool ShowRarity
        {
            get => GlobalSettings.ShowRarity;
            set => GlobalSettings.ShowRarity = value;
        }

        public bool UseColor
        {
            get => GlobalSettings.UseColor;
            set => GlobalSettings.UseColor = value;
        }

        public bool UseHexInAdvancedMode
        {
            get => GlobalSettings.UseHexInAdvancedMode;
            set => GlobalSettings.UseHexInAdvancedMode = value;
        }

        public void Load(string filename)
        {
            GlobalSettings.Load(filename);
        }

        public void Save()
        {
            GlobalSettings.Save();
        }
    }
}