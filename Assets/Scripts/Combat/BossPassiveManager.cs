using UnityEngine;

/// <summary>
/// Singleton quản lý toàn bộ Boss Innate Passives.
/// Tên method theo đúng Boss.md spec.
/// </summary>
public class BossPassiveManager : MonoBehaviour
{
    public static BossPassiveManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // CheckAllBossPassives(boss)
    // Kiểm tra Innate 2 (Hỏa Điền) + Innate Rồng (Tiếng Rồng Suy Nhược)
    // Gọi ở đầu mỗi lượt boss TRƯỚC khi hành động
    // ─────────────────────────────────────────────────────────────────────────
    public void CheckAllBossPassives(EnemyStats boss)
    {
        if (boss == null) return;

        // Innate 2: HỎA ĐIỀN — chỉ trigger 1 lần khi HP < threshold
        if (boss.hasEnrageOnLowHP && !boss.isEnraged)
        {
            float hpPercent = (float)boss.currentHP / boss.maxHP;
            if (hpPercent <= boss.enrageHPThreshold)
            {
                boss.isEnraged  = true;
                boss.attack     += boss.enrageATKBonus;
                boss.baseSpeed  += boss.enrageSPDBonus;
                boss.currentSpeed = boss.baseSpeed;

                Debug.Log($"[BossPassive] {boss.enemyName} HỎA ĐIỀN! ATK+{boss.enrageATKBonus} SPD+{boss.enrageSPDBonus}");

                if (FloatingTextManager.Instance != null)
                {
                    foreach (var ui in Object.FindObjectsByType<EnemyUI>(FindObjectsSortMode.None))
                        if (ui.stats == boss) { FloatingTextManager.Instance.SpawnText(ui.icon.transform.position, "⚡ HỎA ĐIỀN!", new Color(1f, 0.4f, 0f)); break; }
                }
            }
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // IncrementBossTurnCounter(boss)
    // Tăng counter Rồng và check Tiếng Rồng Suy Nhược (mỗi 5 lượt)
    // Gọi khi boss THỰC SỰ hành động (không bị Stun)
    // ─────────────────────────────────────────────────────────────────────────
    public void IncrementBossTurnCounter(EnemyStats boss)
    {
        if (boss == null || !boss.hasRoarPassive) return;

        boss.roarTurnCounter++;
        if (boss.roarTurnCounter >= boss.roarEveryNTurns)
        {
            boss.roarTurnCounter = 0;

            // 80% Stun player 1 lượt
            if (Random.Range(0f, 100f) <= 80f)
            {
                DebuffManager.Instance?.AddDebuff(PlayerManager.Instance, DebuffType.Stun, 1);
                Debug.Log("[BossPassive] TIẾNG RỒNG SUY NHƯỢC! Player bị Stun 1 lượt.");

                if (FloatingTextManager.Instance != null && CombatManager.Instance?.UI != null)
                {
                    Vector3 pPos = CombatManager.Instance.UI.GetPlayerHPBarTransform()?.position ?? Vector3.zero;
                    FloatingTextManager.Instance.SpawnText(pPos, "💀 TIẾNG RỒNG! Stun!", Color.magenta);
                }
            }
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // ApplyPassiveEffects(boss)
    // Passive Poison sau đòn đánh (Nhện Nữ Vương — Nọc Độc Thụ Động 20%)
    // ─────────────────────────────────────────────────────────────────────────
    public void ApplyPassiveEffects(EnemyStats boss)
    {
        if (boss == null) return;

        // Innate 3: NỌC ĐỘC THỤ ĐỘNG
        if (boss.hasPassivePoison)
        {
            if (Random.Range(0f, 100f) <= boss.passivePoisonChance)
            {
                DebuffManager.Instance?.AddDebuff(PlayerManager.Instance, DebuffType.Poison, boss.passivePoisonDuration);
                Debug.Log($"[BossPassive] NỌC ĐỘC THỤ ĐỘNG! Poison {boss.passivePoisonDuration} lượt.");
            }
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // OnPlayerCritHit(target)
    // Vảy Rắn Nứt — khi player crit VÀO Rồng: áp Fracture 2 lượt (stack) + Poison x4
    // ─────────────────────────────────────────────────────────────────────────
    public void OnPlayerCritHit(EnemyStats target)
    {
        if (target == null || !target.hasCrackScales) return;

        DebuffManager.Instance?.AddDebuff(target, DebuffType.Fracture, 2);
        target.poisonDamageMultiplier = 4f;

        Debug.Log($"[BossPassive] VẢY RẮN NỨT! Fracture 2 lượt + Poison x4 trên {target.enemyName}");

        if (FloatingTextManager.Instance != null)
        {
            foreach (var ui in Object.FindObjectsByType<EnemyUI>(FindObjectsSortMode.None))
                if (ui.stats == target) { FloatingTextManager.Instance.SpawnText(ui.icon.transform.position, "🐍 VẢY RẮN NỨT!", Color.yellow); break; }
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // CheckSpawnOnAllyDeath()
    // Sinh Đàn: chỉ trigger khi chỉ còn boss sống (aliveCount == 1), 1 lần duy nhất
    // ─────────────────────────────────────────────────────────────────────────
    public bool CheckSpawnOnAllyDeath(CombatManager combat)
    {
        if (combat == null) return false;

        EnemyStats boss = null;
        int aliveCount  = 0;

        foreach (var e in combat.activeEnemies)
        {
            if (e.currentHP <= 0) continue;
            aliveCount++;
            if (e.hasSpawnOnAllyDeath) boss = e;
        }

        if (boss != null && aliveCount == 1 && !boss.spawnedOnAllyDeath)
        {
            boss.spawnedOnAllyDeath = true;
            Debug.Log("[BossPassive] SINH ĐÀN! Spawn 2 Nhện Hang.");
            return true;
        }
        return false;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // OnBurnOrFireHitBoss(target)
    // Innate 4 (Nhện Nữ Vương) — Yếu với Lửa:
    //   Khi bị Burn HOẶC bị tấn công bằng lửa → DEF -20% trong 2 lượt
    //   (Dùng Fracture để giảm DEF; Burn tự nhiên làm DEF giảm thêm theo hệ thống)
    // ─────────────────────────────────────────────────────────────────────────
    public void OnFireHitBoss(EnemyStats target)
    {
        if (target == null || !target.hasWeakToFire) return;

        // Áp Fracture để biểu diễn DEF -20% trong 2 lượt
        DebuffManager.Instance?.AddDebuff(target, DebuffType.Fracture, 2);

        Debug.Log($"[BossPassive] YẾU VỚI LỬA! {target.enemyName} DEF -20% trong 2 lượt.");

        if (FloatingTextManager.Instance != null)
        {
            foreach (var ui in Object.FindObjectsByType<EnemyUI>(FindObjectsSortMode.None))
                if (ui.stats == target) { FloatingTextManager.Instance.SpawnText(ui.icon.transform.position, "🔥 Yếu với Lửa!", Color.red); break; }
        }
    }
}
