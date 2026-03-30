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
    public int expToNextLevel = 100;

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
        UpdateMaxHP();
        currentHP = maxHP;
        currentSpeed = GetTotalSpeed();
        currentDefense = baseDefense;
    }

    public void UpdateMaxHP()
    {
        maxHP = 5 + Mathf.FloorToInt(vit * 0.75f);
        if (currentHP > maxHP) currentHP = maxHP;
    }

    public float GetTotalAttack() => baseAttack + (str * 0.175f);
    public float GetTotalSpeed() => baseSpeed + (agl * 1f);
    public float GetTotalCrit() => baseCrit + (dex * 0.1f);
    public float GetTotalEvasion() => baseEvasion + (dex * 0.1f) + (isDefending ? 25f : 0f);

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
        float defToUse = currentDefense;
        if (isDefending)
        {
            defToUse += 1f + Mathf.FloorToInt(baseDefense * 0.5f);
        }

        float finalDamage = isTrueDamage ? damage : Mathf.Max(1, damage - defToUse);

        currentHP -= Mathf.FloorToInt(finalDamage);
        if (currentHP < 0) currentHP = 0;
    }
}