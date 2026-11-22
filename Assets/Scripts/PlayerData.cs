using System;

[Serializable]
public class PlayerData
{
    public string name = "dev-test";
    public int level = 1;
    public int xp = 0;

    public int XpToNext => 100 * level;
    public float Progress => (float)xp / XpToNext;
}