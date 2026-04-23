using System;

public enum DebuffType
{
    Stun,
    Poison,
    Burn,
    Bleed,
    Fracture
}

[Serializable]
public class DebuffInstance
{
    public DebuffType Type;
    public int Duration;
    public int Stacks;
    public float StoredDamage;

    public DebuffInstance(DebuffType type, int duration, int stacks = 1, float storedDamage = 0f)
    {
        Type = type;
        Duration = duration;
        Stacks = stacks;
        StoredDamage = storedDamage;
    }
}