using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

/// <summary>
/// Chịu trách nhiệm TOÀN BỘ việc sinh quái vật vào màn chiến đấu.
/// - Chọn quái theo BossFloorConfig linh hoạt
/// - Xử lý Quái Tinh Anh (Elite Encounter: luôn 3 quái, có 1-3 quái tinh anh)
/// - Phân phối quái nâng cao ở Tầng 11+
/// - Spawn từng quái vào activeEnemies và tạo EnemyUI tương ứng
/// </summary>
public class EncounterSpawner : MonoBehaviour
{
    public static EncounterSpawner Instance { get; private set; }

    // ─────────────────────────────────────────────────────────────────────────
    //  INSPECTOR FIELDS
    // ─────────────────────────────────────────────────────────────────────────
    [Header("Prefab Kẻ Thù Thường")]
    public GameObject[] normalEnemyPrefabs;

    [Header("Prefab Kẻ Thù Nâng Cao (Tầng 11+)")]
    [Tooltip("Quái xuất hiện ở tầng 11+ với tỷ lệ cao hơn quái thường")]
    public GameObject[] advancedEnemyPrefabs;

    [Header("Prefab Quái Tinh Anh (Elite Enemies)")]
    [Tooltip("Quái xuất hiện trong các trận Quái Tinh Anh (Elite Encounter)")]
    public GameObject[] eliteEnemyPrefabs;

    [Header("Boss Configs Linh Hoạt (Tầng 10, 20...)")]
    [Tooltip("Danh sách cấu hình Boss theo tầng. Cấu hình đội hình 1-3 quái.")]
    public List<BossFloorConfig> bossFloorConfigs = new List<BossFloorConfig>();

    [Header("EnemyUI Prefab & Vùng Hiển Thị")]
    public GameObject enemyUIPrefab;
    public Transform  enemyArea;

