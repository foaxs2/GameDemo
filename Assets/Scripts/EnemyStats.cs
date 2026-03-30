using UnityEngine;
using UnityEngine.Rendering;

public class EnemyStats : Unit
{
    [Header("Thông tin cơ bản")]
    public string enemyName;
    public Sprite enemySprite;

    [Header("Chỉ số chiến đấu bổ sung")]
    public float attack;
    public float critChance;
    public float evasion;

    [Header("Kỹ năng (Skill)")]
    public string skillName;
    [TextArea(3, 5)]
    public string skillDescription;
    public int maxCooldown;
    [HideInInspector] public int currentCooldown = 0;

    [Header("Phần thưởng")]
    public int goldDrop;
    public int expDrop;

    void Start()
    {
        currentHP = maxHP;
        currentDefense = baseDefense;
        currentSpeed = baseSpeed;
        currentCooldown = 0;
    }

    public override void TakeDamage(float damage, bool isTrueDamage = false, bool ignoreFracture = false)
    {
        if (!ignoreFracture && DebuffManager.Instance != null)
        {
            damage *= DebuffManager.Instance.GetDamageTakenMultiplier(this);
        }

        float finalDamage = isTrueDamage ? damage : Mathf.Max(1, damage - currentDefense);

        currentHP -= Mathf.FloorToInt(finalDamage);
        if (currentHP < 0) currentHP = 0;
    }
}