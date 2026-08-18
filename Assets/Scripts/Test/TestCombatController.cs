using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class TestCombatController : MonoBehaviour
{
    [Header("UI References")]
    public GameObject testPanel;
    public TMP_Dropdown enemyDropdown;
    public TMP_Dropdown itemDropdown;
    public Button btnSpawn;
    public Button btnClear;
    public Button btnAddItem;
    public Button btnApplyStats;
    public Button btnResetFullStats;
    
    [Header("Skill Management")]
    public TMP_Dropdown skillDropdown;
    public Button btnAddOrUpgradeSkill;
    public Button btnRemoveSkill;
    public SkillData[] allSkills;

    [Header("Stat Inputs")]
    public TMP_InputField hpInput;
    public TMP_InputField atkInput;
    public TMP_InputField defInput;
    public TMP_InputField spdInput;
    public TMP_InputField senInput;

    [Header("References")]
    public ItemData[] allItems;

    private List<GameObject> availableEnemies = new List<GameObject>();
    private List<SkillData> availableSkills = new List<SkillData>();
    private CanvasGroup canvasGroup;
    private bool isPanelVisible = true;

    void Awake()
    {
        CombatManager.isTestMode = true;
        // Nếu không phải chuyển qua từ TestDungeon thì mặc định khi kết thúc quay lại TestCombat
        if (CombatManager.returnSceneName != "TestDungeon")
        {
            CombatManager.returnSceneName = "TestCombat";
            PlayerMovement.currentFloor = 1;
            PlayerMovement.currentMapSeed = 42;
            PlayerMovement.isBossFight = false;
        }
    }

    void Start()
    {
        InitCanvasGroup();
        PopulateEnemyDropdown();
        PopulateItemDropdown();
        PopulateSkillDropdown();
        UpdateStatInputs();

        if (btnSpawn != null) btnSpawn.onClick.AddListener(SpawnSelectedEnemy);
        if (btnClear != null) btnClear.onClick.AddListener(ClearAllEnemies);
        if (btnAddItem != null) btnAddItem.onClick.AddListener(AddSelectedItem);
        if (btnApplyStats != null) btnApplyStats.onClick.AddListener(ApplyStats);
        if (btnResetFullStats != null) btnResetFullStats.onClick.AddListener(ResetPlayerFullStats);
        if (btnAddOrUpgradeSkill != null) btnAddOrUpgradeSkill.onClick.AddListener(AddOrUpgradeSelectedSkill);
        if (btnRemoveSkill != null) btnRemoveSkill.onClick.AddListener(RemoveSelectedSkill);
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

    void OnDestroy()
    {
        CombatManager.isTestMode = false;
    }

    private void PopulateEnemyDropdown()
    {
        if (enemyDropdown == null) return;
        enemyDropdown.ClearOptions();
        availableEnemies.Clear();

        List<string> options = new List<string>();
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

        var spawner = Object.FindAnyObjectByType<EncounterSpawner>();
        if (spawner != null)
        {
            AddFromList(spawner.normalEnemyPrefabs);
            AddFromList(spawner.advancedEnemyPrefabs);
            AddFromList(spawner.eliteEnemyPrefabs);
            if (spawner.bossFloorConfigs != null)
            {
                foreach (var cfg in spawner.bossFloorConfigs)
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

        foreach (var enemy in allPools)
        {
            if (enemy != null)
            {
                options.Add(enemy.name);
                availableEnemies.Add(enemy);
            }
        }

        enemyDropdown.AddOptions(options);
    }

    private void PopulateItemDropdown()
    {
        if (itemDropdown == null) return;
        itemDropdown.ClearOptions();
        
        List<string> options = new List<string>();
        if (allItems != null)
        {
            foreach (var item in allItems)
            {
                if (item != null)
                {
                    options.Add(item.itemName);
                }
            }
        }
        itemDropdown.AddOptions(options);
    }

    private void PopulateSkillDropdown()
    {
        if (skillDropdown == null) return;
        skillDropdown.ClearOptions();
        availableSkills.Clear();

        List<string> options = new List<string>();

        SkillData[] list = allSkills;
        if ((list == null || list.Length == 0) && SkillManager.Instance != null)
        {
            list = SkillManager.Instance.allSkills;
        }

        if (list != null)
        {
            foreach (var skill in list)
            {
                if (skill != null)
                {
                    options.Add(skill.skillName);
                    availableSkills.Add(skill);
                }
            }
        }
        skillDropdown.AddOptions(options);
    }

    public void AddOrUpgradeSelectedSkill()
    {
        if (skillDropdown == null || availableSkills.Count == 0) return;
        int index = skillDropdown.value;
        if (index >= 0 && index < availableSkills.Count)
        {
            SkillData skill = availableSkills[index];
            if (skill == null || SkillManager.Instance == null) return;

            int currentLvl = SkillManager.Instance.GetSkillLevel(skill.skillID);
            if (currentLvl < 5)
            {
                SkillManager.Instance.learnedSkills[skill.skillID] = currentLvl + 1;
                Debug.Log($"[TEST] Kỹ năng '{skill.skillName}' đạt cấp {currentLvl + 1}/5");
            }
            else
            {
                Debug.Log($"[TEST] Kỹ năng '{skill.skillName}' đã đạt cấp tối đa (5)!");
            }
        }
    }

    public void RemoveSelectedSkill()
    {
        if (skillDropdown == null || availableSkills.Count == 0) return;
        int index = skillDropdown.value;
        if (index >= 0 && index < availableSkills.Count)
        {
            SkillData skill = availableSkills[index];
            if (skill == null || SkillManager.Instance == null) return;

            if (SkillManager.Instance.learnedSkills.ContainsKey(skill.skillID))
            {
                SkillManager.Instance.learnedSkills.Remove(skill.skillID);
                Debug.Log($"[TEST] Đã xóa kỹ năng '{skill.skillName}' khỏi người chơi!");
            }
            else
            {
                Debug.Log($"[TEST] Người chơi chưa học kỹ năng '{skill.skillName}' để xóa!");
            }
        }
    }

    public void SpawnSelectedEnemy()
    {
        if (enemyDropdown == null || availableEnemies.Count == 0) return;
        int index = enemyDropdown.value;
        if (index >= 0 && index < availableEnemies.Count)
        {
            var spawner = Object.FindAnyObjectByType<EncounterSpawner>();
            if (spawner != null)
            {
                spawner.SpawnSpecificEnemy(availableEnemies[index]);
            }
        }
    }

    public void ClearAllEnemies()
    {
        // 1. Xóa toàn bộ enemy stats
        var cm = CombatManager.Instance;
        if (cm != null && cm.activeEnemies != null)
        {
            foreach (var enemy in cm.activeEnemies)
            {
                if (enemy != null && enemy.gameObject != null)
                {
                    Destroy(enemy.gameObject);
                }
            }
            cm.activeEnemies.Clear();
        }

        // 2. Xóa toàn bộ UI Enemy cards (icon chuột, thanh máu quái...)
        var enemyUIs = Object.FindObjectsByType<EnemyUI>(FindObjectsSortMode.None);
        if (enemyUIs != null)
        {
            foreach (var ui in enemyUIs)
            {
                if (ui != null && ui.gameObject != null)
                {
                    Destroy(ui.gameObject);
                }
            }
        }

        Debug.Log("[TEST] Đã dọn dẹp sạch toàn bộ quái và icon hiển thị UI quái!");
    }

    public void AddSelectedItem()
    {
        if (itemDropdown == null || allItems == null || allItems.Length == 0) return;
        int index = itemDropdown.value;
        if (index >= 0 && index < allItems.Length)
        {
            if (InventoryManager.Instance != null && allItems[index] != null)
            {
                InventoryManager.Instance.AddItem(allItems[index], 1);
                SaveSystem.Instance?.Save();
                Debug.Log($"[TEST] Added item: {allItems[index].itemName} và đã Lưu Game!");
            }
        }
    }

    private void UpdateStatInputs()
    {
        if (PlayerManager.Instance != null)
        {
            if (hpInput != null) hpInput.text = PlayerManager.Instance.currentHP.ToString();
            if (atkInput != null) atkInput.text = PlayerManager.Instance.str.ToString();
            if (defInput != null) defInput.text = PlayerManager.Instance.vit.ToString();
            if (spdInput != null) spdInput.text = PlayerManager.Instance.agl.ToString();
            if (senInput != null) senInput.text = PlayerManager.Instance.sen.ToString();
        }
    }

    public void ApplyStats()
    {
        if (PlayerManager.Instance != null)
        {
            if (atkInput != null && int.TryParse(atkInput.text, out int atk))
                PlayerManager.Instance.str = atk;
            if (defInput != null && int.TryParse(defInput.text, out int def))
                PlayerManager.Instance.vit = def;
            if (spdInput != null && int.TryParse(spdInput.text, out int spd))
                PlayerManager.Instance.agl = spd;
            if (senInput != null && int.TryParse(senInput.text, out int sen))
            {
                PlayerManager.Instance.sen = sen;
                if (sen > PlayerManager.Instance.maxSen)
                    PlayerManager.Instance.maxSen = sen;
            }

            if (hpInput != null && int.TryParse(hpInput.text, out int hp))
            {
                PlayerManager.Instance.maxHP = hp;
                PlayerManager.Instance.currentHP = hp;
            }

            if (CombatManager.Instance != null)
                CombatManager.Instance.ForceUpdatePlayerUI();

            SaveSystem.Instance?.Save(); // Tự động lưu game khi ApplyStats
            Debug.Log($"[TEST] Đã thiết lập Stats: HP={PlayerManager.Instance.currentHP}/{PlayerManager.Instance.maxHP}, STR={PlayerManager.Instance.str}, VIT={PlayerManager.Instance.vit}, AGL={PlayerManager.Instance.agl}, SEN={PlayerManager.Instance.sen} và đã Lưu Game!");
        }
    }

    public void ResetPlayerFullStats()
    {
        if (PlayerManager.Instance != null)
        {
            // Giữ nguyên các chỉ số tùy chỉnh (STR, VIT, AGL, maxHP, maxSen) đã cài đặt, chỉ hồi đầy HP và SEN
            PlayerManager.Instance.currentHP = PlayerManager.Instance.maxHP;
            PlayerManager.Instance.sen = PlayerManager.Instance.maxSen;
            PlayerManager.Instance.food = PlayerManager.Instance.maxFood;
            PlayerManager.Instance.buffs.Clear();
            PlayerManager.Instance.debuffs.Clear();
            PlayerManager.Instance.RestoreSanityCollapse();
            
            UpdateStatInputs();

            if (CombatManager.Instance != null)
                CombatManager.Instance.ForceUpdatePlayerUI();

            SaveSystem.Instance?.Save(); // Tự động lưu game khi ResetPlayerFullStats
            Debug.Log($"[TEST] Đã hồi phục đầy đủ trạng thái nhân vật: Full HP ({PlayerManager.Instance.currentHP}/{PlayerManager.Instance.maxHP}), SEN ({PlayerManager.Instance.sen}/{PlayerManager.Instance.maxSen}), Food và đã Lưu Game!");
        }
    }
}
