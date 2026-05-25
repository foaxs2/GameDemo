using System.Collections;
using UnityEngine;

// ═══════════════════════════════════════════════════════════════════════════
//  PLAYER SKILLS — Sword
// ═══════════════════════════════════════════════════════════════════════════

/// <summary>Sword_Skill_1 — Tấn công ngẫu nhiên 4 đòn với nhân hệ giảm dần (1.0 / 0.85 / 0.70 / 0.55).</summary>
public class SwordSkill1 : SkillAction
{
    public override bool IsAsync => true;

    public override void Execute(EnemyStats target, float flatDmg, float effectChance)
    {
        float[] mults = { 1f, 0.85f, 0.70f, 0.55f };
        SkillExecutor.Instance.RunCoroutine(MultiHit(mults, flatDmg));
    }

    private IEnumerator MultiHit(float[] mults, float flatDmg)
    {
        foreach (float m in mults)
        {
            if (CM.CheckBattleEndPublic() || Player.currentHP <= 0) yield break;
            EnemyStats rnd = CM.GetRandomAliveEnemyPublic();
            if (rnd != null) DealToEnemy(rnd, m, flatDmg);
            yield return new UnityEngine.WaitForSeconds(0.18f);
        }
        FinishTurn();
    }
}

/// <summary>Sword_Skill_2 — Đòn đánh đơn chuẩn có thể crit.</summary>
public class SwordSkill2 : SkillAction
{
    public override void Execute(EnemyStats target, float flatDmg, float effectChance)
    {
        DealToEnemy(target, 1f, flatDmg, effectChance, true);
        FinishTurn();
    }
}

/// <summary>Sword_Skill_3 — Đánh ngay + đòn phụ sau delay 0.22s.</summary>
public class SwordSkill3 : SkillAction
{
    public override bool IsAsync => true;

    public override void Execute(EnemyStats target, float flatDmg, float effectChance)
    {
        DealToEnemy(target, 1f, flatDmg, 0f, true);
        float secondMult = (100f - effectChance) / 100f;
        SkillExecutor.Instance.RunCoroutine(DelayedHit(target, secondMult, flatDmg));
    }

    private IEnumerator DelayedHit(EnemyStats target, float mult, float flatDmg)
    {
        yield return new UnityEngine.WaitForSeconds(0.22f);
        if (!CM.CheckBattleEndPublic() && Player.currentHP > 0)
            if (target != null && target.currentHP > 0)
                DealToEnemy(target, mult, flatDmg, 0f, true);
        FinishTurn();
    }
}

/// <summary>Sword_Skill_4 — Tẩm Bleed lên vũ khí qua BuffManager.</summary>
public class SwordSkill4 : SkillAction
{
    public override void Execute(EnemyStats target, float flatDmg, float effectChance)
    {
        int coatDur = 4; // default; nếu muốn dùng skill.buffDuration cần truyền thêm
        Buffs?.AddWeaponCoating(Player, WeaponCoatingType.Bleed, effectChance, coatDur);
        Debug.Log($"[SwordSkill4] Bleed coating +{effectChance}% trong {coatDur} lượt.");
        FinishTurn();
    }
}
