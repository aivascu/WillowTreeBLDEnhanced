using System.Collections.Generic;
using WillowTree.Inventory;

namespace WillowTree.Services.DataAccess
{
    public class GameDataWrapper : IGameData
    {
        public InventoryList BankList => InventoryData.BankList;

        public string DataPath => GameData.DataPath;

        public XmlFile EchoesXml => GameData.EchoesXml;

        public InventoryList ItemList => InventoryData.ItemList;

        public XmlFile LocationsXml => GameData.LocationsXml;

        public InventoryList LockerList => InventoryData.LockerList;

        public XmlFile PartNamesXml => GameData.PartNamesXml;

        public XmlFile QuestsXml => GameData.QuestsXml;

        public XmlFile SkillsAllXml => GameData.SkillsAllXml;

        public XmlFile SkillsBerserkerXml => GameData.SkillsBerserkerXml;

        public XmlFile SkillsCommonXml => GameData.SkillsCommonXml;

        public XmlFile SkillsHunterXml => GameData.SkillsHunterXml;

        public XmlFile SkillsSirenXml => GameData.SkillsSirenXml;

        public XmlFile SkillsSoldierXml => GameData.SkillsSoldierXml;

        public InventoryList WeaponList => InventoryData.WeaponList;

        public string XmlPath => GameData.XmlPath;

        public int[] XPChart => GameData.XPChart;

        public string CreateUniqueKey()
        {
            return GameData.CreateUniqueKey();
        }

        public int GetEffectiveLevelItem(string[] itemParts, int quality, int levelIndex)
        {
            return GameData.GetEffectiveLevelItem(itemParts, quality, levelIndex);
        }

        public int GetEffectiveLevelWeapon(string[] weaponParts, int quality, int levelIndex)
        {
            return GameData.GetEffectiveLevelWeapon(weaponParts, quality, levelIndex);
        }

        public string GetName(string part)
        {
            return GameData.GetName(part);
        }

        public string GetPartAttribute(string part, string attributeName)
        {
            return GameData.GetPartAttribute(part, attributeName);
        }

        public int GetPartRarity(string part)
        {
            return GameData.GetPartRarity(part);
        }

        public List<string> GetPartSection(string part)
        {
            return GameData.GetPartSection(part);
        }

        public void InitializeNameLookup()
        {
            GameData.InitializeNameLookup();
        }

        public string OpenedLockerFilename()
        {
            return GameData.OpenedLockerFilename();
        }

        public void OpenedLockerFilename(string sOpenedLocker)
        {
            GameData.OpenedLockerFilename(sOpenedLocker);
        }

        public void SetXPchart()
        {
            GameData.SetXPchart();
        }

        public string WeaponInfo(string[] parts, int qualityIndex, int levelIndex)
        {
            return GameData.WeaponInfo(parts, qualityIndex, levelIndex);
        }
    }
}
