using System;

public enum QuestStatus { Available, InProgress, Completed }

[Serializable]
public class QuestData
{
    public string id;
    public string title;
    public string description;
    public string difficulty;      // "Easy", "Medium", "Hard"
    public int xpReward;
    public string collectibleName;
    public string collectibleRarity;
    public QuestStatus status = QuestStatus.Available;
}

[Serializable]
public class QuestResult
{
    public QuestData quest;
    public int xpGained;
    public string collectibleName;
    public string collectibleRarity;
    public bool leveledUp;
    public int newLevel;
}