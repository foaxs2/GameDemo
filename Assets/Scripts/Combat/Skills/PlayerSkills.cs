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
        // Góc xoay cho từng nhát chém: Nhát 1 chém ngang (0°), Nhát 2 chém chéo phải (+35°), Nhát 3 chém chéo trái (-35°), Nhát 4 chém xéo nhẹ (+15°)
        float[] slashAngles = { 0f, 35f, -35f, 15f };
        try
        {
            for (int i = 0; i < mults.Length; i++)
            {
                if (CM.CheckBattleEndPublic() || Player.currentHP <= 0) yield break;
                EnemyStats rnd = CM.GetRandomAliveEnemyPublic();
                float angle = i < slashAngles.Length ? slashAngles[i] : 0f;
                if (rnd != null) DealToEnemy(rnd, mults[i], flatDmg, vfxType: VFXType.SlashNormal, vfxAngle: angle);
                yield return new UnityEngine.WaitForSeconds(0.18f);
            }
        }
        finally
        {
            FinishTurn();
        }
    }
}

/// <summary>Sword_Skill_2 — Đòn đánh đơn chuẩn có thể crit (Chém Mạnh).</summary>
public class SwordSkill2 : SkillAction
{
    public override void Execute(EnemyStats target, float flatDmg, float effectChance)
    {
        DealToEnemy(target, 1f, flatDmg, effectChance, true,
                    vfxType: VFXType.SlashHeavy, shakeMagnitude: 10f, shakeDuration: 0.2f, vfxAngle: 0f);
        FinishTurn();
    }
}

/// <summary>Sword_Skill_3 — Đánh ngay + đòn phụ sau delay 0.22s (Trọng Kích — Cắt chéo chữ X).</summary>
public class SwordSkill3 : SkillAction
{
    public override bool IsAsync => true;

    public override void Execute(EnemyStats target, float flatDmg, float effectChance)
    {
        // Nhát 1: Chém cơ bản chéo từ trái qua phải (-35°), không rung màn hình
        DealToEnemy(target, 1f, flatDmg, 0f, true,
                    vfxType: VFXType.SlashNormal, shakeMagnitude: 0f, vfxAngle: -35f);
        float secondMult = (100f - effectChance) / 100f;
        SkillExecutor.Instance.RunCoroutine(DelayedHit(target, secondMult, flatDmg));
    }

    private IEnumerator DelayedHit(EnemyStats target, float mult, float flatDmg)
    {
        try
        {
            yield return new UnityEngine.WaitForSeconds(0.22f);
            if (!CM.CheckBattleEndPublic() && Player.currentHP > 0)
            {
                // Nếu mục tiêu ban đầu đã chết hoặc null, tự động chuyển sang kẻ địch ngẫu nhiên còn sống
                EnemyStats finalTarget = (target != null && target.currentHP > 0)
                    ? target
                    : CM.GetRandomAliveEnemyPublic();

                if (finalTarget != null && finalTarget.currentHP > 0)
                {
                    // Nhát 2: Chém chéo ngược lại (+35°) tạo thành vệt chém hình chữ X
                    DealToEnemy(finalTarget, mult, flatDmg, 0f, true,
                                vfxType: VFXType.SlashNormal, vfxAngle: 35f);
                }
            }
        }
        finally
        {
            FinishTurn();
        }
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

        // [VFX] Aura buff bao quanh PlayerIcon (Icon hiệp sĩ FOAX)
        RectTransform playerUI = UI?.GetPlayerIconTransform() as RectTransform;
        if (CombatVFX.Instance != null && playerUI != null)
            CombatVFX.Instance.PlayVFX(VFXType.AuraBuff, playerUI);

        FinishTurn();
    }
}