    // ─────────────────────────────────────────────────────────────────────────
    //  LIFECYCLE
    // ─────────────────────────────────────────────────────────────────────────
    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(this); return; }
        Instance = this;
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  PUBLIC API — được gọi từ CombatManager.Start()
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Quyết định spawn Boss, Quái Tinh Anh (Elite), hay Quái Thường dựa theo trạng thái PlayerMovement.
    /// Seed ngẫu nhiên đã được thiết lập bởi CombatManager trước khi gọi hàm này.
    /// </summary>
    public void RunEncounter()
    {
        // 1. TRẬN BOSS
        if (PlayerMovement.isBossFight)
        {
            SpawnConfiguredBossEncounter();
            return;
        }

        // 2. TRẬN QUÁI TINH ANH (ELITE ENCOUNTER)
        if (PlayerMovement.isEliteFight)
        {
            SpawnEliteEncounter();
            return;
        }

        // 3. TRẬN QUÁI THƯỜNG
        int enemyCount = Random.Range(1, 4);
        for (int i = 0; i < enemyCount; i++)
        {
            SpawnRandomEnemyForFloor(PlayerMovement.currentFloor);
        }
    }

    /// <summary>
    /// Spawn quái theo danh sách override (dùng cho TestDungeon / TestCombat).
    /// </summary>
    public void RunOverrideEncounter(GameObject[] overridePrefabs)
    {
        if (overridePrefabs == null) { RunEncounter(); return; }
        
        foreach (var prefab in overridePrefabs)
        {
            if (prefab != null)
                SpawnSpecificEnemy(prefab);
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  SPAWN METHODS CHI TIẾT
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>Lấy cấu hình Boss theo tầng cụ thể.</summary>
    public BossFloorConfig GetBossFloorConfig(int floor)
    {
        if (bossFloorConfigs != null && bossFloorConfigs.Count > 0)
            return bossFloorConfigs.Find(b => b != null && b.floorNumber == floor);
        return null;
    }

    /// <summary>
    /// Xử lý spawn trận Boss theo cấu hình BossFloorConfig.
    /// Bố cục: [Quái Hỗ Trợ 1 - Trái] -> [Boss Chính - Giữa] -> [Quái Hỗ Trợ 2 - Phải]
    /// </summary>
    private void SpawnConfiguredBossEncounter()
    {
        int floor = PlayerMovement.currentFloor;
        BossFloorConfig config = GetBossFloorConfig(floor);

        if (config != null && config.bossPrefab != null)
        {
            GameObject allyLeft  = (config.supportEnemyPrefabs != null && config.supportEnemyPrefabs.Length > 0) ? config.supportEnemyPrefabs[0] : null;
            GameObject allyRight = (config.supportEnemyPrefabs != null && config.supportEnemyPrefabs.Length > 1) ? config.supportEnemyPrefabs[1] : null;

            // Bố cục quái xuất hiện: Trái -> Giữa -> Phải
            if (allyLeft != null)  SpawnSpecificEnemy(allyLeft);
            SpawnSpecificEnemy(config.bossPrefab);
            if (allyRight != null) SpawnSpecificEnemy(allyRight);

            return;
        }

        Debug.LogWarning("[EncounterSpawner] Chưa cấu hình Boss Prefab cho Tầng " + floor + " trong Boss Floor Configs! Spawn quái thường thay thế.");
        SpawnRandomEnemyForFloor(floor);
    }

    /// <summary>
    /// Xử lý spawn trận Quái Tinh Anh (Elite Encounter):
    /// - Luôn luôn xuất hiện 3 con quái.
    /// - Tỷ lệ 1-3 con chắc chắn là quái tinh anh (từ eliteEnemyPrefabs).
    /// - Các ô còn lại lấp đầy bằng quái thường/nâng cao.
    /// </summary>
    private void SpawnEliteEncounter()
    {
        // Đổ xúc xắc số lượng quái tinh anh: 50% ra 1 con, 35% ra 2 con, 15% ra 3 con
        float roll = Random.Range(0f, 100f);
        int eliteCount = 1;
        if (roll >= 85f) eliteCount = 3;
        else if (roll >= 50f) eliteCount = 2;

        for (int i = 0; i < 3; i++)
        {
            if (i < eliteCount && eliteEnemyPrefabs != null && eliteEnemyPrefabs.Length > 0)
            {
                // Spawn Quái Tinh Anh
                int r = Random.Range(0, eliteEnemyPrefabs.Length);
                if (eliteEnemyPrefabs[r] != null)
                {
                    SpawnSpecificEnemy(eliteEnemyPrefabs[r]);
                    continue;
                }
            }

            // Lấp đầy ô trống bằng Quái thường/nâng cao
            SpawnRandomEnemyForFloor(PlayerMovement.currentFloor);
        }
    }

    /// <summary>
    /// Spawn 1 quái ngẫu nhiên phù hợp với độ khó của Tầng.
    /// Tầng 11+: Có 70% tỷ lệ lấy từ advancedEnemyPrefabs, 30% từ normalEnemyPrefabs.
    /// </summary>
    public void SpawnRandomEnemyForFloor(int floor)
    {
        if (CombatManager.Instance != null && CombatManager.Instance.activeEnemies.Count >= 3) return;

        bool useAdvanced = (floor >= 11) && 
                           (advancedEnemyPrefabs != null && advancedEnemyPrefabs.Length > 0) &&
                           (Random.Range(0f, 100f) < 70f);

        GameObject[] pool = (useAdvanced) ? advancedEnemyPrefabs : normalEnemyPrefabs;

        if (pool == null || pool.Length == 0)
        {
            // Fallback nếu 1 trong 2 pool rỗng
            pool = (normalEnemyPrefabs != null && normalEnemyPrefabs.Length > 0) ? normalEnemyPrefabs : advancedEnemyPrefabs;
        }

        if (pool != null && pool.Length > 0)
        {
            int randomIndex = Random.Range(0, pool.Length);
            if (pool[randomIndex] != null)
                SpawnSpecificEnemy(pool[randomIndex]);
        }
    }

    /// <summary>Spawn một quái thường ngẫu nhiên từ normalEnemyPrefabs.</summary>
    public void SpawnRandomEnemy()
    {
        SpawnRandomEnemyForFloor(PlayerMovement.currentFloor);
    }

    /// <summary>Spawn theo thứ tự: [Ally Trái] [BOSS Giữa] [Ally Phải] nếu boss có hasSpawnOnAllyDeath.</summary>
    public void SpawnBossEncounter(GameObject bossPrefab)
    {
        if (bossPrefab == null) return;

        EnemyStats tempStats = bossPrefab.GetComponent<EnemyStats>();
        bool hasAllies = tempStats != null && tempStats.hasSpawnOnAllyDeath;

        if (hasAllies)
        {
            GameObject ally = FindNormalPrefabByName("nhentinhanh");
            if (ally == null && normalEnemyPrefabs != null && normalEnemyPrefabs.Length > 0)
                ally = normalEnemyPrefabs[0];

            if (ally != null)
            {
                SpawnSpecificEnemy(ally);       // Slot 0 — trái
                SpawnSpecificEnemy(bossPrefab); // Slot 1 — GIỮA
                SpawnSpecificEnemy(ally);       // Slot 2 — phải
            }
            else SpawnSpecificEnemy(bossPrefab);
        }
        else SpawnSpecificEnemy(bossPrefab);
    }

    /// <summary>
    /// Khởi tạo dữ liệu EnemyStats và tạo EnemyUI GameObject, trả về GameObject UI vừa tạo.
    /// </summary>
    public GameObject CreateEnemyAndUI(GameObject prefab)
    {
        var combat = CombatManager.Instance;
        if (combat == null || prefab == null || enemyUIPrefab == null || enemyArea == null) return null;

        GameObject enemyObject = Instantiate(prefab);
        EnemyStats stats = enemyObject.GetComponent<EnemyStats>();
        if (stats == null) { Destroy(enemyObject); return null; }

        stats.currentHP       = stats.maxHP;
        stats.currentDefense  = stats.baseDefense;
        stats.currentSpeed    = stats.baseSpeed;
        stats.currentCooldown = stats.maxCooldown;

        GameObject uiObject = Instantiate(enemyUIPrefab, enemyArea);
        EnemyUI uiScript = uiObject.GetComponent<EnemyUI>();
        if (uiScript == null)
        {
            Destroy(uiObject);
            Destroy(enemyObject);
            return null;
        }

        uiScript.Setup(stats);

        // Click → OnEnemyClicked
        Button uiButton = uiObject.GetComponent<Button>();
        if (uiButton != null) uiButton.onClick.AddListener(() => combat.OnEnemyClicked(stats));

        // Hover → di chuyển targetArrow khi đang nhắm mục tiêu
        EventTrigger trigger = uiObject.GetComponent<EventTrigger>();
        if (trigger == null) trigger = uiObject.AddComponent<EventTrigger>();
        var hoverEntry = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
        hoverEntry.callback.AddListener((_) =>
        {
            if (!combat.IsTargetingMode) return;
            if (stats.currentHP <= 0) return;
            combat.SetCurrentTarget(stats);
            combat.UI?.MoveArrowToTarget(stats);
        });
        trigger.triggers.Add(hoverEntry);

        enemyObject.SetActive(false);
        return uiObject;
    }

    /// <summary>
    /// Spawn một prefab cụ thể: khởi tạo EnemyStats, đăng ký vào CombatManager.activeEnemies,
    /// tạo EnemyUI và gắn click/hover event.
    /// </summary>
    public void SpawnSpecificEnemy(GameObject prefab)
    {
        var combat = CombatManager.Instance;
        if (combat == null) return;

        // Giới hạn tối đa 3 quái sống
        int aliveCount = 0;
        foreach (var e in combat.activeEnemies) if (e.currentHP > 0) aliveCount++;
        if (aliveCount >= 3) return;

        GameObject uiObject = CreateEnemyAndUI(prefab);
        if (uiObject != null)
        {
            EnemyUI uiScript = uiObject.GetComponent<EnemyUI>();
            if (uiScript != null && uiScript.stats != null)
            {
                combat.activeEnemies.Add(uiScript.stats);
            }
            LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)enemyArea);
        }
    }

    /// <summary>
    /// Xử lý triệu hồi Sinh Đàn cho Nhện Nữ Vương (hoặc Boss tương đương):
    /// - Dọn dẹp các GameObject UI quái đệ đã chết trong enemyArea.
    /// - Đặt Boss ở vị trí GIỮA (Sibling Index 1).
    /// - Tạo 2 quái con mới lấp đầy 2 bên: Quái 1 ở TRÁI (Sibling Index 0), Quái 2 ở PHẢI (Sibling Index 2).
    /// - Đảm bảo thứ tự hiển thị [Quái Trái] - [Boss Giữa] - [Quái Phải] không bao giờ bị xô lệch vị trí.
    /// </summary>
    public void SpawnBossSinhDanMinions(GameObject summonPrefab)
    {
        var combat = CombatManager.Instance;
        if (combat == null || summonPrefab == null || enemyArea == null) return;

        // 1. Tìm Boss còn sống và EnemyUI của Boss
        EnemyStats bossStats = null;
        EnemyUI bossUI = null;

        foreach (var ui in enemyArea.GetComponentsInChildren<EnemyUI>(true))
        {
            if (ui != null && ui.stats != null && ui.stats.currentHP > 0 && ui.stats.hasSpawnOnAllyDeath)
            {
                bossStats = ui.stats;
                bossUI = ui;
                break;
            }
        }

        // Fallback nếu không có cờ hasSpawnOnAllyDeath
        if (bossStats == null)
        {
            foreach (var ui in enemyArea.GetComponentsInChildren<EnemyUI>(true))
            {
                if (ui != null && ui.stats != null && ui.stats.currentHP > 0)
                {
                    bossStats = ui.stats;
                    bossUI = ui;
                    break;
                }
            }
        }

        // 2. Tiêu hủy các GameObject EnemyUI đã chết / ẩn trong enemyArea để giải phóng layout slot
        foreach (var ui in enemyArea.GetComponentsInChildren<EnemyUI>(true))
        {
            if (ui != null && ui != bossUI && (ui.stats == null || ui.stats.currentHP <= 0 || !ui.gameObject.activeSelf))
            {
                Destroy(ui.gameObject);
            }
        }

        // 3. Spawn Quái con 1 (Bên Trái - Sibling Index 0)
        GameObject leftMinionUI = CreateEnemyAndUI(summonPrefab);
        EnemyStats leftStats = null;
        if (leftMinionUI != null)
        {
            leftMinionUI.transform.SetSiblingIndex(0);
            EnemyUI ui = leftMinionUI.GetComponent<EnemyUI>();
            if (ui != null) leftStats = ui.stats;
        }

        // 4. Cố định Boss ở vị trí Giữa (Sibling Index 1)
        if (bossUI != null)
        {
            bossUI.transform.SetSiblingIndex(1);
        }

        // 5. Spawn Quái con 2 (Bên Phải - Sibling Index 2)
        GameObject rightMinionUI = CreateEnemyAndUI(summonPrefab);
        EnemyStats rightStats = null;
        if (rightMinionUI != null)
        {
            rightMinionUI.transform.SetSiblingIndex(2);
            EnemyUI ui = rightMinionUI.GetComponent<EnemyUI>();
            if (ui != null) rightStats = ui.stats;
        }

        // 6. Cập nhật lại danh sách activeEnemies theo thứ tự trực quan [Trái, Giữa, Phải]
        List<EnemyStats> deadEnemies = combat.activeEnemies.FindAll(e => e != null && e.currentHP <= 0);

        combat.activeEnemies.Clear();
        if (leftStats != null) combat.activeEnemies.Add(leftStats);
        if (bossStats != null) combat.activeEnemies.Add(bossStats);
        if (rightStats != null) combat.activeEnemies.Add(rightStats);

        // Giữ lại các quái đã chết để tính đầy đủ EXP & Gold khi kết thúc trận
        foreach (var d in deadEnemies)
        {
            if (!combat.activeEnemies.Contains(d))
                combat.activeEnemies.Add(d);
        }

        // 7. Force rebuild layout cho enemyArea
        LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)enemyArea);

        // 8. Cập nhật target
        if (combat.CurrentTarget == null || combat.CurrentTarget.currentHP <= 0)
        {
            combat.SetCurrentTarget(bossStats);
            combat.UI?.MoveArrowToTarget(bossStats);
        }
        else
        {
            combat.UI?.MoveArrowToTarget(combat.CurrentTarget);
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  HELPER — tìm prefab theo tên (dùng bởi CheckBattleEnd / Sinh Đàn)
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>Tìm prefab trong tất cả các pool có tên chứa chuỗi keyword (không phân biệt hoa/thường).</summary>
    public GameObject FindNormalPrefabByName(string keyword)
    {
        if (string.IsNullOrEmpty(keyword)) return null;
        string kw = keyword.ToLower();

        // 1. Tìm trong normalEnemyPrefabs
        if (normalEnemyPrefabs != null)
        {
            foreach (var p in normalEnemyPrefabs)
                if (p != null && p.name.ToLower().Contains(kw)) return p;
        }

        // 2. Tìm trong advancedEnemyPrefabs
        if (advancedEnemyPrefabs != null)
        {
            foreach (var p in advancedEnemyPrefabs)
                if (p != null && p.name.ToLower().Contains(kw)) return p;
        }

        // 3. Tìm trong eliteEnemyPrefabs
        if (eliteEnemyPrefabs != null)
        {
            foreach (var p in eliteEnemyPrefabs)
                if (p != null && p.name.ToLower().Contains(kw)) return p;
        }

        // 4. Tìm trong bossFloorConfigs
        if (bossFloorConfigs != null)
        {
            foreach (var cfg in bossFloorConfigs)
            {
                if (cfg != null)
                {
                    if (cfg.bossPrefab != null && cfg.bossPrefab.name.ToLower().Contains(kw)) return cfg.bossPrefab;
                    if (cfg.supportEnemyPrefabs != null)
                    {
                        foreach (var p in cfg.supportEnemyPrefabs)
                            if (p != null && p.name.ToLower().Contains(kw)) return p;
                    }
                    if (cfg.summonOnAlliesDeadPrefab != null && cfg.summonOnAlliesDeadPrefab.name.ToLower().Contains(kw))
                        return cfg.summonOnAlliesDeadPrefab;
                }
            }
        }

        return null;
    }
}

