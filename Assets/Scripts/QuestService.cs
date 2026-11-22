
using System.Collections.Generic;
using System.Linq;

public static class QuestService
{
    static readonly List<QuestData> quests = new()
    {
        new QuestData {
            id = "q1",
            title = "Campus Tree",
            description = "Find and photograph a tree on campus.",
            difficulty = "Easy",
            xpReward = 50,
            collectibleName = "Old Oak",
            collectibleRarity = "Common"
        },
        new QuestData {
            id = "q2",
            title = "Library Hunt",
            description = "Take a photo of a book in the library.",
            difficulty = "Medium",
            xpReward = 80,
            collectibleName = "Dusty Tome",
            collectibleRarity = "Uncommon"
        },
        new QuestData {
            id = "q3",
            title = "Coffee Break",
            description = "Photograph a coffee cup.",
            difficulty = "Hard",
            xpReward = 120,
            collectibleName = "Golden Mug",
            collectibleRarity = "Rare"
        }
    };

    public static event System.Action QuestsChanged;

    public static List<QuestData> GetQuests() => quests;

    public static List<QuestData> ActiveQuests =>
        quests.Where(q => q.status != QuestStatus.Completed).ToList();

    public static void Start(string questId)
    {
        var quest = quests.FirstOrDefault(q => q.id == questId);

        if (quest == null || quest.status == QuestStatus.Completed)
            return;

        quest.status = QuestStatus.InProgress;
        QuestsChanged?.Invoke();
    }

    public static QuestResult Complete(string questId)
    {
        var quest = quests.FirstOrDefault(q => q.id == questId);

        if (quest == null || quest.status == QuestStatus.Completed)
            return null;

        quest.status = QuestStatus.Completed;

        bool leveled = PlayerService.AddXp(quest.xpReward);

        QuestsChanged?.Invoke();

        return new QuestResult
        {
            quest = quest,
            xpGained = quest.xpReward,
            collectibleName = quest.collectibleName,
            collectibleRarity = quest.collectibleRarity,
            leveledUp = leveled,
            newLevel = PlayerService.Current.level
        };
    }
}
