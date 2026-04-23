using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;

public class CombatManager : MonoBehaviour
{
    public static CombatManager Instance { get; private set; }

    [Header("Dữ liệu Kẻ thù")]
    public GameObject[] normalEnemyPrefabs;
    public GameObject[] bossPrefabs;

    public List<EnemyStats> activeEnemies = new List<EnemyStats>();
    public Transform enemyArea;
    public GameObject enemyUIPrefab;

    [Header("Hệ thống ATB")]
    private float maxAP = 100f;
    private bool isCombatPaused = false;
    public float tickSpeedMultiplier = 5f;
    private int playerWaitTurns = 0;

    [Header("UI Người chơi")]
    public TextMeshProUGUI playerHPText;
    public TextMeshProUGUI playerAPText;
    public TextMeshProUGUI playerSanityText;
    public Slider playerHPBar;
    public Slider playerAPBar;
    public Slider playerSanityBar;

    [Header("UI Điều khiển & Bảng thông tin")]
    public GameObject actionMenu;
    public GameObject enemyInfoPanel;
    public TextMeshProUGUI infoStatsText;
    public TextMeshProUGUI infoSkillText;
    public GameObject[] actionButtons;

    [Header("Hệ thống Nhắm Mục Tiêu")]
    public GameObject targetArrow;
    private bool isTargetingMode = false;
    private EnemyStats currentTarget;
    private int selectedButtonIndex = 0;

    [Header("Tham chiếu Trang Bị & UI Phụ")]
    public GameObject equipmentCanvas; // Kéo Canvas_Equipment vào đây

    [Header("Bảng Chiến Thắng (Victory Panel)")]
    public GameObject victoryPanel;
    public TextMeshProUGUI txtVictoryExp;
    public TextMeshProUGUI txtVictoryGold;
    private int earnedExp = 0;
    private int earnedGold = 0;

    [Header("Hệ thống Kỹ năng")]
    public GameObject skillMenuPanel;
    private SkillData pendingSkill;
    /// <summary>Theo dõi CD còn lại của từng kỹ năng (skillID → số lượt còn lại).</summary>
    public Dictionary<string, int> skillCooldowns = new Dictionary<string, int>();
    public void OnEquipmentButton()
    {
        actionMenu.SetActive(false);
        isCombatPaused = true;

        EquipmentUI targetUI = EquipmentUI.Instance;
        if (targetUI == null)
        {
            // Tìm thủ công kể cả khi Object đang bị tắt (inactive) lúc mới khởi động game
            targetUI = Object.FindAnyObjectByType<EquipmentUI>(FindObjectsInactive.Include);
        }

        if (targetUI != null && targetUI.rootCanvas != null)
        {
            targetUI.rootCanvas.SetActive(true);
        }
        else if (equipmentCanvas != null)
        {
            equipmentCanvas.SetActive(true); // Dự phòng nếu chưa gán rootCanvas
        }
        else
        {
            Debug.LogError("LỖI: Không tìm thấy EquipmentUI!");
            isCombatPaused = false;
            actionMenu.SetActive(true);
        }
    }

    // Gọi khi thoát Equipment/Inventory để tiếp tục chiến đấu
    public void ResumeCombat()
    {
        isCombatPaused = false;
    }

