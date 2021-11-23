using System.Collections.Generic;
using WillowTree.Inventory;

namespace WillowTree
{
    public interface IGameData
    {
        InventoryList BankList { get; }
        string DataPath { get; }
        XmlFile EchoesXml { get; }
        InventoryList ItemList { get; }
        XmlFile LocationsXml { get; }
        InventoryList LockerList { get; }
        XmlFile PartNamesXml { get; }
        XmlFile QuestsXml { get; }
        XmlFile SkillsAllXml { get; }
        XmlFile SkillsBerserkerXml { get; }
        XmlFile SkillsCommonXml { get; }
        XmlFile SkillsHunterXml { get; }
        XmlFile SkillsSirenXml { get; }
        XmlFile SkillsSoldierXml { get; }
        InventoryList WeaponList { get; }
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
        string WeaponInfo(InventoryEntry invEntry);
    }
}