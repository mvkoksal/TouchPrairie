using System;

public static class PlayerService
{
    public static PlayerData Current { get; private set; } = new PlayerData();
    public static event Action<PlayerData> Changed;

    public static bool AddXp(int amount)
    {
        bool leveled = false;
        Current.xp += amount;

        while (Current.xp >= Current.XpToNext)
        {
            Current.xp -= Current.XpToNext;
            Current.level++;
            leveled = true;
        }

        Changed?.Invoke(Current);
        return leveled;
    }

    public static void Set(PlayerData data)
    {
        Current = data;
        Changed?.Invoke(Current);
    }
}