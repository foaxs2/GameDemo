using System.Collections;
using UnityEngine;

// ═══════════════════════════════════════════════════════════════════════════
//  ENEMY SKILLS — Normal Enemies
// ═══════════════════════════════════════════════════════════════════════════

/// <summary>SummonSpider — Nhện Tinh Anh gọi thêm Nhện Hang (tối đa 3 quái).</summary>
public class ESkill_SummonSpider : EnemySkillAction
{
    public override void Execute(EnemyStats attacker)
    {
        int aliveCount = 0;
        foreach (var e in CM.activeEnemies) if (e.currentHP > 0) aliveCount++;

        if (aliveCount >= 3)
        {
            // Không set CD — sẽ thử lại vòng sau
            CM.InvokeEnemyNormalAttack(attacker);
            return;
        }

        attacker.currentCooldown = attacker.maxCooldown;
        GameObject prefab = CM.Spawner?.FindNormalPrefabByName("nhenhang");
        if (prefab != null) CM.Spawner.SpawnSpecificEnemy(prefab);
        else CM.InvokeEnemyNormalAttack(attacker);
    }
}

/// <summary>Bloodsuck — Cắn hút máu: gây sát thương + 30% Bleed + hồi HP bản thân.</summary>
public class ESkill_Bloodsuck : EnemySkillAction
{
    public override void Execute(EnemyStats attacker)
    {
        attacker.currentCooldown = attacker.maxCooldown;
        DealToPlayer(attacker.attack, shakeMagnitude: 12f, shakeDuration: 0.25f);
        ShowPlayerDmgText(attacker.attack);
        if (UnityEngine.Random.Range(0f, 100f) <= 80f)
            Debuffs?.AddDebuff(Player, DebuffType.Bleed, 1, 1, attacker.attack);
        attacker.currentHP = Mathf.Min(attacker.maxHP, attacker.currentHP + 5);
    }
}

/// <summary>Venom — Tấn công + 30% Poison 3 lượt.</summary>
public class ESkill_Venom : EnemySkillAction
{
    public override void Execute(EnemyStats attacker)
    {
        attacker.currentCooldown = attacker.maxCooldown;
        DealToPlayer(attacker.attack, shakeMagnitude: 12f, shakeDuration: 0.25f);
        ShowPlayerDmgText(attacker.attack);
        if (UnityEngine.Random.Range(0f, 100f) <= 80f)
            Debuffs?.AddDebuff(Player, DebuffType.Poison, 3);
    }
}

/// <summary>PowerBite — Cắn mạnh: ATK + 3 bonus.</summary>
public class ESkill_PowerBite : EnemySkillAction
{
    public override void Execute(EnemyStats attacker)
    {
        attacker.currentCooldown = attacker.maxCooldown;
        DealToPlayer(attacker.attack + 3f, shakeMagnitude: 12f, shakeDuration: 0.25f);
        ShowPlayerDmgText(attacker.attack + 3f);
    }
}

/// <summary>SpeedDash — Tăng tốc bản thân SPD +5 trong 2 lượt.</summary>
public class ESkill_SpeedDash : EnemySkillAction
{
    public override void Execute(EnemyStats attacker)
    {
        attacker.currentCooldown = attacker.maxCooldown;
        Buffs?.AddBuff(attacker, BuffType.SPD_Up, 5f, 2, 1);

        // [VFX] Aura buff tại vị trí quái
        Transform attackerTf = UI?.GetEnemyUITransform(attacker);
        RectTransform attackerUI = attackerTf as RectTransform;
        if (CombatVFX.Instance != null && attackerUI != null)
            CombatVFX.Instance.PlayVFX(VFXType.AuraBuff, attackerUI);
    }
}

/// <summary>StickyWeb — Tăng ATK +2 cho bản thân 3 lượt + 30% Fracture player.</summary>
public class ESkill_StickyWeb : EnemySkillAction
{
    public override void Execute(EnemyStats attacker)
    {
        attacker.currentCooldown = attacker.maxCooldown;
        Buffs?.AddBuff(attacker, BuffType.ATK_Up, 2f, 3, 1);
        if (UnityEngine.Random.Range(0f, 100f) <= 80f)
            Debuffs?.AddDebuff(Player, DebuffType.Fracture, 2);

        // [VFX] Aura buff tại vị trí quái
        Transform attackerTf = UI?.GetEnemyUITransform(attacker);
        RectTransform attackerUI = attackerTf as RectTransform;
        if (CombatVFX.Instance != null && attackerUI != null)
            CombatVFX.Instance.PlayVFX(VFXType.AuraBuff, attackerUI);
    }
}

