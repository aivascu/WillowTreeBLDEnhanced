using System.Collections.Generic;

namespace WillowTreeSharp.Domain
{
    public class QuestTable
    {
        public List<QuestEntry> Quests;
        public int Index;
        public string CurrentQuest;
        public int TotalQuests;
    }
}