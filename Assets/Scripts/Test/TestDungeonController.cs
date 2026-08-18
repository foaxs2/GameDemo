using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Tilemaps;
using TMPro;

public class TestDungeonController : MonoBehaviour
{
    [Header("UI References")]
    public GameObject testPanel;
    public TMP_InputField floorInput;
    public Button btnGenerateFloor;
    public Toggle autoWinToggle;
    public Button btnResetFullStats;
    
    [Header("Override Quái Dropdowns")]
    public TMP_Dropdown slot1Dropdown;
    public TMP_Dropdown slot2Dropdown;
    public TMP_Dropdown slot3Dropdown;

    [Header("Override Sự Kiện Dropdowns")]
    public TMP_Dropdown event1Dropdown;
    public TMP_Dropdown event2Dropdown;
    public Button btnConfirmEvents;

    [Header("Override Quest Dropdown (Nhận Quest Trực Tiếp Trong Test)")]
    public TMP_Dropdown questDropdown;
    public Button btnAcceptQuest;
    public Button btnClearQuests;
    private List<QuestConfig> availableQuests = new List<QuestConfig>();

    [Header("Danh Sách Quái (Kéo Spawner HOẶC kéo trực tiếp Prefabs vào)")]
    public EncounterSpawner spawnerReference;
    public GameObject[] normalEnemyPrefabs;
    public GameObject[] advancedEnemyPrefabs;
    public GameObject[] eliteEnemyPrefabs;
    public GameObject[] bossPrefabs;

    [Header("Danh Sách Sự Kiện")]
    public List<EventMapping> customEventMappings = new List<EventMapping>();

    private List<GameObject> availableEnemies = new List<GameObject>();
    private List<EventMapping> availableEvents = new List<EventMapping>();

    private EventMapping chosenEvent1 = null;
    private EventMapping chosenEvent2 = null;

    private CanvasGroup canvasGroup;
    private bool isPanelVisible = true;

    void Awake()
    {
        CombatManager.returnSceneName = "TestDungeon";
    }

    void Start()
    {
        InitCanvasGroup();
        PopulateEnemyDropdowns();
        PopulateEventDropdowns();
        PopulateQuestDropdown();
        if (floorInput != null)
        {
            floorInput.text = PlayerMovement.currentFloor.ToString();
        }

        if (autoWinToggle != null)
        {
            autoWinToggle.SetIsOnWithoutNotify(CombatManager.autoWinCombat);
            autoWinToggle.onValueChanged.AddListener(_ => UpdateAutoWinToggle());
        }

        if (slot1Dropdown != null) slot1Dropdown.onValueChanged.AddListener(_ => UpdateOverrideArray());
        if (slot2Dropdown != null) slot2Dropdown.onValueChanged.AddListener(_ => UpdateOverrideArray());
        if (slot3Dropdown != null) slot3Dropdown.onValueChanged.AddListener(_ => UpdateOverrideArray());
        if (btnGenerateFloor != null) btnGenerateFloor.onClick.AddListener(GenerateDungeon);
        if (btnConfirmEvents != null) btnConfirmEvents.onClick.AddListener(ConfirmEventSelection);
        if (btnAcceptQuest != null) btnAcceptQuest.onClick.AddListener(AcceptSelectedQuest);
        if (btnClearQuests != null) btnClearQuests.onClick.AddListener(ClearAllQuests);
        if (btnResetFullStats != null) btnResetFullStats.onClick.AddListener(ResetPlayerFullStats);
    }

