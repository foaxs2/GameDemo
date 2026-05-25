using UnityEngine;

/// <summary>
/// [Phase 5] Base class cho kỹ năng kẻ thù.
/// Khác PlayerSkillAction ở chỗ: Execute nhận EnemyStats attacker, không có flatDmg/effectChance.
/// </summary>
public abstract class EnemySkillAction
{
    protected CombatManager CM { get; private set; }
    protected CombatUIManager   UI      => CM?.UI;
    protected PlayerManager     Player  => PlayerManager.Instance;
    protected DebuffManager     Debuffs => DebuffManager.Instance;
    protected BuffManager       Buffs   => BuffManager.Instance;
    protected FloatingTextManager FTM   => FloatingTextManager.Instance;

    public void Init(CombatManager cm) { CM = cm; }

    /// <summary>Thực thi skill của kẻ thù.</summary>
    public abstract void Execute(EnemyStats attacker);

    public virtual bool IsAsync => false;

    /// <summary>Gây sát thương lên player.</summary>
    protected void DealToPlayer(float rawDamage, bool trueDmg = false, bool canCrit = false)
    {
        Player?.TakeDamage(rawDamage, trueDmg, canCrit);
    }

    /// <summary>Hiện floating text tại vị trí HP bar của player.</summary>
    protected void ShowPlayerDmgText(float amount, Color? color = null)
    {
        var pos = UI?.GetPlayerHPBarTransform()?.position ?? Vector3.zero;
        FTM?.SpawnText(pos, amount.ToString(), color ?? Color.white);
    }

    /// <summary>
    /// Hồi HP cho một EnemyStats bất kỳ và hiện số xanh lá tại vị trí của nó.
    /// </summary>
    protected void HealUnit(EnemyStats target, int amount)
    {
        if (target == null || target.currentHP <= 0) return;
        target.currentHP = Mathf.Min(target.maxHP, target.currentHP + amount);

        // Hiện số xanh lá tại vị trí của quái được heal
        Transform t = UI?.GetEnemyUITransform(target);
        Vector3 pos = t != null ? t.position : Vector3.zero;
        FTM?.SpawnText(pos, "+" + amount, Color.green);
    }
}
