using UnityEngine;

/// <summary>
/// [Phase 5 – OOP Skill System]
/// Base class trừu tượng cho MỌI kỹ năng trong game (cả player lẫn enemy).
///
/// Mỗi skill cụ thể kế thừa class này và override Execute().
/// CombatManager chỉ cần gọi: skill.Execute()
/// </summary>
public abstract class SkillAction
{
    // ─── Context được inject khi tạo ─────────────────────────────────────────
    /// <summary>CombatManager để gọi FinishPlayerTurn, UI, v.v.</summary>
    protected CombatManager CM { get; private set; }

    /// <summary>
    /// Inject context sau khi tạo instance.
    /// SkillExecutor sẽ tự gọi trước khi Execute().
    /// </summary>
    public void Init(CombatManager cm)
    {
        CM = cm;
    }

    // ─── Shortcut helpers ────────────────────────────────────────────────────
    protected CombatUIManager   UI       => CM?.UI;
    protected PlayerManager     Player   => PlayerManager.Instance;
    protected DebuffManager     Debuffs  => DebuffManager.Instance;
    protected BuffManager       Buffs    => BuffManager.Instance;
    protected FloatingTextManager FTM    => FloatingTextManager.Instance;

    // ─── Abstract API ────────────────────────────────────────────────────────

    /// <summary>
    /// Thực thi kỹ năng. Được gọi bởi SkillExecutor.
    /// - target: mục tiêu chính (null nếu là buff/random).
    /// - flatDmg: bonus damage từ skill level.
    /// - effectChance: xác suất effect phụ từ skill level.
    /// </summary>
    public abstract void Execute(EnemyStats target, float flatDmg, float effectChance);

    /// <summary>
    /// Nếu skill cần chạy qua nhiều frame (Coroutine), trả về true.
    /// CombatManager sẽ KHÔNG tự gọi FinishPlayerTurn — skill tự lo.
    /// </summary>
    public virtual bool IsAsync => false;

    // ─── Common helpers cho subclass ─────────────────────────────────────────

    /// <summary>Gây sát thương cho kẻ thù và hiện FloatingText.</summary>
    protected void DealToEnemy(EnemyStats target, float multiplier, float flatDmg,
                               float extraCritChance = 0f, bool triggerOnHit = true)
    {
        if (target == null || target.currentHP <= 0) return;

        var result = CombatCalculator.CalculatePlayerDamage(
            Player, target, multiplier, flatDmg, extraCritChance);

        if (result.missed) { UI?.ShowMissText(target); return; }

        target.currentHP -= result.finalDamage;
        if (target.currentHP < 0) target.currentHP = 0;

        Color col = result.isCrit ? Color.yellow : Color.white;
        Transform tf = UI?.GetEnemyUITransform(target);
        FTM?.SpawnText(tf != null ? tf.position : Vector3.zero,
                       result.finalDamage.ToString(), col);

        if (result.isCrit) BossPassiveManager.Instance?.OnPlayerCritHit(target);
        if (triggerOnHit)  CM?.InvokeApplyOnHitEffects(target, result.rawDamage);
    }

    /// <summary>Gây sát thương lên player và hiện FloatingText.</summary>
    protected void DealToPlayer(float rawDamage, bool trueDamage = false, bool canCrit = false)
    {
        Player?.TakeDamage(rawDamage, trueDamage, canCrit);
        FTM?.SpawnText(UI?.GetPlayerHPBarTransform()?.position ?? Vector3.zero,
                       rawDamage.ToString(), Color.white);
    }

    /// <summary>Gọi sau khi skill đồng bộ hoàn tất.</summary>
    protected void FinishTurn()
    {
        CM?.FinishPlayerTurnAfterSkill();
    }
}
