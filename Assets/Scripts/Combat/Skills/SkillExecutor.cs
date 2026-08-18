using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// [Phase 5 – OOP Skill System]
/// MonoBehaviour chứa Registry (từ điển) của tất cả SkillAction.
/// CombatManager chỉ cần gọi:  Executor.Run(skill, target, dmg, eff)
///
/// Cách gắn: Add Component vào cùng GameObject chứa CombatManager.
/// </summary>
public class SkillExecutor : MonoBehaviour
{
    public static SkillExecutor Instance { get; private set; }

    private CombatManager CM;
    private Dictionary<string, SkillAction> playerSkills = new Dictionary<string, SkillAction>();
    private Dictionary<EnemySkillID, EnemySkillAction> enemySkills = new Dictionary<EnemySkillID, EnemySkillAction>();

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(this); return; }
        Instance = this;
    }

    private void Start()
    {
        CM = CombatManager.Instance;
        RegisterAllSkills();
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  REGISTRY
    // ─────────────────────────────────────────────────────────────────────────
    private void RegisterAllSkills()
    {
        // ── Player Skills ──────────────────────────────────────────────────
        Register("Sword_Skill_1", new SwordSkill1());
        Register("Sword_Skill_2", new SwordSkill2());
        Register("Sword_Skill_3", new SwordSkill3());
        Register("Sword_Skill_4", new SwordSkill4());

        // ── Enemy Skills ───────────────────────────────────────────────────
        RegisterEnemy(EnemySkillID.SummonSpider,        new ESkill_SummonSpider());
        RegisterEnemy(EnemySkillID.Bloodsuck,           new ESkill_Bloodsuck());
        RegisterEnemy(EnemySkillID.Venom,               new ESkill_Venom());
        RegisterEnemy(EnemySkillID.PowerBite,           new ESkill_PowerBite());
        RegisterEnemy(EnemySkillID.SpeedDash,           new ESkill_SpeedDash());
        RegisterEnemy(EnemySkillID.StickyWeb,           new ESkill_StickyWeb());
        RegisterEnemy(EnemySkillID.HealEnemy,           new ESkill_HealEnemy());
        RegisterEnemy(EnemySkillID.NhenNuVuong_ClawRip, new ESkill_ClawRip());
        RegisterEnemy(EnemySkillID.Dragon_FlameBreath,  new ESkill_FlameBreath());
        RegisterEnemy(EnemySkillID.BocGiap,             new ESkill_BocGiap());
    }

    private void Register(string id, SkillAction skill)
    {
        skill.Init(CM);
        playerSkills[id] = skill;
    }

    private void RegisterEnemy(EnemySkillID id, EnemySkillAction skill)
    {
        skill.Init(CM);
        enemySkills[id] = skill;
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  PUBLIC API
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Chạy player skill. Trả về true nếu async (CM không cần finish turn).
    /// </summary>
    public bool RunPlayerSkill(string skillID, EnemyStats target, float flatDmg, float effectChance)
    {
        if (!playerSkills.TryGetValue(skillID, out SkillAction skill))
        {
            Debug.LogWarning($"[SkillExecutor] Player skill không tìm thấy: {skillID}");
            return false;
        }
        skill.Execute(target, flatDmg, effectChance);
        return skill.IsAsync;
    }

    /// <summary>
    /// Chạy enemy skill. Trả về true nếu async (EnemyTurn không cần resume ngay).
    /// </summary>
    public bool RunEnemySkill(EnemySkillID skillID, EnemyStats attacker)
    {
        if (!enemySkills.TryGetValue(skillID, out EnemySkillAction skill))
        {
            Debug.LogWarning($"[SkillExecutor] Enemy skill không tìm thấy: {skillID}");
            return false;
        }
        skill.Execute(attacker);
        return skill.IsAsync;
    }

    /// <summary>Chạy một Coroutine từ SkillAction (vì SkillAction không phải MonoBehaviour).</summary>
    public Coroutine RunCoroutine(IEnumerator routine) => StartCoroutine(routine);
}