    private void InitCanvasGroup()
    {
        GameObject target = testPanel != null ? testPanel : gameObject;
        canvasGroup = target.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = target.AddComponent<CanvasGroup>();
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            ToggleTestPanel();
        }
    }

    public void ToggleTestPanel()
    {
        isPanelVisible = !isPanelVisible;
        if (canvasGroup != null)
        {
            canvasGroup.alpha = isPanelVisible ? 1f : 0f;
            canvasGroup.blocksRaycasts = isPanelVisible;
            canvasGroup.interactable = isPanelVisible;
        }
        else if (testPanel != null && testPanel != gameObject)
        {
            testPanel.SetActive(isPanelVisible);
        }
    }

    private void PopulateEnemyDropdowns()
    {
        List<GameObject> allPools = new List<GameObject>();

        void AddFromList(GameObject[] arr)
        {
            if (arr == null) return;
            foreach (var go in arr)
            {
                if (go != null && !allPools.Contains(go))
                    allPools.Add(go);
            }
        }

        if (spawnerReference != null)
        {
            AddFromList(spawnerReference.normalEnemyPrefabs);
            AddFromList(spawnerReference.advancedEnemyPrefabs);
            AddFromList(spawnerReference.eliteEnemyPrefabs);
            if (spawnerReference.bossFloorConfigs != null)
            {
                foreach (var cfg in spawnerReference.bossFloorConfigs)
                {
                    if (cfg != null)
                    {
                        if (cfg.bossPrefab != null && !allPools.Contains(cfg.bossPrefab)) allPools.Add(cfg.bossPrefab);
                        AddFromList(cfg.supportEnemyPrefabs);
                        if (cfg.summonOnAlliesDeadPrefab != null && !allPools.Contains(cfg.summonOnAlliesDeadPrefab)) allPools.Add(cfg.summonOnAlliesDeadPrefab);
                    }
                }
            }
        }

        // Fallback từ các field Inspector của TestDungeonController
        AddFromList(normalEnemyPrefabs);
        AddFromList(advancedEnemyPrefabs);
        AddFromList(eliteEnemyPrefabs);

        List<string> options = new List<string>();
        options.Add("Trống"); // Index 0
        availableEnemies.Clear();

        foreach (var enemy in allPools)
        {
            if (enemy != null)
            {
                options.Add(enemy.name);
                availableEnemies.Add(enemy);
            }
        }

        if (slot1Dropdown != null) { slot1Dropdown.ClearOptions(); slot1Dropdown.AddOptions(options); }
        if (slot2Dropdown != null) { slot2Dropdown.ClearOptions(); slot2Dropdown.AddOptions(options); }
        if (slot3Dropdown != null) { slot3Dropdown.ClearOptions(); slot3Dropdown.AddOptions(options); }
    }

    private void PopulateEventDropdowns()
    {
        availableEvents.Clear();
        List<string> options = new List<string>();
        options.Add("Ngẫu nhiên / Không chọn"); // Index 0

        // Lấy từ PlayerMovement nếu có
        List<EventMapping> sourceList = null;
        if (PlayerMovement.Instance != null && PlayerMovement.Instance.eventMappings != null && PlayerMovement.Instance.eventMappings.Count > 0)
        {
            sourceList = PlayerMovement.Instance.eventMappings;
        }
        else if (customEventMappings != null && customEventMappings.Count > 0)
        {
            sourceList = customEventMappings;
        }

        if (sourceList != null)
        {
            foreach (var mapping in sourceList)
            {
                if (mapping != null && mapping.eventData != null)
                {
                    options.Add(mapping.eventData.eventName);
                    availableEvents.Add(mapping);
                }
            }
        }

        if (event1Dropdown != null) { event1Dropdown.ClearOptions(); event1Dropdown.AddOptions(options); }
        if (event2Dropdown != null) { event2Dropdown.ClearOptions(); event2Dropdown.AddOptions(options); }
    }

    public void ConfirmEventSelection()
    {
        chosenEvent1 = GetEventFromDropdown(event1Dropdown);
        chosenEvent2 = GetEventFromDropdown(event2Dropdown);

        string name1 = chosenEvent1 != null && chosenEvent1.eventData != null ? chosenEvent1.eventData.eventName : "Ngẫu nhiên";
        string name2 = chosenEvent2 != null && chosenEvent2.eventData != null ? chosenEvent2.eventData.eventName : "Ngẫu nhiên";
        Debug.Log($"[TEST] Đã ghi nhận lựa chọn sự kiện: Sự kiện 1: {name1} | Sự kiện 2: {name2}. Sẽ sinh khi bấm 'Tạo Tầng'!");
    }

    private EventMapping GetEventFromDropdown(TMP_Dropdown dropdown)
    {
        if (dropdown == null) return null;
        int index = dropdown.value - 1; // 0 là Ngẫu nhiên
        if (index >= 0 && index < availableEvents.Count)
        {
            return availableEvents[index];
        }
        return null;
    }

    private void PopulateQuestDropdown()
    {
        if (questDropdown == null) return;
        questDropdown.ClearOptions();
        availableQuests.Clear();

        List<string> options = new List<string>();
        options.Add("-- Chọn Quest để Test --");

        if (GuildManager.Instance != null && GuildManager.Instance.predefinedQuests != null)
        {
            foreach (var q in GuildManager.Instance.predefinedQuests)
            {
                if (q == null) continue;
                availableQuests.Add(q);
                string typeStr = q.type == QuestType.Hunt ? "Săn" : "Tìm";
                options.Add($"[{typeStr}] {q.targetName} (x{q.requiredAmount})");
            }
        }

        questDropdown.AddOptions(options);
    }

    public void AcceptSelectedQuest()
    {
        if (questDropdown == null || availableQuests.Count == 0) return;
        int index = questDropdown.value - 1; // 0 là "-- Chọn Quest --"
        if (index >= 0 && index < availableQuests.Count)
        {
            QuestConfig quest = availableQuests[index];
            if (quest != null && GuildManager.Instance != null)
            {
                bool added = GuildManager.Instance.ForceAddAndAcceptQuest(quest.questID);
                if (added)
                {
                    DungeonUIManager.Instance?.UpdateQuestList();
                    SaveSystem.Instance?.Save();
                    Debug.Log($"[TEST] Đã nhận quest '{quest.targetName}' thành công và hiển thị lên bảng nhiệm vụ!");
                }
                else
                {
                    Debug.LogWarning("[TEST] Không thể nhận thêm (Đã đủ 3/3 nhiệm vụ hoặc đã nhận nhiệm vụ này rồi)!");
                }
            }
        }
    }

    public void ClearAllQuests()
    {
        if (GuildManager.Instance != null)
        {
            GuildManager.Instance.ClearAllQuests();
            DungeonUIManager.Instance?.UpdateQuestList();
            SaveSystem.Instance?.Save();
            Debug.Log("[TEST] Đã xóa sạch toàn bộ nhiệm vụ và cập nhật lại giao diện!");
        }
    }

    public void GenerateDungeon()
    {
        // Đảm bảo cập nhật lựa chọn sự kiện nếu chưa bấm xác nhận
        ConfirmEventSelection();

        int floor = 1;
        if (floorInput != null && int.TryParse(floorInput.text, out int parsedFloor))
        {
            floor = parsedFloor;
        }

        PlayerMovement.currentFloor = floor;
        PlayerMovement.currentMapSeed = Random.Range(1000, 9999);
        PlayerMovement.isReturningFromCombat = false;
        
        PlayerMovement.clearedFogTiles.Clear();
        PlayerMovement.defeatedEnemiesTiles.Clear();
        PlayerMovement.activatedEventTiles.Clear();
        PlayerMovement.ResetFloorTracking();
        
        if (DungeonGenerator.Instance != null)
        {
            DungeonGenerator.Instance.GenerateDungeon(PlayerMovement.currentFloor, PlayerMovement.currentMapSeed);
            Debug.Log($"[TEST] Đã tạo map tầng {floor} với seed {PlayerMovement.currentMapSeed}");

            // Đặt sự kiện Override đã chọn lên Map
            ApplyOverrideEventsToMap();

            // Đồng bộ lại tọa độ ô lưới, mở sương mù và giải phóng tương tác cho Player
            PlayerMovement.Instance?.ResetPositionToCurrentCell();
            SaveSystem.Instance?.Save(); // Tự động lưu tiến trình trong TestDungeon
        }
    }

    private void ApplyOverrideEventsToMap()
    {
        var dg = DungeonGenerator.Instance;
        if (dg == null || dg.eventMap == null || dg.groundMap == null) return;

        List<EventMapping> toPlace = new List<EventMapping>();
        if (chosenEvent1 != null && chosenEvent1.eventTile != null) toPlace.Add(chosenEvent1);
        if (chosenEvent2 != null && chosenEvent2.eventTile != null) toPlace.Add(chosenEvent2);

        if (toPlace.Count == 0) return;

        // Tìm các vị trí event hiện có trên eventMap
        List<Vector3Int> eventPositions = new List<Vector3Int>();
        foreach (var pos in dg.eventMap.cellBounds.allPositionsWithin)
        {
            if (dg.eventMap.HasTile(pos))
            {
                TileBase t = dg.eventMap.GetTile(pos);
                // Không ghi đè cửa ra/cửa vào
                if (t != dg.exitTile && t != dg.entryTileGen && t != dg.lockedDoorTileGen && t != dg.keyTileGen)
                {
                    eventPositions.Add(pos);
                }
            }
        }

        // Đặt tile sự kiện đã chọn vào các ô này
        for (int i = 0; i < toPlace.Count; i++)
        {
            if (i < eventPositions.Count)
            {
                Vector3Int p = eventPositions[i];
                dg.eventMap.SetTile(p, toPlace[i].eventTile);
                Debug.Log($"[TEST] Đã đặt sự kiện '{toPlace[i].eventData.eventName}' tại ô tọa độ {p}");
            }
        }
    }

    public void UpdateOverrideArray()
    {
        GameObject slot1Prefab = GetPrefabFromDropdown(slot1Dropdown);
        GameObject slot2Prefab = GetPrefabFromDropdown(slot2Dropdown);
        GameObject slot3Prefab = GetPrefabFromDropdown(slot3Dropdown);

        GameObject[] selected = { slot1Prefab, slot2Prefab, slot3Prefab };
        
        bool allEmpty = true;
        foreach (var go in selected)
        {
            if (go != null) allEmpty = false;
        }

        CombatManager.overrideEnemyPrefabs = allEmpty ? null : selected;
        Debug.Log("[TEST] Đã cập nhật mảng quái override: " + (allEmpty ? "Trống" : "Đã chọn quái"));
    }

    private GameObject GetPrefabFromDropdown(TMP_Dropdown dropdown)
    {
        if (dropdown == null) return null;
        int index = dropdown.value - 1; // -1 vì index 0 là "Trống"
        
        if (index >= 0 && index < availableEnemies.Count)
        {
            return availableEnemies[index];
        }
        return null;
    }

    public void UpdateAutoWinToggle()
    {
        if (autoWinToggle != null)
        {
            CombatManager.autoWinCombat = autoWinToggle.isOn;
            Debug.Log($"[TEST] AutoWin = {autoWinToggle.isOn}");
        }
    }

    public void ResetPlayerFullStats()
    {
        if (PlayerManager.Instance != null)
        {
            PlayerManager.Instance.UpdateMaxHP();
            PlayerManager.Instance.currentHP = PlayerManager.Instance.maxHP;
            PlayerManager.Instance.sen = PlayerManager.Instance.maxSen;
            PlayerManager.Instance.food = PlayerManager.Instance.maxFood;
            PlayerManager.Instance.buffs.Clear();
            PlayerManager.Instance.debuffs.Clear();
            PlayerManager.Instance.RestoreSanityCollapse();
            SaveSystem.Instance?.Save(); // Lưu trạng thái chỉ số
            Debug.Log("[TEST] Đã hồi phục đầy đủ trạng thái nhân vật: Full HP, SEN, Food và đã Lưu Game!");
        }
    }
}
