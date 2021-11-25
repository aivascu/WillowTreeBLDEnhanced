using System.Collections.Generic;
using WillowTree.Inventory;

namespace WillowTree.Services.DataAccess
{
    public interface IGameData
    {
        string DataPath { get; }
        XmlFile EchoesXml { get; }
        XmlFile LocationsXml { get; }
        XmlFile PartNamesXml { get; }
        XmlFile QuestsXml { get; }
        XmlFile SkillsAllXml { get; }
        XmlFile SkillsBerserkerXml { get; }
        XmlFile SkillsCommonXml { get; }
        XmlFile SkillsHunterXml { get; }
        XmlFile SkillsSirenXml { get; }
        XmlFile SkillsSoldierXml { get; }
        string XmlPath { get; }
        int[] XPChart { get; }

        string CreateUniqueKey();

        int GetEffectiveLevelItem(string[] itemParts, int quality, int levelIndex);

        int GetEffectiveLevelWeapon(string[] weaponParts, int quality, int levelIndex);

        string GetName(string part);

        string GetPartAttribute(string part, string attributeName);

        int GetPartRarity(string part);

        List<string> GetPartSection(string part);

        void InitializeNameLookup();

        string OpenedLockerFilename();

        void OpenedLockerFilename(string sOpenedLocker);

        void SetXPchart();

        string WeaponInfo(string[] parts, int qualityIndex, int levelIndex);
    }
}
