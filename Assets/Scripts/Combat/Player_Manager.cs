using UnityEngine;
using UnityEngine.Rendering;

public class PlayerManager : Unit
{
    public static PlayerManager Instance;

    [Header("Tài nguyên sinh tồn riêng")]
    public int sen = 10;
    public int maxSen = 10;
    public int food = 50;
    public int maxFood = 50;
    public int gold = 0;

    [Header("Chỉ số thuộc tính")]
    public int str = 99;
    public int dex = 99;
    public int vit = 99;
    public int agl = 99;

    [Header("Chỉ số chiến đấu bổ sung")]
    public float baseAttack = 1f;
    public float baseCrit = 5f;
    public float baseEvasion = 5f;

    [Header("Cấp độ & Kinh nghiệm")]
    public int level = 1;
    public int currentExp = 0;
    public int expToNextLevel = 50;
    public int unspentStatPoints = 0;

    [Header("Equipment Bonuses")]
    public float equipmentDamageBonus;
    public float equipmentDefenseBonus;
    public float equipmentSpeedBonus;
    public float equipmentHPBonus;
    public float equipmentCritBonus;
    public float equipmentEvasionBonus;
    public float equipmentArmorPenetration;

    [Header("Equipment Debuffs & Type Bonuses")]
    public float equipmentBleedChance;
    public float equipmentStunChance;
    public float equipmentBonusDamageVsHuman;
    public float equipmentBonusDamageVsNonHuman;

    [HideInInspector] public bool isDefending = false;
    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        UpdateEquipmentStats();
        UpdateMaxHP();
        currentHP = maxHP;
        currentSpeed = GetTotalSpeed();
        currentDefense = baseDefense;
    }

    public void UpdateMaxHP()
    {
        int oldMaxHP = maxHP;
        maxHP = 5 + Mathf.FloorToInt(vit * 1f) + Mathf.FloorToInt(equipmentHPBonus);
        
        // Game RPG: Khi Máu tối đa được cộng thêm từ trang bị, thì máu hiện tại cũng được buff lên tương ứng
        if (maxHP > oldMaxHP && oldMaxHP > 0)
        {
            currentHP += (maxHP - oldMaxHP);
        }

        if (currentHP > maxHP) currentHP = maxHP;
    }

    public override float GetTotalAttack()
    {
        float baseAtk = baseAttack + (str * 0.2f);
        return baseAtk + equipmentDamageBonus;
    }

    public override float GetTotalSpeed()
    {
        float baseSpd = baseSpeed + (agl * 0.5f);
        return baseSpd + equipmentSpeedBonus;
    }

    public override float GetTotalCrit()
    {
        float baseCrit = this.baseCrit + (dex * 0.15f);
        return baseCrit + equipmentCritBonus;
    }

    public override float GetTotalEvasion()
    {
        float baseEva = this.baseEvasion + (dex * 0.1f);
        if (isDefending) baseEva += 25f;
        return baseEva + equipmentEvasionBonus;
    }

    public void UpdateEquipmentStats()
    {
        if (InventoryManager.Instance == null) return;

        equipmentDamageBonus = 0;
        equipmentDefenseBonus = 0;
        equipmentSpeedBonus = 0;
        equipmentHPBonus = 0;
        equipmentCritBonus = 0;
        equipmentEvasionBonus = 0;
        equipmentArmorPenetration = 0;
        
        equipmentBleedChance = 0;
        equipmentStunChance = 0;
        equipmentBonusDamageVsHuman = 0;
        equipmentBonusDamageVsNonHuman = 0;

        AddEquipmentStats(InventoryManager.Instance.equippedWeapon);
        AddEquipmentStats(InventoryManager.Instance.equippedArmor);
        AddEquipmentStats(InventoryManager.Instance.equippedAccessory1);
        AddEquipmentStats(InventoryManager.Instance.equippedAccessory2);

        // Update max HP
        UpdateMaxHP();
    }

    void AddEquipmentStats(ItemData eq)
    {
        if (eq == null) return;

        equipmentDamageBonus += eq.damageBonus;
        equipmentDefenseBonus += eq.defenseBonus;
        equipmentSpeedBonus += eq.speedBonus;
        equipmentHPBonus += eq.hpBonus;
        equipmentCritBonus += eq.critBonus;
        equipmentEvasionBonus += eq.evasionBonus;
        equipmentArmorPenetration += eq.armorPenetration;

        equipmentBleedChance += eq.bleedChance;
        equipmentStunChance += eq.stunChance;
        equipmentBonusDamageVsHuman += eq.bonusDamageVsHuman;
        equipmentBonusDamageVsNonHuman += eq.bonusDamageVsNonHuman;
    }

    public void AddSanity(int amount)
    {
        sen += amount;
        if (sen > maxSen) sen = maxSen;
    }

    public void ReduceSanity(int amount)
    {
        sen -= amount;
        if (sen <= 0)
        {
            sen = 0;
            Debug.Log("Sanity đã cạn kiệt! Chuyến thám hiểm thất bại.");
        }
    }

    // Ghi đè hàm nhận sát thương từ Unit
    public override void TakeDamage(float damage, bool isTrueDamage = false, bool ignoreFracture = false)
    {
        if (!ignoreFracture && DebuffManager.Instance != null)
            damage *= DebuffManager.Instance.GetDamageTakenMultiplier(this);

        //PHÒNG THỦ ---
        float defToUse = currentDefense + equipmentDefenseBonus;
        if (isDefending)
        {
            defToUse += 1f + Mathf.FloorToInt((baseDefense + equipmentDefenseBonus) * 0.5f);
        }

        float finalDamage = isTrueDamage ? damage : Mathf.Max(1, damage - defToUse);

        currentHP -= Mathf.FloorToInt(finalDamage);
        if (currentHP < 0) currentHP = 0;
    }

    public void AddExp(int amount)
    {
        currentExp += amount;

        // Dùng vòng lặp while lỡ người chơi nhận 1 lượng EXP khổng lồ nhảy liền 2-3 cấp
        while (currentExp >= expToNextLevel)
        {
            currentExp -= expToNextLevel;
            LevelUp();
        }
    }

    private void LevelUp()
    {
        level++;
        expToNextLevel += 25; // Cấp sau cần nhiều hơn cấp trước 25 EXP

        maxFood += 3; // Tăng giới hạn lương thực thêm 3
        food = maxFood; // Bơm đầy thức ăn khi lên cấp

        unspentStatPoints += 3; // Cho 3 điểm chỉ số

        Debug.Log($"[LÊN CẤP] Chúc mừng! Bạn đạt Cấp {level}. Giới hạn lương thực: {maxFood}. Nhận 3 điểm chỉ số.");
    }

    // THÊM HÀM NÀY ĐỂ CỘNG CHỈ SỐ TỪ GIAO DIỆN
    public bool UpgradeStat(string statType)
    {
        if (unspentStatPoints <= 0) return false;

        switch (statType)
        {
            case "STR": str++; break;
            case "DEX": dex++; break;
            case "VIT":
                vit++;
                UpdateMaxHP(); // VIT tăng thì Máu tối đa cũng phải tính lại ngay
                break;
            case "AGL":
                agl++;
                currentSpeed = GetTotalSpeed(); // AGL tăng thì Tốc độ phải tính lại
                break;
            default: return false;
        }

        unspentStatPoints--;
        return true;
    }
}