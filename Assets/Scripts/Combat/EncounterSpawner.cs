using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

/// <summary>
/// [Phase 3 Refactor] Chịu trách nhiệm TOÀN BỘ việc sinh quái vật vào màn chiến đấu.
/// - Chọn prefab quái theo seed
/// - Sắp xếp đội hình Boss (Ally-Boss-Ally)
/// - Spawn từng quái vào activeEnemies và tạo EnemyUI tương ứng
///
/// Cách gắn vào Unity:
///   1. Add Component script này vào cùng GameObject chứa CombatManager.
///   2. Kéo các mảng normalEnemyPrefabs, bossPrefabs, enemyUIPrefab, enemyArea vào Inspector.
///      (Các field này đã bị xoá khỏi CombatManager — cần kéo lại vào đây.)
/// </summary>
public class EncounterSpawner : MonoBehaviour
{
    public static EncounterSpawner Instance { get; private set; }

    // ─────────────────────────────────────────────────────────────────────────
    //  INSPECTOR FIELDS
    // ─────────────────────────────────────────────────────────────────────────
    [Header("Prefab Kẻ Thù Thường")]
    public GameObject[] normalEnemyPrefabs;

    [Header("Prefab Boss (theo Floor)")]
    public GameObject[] bossPrefabs;

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
    /// Dựa vào PlayerMovement.isBossFight và floor để quyết định spawn Boss hay quái thường.
    /// Seed ngẫu nhiên đã được thiết lập bởi CombatManager trước khi gọi hàm này.
    /// </summary>
    public void RunEncounter()
    {
        if (PlayerMovement.isBossFight && PlayerMovement.currentFloor == 5 && bossPrefabs.Length > 0)
            SpawnBossEncounter(bossPrefabs[0]);
        else if (PlayerMovement.isBossFight && PlayerMovement.currentFloor == 10 && bossPrefabs.Length > 1)
            SpawnBossEncounter(bossPrefabs[1]);
        else
        {
            int enemyCount = Random.Range(1, 4);
            for (int i = 0; i < enemyCount; i++) SpawnRandomEnemy();
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  SPAWN METHODS
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>Spawn một quái thường ngẫu nhiên từ normalEnemyPrefabs.</summary>
    public void SpawnRandomEnemy()
    {
        if (normalEnemyPrefabs == null || normalEnemyPrefabs.Length == 0) return;
        if (CombatManager.Instance != null && CombatManager.Instance.activeEnemies.Count >= 3) return;
        int randomIndex = Random.Range(0, normalEnemyPrefabs.Length);
        SpawnSpecificEnemy(normalEnemyPrefabs[randomIndex]);
    }

    /// <summary>Spawn theo thứ tự: [Ally Trái] [BOSS Giữa] [Ally Phải] nếu boss có hasSpawnOnAllyDeath.</summary>
    public void SpawnBossEncounter(GameObject bossPrefab)
    {
        if (bossPrefab == null) return;

        EnemyStats tempStats = bossPrefab.GetComponent<EnemyStats>();
        bool hasAllies = tempStats != null && tempStats.hasSpawnOnAllyDeath;

        if (hasAllies)
        {
            GameObject ally = null;
            foreach (var p in normalEnemyPrefabs)
                if (p.name.ToLower().Contains("nhentinhanh")) { ally = p; break; }

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

        GameObject enemyObject = Instantiate(prefab);
        EnemyStats stats = enemyObject.GetComponent<EnemyStats>();
        if (stats == null) { Destroy(enemyObject); return; }

        stats.currentHP       = stats.maxHP;
        stats.currentDefense  = stats.baseDefense;
        stats.currentSpeed    = stats.baseSpeed;
        stats.currentCooldown = stats.initialCooldown;

        combat.activeEnemies.Add(stats);

        if (enemyUIPrefab == null || enemyArea == null)
        {
            combat.activeEnemies.Remove(stats);
            Destroy(enemyObject);
            Debug.LogWarning("[EncounterSpawner] enemyUIPrefab hoặc enemyArea chưa được gán trong Inspector!");
            return;
        }

        GameObject uiObject = Instantiate(enemyUIPrefab, enemyArea);
        EnemyUI uiScript = uiObject.GetComponent<EnemyUI>();
        if (uiScript == null)
        {
            Destroy(uiObject);
            combat.activeEnemies.Remove(stats);
            Destroy(enemyObject);
            return;
        }

        uiScript.Setup(stats);
        UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)enemyArea);

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
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  HELPER — tìm prefab theo tên (dùng bởi CheckBattleEnd / Sinh Đàn)
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>Tìm prefab trong normalEnemyPrefabs có tên chứa chuỗi keyword (không phân biệt hoa/thường).</summary>
    public GameObject FindNormalPrefabByName(string keyword)
    {
        if (normalEnemyPrefabs == null) return null;
        foreach (var p in normalEnemyPrefabs)
            if (p != null && p.name.ToLower().Contains(keyword.ToLower())) return p;
        return null;
    }
}
