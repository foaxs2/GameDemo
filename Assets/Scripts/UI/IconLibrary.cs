using UnityEngine;

/// <summary>
/// ScriptableObject chứa toàn bộ sprite icon cho buff/debuff.
/// Tạo 1 asset duy nhất qua menu: Create → Game → Icon Library
/// </summary>
[CreateAssetMenu(fileName = "IconLibrary", menuName = "Game/Icon Library")]
public class IconLibrary : ScriptableObject
{
    [Header("Buff Icons")]
    public Sprite atkUpIcon;
    public Sprite defUpIcon;
    public Sprite spdUpIcon;
    public Sprite critUpIcon;
    public Sprite evaUpIcon;

    [Header("Weapon Coating Icons")]
    public Sprite coatingBleedIcon;
    public Sprite coatingFireIcon;
    public Sprite coatingPoisonIcon;

    [Header("Debuff Icons")]
    public Sprite stunIcon;
    public Sprite poisonIcon;
    public Sprite burnIcon;
    public Sprite bleedIcon;
    public Sprite fractureIcon;

    [Header("Stack Arrows (đặt ở góc dưới icon)")]
    [Tooltip("Mũi tên stack x1")] public Sprite arrow1;
    [Tooltip("Mũi tên stack x2")] public Sprite arrow2;
    [Tooltip("Mũi tên stack x3")] public Sprite arrow3;

    public Sprite GetBuffIcon(BuffType type, WeaponCoatingType coating = WeaponCoatingType.None)
    {
        return type switch
        {
            BuffType.ATK_Up        => atkUpIcon,
            BuffType.DEF_Up        => defUpIcon,
            BuffType.SPD_Up        => spdUpIcon,
            BuffType.CRIT_Up       => critUpIcon,
            BuffType.EVA_Up        => evaUpIcon,
            BuffType.WeaponCoating => coating switch
            {
                WeaponCoatingType.Bleed  => coatingBleedIcon,
                WeaponCoatingType.Fire   => coatingFireIcon,
                WeaponCoatingType.Poison => coatingPoisonIcon,
                _                        => null
            },
            _ => null
        };
    }

    public Sprite GetDebuffIcon(DebuffType type)
    {
        return type switch
        {
            DebuffType.Stun     => stunIcon,
            DebuffType.Poison   => poisonIcon,
            DebuffType.Burn     => burnIcon,
            DebuffType.Bleed    => bleedIcon,
            DebuffType.Fracture => fractureIcon,
            _                   => null
        };
    }

    public Sprite GetStackArrow(int stacks)
    {
        return stacks switch { 1 => arrow1, 2 => arrow2, _ => arrow3 };
    }
}
