using UnityEngine;

public enum EnemyType
{
    Human,
    NonHuman
}

public enum EnemySkillID
{
    None,                    // Không có skill
    SummonSpider,            // Nhện Tinh Anh: Gọi Đàn
    Bloodsuck,               // Đỉa: Hút Máu
    Venom,                   // Thằn Lằn Đen: Cắn Độc
    PowerBite,               // Nhện Hang: Cắn +3 ATK
    SpeedDash,               // Chuột Nhảy: Tăng Tốc
    StickyWeb,               // Nhện: Tơ Dính
    HealEnemy,               // Cốt Y: Hồi máu đồng minh (hoặc bản thân)
    NhenNuVuong_ClawRip,     // Boss LV5 — Nhện Nữ Vương: Cào Xé Thịt
    Dragon_FlameBreath,      // Boss LV10 — Rồng Cổ Đại: Phun Lửa
}

public class EnemyStats : Unit
{
    [Header("Thông tin cơ bản")]
    public string enemyName;
    public Sprite enemySprite;
    public EnemyType enemyType = EnemyType.NonHuman;

    [Header("Chỉ số chiến đấu bổ sung")]
    public float attack;
    public float critChance;
    public float evasion;

    [Header("Kỹ năng (Skill)")]
    public EnemySkillID skillID = EnemySkillID.None;
    public string skillName;
    [TextArea(3, 5)]
    public string skillDescription;
    public int maxCooldown;
    public int initialCooldown = 2;
    [HideInInspector] public int currentCooldown = 0;

    [Tooltip("Lượng HP hồi khi dùng kĩ năng HealEnemy (cài theo từng prefab).")]
    public int healAmount = 15;

    [Header("Phần thưởng")]
    public int goldDrop;
    public int expDrop;

    // ─── BOSS INNATE PASSIVES ──────────────────────────────────────────────
    [Header("Boss Innate Passives")]
    public bool hasSpawnOnAllyDeath;                        // Nhện Nữ Vương — Sinh Đàn
    public bool hasEnrageOnLowHP;                           // Cả 2 boss — Hỏa Điền
    [Range(0f, 1f)] public float enrageHPThreshold = 0.4f;  // Ngưỡng HP kích hoạt (mặc định 40%)
    [SerializeField] public float enrageATKBonus = 3f;      // ATK tăng khi Hỏa Điền
    [SerializeField] public float enrageSPDBonus = 5f;      // SPD tăng khi Hỏa Điền
    public bool hasPassivePoison;                           // Nhện Nữ Vương — Nọc Độc Thụ Động
    [Range(0f, 100f)] public float passivePoisonChance = 20f;
    public int passivePoisonDuration = 4;
    public bool hasCrackScales;                             // Rồng — Vảy Rắn Nứt
    public bool hasRoarPassive;                             // Rồng — Tiếng Rồng Suy Nhược
    public int roarEveryNTurns = 5;                         // Mặc định 5 lượt theo Boss.md
    public bool hasWeakToFire;                              // Nhện Nữ Vương — Yếu với Lửa
    [Range(0f, 1f)] public float fireWeakDEFReduction = 0.2f; // Giảm 20% DEF khi bị đánh bằng lửa

    [HideInInspector] public int roarTurnCounter = 0;
    [HideInInspector] public bool isEnraged = false;
    [HideInInspector] public bool spawnedOnAllyDeath = false;

    /// <summary>Hệ số nhân damage Poison (mặc định 1). Tăng lên 4 khi Vảy Rắn Nứt kích hoạt.</summary>
    [HideInInspector] public float poisonDamageMultiplier = 1f;

    [Header("Boss Warning Text")]
    [Tooltip("Text nhỏ xuất hiện khi boss chuẩn bị dùng skill (để trống = không hiện).")]
    public string bossSkillWarningText = "";

    void Start()
    {
        currentHP       = maxHP;
        currentDefense  = baseDefense;
        currentSpeed    = baseSpeed;
        currentCooldown = initialCooldown;
    }

    public override void TakeDamage(float damage, bool isTrueDamage = false, bool ignoreFracture = false)
    {
        if (!ignoreFracture && DebuffManager.Instance != null)
            damage *= DebuffManager.Instance.GetDamageTakenMultiplier(this);

        float finalDamage = isTrueDamage ? damage : Mathf.Max(1, damage - currentDefense);

        currentHP -= Mathf.FloorToInt(finalDamage);
        if (currentHP < 0) currentHP = 0;
    }

    /// <summary>ATK base + buff ATK_Up từ BuffManager.</summary>
    public override float GetTotalAttack()
    {
        float buffBonus = BuffManager.Instance != null ? BuffManager.Instance.GetBuffValue(this, BuffType.ATK_Up) : 0f;
        return attack + buffBonus;
    }

    /// <summary>currentSpeed (đã tính debuff) + buff SPD_Up từ BuffManager.</summary>
    public override float GetTotalSpeed()
    {
        float buffBonus = BuffManager.Instance != null ? BuffManager.Instance.GetBuffValue(this, BuffType.SPD_Up) : 0f;
        return currentSpeed + buffBonus;
    }
}