/// <summary>
/// HealEnemy — Cốt Y: Hồi máu đồng minh có HP thấp nhất trên sân.
/// Nếu không còn đồng minh nào sống, hồi cho chính mình.
/// Lượng hồi lấy từ attacker.healAmount (cài trong Inspector của prefab).
/// Số xanh lá hiển thị tại vị trí quái được hồi.
/// </summary>
public class ESkill_HealEnemy : EnemySkillAction
{
    public override void Execute(EnemyStats attacker)
    {
        attacker.currentCooldown = attacker.maxCooldown;

        // Thu thập danh sách đồng minh đang còn sống (không tính bản thân)
        System.Collections.Generic.List<EnemyStats> allies =
            new System.Collections.Generic.List<EnemyStats>();

        foreach (var e in CM.activeEnemies)
        {
            if (e != attacker && e.currentHP > 0)
                allies.Add(e);
        }

        EnemyStats healTarget;

        if (allies.Count > 0)
        {
            // Chọn đồng minh có HP thấp nhất
            healTarget = allies[0];
            foreach (var ally in allies)
            {
                if (ally.currentHP < healTarget.currentHP)
                    healTarget = ally;
            }
        }
        else
        {
            // Không có đồng minh → hồi cho chính mình
            healTarget = attacker;
        }

        HealUnit(healTarget, attacker.healAmount);
        CM.UpdatePlayerUIPublic(); // Cập nhật UI HP bar quái
    }
}

// ═══════════════════════════════════════════════════════════════════════════
//  ENEMY SKILLS — Boss
// ═══════════════════════════════════════════════════════════════════════════

/// <summary>NhenNuVuong_ClawRip — Vuốt xé: có thể crit, áp passive poison.</summary>
public class ESkill_ClawRip : EnemySkillAction
{
    public override void Execute(EnemyStats attacker)
    {
        attacker.currentCooldown = attacker.maxCooldown;
        var result = CombatCalculator.CalculateClawRip(Player);
        if (result.missed) { UI?.ShowMissText(Player); }
        else
        {
            DealToPlayer(result.rawDamage, shakeMagnitude: 12f, shakeDuration: 0.25f);
            Color col = result.isCrit ? Color.yellow : new Color(1f, 0.4f, 0f);
            FTM?.SpawnText(UI?.GetPlayerHPBarTransform()?.position ?? Vector3.zero,
                           result.displayDamage.ToString(), col);
        }
        BossPassiveManager.Instance?.ApplyPassiveEffects(attacker);
    }
}

/// <summary>Dragon_FlameBreath — Phun Lửa: 3 đòn x 6 DMG, 40% Burn/đòn.</summary>
public class ESkill_FlameBreath : EnemySkillAction
{
    public override bool IsAsync => true;

    public override void Execute(EnemyStats attacker)
    {
        attacker.currentCooldown = attacker.maxCooldown;
        SkillExecutor.Instance.RunCoroutine(FlameBreath(attacker));
    }

    private IEnumerator FlameBreath(EnemyStats attacker)
    {
        // [VFX] Phun lửa diện rộng + rung mạnh — 1 lần duy nhất ở đầu
        RectTransform playerUI = UI?.GetPlayerIconTransform() as RectTransform;
        if (CombatVFX.Instance != null && playerUI != null)
            CombatVFX.Instance.PlayVFX(VFXType.FlameBreath, playerUI);
        if (UIShake.Instance != null)
            UIShake.Instance.Shake(0.4f, 18f);

        for (int i = 0; i < 3; i++)
        {
            if (CM.CheckBattleEndPublic() || Player.currentHP <= 0) yield break;
            Player.TakeDamage(6f, true);
            UI?.FlashPlayerHit(0.2f);
            FTM?.SpawnText(UI?.GetPlayerHPBarTransform()?.position ?? Vector3.zero,
                           "6", new Color(1f, 0.3f, 0f));
            if (UnityEngine.Random.Range(0f, 100f) <= 80f)
                Debuffs?.AddDebuff(Player, DebuffType.Burn, 3);
            yield return new UnityEngine.WaitForSeconds(0.25f);
        }
        CM.UpdatePlayerUIPublic();
        CM.CheckPlayerDeathPublic();
    }
}
