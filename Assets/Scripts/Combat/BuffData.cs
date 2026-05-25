using System;
using System.Collections.Generic;

public enum BuffType
{
    // === BUFFS (hiển thị trước debuff) ===
    ATK_Up,         // Tăng sát thương
    DEF_Up,         // Tăng phòng thủ
    SPD_Up,         // Tăng tốc độ
    CRIT_Up,        // Tăng chí mạng
    EVA_Up,         // Tăng né tránh
    WeaponCoating,  // Tẩm vũ khí — độc nhất, ghi đè nếu đổi loại
}

public enum WeaponCoatingType
{
    None,
    Bleed,   // Tẩm chảy máu (Skill 4 + item)
    Fire,    // Tẩm lửa (tương lai)
    Poison,  // Tẩm độc (tương lai)
}

[Serializable]
public class BuffInstance
{
    public BuffType          Type;
    public float             Value;          // Giá trị buff mỗi stack
    public int               Stacks;         // Stack hiện tại (1 – MaxStacks)
    public int               MaxStacks;      // Giới hạn stack
    public int               RemainingTurns; // Số lượt còn lại

    // Chỉ dùng cho WeaponCoating
    public WeaponCoatingType CoatingType;
    public float             CoatingChance;  // % áp dụng hiệu ứng khi đánh

    // Constructor cho buff thông thường
    public BuffInstance(BuffType type, float value, int duration, int maxStacks = 3)
    {
        Type           = type;
        Value          = value;
        Stacks         = 1;
        MaxStacks      = maxStacks;
        RemainingTurns = duration;
        CoatingType    = WeaponCoatingType.None;
        CoatingChance  = 0f;
    }

    // Constructor cho WeaponCoating
    public BuffInstance(WeaponCoatingType coatingType, float chance, int duration)
    {
        Type           = BuffType.WeaponCoating;
        Value          = 0f;
        Stacks         = 1;
        MaxStacks      = 1; // Coating là độc nhất — không stack
        RemainingTurns = duration;
        CoatingType    = coatingType;
        CoatingChance  = chance;
    }
}