    private void Awake()
    {
        // 1. NGĂN CHẶN LỖI RÒ RỈ COMBATMANAGER SANG DUNGEON SCENE
        // Nếu lỡ bị đưa sang Scene khác, tự hủy hoặc khóa ngay.
        if (SceneManager.GetActiveScene().name != "Combat")
        {
            Debug.LogWarning("CombatManager xuất hiện sai Scene! Tự động vô hiệu hóa để chống rò rỉ lỗi.");
            this.enabled = false;
            return;
        }

        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }
    void Start()
    {
        // 1. Lưu lại trạng thái Ngẫu Nhiên máy chủ hiện tại
        Random.State oldState = Random.state;

        // 2. Ép cứng Seed ngẫu nhiên CỦA RIÊNG Ô ĐẤT NÀY 
        // Phép toán trộn vị trí X, Y chéo với Seed của Map để đảm bảo tạo ra một mã duy nhất cho vị trí đó
        int encounterSeed = PlayerMovement.currentMapSeed + (PlayerMovement.combatEnemyPosition.x * 37) + (PlayerMovement.combatEnemyPosition.y * 101);
        Random.InitState(encounterSeed);

        // 3. Đẻ quái dựa trên cục Seed cứng này (Ai chạy trốn rồi quay lại dẫm ô này sẽ gặp Y HỆT)
        int enemyCount = Random.Range(1, 4);
        for (int i = 0; i < enemyCount; i++) SpawnRandomEnemy();

        // 4. Trả lại sự ngẫu nhiên bình thường cho các luồng xử lý khác (Tỉ lệ đánh trúng, % bạo kích...)
        // Tránh tình trạng bạn đánh trượt thì lần sau quay lại cũng bị trượt đúng chiêu đó.
        Random.state = oldState;

        UpdatePlayerUI();
        actionMenu.SetActive(false);
        if (targetArrow != null) targetArrow.SetActive(false);
        if (skillMenuPanel != null) skillMenuPanel.SetActive(false);

        // Khởi tạo actionButtons bằng code (dự phòng chưa gán trong Inspector)
        if (actionButtons == null || actionButtons.Length == 0)
        {
            actionButtons = new GameObject[]
            {
                GameObject.Find("Btn_Attack"),
                GameObject.Find("Btn_Skill"),
                GameObject.Find("Btn_Defend"),
                GameObject.Find("Btn_Item"),
                GameObject.Find("Btn_Flee")
            };
        }

        UpdateButtonSelection();

        if (EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(null);

        // Kiểm tra kỹ năng đã học — SkillMenuUI sẽ tự lọc level > 0 khi mở
        if (SkillManager.Instance != null && SkillManager.Instance.allSkills != null)
        {
            foreach (var skill in SkillManager.Instance.allSkills)
            {
                if (skill == null) continue;
                int lv = SkillManager.Instance.GetSkillLevel(skill.skillID);
                Debug.Log($"[SKILL CHECK] {skill.skillName}: Cấp {lv}{(lv > 0 ? " ✓ Có thể dùng" : " (Chưa học)" )}");
            }
        }
    }

    void Update()
    {
        // isCombatPaused = true khi Equipment hoặc Inventory đang mở → không reset để không block input của chúng
        if (!isCombatPaused && EventSystem.current != null && EventSystem.current.currentSelectedGameObject != null)
        {
            EventSystem.current.SetSelectedGameObject(null);
        }

        // === 1. XỬ LÝ INPUT TRƯỚC TIÊN ===
        // Chuột phải = X (hủy/lùi về toàn cục)
        if (Input.GetMouseButtonDown(1)) HandleRightClick();
        // Chuột trái = Z (xác nhận toàn cục — chỉ khi đang nhắm mục tiêu)
        if (Input.GetMouseButtonDown(0) && isTargetingMode) HandleLeftClickConfirm();

        // Nếu đang ở menu hành động
        if (actionMenu != null && actionMenu.activeSelf)
        {
            HandleActionMenuKeyboard();
        }
        // FIX: Thêm 'else if' để tránh nút Z kích hoạt cả 2 hàm trong cùng 1 frame
        else if (isTargetingMode)
        {
            HandleTargetingKeyboard();
        }

        // === 2. KIỂM TRA PAUSE SAU KHI ĐÃ XỬ LÝ INPUT ===
        if (isCombatPaused || PlayerManager.Instance == null) return;
        if (CheckBattleEnd()) return;

        if (playerWaitTurns <= 0)
        {
            PlayerManager.Instance.currentAP += PlayerManager.Instance.GetTotalSpeed() * Time.deltaTime * tickSpeedMultiplier;
        }

        float highestAP = 99.99f;
        Unit readyUnit = null;

        if (PlayerManager.Instance.currentAP >= 100f && playerWaitTurns <= 0)
        {
            highestAP = PlayerManager.Instance.currentAP;
            readyUnit = PlayerManager.Instance;
        }

        foreach (var enemy in activeEnemies)
        {
            if (enemy.currentHP <= 0) continue;

            if (enemy.currentSpeed == 0)
            {
                enemy.currentSpeed = enemy.baseSpeed;
                enemy.currentDefense = enemy.baseDefense;
                enemy.currentHP = enemy.maxHP;
            }

            enemy.currentAP += enemy.currentSpeed * Time.deltaTime * tickSpeedMultiplier;

            if (enemy.currentAP > highestAP)
            {
                highestAP = enemy.currentAP;
                readyUnit = enemy;
            }
        }

        UpdateAPUI();

        if (readyUnit != null)
        {
            isCombatPaused = true;

            if (DebuffManager.Instance.HasDebuff(readyUnit, DebuffType.Stun))
            {
                readyUnit.currentAP -= maxAP;
                DebuffManager.Instance.ProcessTurnEnd(readyUnit);
                isCombatPaused = false;
                return;
            }

            DebuffManager.Instance.ProcessTurnStart(readyUnit);
            UpdatePlayerUI();

            if (PlayerManager.Instance.currentHP <= 0) { CheckPlayerDeath(); return; }
            if (readyUnit.currentHP <= 0) { isCombatPaused = false; return; }

            if (readyUnit == PlayerManager.Instance) PlayerTurn();
            else EnemyTurn((EnemyStats)readyUnit);
        }
    }

    void HandleActionMenuKeyboard()
    {
        if (Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.W))
        {
            selectedButtonIndex--;
            if (selectedButtonIndex < 0) selectedButtonIndex = actionButtons.Length - 1;
            UpdateButtonSelection();
        }

        if (Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.S))
        {
            selectedButtonIndex++;
            if (selectedButtonIndex >= actionButtons.Length) selectedButtonIndex = 0;
            UpdateButtonSelection();
        }

