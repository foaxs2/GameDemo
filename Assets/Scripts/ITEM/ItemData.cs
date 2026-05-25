using UnityEngine;

public enum ItemType
{
    Consumable,      // Vật phẩm tiêu hao (dùng được trong combat)
    Equipment,       // Trang bị (vũ khí, giáp, phụ kiện)
    Material,        // Nguyên liệu (chỉ dùng ở town)
    KeyItem          // Vật phẩm nhiệm vụ
}

public enum ConsumableType
{
    HP,              // Hồi máu
    SEN,             // Hồi sanity
    Food,            // Hồi lương thực
    Buff_ATK,        // Tăng sát thương
    Buff_DEF,        // Tăng phòng thủ
    Buff_SPD,        // Tăng tốc độ
    Buff_CRIT,       // Tăng chí mạng
    Buff_EVA,        // Tăng né tránh
    WeaponCoating,   // Tẩm vũ khí (Bleed/Fire/Poison) — xem coatingType
    Cleanse          // Giải debuff
}

public enum EquipmentSlot
{
    Weapon,
    Armor,
    Accessory1,
    Accessory2
}

[CreateAssetMenu(fileName = "New_Item", menuName = "Game/Item")]
public class ItemData : ScriptableObject
{
    [Header("Thông tin cơ bản")]
    public string itemName;
    [TextArea(2, 4)]
    public string description;
    public ItemType itemType;
    public Sprite icon;

    [Header("Giá cả")]
    public int buyPrice;
    public int sellPrice;
    public bool canBeSold = true;

    [Header("Cho Consumable")]
    public ConsumableType consumableType;
    public int healAmount;           // Lượng hồi cố định (HP, SEN)
    public int foodRestoreAmount;    // Lượng hồi Lương Thực riêng lẻ
    public float healPercentage;     // % hồi (0.4 = 40%)
    public int buffValue;            // Giá trị buff cố định (ATK/DEF/SPD/CRIT/EVA)
    public float buffPercentage;     // Giá trị buff %
    public int buffDuration;         // Số lượt buff tồn tại
    public int maxStacks;            // Stack tối đa (1-3)
    public WeaponCoatingType coatingType;   // Loại tẩm vũ khí (chỉ cho WeaponCoating)
    [Range(0f, 100f)]
    public float coatingChance;      // % áp dụng hiệu ứng tẩm khi đánh (0-100)

    [Header("Cho Equipment")]
    public EquipmentSlot equipmentSlot;

    // Chỉ số cơ bản
    public float damageBonus;
    public float defenseBonus;
    public float speedBonus;
    public float hpBonus;
    public float critBonus;
    public float evasionBonus;

    // Hiệu ứng đặc biệt
    public float armorPenetration;   // Xuyên giáp
    public float bleedChance;        // % gây chảy máu
    public float stunChance;         // % gây choáng
    public float bonusDamageVsHuman; // +damage vs human
    public float bonusDamageVsNonHuman; // +damage vs non-human

    [Header("Stack & Sử dụng")]
    public bool isStackable;
    public int maxStackSize = 99;
    public bool usableInCombat;      // Có dùng được trong combat không
}