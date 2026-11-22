using System;

[Serializable]
public class Quest
{
    public string id;
    public string title;
    public string description;
    public string category;
    public string difficulty;
    public int xp_reward;
    public string collectible_reward;
    public string target_object;
}

// JsonUtility cannot directly deserialize a JSON array.
[Serializable]
public class QuestList
{
    public Quest[] items;
}