using UnityEngine;
using System.Collections.Generic;
public class PlayerManager : Unit
{
    public static PlayerManager Instance;

    [Header("Thông tin nhân vật (Save Slot)")]
    public string playerName = "Người Chơi";
    public float playTime = 0f;
    public int characterIconIndex = 0;

    [Header("Tài nguyên sinh tồn riêng")]
    public int sen = 10;
    public int maxSen = 10;
    public int food = 50;
    public int maxFood = 50;
    public int gold = 50;

    [Header("Chỉ số thuộc tính (Mặc định = 10)")]
    public int str = 10;
    public int dex = 10;
    public int vit = 10;
    public int agl = 10;

    [Header("Chỉ số chiến đấu bổ sung")]
    public float baseAttack = 1f;
    public float baseCrit = 5f;
    public float baseEvasion = 5f;

    [Header("Cấp độ & Kinh nghiệm")]
    public int level = 1;
    public int currentExp = 0;
    public int expToNextLevel = 50;
    public int unspentStatPoints = 0;

    [Header("Công thức EXP (Cố định mỗi cấp)")]
    [SerializeField] private int fixedExpPerLevel = 50;

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
    public Dictionary<string, int> killedMonsters = new Dictionary<string, int>();

    private bool _isSanityCollapsing = false;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else if (Instance != this)
        {
            if (gameObject.GetComponent<Canvas>() != null || gameObject.GetComponent<Camera>() != null)
            {
                Destroy(this);
            }
            else
            {
                Destroy(gameObject);
            }
        }
    }

    void OnEnable()
    {
        UnityEngine.SceneManagement.SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDisable()
    {
        UnityEngine.SceneManagement.SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(UnityEngine.SceneManagement.Scene scene, UnityEngine.SceneManagement.LoadSceneMode mode)
    {
        // Reset flag khi vào Town để lần sau ReduceSanity hoạt động bình thường
        if (scene.name == "Town")
            _isSanityCollapsing = false;
    }

    void Start()
    {
        UpdateEquipmentStats();
        UpdateMaxHP();
        currentHP = maxHP;
        currentSpeed = GetTotalSpeed();
        currentDefense = baseDefense;
    }

    void Update()
    {
        // Tăng thời gian chơi trong giây
        playTime += Time.deltaTime;
    }

    public void UpdateMaxHP()
    {
        int oldMaxHP = maxHP;
        maxHP = 5 + Mathf.FloorToInt(vit * 1f) + Mathf.FloorToInt(equipmentHPBonus);

        if (oldMaxHP > 0 && maxHP > oldMaxHP)
            currentHP += (maxHP - oldMaxHP);

        if (currentHP > maxHP) currentHP = maxHP;
    }

    public override float GetTotalAttack()
    {
        float base_ = baseAttack + (str * 0.2f) + equipmentDamageBonus;
        float buff  = BuffManager.Instance != null ? BuffManager.Instance.GetBuffValue(this, BuffType.ATK_Up) : 0f;
        return base_ + buff;
    }

    public override float GetTotalSpeed()
    {
        float base_ = baseSpeed + (agl * 0.5f) + equipmentSpeedBonus;
        float buff  = BuffManager.Instance != null ? BuffManager.Instance.GetBuffValue(this, BuffType.SPD_Up) : 0f;
        return base_ + buff;
    }

    public override float GetTotalCrit()
    {
        float base_ = baseCrit + (dex * 0.15f) + equipmentCritBonus;
        float buff  = BuffManager.Instance != null ? BuffManager.Instance.GetBuffValue(this, BuffType.CRIT_Up) : 0f;
        return base_ + buff;
    }

    public override float GetTotalEvasion()
    {
        float eva  = baseEvasion + (dex * 0.1f) + equipmentEvasionBonus;
        float buff = BuffManager.Instance != null ? BuffManager.Instance.GetBuffValue(this, BuffType.EVA_Up) : 0f;
        if (isDefending) eva += 25f;
        return eva + buff;
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

        UpdateMaxHP();
    }

    private void AddEquipmentStats(ItemData eq)
    {
        if (eq == null) return;
        equipmentDamageBonus       += eq.damageBonus;
        equipmentDefenseBonus      += eq.defenseBonus;
        equipmentSpeedBonus        += eq.speedBonus;
        equipmentHPBonus           += eq.hpBonus;
        equipmentCritBonus         += eq.critBonus;
        equipmentEvasionBonus      += eq.evasionBonus;
        equipmentArmorPenetration  += eq.armorPenetration;
        equipmentBleedChance       += eq.bleedChance;
        equipmentStunChance        += eq.stunChance;
        equipmentBonusDamageVsHuman    += eq.bonusDamageVsHuman;
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
        if (sen < 0) sen = 0;

        if (sen <= 0 && !_isSanityCollapsing)
            OnSanityCollapse();
    }

    /// <summary>
    /// SEN = 0: phân biệt scene hiện tại.
    /// - Combat: hiện madnessPanel, không về Town ngay.
    /// - Dungeon / khác: set DeathContext rồi về Town, SEN = 2, HP giữ nguyên.
    /// </summary>
    private void OnSanityCollapse()
    {
        _isSanityCollapsing = true;

        string sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;

        if (sceneName == "Combat")
        {
            // Pause combat và hiện bảng phát điên — CombatManager xử lý việc về Town
            if (CombatManager.Instance != null)
                CombatManager.Instance.ShowMadnessPanel();
            else
            {
                // Fallback nếu không có CombatManager
                sen = 2;
                DeathContext.Pending = DeathContext.DeathType.CombatMadness;
                SaveSystem.Instance?.Save();
                UnityEngine.SceneManagement.SceneManager.LoadScene("Town");
            }
        }
        else
        {
            // Dungeon hoặc bất kỳ scene nào khác — hiện bảng phát điên trong Dungeon
            if (DungeonDeathUI.Instance != null)
                DungeonDeathUI.Instance.ShowMadnessPanel();
            else
            {
                // Fallback nếu không có DungeonDeathUI trong scene
                sen = 2;
                DeathContext.Pending = DeathContext.DeathType.DungeonMadness;
                SaveSystem.Instance?.Save();
                UnityEngine.SceneManagement.SceneManager.LoadScene("Town");
            }
        }
    }

    public void RestoreSanityCollapse()
    {
        _isSanityCollapsing = false;
    }

    public override void TakeDamage(float damage, bool isTrueDamage = false, bool ignoreFracture = false)
    {
        if (!ignoreFracture && DebuffManager.Instance != null)
            damage *= DebuffManager.Instance.GetDamageTakenMultiplier(this);

        float defToUse = currentDefense + equipmentDefenseBonus;
        if (isDefending)
            defToUse += 1f + Mathf.FloorToInt((baseDefense + equipmentDefenseBonus) * 0.5f);

        float finalDamage = isTrueDamage ? damage : Mathf.Max(1, damage - defToUse);

        currentHP -= Mathf.FloorToInt(finalDamage);
        if (currentHP < 0) currentHP = 0;
    }

    public void AddExp(int amount)
    {
        currentExp += amount;
        // Kiểm tra lên cấp (có thể lên nhiều cấp cùng lúc)
        while (currentExp >= GetRequiredExpForLevel(level))
        {
            currentExp -= GetRequiredExpForLevel(level);
            LevelUp();
        }
        
        // Cập nhật lại mốc hiển thị cho UI
        expToNextLevel = GetRequiredExpForLevel(level);
    }

    /// <summary>
    /// Tính toán lượng EXP cần thiết để từ cấp hiện tại lên cấp tiếp theo.
    /// Công thức mới: Cố định theo fixedExpPerLevel.
    /// </summary>
    public int GetRequiredExpForLevel(int lv)
    {
        return fixedExpPerLevel;
    }

    private void LevelUp()
    {
        level++;
        // Không tăng maxFood và không hồi maxFood khi lên cấp nữa
        unspentStatPoints += 1; // Cộng 1 điểm kĩ năng/chỉ số mỗi cấp thay vì +3
        Debug.Log($"[LÊN CẤP] Chúc mừng! Bạn đã đạt cấp {level}. Nhận 1 điểm chỉ số.");
    }

    public bool UpgradeStat(string statType)
    {
        if (unspentStatPoints <= 0) return false;
        switch (statType)
        {
            case "STR": str++;  break;
            case "DEX": dex++;  break;
            case "VIT": vit++; UpdateMaxHP(); break;
            case "AGL": agl++; currentSpeed = GetTotalSpeed(); break;
            default: return false;
        }
        unspentStatPoints--;
        return true;
    }

    [ContextMenu("Thêm Đồ Test (Editor Only)")]
    public void AddTestItemsEditor()
    {
        Debug.Log("[CONTEXTMENU] Gán ItemData bằng code trong hàm này để test.");
    }
}