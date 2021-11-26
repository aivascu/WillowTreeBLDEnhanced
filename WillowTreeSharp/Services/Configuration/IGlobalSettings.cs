using System.Drawing;

namespace WillowTree.Services.Configuration
{
    public interface IGlobalSettings
    {
        Color BackgroundColor { get; set; }
        InputMode InputMode { get; set; }
        string LastLockerFile { get; set; }
        int MaxBackpackSlots { get; set; }
        int MaxBankSlots { get; set; }
        int MaxCash { get; set; }
        int MaxExperience { get; set; }
        int MaxLevel { get; set; }
        int MaxSkillPoints { get; set; }
        bool PartSelectorTracking { get; set; }
        Color[] RarityColor { get; set; }
        bool ShowLevel { get; set; }
        bool ShowManufacturer { get; set; }
        bool ShowRarity { get; set; }
        bool UseColor { get; set; }
        bool UseHexInAdvancedMode { get; set; }

        void Load(string filename);

        void Save(string filePath);
    }
}