        if (Input.GetKeyDown(KeyCode.Z) || Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Space))
            OnActionButtonConfirmed();
    }

    void HandleTargetingKeyboard()
    {
        if (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.A)) SelectPreviousEnemy();
        if (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D)) SelectNextEnemy();

        if (Input.GetKeyDown(KeyCode.Z) || Input.GetKeyDown(KeyCode.Return))
            ConfirmTargeting();

        if (Input.GetKeyDown(KeyCode.X))
            CancelTargeting();
    }

    /// <summary>Xác nhận tấn công hoặc xuất chiêu vào currentTarget (dùng chung cho phím Z và chuột trái)</summary>
    void ConfirmTargeting()
    {
        if (currentTarget == null || currentTarget.currentHP <= 0) return;

        if (pendingSkill != null)
        {
            int skillLevel = SkillManager.Instance.GetSkillLevel(pendingSkill.skillID);
            float dmgBonus = pendingSkill.damageBonusPerLevel[skillLevel - 1];
            float effectBonus = pendingSkill.effectChancePerLevel[skillLevel - 1];
            ExecuteSkill(currentTarget, pendingSkill, dmgBonus, effectBonus);
        }
        else
        {
            ExecutePlayerAttack(currentTarget);
        }
    }

    /// <summary>Chuột trái trong targeting mode — xác nhận ngay lập tức</summary>
    void HandleLeftClickConfirm()
    {
        ConfirmTargeting();
    }

    void UpdateButtonSelection()
    {
        for (int i = 0; i < actionButtons.Length; i++)
        {
            if (actionButtons[i] != null)
            {
                Button btn = actionButtons[i].GetComponent<Button>();
                if (btn != null)
                {
                    ColorBlock cb = btn.colors;
                    if (i == selectedButtonIndex)
                    {
                        cb.normalColor = Color.yellow;
                        cb.highlightedColor = new Color(1f, 0.9f, 0.5f);
                    }
                    else
                    {
                        cb.normalColor = Color.white;
                        cb.highlightedColor = Color.gray;
                    }
                    btn.colors = cb;
                }
            }
        }
    }

    void OnActionButtonConfirmed()
    {
        if (selectedButtonIndex >= 0 && selectedButtonIndex < actionButtons.Length)
        {
            Button btn = actionButtons[selectedButtonIndex]?.GetComponent<Button>();
            if (btn != null)
            {
                btn.onClick.Invoke();
            }
        }
    }

    void SelectNextEnemy()
    {
        if (activeEnemies.Count <= 1) return;

        int currentIndex = activeEnemies.IndexOf(currentTarget);
        int nextIndex = (currentIndex + 1) % activeEnemies.Count;

        // Tìm enemy còn sống tiếp theo
        int attempts = 0;
        while (activeEnemies[nextIndex].currentHP <= 0 && attempts < activeEnemies.Count)
        {
            nextIndex = (nextIndex + 1) % activeEnemies.Count;
            attempts++;
        }

        if (activeEnemies[nextIndex].currentHP > 0)
        {
            currentTarget = activeEnemies[nextIndex];
            MoveArrowToTarget(currentTarget);
        }
    }

    void SelectPreviousEnemy()
    {
        if (activeEnemies.Count <= 1) return;

        int currentIndex = activeEnemies.IndexOf(currentTarget);
        int prevIndex = (currentIndex - 1 + activeEnemies.Count) % activeEnemies.Count;

        // Tìm enemy còn sống trước đó
        int attempts = 0;
        while (activeEnemies[prevIndex].currentHP <= 0 && attempts < activeEnemies.Count)
        {
            prevIndex = (prevIndex - 1 + activeEnemies.Count) % activeEnemies.Count;
            attempts++;
        }

        if (activeEnemies[prevIndex].currentHP > 0)
        {
            currentTarget = activeEnemies[prevIndex];
            MoveArrowToTarget(currentTarget);
        }
    }

    void CancelTargeting()
    {
        isTargetingMode = false;
        pendingSkill = null;
        if (targetArrow != null) targetArrow.SetActive(false);
        actionMenu.SetActive(true);
    }

    /// <summary>Chuột phải hoặc X toàn cục — hủy targeting hoặc đóng skill menu</summary>
    void HandleRightClick()
    {
        if (isTargetingMode)
        {
            CancelTargeting(); // đã clear pendingSkill bên trong
        }
        else if (SkillMenuUI.Instance != null && SkillMenuUI.Instance.gameObject.activeSelf)
        {
            CancelSkillMenu();
        }
    }

    /// <summary>Nút "Kĩ Năng" trong ActionMenu — mở bảng kỹ năng</summary>
    public void OnSkillMenuButton()
    {
        actionMenu.SetActive(false);
        isCombatPaused = true;

        // Ưu tiên dùng reference đã kéo vào Inspector
        SkillMenuUI ui = (skillMenuPanel != null) ? skillMenuPanel.GetComponent<SkillMenuUI>() : SkillMenuUI.Instance;

        if (ui != null)
        {
            ui.Open();
        }
        else
        {
            // Dự phòng cuối cùng: tìm trong hierarchy kể cả khi đang bị tắt (inactive)
            ui = Object.FindAnyObjectByType<SkillMenuUI>(FindObjectsInactive.Include);
            if (ui != null) ui.Open();
            else Debug.LogError("LỖI: Không tìm thấy kịch bản SkillMenuUI trên scene!");
        }
    }

    /// <summary>Đóng bảng kỹ năng, quay về ActionMenu</summary>
    public void CancelSkillMenu()
    {
        SkillMenuUI ui = (skillMenuPanel != null) ? skillMenuPanel.GetComponent<SkillMenuUI>() : SkillMenuUI.Instance;
        if (ui == null) ui = Object.FindAnyObjectByType<SkillMenuUI>(FindObjectsInactive.Include);
        
        if (ui != null) ui.Close();

        isCombatPaused = false;
        actionMenu.SetActive(true);
    }
    public void SpawnRandomEnemy()
    {
        if (normalEnemyPrefabs.Length == 0 || activeEnemies.Count >= 3) return;
        int randomIndex = Random.Range(0, normalEnemyPrefabs.Length);
        SpawnSpecificEnemy(normalEnemyPrefabs[randomIndex]);
    }

    public void SpawnSpecificEnemy(GameObject prefab)
    {
        Debug.Log($"[SPAWN] Bắt đầu spawn: {prefab.name}");
        Debug.Log($"[SPAWN] activeEnemies.Count trước: {activeEnemies.Count}");

        if (activeEnemies.Count >= 3)
        {
            Debug.LogError("[SPAWN] Hủy vì sân đã đầy!");
            return;
        }

        GameObject enemyObject = Instantiate(prefab);
        Debug.Log($"[SPAWN] Đã instantiate enemyObject");

        EnemyStats stats = enemyObject.GetComponent<EnemyStats>();
        if (stats == null)
        {
            Debug.LogError("[SPAWN] LỖI: Prefab không có EnemyStats!");
            Destroy(enemyObject);
            return;
        }

        stats.currentHP = stats.maxHP;
        stats.currentDefense = stats.baseDefense;
        stats.currentSpeed = stats.baseSpeed;
        stats.currentCooldown = stats.initialCooldown;

        activeEnemies.Add(stats);
        Debug.Log($"[SPAWN] Đã add vào activeEnemies, count = {activeEnemies.Count}");

        if (enemyUIPrefab == null)
        {
            Debug.LogError("[SPAWN] LỖI: enemyUIPrefab chưa gán trong Inspector!");
            activeEnemies.Remove(stats);
            Destroy(enemyObject);
            return;
        }

        GameObject uiObject = Instantiate(enemyUIPrefab, enemyArea);
        Debug.Log($"[SPAWN] Đã instantiate UI");

        EnemyUI uiScript = uiObject.GetComponent<EnemyUI>();
        if (uiScript == null)
        {
            Debug.LogError("[SPAWN] LỖI: UI không có component EnemyUI!");
            Destroy(uiObject);
            activeEnemies.Remove(stats);
            Destroy(enemyObject);
            return;
        }

        uiScript.Setup(stats);
        Debug.Log($"[SPAWN] ✓ Spawn thành công!");

        // === FIX UI KHÔNG HIỆN ===
        UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)enemyArea);
        Debug.Log($"[SPAWN] Đã force rebuild layout");
        // =========================

        Button uiButton = uiObject.GetComponent<Button>();
        if (uiButton != null)
        {
            uiButton.onClick.AddListener(() => OnEnemyClicked(stats));
        }

        enemyObject.SetActive(false);
    }

    public EnemyStats GetFirstAliveEnemy()
    {
        foreach (var enemy in activeEnemies)
            if (enemy.currentHP > 0) return enemy;
        return null;
    }

    bool CheckBattleEnd()
    {
        // Phải có quái thì mới đánh giá trận đấu! Ngăn lỗi null
        if (activeEnemies == null || activeEnemies.Count == 0) return false;

        bool allDead = true;
        foreach (var enemy in activeEnemies)
            if (enemy.currentHP > 0) allDead = false;

        if (allDead)
        {
            isCombatPaused = true;

            // 1. Nhặt Loot: Cứ cộng dồn EXP và Vàng từ các quái chết
            earnedExp = 0;
            earnedGold = 0;
            foreach (var enemy in activeEnemies)
            {
                earnedExp += enemy.expDrop;
                earnedGold += enemy.goldDrop;
            }

            // 2. Điền số vào chữ (nếu bạn đã gắn dây trên Inspector)
            if (txtVictoryExp != null) txtVictoryExp.text = earnedExp.ToString();
            if (txtVictoryGold != null) txtVictoryGold.text = earnedGold.ToString();

            // 3. Hiện bảng Chiến thắng lên che màn hình thay vì té gấp
            if (victoryPanel != null)
            {
                victoryPanel.SetActive(true);
                Debug.Log($"Chiến thắng! Nhận {earnedExp} EXP và {earnedGold} Vàng. Đang chờ bạn bấm Tiếp Tục...");
            }
            else
            {
                // Dự phòng: Mất UI thì vẫn tự ăn điểm rồi về
                OnVictoryContinueButton();
            }
            return true;
        }
        return false;
    }

    // Hàm gắn vào Nút bấm "Tiếp tục"
    public void OnVictoryContinueButton()
    {
        Debug.Log("Đang nạp tiền vào người rồi biến!");
        
        // 1. Chuyển phần thưởng thực sự vào túi mình
        if (PlayerManager.Instance != null)
        {
            PlayerManager.Instance.AddExp(earnedExp);
            PlayerManager.Instance.gold += earnedGold;
        }

        // 2. Xác nhận quái vật tại vị trí này ở Dungeon đã bị diệt
        PlayerMovement.defeatedEnemiesTiles.Add(PlayerMovement.combatEnemyPosition);

        // 3. Load lại Dungeon
        SceneManager.LoadScene("Dungeon");
    }

    void UpdatePlayerUI()
    {
        playerHPBar.maxValue = PlayerManager.Instance.maxHP;
        playerHPBar.value = PlayerManager.Instance.currentHP;
        playerHPText.text = PlayerManager.Instance.currentHP + "/" + PlayerManager.Instance.maxHP;

        if (playerSanityBar != null)
        {
            playerSanityBar.maxValue = PlayerManager.Instance.maxSen;
            playerSanityBar.value = PlayerManager.Instance.sen;
        }
        if (playerSanityText != null) playerSanityText.text = PlayerManager.Instance.sen.ToString();
    }

    // Public wrapper để các class khác (ItemUsageManager...) có thể gọi
    public void ForceUpdatePlayerUI() => UpdatePlayerUI();

    void UpdateAPUI()
    {
        playerAPBar.value = PlayerManager.Instance.currentAP;
        playerAPText.text = Mathf.FloorToInt(PlayerManager.Instance.currentAP) + "%";
    }

    void PlayerTurn()
    {
        actionMenu.SetActive(true);
        PlayerManager.Instance.isDefending = false;
        isTargetingMode = false;
        if (targetArrow != null) targetArrow.SetActive(false);

        // Đảm bảo EventSystem không giữ focus vào button nào
        // để Input.GetKeyDown có thể nhận phím mũi tên/Z/X ngay lập tức
        if (EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(null);
    }

    void EnemyTurn(EnemyStats attacker)
    {
        if (attacker.currentCooldown > 0) attacker.currentCooldown--;

        bool usedSkill = false;
        if (attacker.maxCooldown > 0 && attacker.currentCooldown == 0)
        {
            if (Random.Range(0f, 100f) <= 70f)
            {
                ExecuteEnemySkill(attacker);
                usedSkill = true;
            }
            else
            {
                attacker.currentCooldown = attacker.maxCooldown;
            }
        }

        if (!usedSkill) ExecuteEnemyNormalAttack(attacker);

        UpdatePlayerUI();
        CheckPlayerDeath();

        attacker.currentAP -= maxAP;
        if (playerWaitTurns > 0) playerWaitTurns--;

        isCombatPaused = false;
    }

    public void OnAttackButton()
    {
        actionMenu.SetActive(false);
        isTargetingMode = true;
        currentTarget = GetFirstAliveEnemy();
        if (currentTarget == null)
        {
            Debug.LogWarning("Không có kẻ thù nào còn sống!");
            isTargetingMode = false;
            actionMenu.SetActive(true);
            return;
        }
        MoveArrowToTarget(currentTarget);
        if (EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(null);
        Debug.Log("Chế độ nhắm mục tiêu: Dùng phím Mũi tên Trái/Phải để chọn kẻ thù, Z để tấn công, X để hủy!");
    }

    public void OnItemButton()
    {
        actionMenu.SetActive(false);
        isCombatPaused = true;

        InventoryUI targetUI = InventoryUI.Instance;
        if (targetUI == null)
            targetUI = Object.FindAnyObjectByType<InventoryUI>(FindObjectsInactive.Include);

        if (targetUI != null)
        {
            targetUI.ToggleInventory();
            Debug.Log("Đã mở túi đồ. Dùng phím mũi tên để di chuyển, Z để dùng item, X hoặc I để đóng.");
        }
        else
        {
            Debug.LogError("LỖI: Không tìm thấy InventoryUI!");
            isCombatPaused = false;
            actionMenu.SetActive(true);
        }
    }
    public void OnEnemyClicked(EnemyStats clickedEnemy)
    {
        if (clickedEnemy == null || clickedEnemy.currentHP <= 0) return;

        if (isTargetingMode)
        {
            if (currentTarget == clickedEnemy)
                ConfirmTargeting(); // click lại ô đang target → xác nhận
            else
            {
                currentTarget = clickedEnemy;
                MoveArrowToTarget(currentTarget);
            }
        }
        else
        {
            ToggleEnemyInfo(clickedEnemy);
        }
    }

    private void MoveArrowToTarget(EnemyStats target)
    {
        if (targetArrow == null || target == null) return;
        targetArrow.SetActive(true);

        foreach (var ui in FindObjectsByType<EnemyUI>(FindObjectsSortMode.None))
        {
            if (ui.stats == target)
            {
                targetArrow.transform.SetParent(ui.icon.transform);
                RectTransform rect = targetArrow.GetComponent<RectTransform>();
                rect.anchoredPosition = new Vector2(0, 100f);
                targetArrow.transform.SetAsLastSibling();
                break;
            }
        }
    }

    private void ExecutePlayerAttack(EnemyStats target)
    {
        isTargetingMode = false;
        if (targetArrow != null) targetArrow.SetActive(false);

        if (CheckEvasion(target))
        {
            Debug.Log(target.enemyName + " đã né được đòn tấn công của bạn!");
        }
        else
        {
            bool isCrit = UnityEngine.Random.Range(0f, 100f) <= PlayerManager.Instance.GetTotalCrit();
            float rawDamage = PlayerManager.Instance.GetTotalAttack();

            // Tính sát thương bonus theo loại quái vật
            if (target.enemyType == EnemyType.Human)
            {
                rawDamage += PlayerManager.Instance.equipmentBonusDamageVsHuman;
            }
            else
            {
                rawDamage += PlayerManager.Instance.equipmentBonusDamageVsNonHuman;
            }

            if (isCrit) rawDamage *= 1.5f;

            int finalDamage = Mathf.FloorToInt(Mathf.Max(1, rawDamage - target.currentDefense));
            target.currentHP -= finalDamage;
            if (target.currentHP < 0) target.currentHP = 0;

            Color dmgColor = isCrit ? Color.yellow : Color.white;

            Transform targetTransform = playerHPBar.transform;
            foreach (var ui in FindObjectsByType<EnemyUI>(FindObjectsSortMode.None))
            {
                if (ui.stats == target) { targetTransform = ui.icon.transform; break; }
            }
            
            if (FloatingTextManager.Instance != null)
            {
                FloatingTextManager.Instance.SpawnText(targetTransform.position, finalDamage.ToString(), dmgColor);
            }

            // Gieo xúc xắc tạo Debuff
            if (UnityEngine.Random.Range(0f, 100f) <= PlayerManager.Instance.equipmentBleedChance)
            {
                if (DebuffManager.Instance != null)
                {
                    DebuffManager.Instance.AddDebuff(target, DebuffType.Bleed, 3, 1, rawDamage * 0.2f);
                    Debug.Log($"{target.enemyName} bị CHẢY MÁU do vũ khí của bạn!");
                }
            }

            if (UnityEngine.Random.Range(0f, 100f) <= PlayerManager.Instance.equipmentStunChance)
            {
                if (DebuffManager.Instance != null)
                {
                    DebuffManager.Instance.AddDebuff(target, DebuffType.Stun, 1);
                    Debug.Log($"{target.enemyName} bị CHOÁNG do vũ khí của bạn!");
                }
            }
        }

        PlayerManager.Instance.currentAP -= maxAP;
        TickDownSkillCooldowns();
        isCombatPaused = false;
    }

    public void OnDefendButton()
    {
        actionMenu.SetActive(false);
        Debug.Log("Bạn chọn Phòng thủ! +1 DEF cứng, +50% Base DEF, +25% EVA lượt này.");
        
        PlayerManager.Instance.currentAP -= maxAP;
        TickDownSkillCooldowns(); // Trừ CD khi phòng thủ
        isCombatPaused = false;
    }

    public void OnFleeButton()
    {
        actionMenu.SetActive(false);
        isCombatPaused = true;

        if (Random.Range(0f, 100f) <= 80f)
        {
            Debug.Log("Chạy trốn thành công! Tạm rút lui về Dungeon...");
            PlayerMovement.isReturningFromCombat = true; // Bật cờ để map nhận diện quay về
            SceneManager.LoadScene("Dungeon"); // Phải về Dungeon thay vì ActiveScene (tránh nạp lại Combat)
        }
        else
        {
            Debug.Log("<color=red>Chạy trốn thất bại! Đóng băng AP chờ quái hành động.</color>");
            if (FloatingTextManager.Instance != null)
                FloatingTextManager.Instance.SpawnText(playerHPBar.transform.position, "Chạy thất bại!", Color.red);

            PlayerManager.Instance.currentAP = 0;
            playerWaitTurns = 0;
            foreach (var e in activeEnemies) if (e.currentHP > 0) playerWaitTurns++;

            isCombatPaused = false;
        }
    }



    public void ToggleEnemyInfo(EnemyStats stats)
    {
        if (isTargetingMode) return;

        enemyInfoPanel.SetActive(!enemyInfoPanel.activeSelf);
        if (enemyInfoPanel.activeSelf && stats != null)
        {
            infoStatsText.text = $"HP: {stats.maxHP}\nATK: {stats.attack}\nDEF: {stats.currentDefense}\nSPD: {stats.currentSpeed}\nCRIT: {stats.critChance}%\nEVA: {stats.evasion}%";
            if (stats.maxCooldown > 0)
                infoSkillText.text = $"<color=yellow>Kỹ năng: {stats.skillName}</color>\n{stats.skillDescription}\n(Hồi chiêu: {stats.maxCooldown} lượt)";
            else
                infoSkillText.text = "Kẻ thù này không có kỹ năng đặc biệt.";
        }
    }

    void ExecuteEnemyNormalAttack(EnemyStats attacker)
    {
        if (CheckEvasion(PlayerManager.Instance)) return;

        bool isCrit = UnityEngine.Random.Range(0f, 100f) <= attacker.critChance;
        float rawDamage = attacker.attack;
        if (isCrit) rawDamage *= 1.5f;

        PlayerManager.Instance.TakeDamage(rawDamage, false, false);

        float defToUse = PlayerManager.Instance.currentDefense;
        if (PlayerManager.Instance.isDefending) defToUse += 1f + Mathf.FloorToInt(PlayerManager.Instance.baseDefense * 0.5f);
        int displayDmg = Mathf.FloorToInt(Mathf.Max(1, rawDamage - defToUse));

        Color dmgColor = isCrit ? Color.yellow : Color.white;
        FloatingTextManager.Instance.SpawnText(playerHPBar.transform.position, displayDmg.ToString(), dmgColor);

        if (isCrit && UnityEngine.Random.Range(0f, 100f) <= 12f) PlayerManager.Instance.ReduceSanity(1);
    }

    void ExecuteEnemySkill(EnemyStats attacker)
    {
        attacker.currentCooldown = attacker.maxCooldown;
        Debug.Log($"<color=red>{attacker.enemyName} dùng kỹ năng: {attacker.skillName}!</color>");

        // Chuyển tên quái và tên kĩ năng về chữ thường để quét từ khóa
        string eName = attacker.enemyName.ToLower();
        string sName = attacker.skillName.ToLower();

        // 1. KỸ NĂNG GỌI ĐÀN (Quét theo tên kĩ năng hoặc từ khóa "tinh anh")
        if (sName.Contains("gọi đàn") || sName.Contains("goi dan") || eName.Contains("tinh anh"))
        {
            Debug.Log($"[GỌI ĐÀN] Kích hoạt! Sân đang có {activeEnemies.Count}/3 quái.");
            if (activeEnemies.Count < 3)
            {
                GameObject nhenHangPrefab = null;
                foreach (var p in normalEnemyPrefabs)
                {
                    if (p.name.ToLower().Contains("nhenhang"))
                    {
                        nhenHangPrefab = p;
                        break;
                    }
                }

                if (nhenHangPrefab != null)
                {
                    Debug.Log("[GỌI ĐÀN] Đã tìm thấy Prefab Nhện Hang, tiến hành triệu hồi...");
                    SpawnSpecificEnemy(nhenHangPrefab);
                }
                else
                {
                    Debug.LogError("❌ LỖI GỌI ĐÀN: Không tìm thấy file Prefab nào tên 'nhenhang' trong mảng Normal Enemy Prefabs!");
                }
            }
            else
            {
                Debug.Log("⚠ Sân đã đầy 3 quái, Nhện Tinh Anh chuyển sang đánh thường!");
                ExecuteEnemyNormalAttack(attacker);
            }
        }
        // 2. KỸ NĂNG CỦA ĐỈA
        else if (eName.Contains("đỉa") || eName.Contains("dia"))
        {
            PlayerManager.Instance.TakeDamage(attacker.attack, false, false);
            if (UnityEngine.Random.Range(0f, 100f) <= 30f)
                DebuffManager.Instance.AddDebuff(PlayerManager.Instance, DebuffType.Bleed, 1, 1, attacker.attack);
            attacker.currentHP = Mathf.Min(attacker.maxHP, attacker.currentHP + 5);
        }
        // 3. KỸ NĂNG THẰN LẰN
        else if (eName.Contains("thằn lằn") || eName.Contains("than lan"))
        {
            PlayerManager.Instance.TakeDamage(attacker.attack, false, false);
            if (UnityEngine.Random.Range(0f, 100f) <= 30f)
                DebuffManager.Instance.AddDebuff(PlayerManager.Instance, DebuffType.Poison, 3);
        }
        // 4. KỸ NĂNG NHỆN HANG
        else if (eName.Contains("nhện hang") || eName.Contains("nhen hang"))
        {
            PlayerManager.Instance.TakeDamage(attacker.attack + 3f, false, false);
        }
        // 5. KỸ NĂNG CHUỘT NHẢY
        else if (eName.Contains("chuột nhảy") || eName.Contains("chuot nhay"))
        {
            attacker.currentSpeed += 5f;
            Debug.Log("Chuột nhảy đã tự tăng tốc độ!");
        }
        // MẶC ĐỊNH LÀ ĐÁNH THƯỜNG
        else
        {
            ExecuteEnemyNormalAttack(attacker);
        }
    }

    void CheckPlayerDeath()
    {
        if (PlayerManager.Instance.currentHP <= 0)
        {
            Debug.Log("Người chơi đã tử vong. Game Over!");
            isCombatPaused = true;
        }
    }

    private bool CheckEvasion(Unit target)
    {
        float evasionChance = 0f;
        if (target == PlayerManager.Instance) evasionChance = PlayerManager.Instance.GetTotalEvasion();
        else if (target is EnemyStats enemyTarget) evasionChance = enemyTarget.evasion;

        if (UnityEngine.Random.Range(0f, 100f) <= evasionChance)
        {
            if (FloatingTextManager.Instance != null)
            {
                Transform uiTransform = playerHPBar.transform;
                if (target is EnemyStats targetEnemy)
                {
                    foreach (var ui in FindObjectsByType<EnemyUI>(FindObjectsSortMode.None))
                    {
                        if (ui.stats == targetEnemy) { uiTransform = ui.icon.transform; break; }
                    }
                }
                FloatingTextManager.Instance.SpawnText(uiTransform.position, "Miss!", Color.cyan);
            }
            return true;
        }
        return false;
    }
    private void DealDamageToEnemy(EnemyStats target, float damageMultiplier, float flatBonus, float extraCritChance, bool triggerOnHit)
    {
        if (CheckEvasion(target)) return;

        float totalCrit = PlayerManager.Instance.GetTotalCrit() + extraCritChance;
        bool isCrit = UnityEngine.Random.Range(0f, 100f) <= totalCrit;

        float rawDamage = (PlayerManager.Instance.GetTotalAttack() * damageMultiplier) + flatBonus;
        if (target.enemyType == EnemyType.Human) rawDamage += PlayerManager.Instance.equipmentBonusDamageVsHuman;
        else rawDamage += PlayerManager.Instance.equipmentBonusDamageVsNonHuman;

        if (isCrit) rawDamage *= 1.5f;

        int finalDamage = Mathf.FloorToInt(Mathf.Max(1, rawDamage - target.currentDefense));
        target.currentHP -= finalDamage;
        if (target.currentHP < 0) target.currentHP = 0;

        Color dmgColor = isCrit ? Color.yellow : Color.white;

        // Tìm vị trí UI của quái để spawn text
        Transform targetTransform = targetArrow.transform;
        foreach (var ui in FindObjectsByType<EnemyUI>(FindObjectsSortMode.None))
        {
            if (ui.stats == target) { targetTransform = ui.icon.transform; break; }
        }

        FloatingTextManager.Instance.SpawnText(targetTransform.position, finalDamage.ToString(), dmgColor);

        // Áp dụng hiệu ứng đòn đánh (On-Hit) [cite: 2]
        if (triggerOnHit)
        {
            if (UnityEngine.Random.Range(0f, 100f) <= PlayerManager.Instance.equipmentBleedChance)
                DebuffManager.Instance.AddDebuff(target, DebuffType.Bleed, 3, 1, rawDamage * 0.2f);
            if (UnityEngine.Random.Range(0f, 100f) <= PlayerManager.Instance.equipmentStunChance)
                DebuffManager.Instance.AddDebuff(target, DebuffType.Stun, 1);
        }
    }
    /// <summary>
    /// Gọi từ SkillButtonUI khi bấm icon kỹ năng (chuột trái hoặc Z).
    /// Kỹ năng Random/Buff → kích hoạt ngay. SingleTarget → vào targeting mode.
    /// </summary>
    public void OnSkillButton(SkillData skill)
    {
        // Đóng SkillMenuUI
        if (SkillMenuUI.Instance != null) SkillMenuUI.Instance.Close();
        else if (skillMenuPanel != null) skillMenuPanel.SetActive(false);

        if (skill == null) return;

        int skillLevel = SkillManager.Instance != null
            ? SkillManager.Instance.GetSkillLevel(skill.skillID) : 0;
        if (skillLevel == 0) { CancelSkillMenu(); return; }

        float dmgBonus = skill.damageBonusPerLevel[skillLevel - 1];
        float effectBonus = skill.effectChancePerLevel[skillLevel - 1];

        // Random/Buff → kích hoạt ngay
        if (skill.targetType == SkillTargetType.Random || skill.targetType == SkillTargetType.SelfBuff)
        {
            ExecuteSkill(null, skill, dmgBonus, effectBonus);
        }
        else // SingleTarget → hiện mũi tên chọn mục tiêu
        {
            isCombatPaused = true;
            isTargetingMode = true;
            pendingSkill = skill;
            currentTarget = GetFirstAliveEnemy();
            MoveArrowToTarget(currentTarget);
            if (EventSystem.current != null)
                EventSystem.current.SetSelectedGameObject(null);
        }
    }

    /// <summary>Lấy một kẻ thù còn sống NGẪU NHIÊN</summary>
    private EnemyStats GetRandomAliveEnemy()
    {
        System.Collections.Generic.List<EnemyStats> alive =
            activeEnemies.FindAll(e => e.currentHP > 0);
        if (alive.Count == 0) return null;
        return alive[Random.Range(0, alive.Count)];
    }

    private void ExecuteSkill(EnemyStats target, SkillData skill, float flatDmg, float effectChance)
    {
        isTargetingMode = false;
        pendingSkill = null;
        if (targetArrow != null) targetArrow.SetActive(false);

        // Ghi nhận CD ngay khi kỹ năng được kích hoạt
        if (skill.cooldown > 0)
            skillCooldowns[skill.skillID] = skill.cooldown;

        bool delayedFinish = false;

        switch (skill.skillID)
        {
            case "Sword_Skill_1":
            {
                float[] mults = { 1f, 0.85f, 0.70f, 0.55f };
                StartCoroutine(ExecuteMultiHitSkill(mults, flatDmg));
                delayedFinish = true;
                break;
            }

            case "Sword_Skill_2":
                DealDamageToEnemy(target, 1f, flatDmg, effectChance, true);
                break;

            case "Sword_Skill_3":
            {
                DealDamageToEnemy(target, 1f, flatDmg, 0f, true);
                float secondMult = (100f - effectChance) / 100f;
                StartCoroutine(ExecuteDelayedHit(target, secondMult, flatDmg));
                delayedFinish = true;
                break;
            }

            case "Sword_Skill_4":
                PlayerManager.Instance.equipmentBleedChance += effectChance;
                break;
        }

        if (!delayedFinish)
        {
            PlayerManager.Instance.currentAP -= maxAP;
            TickDownSkillCooldowns();
            isCombatPaused = false;
        }
    }

    /// <summary>
    /// Giảm 1 lượt CD cho mọ kỹ năng đang hồi sau mỗi lượt.
    /// Gọi sau khi người chơi mất lượt (tấn công, kỹ năng, phòng thủ, chạy...).
    /// </summary>
    private void TickDownSkillCooldowns()
    {
        var keys = new System.Collections.Generic.List<string>(skillCooldowns.Keys);
        foreach (var id in keys)
        {
            skillCooldowns[id] = Mathf.Max(0, skillCooldowns[id] - 1);
            if (skillCooldowns[id] == 0) skillCooldowns.Remove(id);
        }
        // Làm mới hiển thị CD trên bảng kỹ năng nếu đang mở
        if (SkillMenuUI.Instance != null && SkillMenuUI.Instance.gameObject.activeSelf)
            SkillMenuUI.Instance.RefreshAllCDDisplays();
    }

    /// <summary>
    /// Coroutine: chém nhiều đòn vào mục tiêu NGẪU NHIÊN với delay giữa mỗi đòn.
    /// Dùng cho Sword_Skill_1 (4 đòn). FloatingText nhảy tại đúng vị trí quái bị trúng.
    /// </summary>
    private IEnumerator ExecuteMultiHitSkill(float[] multipliers, float flatDmg)
    {
        for (int i = 0; i < multipliers.Length; i++)
        {
            EnemyStats randomTarget = GetRandomAliveEnemy();
            if (randomTarget != null)
                DealDamageToEnemy(randomTarget, multipliers[i], flatDmg, 0f, true);
            yield return new WaitForSeconds(0.18f);
        }
        PlayerManager.Instance.currentAP -= maxAP;
        TickDownSkillCooldowns();
        isCombatPaused = false;
    }

    private IEnumerator ExecuteDelayedHit(EnemyStats target, float multiplier, float flatDmg)
    {
        yield return new WaitForSeconds(0.22f);
        if (target != null && target.currentHP > 0)
            DealDamageToEnemy(target, multiplier, flatDmg, 0f, true);
        PlayerManager.Instance.currentAP -= maxAP;
        TickDownSkillCooldowns();
        isCombatPaused = false;
    }
}