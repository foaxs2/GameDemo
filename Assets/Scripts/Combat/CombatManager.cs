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

    // [Test Data] Các biến này được phía Test SET vào
    public static bool isTestMode = false;
    public static string returnSceneName = "Dungeon";
    public static GameObject[] overrideEnemyPrefabs = null;
    public static bool autoWinCombat = false;

    // [Phase 3] Sinh quÃ¡i Ä‘Æ°á»£c quáº£n lÃ½ bá»Ÿi EncounterSpawner
    [Header("Encounter Spawner (Phase 3)")]
    public EncounterSpawner Spawner;

    // [Phase 4] Input & Targeting Ä‘Æ°á»£c quáº£n lÃ½ bá»Ÿi CombatInputController
    [Header("Input Controller (Phase 4)")]
    public CombatInputController Input;

    // [Phase 5] Skill execution Ä‘Æ°á»£c quáº£n lÃ½ bá»Ÿi SkillExecutor
    [Header("Skill Executor (Phase 5)")]
    public SkillExecutor Executor;

    /// <summary>Expose tráº¡ng thÃ¡i pause Ä‘á»ƒ CombatInputController kiá»ƒm tra.</summary>
    public bool IsCombatPaused => isCombatPaused;

    public List<EnemyStats> activeEnemies = new List<EnemyStats>();

    // Proxy properties cho EncounterSpawner truy cập targeting state
    public bool IsTargetingMode => Input != null ? Input.IsTargetingMode : false;
    public EnemyStats CurrentTarget => Input != null ? Input.CurrentTarget : null;
    public void SetCurrentTarget(EnemyStats t) { if (Input != null) Input.SetTarget(t); }

    [Header("Há»‡ thá»‘ng ATB")]
    private float maxAP = 100f;
    private bool isCombatPaused = false;
    public float tickSpeedMultiplier = 5f;
    private int playerWaitTurns = 0;
    /// <summary>Báº£o vá»‡ damage tick chá»‰ xáº£y ra 1 láº§n/lÆ°á»£t, dÃ¹ player dÃ¹ng Ä‘á»“ rá»“i resume.</summary>
    private bool playerTurnDamageTicked = false;

    // [Phase 2] ToÃ n bá»™ UI Ä‘Æ°á»£c quáº£n lÃ½ bá»Ÿi CombatUIManager
    [Header("UI Manager (Phase 2)")]
    public CombatUIManager UI;

    // Victory tracking
    private int earnedExp = 0;
    private int earnedGold = 0;

    [Header("Há»‡ thá»‘ng Ká»¹ nÄƒng")]
    public GameObject skillMenuPanel;
    private SkillData pendingSkill;
    public Dictionary<string, int> skillCooldowns = new Dictionary<string, int>();

    public void OnEquipmentButton()
    {
        UI.ShowActionMenu(false);
        isCombatPaused = true;

        EquipmentUI targetUI = EquipmentUI.Instance;
        if (targetUI == null) targetUI = Object.FindAnyObjectByType<EquipmentUI>(FindObjectsInactive.Include);

        if (targetUI != null && targetUI.rootCanvas != null) targetUI.rootCanvas.SetActive(true);
        else if (UI.equipmentCanvas != null) UI.equipmentCanvas.SetActive(true);
        else { isCombatPaused = false; UI.ShowActionMenu(true); }
    }

    public void ResumeCombat()
    {
        isCombatPaused = false;
    }

    private System.Collections.IEnumerator AutoWinAfterSpawn()
    {
        yield return new WaitForSeconds(0.1f); // Đợi ngắn để đảm bảo quái và UI đã khởi tạo đầy đủ
        if (activeEnemies != null)
        {
            foreach (var enemy in activeEnemies)
            {
                if (enemy != null)
                    enemy.currentHP = 0;
            }
            yield return null;
            CheckBattleEnd();
        }
    }

    private void Awake()
    {
        string sceneName = SceneManager.GetActiveScene().name;
        if (sceneName != "Combat" && sceneName != "TestCombat")
        {
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
        // [Phase 2] Auto-tìm CombatUIManager nếu chưa gán
        if (UI == null) UI = GetComponent<CombatUIManager>();
        if (UI == null) UI = gameObject.AddComponent<CombatUIManager>();

        // [Phase 3] Auto-tìm EncounterSpawner nếu chưa gán
        if (Spawner == null) Spawner = GetComponent<EncounterSpawner>();
        if (Spawner == null) Spawner = gameObject.AddComponent<EncounterSpawner>();

        if (overrideEnemyPrefabs != null)
        {
            Spawner.RunOverrideEncounter(overrideEnemyPrefabs);
            overrideEnemyPrefabs = null;
        }
        else if (returnSceneName == "TestDungeon" || !isTestMode)
        {
            Random.State oldState = Random.state;
            int encounterSeed = PlayerMovement.currentMapSeed + (PlayerMovement.combatEnemyPosition.x * 37) + (PlayerMovement.combatEnemyPosition.y * 101);
            Random.InitState(encounterSeed);
            Spawner.RunEncounter();
            Random.state = oldState;
        }

        if (autoWinCombat)
        {
            StartCoroutine(AutoWinAfterSpawn());
            autoWinCombat = false;
        }

        // [Phase 4] Auto-tÃ¬m CombatInputController náº¿u chÆ°a gÃ¡n
        if (Input == null) Input = GetComponent<CombatInputController>();
        if (Input == null) Input = gameObject.AddComponent<CombatInputController>();

        // [Phase 5] Auto-tÃ¬m SkillExecutor náº¿u chÆ°a gÃ¡n
        if (Executor == null) Executor = GetComponent<SkillExecutor>();
        if (Executor == null) Executor = gameObject.AddComponent<SkillExecutor>();

        // [VFX] Auto-gắn UIShake & CombatVFX
        if (UIShake.Instance == null) gameObject.AddComponent<UIShake>();
        if (CombatVFX.Instance == null) gameObject.AddComponent<CombatVFX>();

        UI.Initialize();
        UI.UpdatePlayerUI();
        // UI hover events Ä‘Æ°á»£c setup bá»Ÿi CombatInputController.Start()

        if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);
    }

    void Update()
    {
        if (!isCombatPaused && EventSystem.current != null && EventSystem.current.currentSelectedGameObject != null)
            EventSystem.current.SetSelectedGameObject(null);

        // [Phase 4] Input handling Ä‘Æ°á»£c xá»­ lÃ½ bá»Ÿi CombatInputController (cháº¡y Ä‘á»™c láº­p)

        // === Ká»‚M TRA PAUSE CHá»ˆ Äá»‚ Dá»ªNG ATB ===
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

            enemy.currentAP += enemy.GetTotalSpeed() * Time.deltaTime * tickSpeedMultiplier;

            if (enemy.currentAP > highestAP)
            {
                highestAP = enemy.currentAP;
                readyUnit = enemy;
            }
        }

        UI.UpdateAPUI();

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

            // Player: damage Poison/Burn chá»‰ báº¯n 1 láº§n Ä‘áº§u lÆ°á»£t, khÃ´ng báº¯n láº¡i khi dÃ¹ng Ä‘á»“ rá»“i resume.
            if (readyUnit == PlayerManager.Instance)
            {
                if (!playerTurnDamageTicked)
                {
                    DebuffManager.Instance.ProcessTurnDamageOnly(PlayerManager.Instance);
                    playerTurnDamageTicked = true;
                }
            }
            else
            {
                DebuffManager.Instance.ProcessTurnStart(readyUnit);
                BuffManager.Instance?.ProcessUnitTurnTick(readyUnit);
            }
            UI.UpdatePlayerUI();

            if (PlayerManager.Instance.currentHP <= 0) { CheckPlayerDeath(); return; }
            if (readyUnit.currentHP <= 0) { isCombatPaused = false; return; }

            if (readyUnit == PlayerManager.Instance) PlayerTurn();
            else StartCoroutine(EnemyTurnCoroutine((EnemyStats)readyUnit));
        }
    }

    // [Phase 4] HandleActionMenuKeyboard, HandleTargetingKeyboard, ConfirmTargeting,
    // SelectNextEnemy, SelectPreviousEnemy, CancelTargeting, HandleRightClick
    // Ä‘Ã£ chuyá»ƒn vÃ o CombatInputController.

    /// <summary>Bridge: CombatInputController gá»i khi xÃ¡c nháº­n táº¥n cÃ´ng thÆ°á¤ng.</summary>
    public void ExecutePlayerAttackFromInput(EnemyStats target) => ExecutePlayerAttack(target);

    /// <summary>Bridge: CombatInputController gá»i khi xÃ¡c nháº­n dÃ¹ng skill.</summary>
    public void ExecuteSkillFromInput(EnemyStats target, SkillData skill, float dmgBonus, float effBonus)
        => ExecuteSkill(target, skill, dmgBonus, effBonus);

    public void OnSkillMenuButton()
    {
        UI.ShowActionMenu(false);
        isCombatPaused = true;

        SkillMenuUI ui = (UI.skillMenuPanel != null) ? UI.skillMenuPanel.GetComponent<SkillMenuUI>() : SkillMenuUI.Instance;
        if (ui != null) ui.Open();
        else { ui = Object.FindAnyObjectByType<SkillMenuUI>(FindObjectsInactive.Include); if (ui != null) ui.Open(); }
    }

    public void CancelSkillMenu()
    {
        SkillMenuUI ui = (UI.skillMenuPanel != null) ? UI.skillMenuPanel.GetComponent<SkillMenuUI>() : SkillMenuUI.Instance;
        if (ui == null) ui = Object.FindAnyObjectByType<SkillMenuUI>(FindObjectsInactive.Include);
        if (ui != null) ui.Close();
        isCombatPaused = false;
        UI.ShowActionMenu(true);
    }

    // [Phase 3] SpawnRandomEnemy, SpawnBossEncounter, SpawnSpecificEnemy
    // Ä‘Ã£ chuyá»ƒn vÃ o EncounterSpawner.

    public EnemyStats GetFirstAliveEnemy()
    {
        foreach (var enemy in activeEnemies)
            if (enemy.currentHP > 0) return enemy;
        return null;
    }

    // ─── [Phase 5] Public bridges cho SkillAction ─────────────────────────
    public bool   CheckBattleEndPublic()              => CheckBattleEnd();
    public EnemyStats GetRandomAliveEnemyPublic()     => GetRandomAliveEnemy();
    public void   InvokeApplyOnHitEffects(EnemyStats t, float raw) => ApplyOnHitEffects(t, raw);
    public void   InvokeEnemyNormalAttack(EnemyStats a)            => ExecuteEnemyNormalAttack(a);
    public void   UpdatePlayerUIPublic()              => UpdatePlayerUI();
    public void   CheckPlayerDeathPublic()            => CheckPlayerDeath();

    bool CheckBattleEnd()
    {
        if (activeEnemies == null || activeEnemies.Count == 0) return false;

        // ── Sinh Đàn check: trước khi kết luận allDead ──
        if (BossPassiveManager.Instance != null && BossPassiveManager.Instance.CheckSpawnOnAllyDeath(this))
        {
            BossFloorConfig config = Spawner?.GetBossFloorConfig(PlayerMovement.currentFloor);
            GameObject summonPrefab = (config != null && config.summonOnAlliesDeadPrefab != null)
                ? config.summonOnAlliesDeadPrefab
                : Spawner?.FindNormalPrefabByName("nhenhang");

            if (summonPrefab == null)
                summonPrefab = Spawner?.FindNormalPrefabByName("nhen");

            if (summonPrefab != null && Spawner != null)
            {
                Spawner.SpawnBossSinhDanMinions(summonPrefab);
                if (FloatingTextManager.Instance != null)
                    FloatingTextManager.Instance.SpawnText(Vector3.zero, "🕷️ Sinh Đàn: Triệu hồi 2 Quái Con!", Color.red);
            }
            return false;
        }

        bool allDead = true;
        foreach (var enemy in activeEnemies)
            if (enemy.currentHP > 0) allDead = false;

        if (allDead)
        {
            isCombatPaused = true;
            earnedExp = 0;
            earnedGold = 0;
            int killedThisBattle = 0;
            foreach (var enemy in activeEnemies)
            {
                if (enemy == null) continue;
                earnedExp += enemy.expDrop;
                earnedGold += enemy.goldDrop;
                killedThisBattle++;
                
                // Cập nhật số lượng quái diệt được cho hệ thống Nhiệm Vụ Guild
                if (PlayerManager.Instance != null)
                {
                    string mName = enemy.enemyName;
                    if (!PlayerManager.Instance.killedMonsters.ContainsKey(mName))
                        PlayerManager.Instance.killedMonsters[mName] = 0;
                    PlayerManager.Instance.killedMonsters[mName]++;
                }
            }

            PlayerMovement.floorMonstersKilled += killedThisBattle;

            UI.ShowVictoryPanel(earnedExp, earnedGold);
            if (UI.victoryPanel == null) OnVictoryContinueButton();
            return true;
        }
        return false;
    }

    public void OnVictoryContinueButton()
    {
        if (PlayerManager.Instance != null)
        {
            BuffManager.Instance?.ClearAllBuffs(PlayerManager.Instance); // Xóa buff khi thắng
            PlayerManager.Instance.AddExp(earnedExp);
            PlayerManager.Instance.gold += earnedGold;
            // Track floor gold/EXP earned for death penalty
            PlayerMovement.floorGoldEarned += earnedGold;
            PlayerMovement.floorExpEarned  += earnedExp;
        }
        PlayerMovement.defeatedEnemiesTiles.Add(PlayerMovement.combatEnemyPosition);
        PlayerMovement.wasBossFight = PlayerMovement.isBossFight;
        PlayerMovement.isBossFight = false;
        PlayerMovement.isEliteFight = false;

        string target = returnSceneName;
        returnSceneName = "Dungeon";
        SaveSystem.Instance?.Save(); // Lưu lại tiến trình ngay sau khi diệt quái
        SceneManager.LoadScene(target);
    }

    void UpdatePlayerUI() => UI.UpdatePlayerUI();
    public void ForceUpdatePlayerUI() => UI.UpdatePlayerUI();
    void UpdateAPUI() => UI.UpdateAPUI();

    void PlayerTurn()
    {
        UI.ShowActionMenu(true);
        PlayerManager.Instance.isDefending = false;
        Input?.ResetTargeting();
        if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);
    }

    // [MỤC 10] Chuyển sang Coroutine để hỗ trợ flash effect và pacing dừng nghỉ
    IEnumerator EnemyTurnCoroutine(EnemyStats attacker)
    {
        bool isBoss = attacker.hasEnrageOnLowHP || attacker.hasRoarPassive ||
                      attacker.hasPassivePoison || attacker.hasSpawnOnAllyDeath;

        // ── Boss Passives: Enrage + Roar (trước khi hành động) ──
        if (isBoss)
        {
            BossPassiveManager.Instance?.CheckAllBossPassives(attacker);
            BossPassiveManager.Instance?.IncrementBossTurnCounter(attacker);
        }

        bool hasBocGiap = BuffManager.Instance != null && BuffManager.Instance.HasBuff(attacker, BuffType.BocGiap);

        if (hasBocGiap)
        {
            // Trong 3 lượt Bọc Giáp, Tê Tê KHÔNG tấn công người chơi.
            // Số lượt Bọc Giáp đã được ProcessUnitTurnTick() tự động giảm 1 lượt khi bắt đầu lượt này.
            UI.UpdatePlayerUI();
            CheckPlayerDeath();

            attacker.currentAP -= maxAP;
            if (playerWaitTurns > 0) playerWaitTurns--;

            isCombatPaused = false;
            yield break;
        }

        if (attacker.currentCooldown > 0) attacker.currentCooldown--;

        // Tìm EnemyUI tương ứng với attacker để điều khiển bong bóng cảnh báo và flash
        EnemyUI enemyUI = null;
        if (UI != null)
        {
            foreach (var eUI in Object.FindObjectsByType<EnemyUI>(FindObjectsSortMode.None))
            {
                if (eUI.stats == attacker) { enemyUI = eUI; break; }
            }
        }

        // Hiển thị bong bóng cảnh báo TRƯỚC 1 LƯỢT (currentCooldown == 1)
        if (attacker.maxCooldown > 0 && attacker.currentCooldown == 1)
        {
            if (enemyUI != null)
                enemyUI.SetSkillWarning(true, attacker.skillIcon);
        }

        // [MỤC 10] Hiệu ứng sáng lóe khi kẻ thù tấn công
        if (enemyUI != null)
            yield return StartCoroutine(enemyUI.FlashAttack());

        bool usedSkill = false;
        if (attacker.maxCooldown > 0 && attacker.currentCooldown <= 0)
        {
            // Ẩn bong bóng cảnh báo khi quái tung chiêu
            if (enemyUI != null)
                enemyUI.SetSkillWarning(false);

            // Kích hoạt skill 100% cho cả quái thường và Boss
            ExecuteEnemySkill(attacker);
            usedSkill = true;
            attacker.currentCooldown = attacker.maxCooldown;
        }

        if (!usedSkill) ExecuteEnemyNormalAttack(attacker);

        UI.UpdatePlayerUI();
        CheckPlayerDeath();

        // [MỤC 10] Nhịp độ combat: Dừng/chờ nhẹ một khoảng ngắn trước khi chuyển sang lượt tiếp theo
        yield return new WaitForSeconds(0.25f);

        attacker.currentAP -= maxAP;
        if (playerWaitTurns > 0) playerWaitTurns--;

        isCombatPaused = false;
    }

    // [Phase 2] ShowBossWarning vÃ  HideBossWarning Ä‘Ã£ chuyá»ƒn vÃ o CombatUIManager.

    public void OnAttackButton()
    {
        UI.ShowActionMenu(false);
        EnemyStats first = GetFirstAliveEnemy();
        if (first == null) { UI.ShowActionMenu(true); return; }
        Input?.BeginTargeting(first);
    }

    public void OnItemButton()
    {
        UI.ShowActionMenu(false);
        isCombatPaused = true;
        InventoryUI targetUI = InventoryUI.Instance;
        if (targetUI == null) targetUI = Object.FindAnyObjectByType<InventoryUI>(FindObjectsInactive.Include);
        if (targetUI != null) targetUI.ToggleInventory();
        else { isCombatPaused = false; UI.ShowActionMenu(true); }
    }

    public void OnEnemyClicked(EnemyStats clickedEnemy)
    {
        if (clickedEnemy == null || clickedEnemy.currentHP <= 0) return;

        if (Input != null && Input.IsTargetingMode)
        {
            Input.SetTarget(clickedEnemy);
            UI.MoveArrowToTarget(clickedEnemy);
            Input.ConfirmTargeting();
        }
        else UI.ToggleEnemyInfo(clickedEnemy, false);
    }

    // [Phase 2] MoveArrowToTarget đã chuyển vào CombatUIManager.

    private void ExecutePlayerAttack(EnemyStats target)
    {
        StartCoroutine(ExecutePlayerAttackRoutine(target));
    }

    private IEnumerator ExecutePlayerAttackRoutine(EnemyStats target)
    {
        Input?.ResetTargeting();

        // [Phase 1] Gọi CombatCalculator thay vì tự tính
        var result = CombatCalculator.CalculateNormalAttack(PlayerManager.Instance, target);

        if (result.missed)
        {
            UI.ShowMissText(target);
        }
        else
        {
            // Gọi TakeDamage để EnemyStats tự xử lý giảm HP, phản sát thương Bọc Giáp & giảm lượt buff do bị đánh
            target.TakeDamage(result.rawDamage, false, false);

            Color dmgColor = result.isCrit ? Color.yellow : Color.white;
            Transform targetTransform = UI.GetEnemyUITransform(target);
            FloatingTextManager.Instance?.SpawnText(targetTransform.position, result.finalDamage.ToString(), dmgColor);

            // [VFX] Hiệu ứng chém tại vị trí quái khi đánh thường
            RectTransform targetUI = targetTransform as RectTransform;
            if (CombatVFX.Instance != null && targetUI != null)
                CombatVFX.Instance.PlayVFX(VFXType.SlashNormal, targetUI);

            // [Hiệu ứng Trúng Đòn] Cho quái nháy đỏ nhạt khi bị Player đánh trúng (như Darkest Dungeon)
            EnemyUI eUI = targetUI != null ? targetUI.GetComponentInParent<EnemyUI>() : null;
            if (eUI != null) eUI.FlashHit();

            ApplyOnHitEffects(target, result.rawDamage);
        }

        // [MỤC 10] Chờ hoạt ảnh chém của Player hoàn tất trước khi nhường lượt cho kẻ thù
        yield return new WaitForSeconds(0.35f);

        // Countdown duration debuff và buff (chỉ khi Attack, không khi dùng đồ)
        DebuffManager.Instance?.ProcessTurnDurationTick(PlayerManager.Instance);
        BuffManager.Instance?.ProcessUnitTurnTick(PlayerManager.Instance);

        playerTurnDamageTicked = false; // Reset cho lượt tiếp theo
        PlayerManager.Instance.currentAP -= maxAP;
        TickDownSkillCooldowns();
        isCombatPaused = false;
    }

    public void OnDefendButton()
    {
        UI.ShowActionMenu(false);
        PlayerManager.Instance.isDefending = true;

        // Thêm Buff DEF_Up (giảm 50% DMG nhận vào + 1 DEF) và EVA_Up (+25% né) duy trì trong 2 lượt
        if (BuffManager.Instance != null)
        {
            BuffManager.Instance.AddBuff(PlayerManager.Instance, BuffType.DEF_Up, 0.5f, 2, 1);
            BuffManager.Instance.AddBuff(PlayerManager.Instance, BuffType.EVA_Up, 25f, 2, 1);
        }

        playerTurnDamageTicked = false;
        PlayerManager.Instance.currentAP -= maxAP;
        TickDownSkillCooldowns();
        isCombatPaused = false;
    }

    public void OnFleeButton()
    {
        UI.ShowActionMenu(false);
        isCombatPaused = true;

        float fleeChance = CombatCalculator.CalculateFleeChance(PlayerManager.Instance);

        if (Random.Range(0f, 100f) <= fleeChance)
        {
            PlayerMovement.isReturningFromCombat = true;
            PlayerMovement.isBossFight = false;
            PlayerMovement.isEliteFight = false;
            PlayerMovement.wasBossFight = false;
            string target = returnSceneName;
            returnSceneName = "Dungeon";
            SceneManager.LoadScene(target);
        }
        else
        {
            UI.ShowFloatingText("Chạy thất bại!", Color.red);
            PlayerManager.Instance.currentAP = 0;
            playerWaitTurns = 0;
            foreach (var e in activeEnemies) if (e.currentHP > 0) playerWaitTurns++;
            isCombatPaused = false;
        }
    }

    // [Phase 2] ToggleEnemyInfo đã chuyển vào CombatUIManager.

    void ExecuteEnemyNormalAttack(EnemyStats attacker)
    {
        var result = CombatCalculator.CalculateEnemyAttack(attacker, PlayerManager.Instance);
        if (result.missed) { UI.ShowMissText(PlayerManager.Instance); return; }

        PlayerManager.Instance.TakeDamage(result.rawDamage, false, false);

        Color dmgColor = result.isCrit ? Color.yellow : Color.white;
        FloatingTextManager.Instance?.SpawnText(UI.GetPlayerHPBarTransform().position, result.displayDamage.ToString(), dmgColor);

        // [FlashHit] PlayerIcon chớp đỏ khi bị quái đánh trúng
        UI?.FlashPlayerHit();

        // [VFX] Hiệu ứng trúng đòn mạnh CHỈ xuất hiện khi bị đánh Chí Mạng (Crit)
        if (result.isCrit)
        {
            RectTransform playerUI = UI?.GetPlayerIconTransform() as RectTransform;
            if (CombatVFX.Instance != null && playerUI != null)
                CombatVFX.Instance.PlayVFX(VFXType.HitHeavy, playerUI);
        }

        if (result.isCrit && UnityEngine.Random.Range(0f, 100f) <= 12f) PlayerManager.Instance.ReduceSanity(1);
        // [VFX] Rung màn hình khi bị quái đánh (đòn thường rung nhẹ 10f, crit rung mạnh 20f)
        if (UIShake.Instance != null)
        {
            float mag = result.isCrit ? 20f : 10f;
            UIShake.Instance.Shake(0.2f, mag);
        }

        BossPassiveManager.Instance?.ApplyPassiveEffects(attacker);
    }

    void ExecuteEnemySkill(EnemyStats attacker)
    {
        // [Phase 5] Delegate to SkillExecutor
        if (Executor != null)
        {
            bool isAsync = Executor.RunEnemySkill(attacker.skillID, attacker);
            // Async skills (FlameBreath) tự giải phóng isCombatPaused qua coroutine
            return;
        }
        // Fallback nếu Executor chưa sẵn (không nên xảy ra)
        ExecuteEnemyNormalAttack(attacker);
    }


    void CheckPlayerDeath()
    {
        if (PlayerManager.Instance.currentHP <= 0)
        {
            isCombatPaused = true;
            UI.ShowDeathPanel();
        }
    }

    public void OnDeathReturnTown()
    {
        BuffManager.Instance?.ClearAllBuffs(PlayerManager.Instance); // Xóa buff khi thua
        if (returnSceneName == "TestDungeon" || returnSceneName == "TestCombat")
        {
            PlayerManager.Instance.UpdateMaxHP();
            PlayerManager.Instance.currentHP = PlayerManager.Instance.maxHP;
            PlayerManager.Instance.sen = PlayerManager.Instance.maxSen;
            PlayerManager.Instance.food = PlayerManager.Instance.maxFood;
            PlayerManager.Instance.RestoreSanityCollapse();
            string target = returnSceneName;
            returnSceneName = "Dungeon";
            SceneManager.LoadScene(target);
            return;
        }

        // Apply death penalty: lose half gold & EXP earned this floor
        PlayerMovement.ApplyDeathPenalty();
        // Hồi full HP, SEN giữ nguyên
        PlayerManager.Instance.currentHP = PlayerManager.Instance.maxHP;
        DeathContext.Pending = DeathContext.DeathType.CombatDeath;
        SaveSystem.Instance?.Save();
        SceneManager.LoadScene("Town");
    }

    public void OnMadnessReturnTown()
    {
        BuffManager.Instance?.ClearAllBuffs(PlayerManager.Instance); // Xóa buff khi thua
        if (returnSceneName == "TestDungeon" || returnSceneName == "TestCombat")
        {
            PlayerManager.Instance.UpdateMaxHP();
            PlayerManager.Instance.currentHP = PlayerManager.Instance.maxHP;
            PlayerManager.Instance.sen = PlayerManager.Instance.maxSen;
            PlayerManager.Instance.food = PlayerManager.Instance.maxFood;
            PlayerManager.Instance.RestoreSanityCollapse();
            string target = returnSceneName;
            returnSceneName = "Dungeon";
            SceneManager.LoadScene(target);
            return;
        }

        // Apply death penalty: lose half gold & EXP earned this floor
        PlayerMovement.ApplyDeathPenalty();
        // HP giữ nguyên như lúc ở trong combat, SEN = 2
        PlayerManager.Instance.sen = 2;
        DeathContext.Pending = DeathContext.DeathType.CombatMadness;
        SaveSystem.Instance?.Save();
        SceneManager.LoadScene("Town");
    }

    /// <summary>
    /// ÄÆ°á»£c gá»i tá»« PlayerManager.OnSanityCollapse() khi SEN = 0 trong scene Combat.
    /// Dá»«ng combat vÃ  hiá»‡n báº£ng phÃ¡t Ä‘iÃªn.
    /// </summary>
    public void ShowMadnessPanel()
    {
        isCombatPaused = true;
        UI.ShowMadnessPanelUI();
    }

    // [Phase 2] ShowMissText vÃ  GetEnemyUITransform Ä‘Ã£ chuyá»ƒn vÃ o CombatUIManager.

    /// <summary>
    /// [Phase 1] Gá»i CombatCalculator.CalculatePlayerDamage() Ä‘á»ƒ tÃ­nh toÃ¡n,
    /// rá»“i Ã¡p dá»¥ng káº¿t quáº£ (trá»« HP, hiá»‡n FloatingText, on-hit effects).
    /// </summary>
    private void DealDamageToEnemy(EnemyStats target, float damageMultiplier, float flatBonus, float extraCritChance, bool triggerOnHit)
    {
        var result = CombatCalculator.CalculatePlayerDamage(
            PlayerManager.Instance, target, damageMultiplier, flatBonus, extraCritChance);

        if (result.missed) { UI.ShowMissText(target); return; }

        // Gọi TakeDamage để EnemyStats tự xử lý giảm HP, phản sát thương Bọc Giáp & giảm lượt buff do bị đánh
        target.TakeDamage(result.rawDamage, false, false);

        Color dmgColor = result.isCrit ? Color.yellow : Color.white;
        Transform targetTransform = UI.GetEnemyUITransform(target);
        FloatingTextManager.Instance?.SpawnText(targetTransform.position, result.finalDamage.ToString(), dmgColor);

        // [VFX] Hiệu ứng chém tại vị trí quái
        RectTransform targetUI = targetTransform as RectTransform;
        if (CombatVFX.Instance != null && targetUI != null)
            CombatVFX.Instance.PlayVFX(VFXType.SlashNormal, targetUI);

        if (result.isCrit) BossPassiveManager.Instance?.OnPlayerCritHit(target);
        if (triggerOnHit) ApplyOnHitEffects(target, result.rawDamage);
    }

    /// <summary>
    /// [Phase 1] Helper: Ã¡p dá»¥ng cÃ¡c on-hit effect (Bleed trang bá»‹, Weapon Coating, Stun).
    /// </summary>
    private void ApplyOnHitEffects(EnemyStats target, float rawDamage)
    {
        if (UnityEngine.Random.Range(0f, 100f) <= PlayerManager.Instance.equipmentBleedChance)
            DebuffManager.Instance?.AddDebuff(target, DebuffType.Bleed, 3, 1, rawDamage * 0.2f);

        if (BuffManager.Instance != null)
        {
            var coating = BuffManager.Instance.GetWeaponCoating(PlayerManager.Instance);
            float coatChance = BuffManager.Instance.GetWeaponCoatingChance(PlayerManager.Instance);
            if (coating == WeaponCoatingType.Bleed && UnityEngine.Random.Range(0f, 100f) <= coatChance)
                DebuffManager.Instance?.AddDebuff(target, DebuffType.Bleed, 3, 1, rawDamage * 0.2f);
        }

        if (UnityEngine.Random.Range(0f, 100f) <= PlayerManager.Instance.equipmentStunChance)
            DebuffManager.Instance?.AddDebuff(target, DebuffType.Stun, 1);
    }

    public void OnSkillButton(SkillData skill)
    {
        if (SkillMenuUI.Instance != null) SkillMenuUI.Instance.Close();
        else if (UI.skillMenuPanel != null) UI.skillMenuPanel.SetActive(false);

        if (skill == null) return;

        int skillLevel = SkillManager.Instance != null ? SkillManager.Instance.GetSkillLevel(skill.skillID) : 0;
        if (skillLevel == 0) { CancelSkillMenu(); return; }

        float dmgBonus = skill.damageBonusPerLevel[skillLevel - 1];
        float effectBonus = skill.effectChancePerLevel[skillLevel - 1];

        if (skill.targetType == SkillTargetType.Random || skill.targetType == SkillTargetType.SelfBuff)
            ExecuteSkill(null, skill, dmgBonus, effectBonus);
        else
        {
            isCombatPaused = true;
            EnemyStats first = GetFirstAliveEnemy();
            Input?.BeginSkillTargeting(first, skill);
        }
    }

    private EnemyStats GetRandomAliveEnemy()
    {
        System.Collections.Generic.List<EnemyStats> alive = activeEnemies.FindAll(e => e.currentHP > 0);
        if (alive.Count == 0) return null;
        return alive[Random.Range(0, alive.Count)];
    }

    private void ExecuteSkill(EnemyStats target, SkillData skill, float flatDmg, float effectChance)
    {
        Input?.ResetTargeting();
        if (skill.cooldown > 0) skillCooldowns[skill.skillID] = skill.cooldown;

        // [Phase 5] Delegate to SkillExecutor
        bool isAsync = Executor != null
            ? Executor.RunPlayerSkill(skill.skillID, target, flatDmg, effectChance)
            : false;

        if (!isAsync) FinishPlayerTurnAfterSkill();
    }

    /// <summary>[Phase 5] Hoàn tất lượt player sau khi skill đồng bộ kết thúc.</summary>
    public void FinishPlayerTurnAfterSkill()
    {
        StartCoroutine(FinishPlayerTurnRoutine());
    }

    private IEnumerator FinishPlayerTurnRoutine()
    {
        // [MỤC 10] Chờ hoạt ảnh kỹ năng của Player hoàn tất trước khi nhường lượt cho kẻ thù
        yield return new WaitForSeconds(0.35f);

        DebuffManager.Instance?.ProcessTurnDurationTick(PlayerManager.Instance);
        BuffManager.Instance?.ProcessUnitTurnTick(PlayerManager.Instance);
        playerTurnDamageTicked = false;
        PlayerManager.Instance.currentAP -= maxAP;
        TickDownSkillCooldowns();
        isCombatPaused = false;
    }


    private void TickDownSkillCooldowns()
    {
        // Tick skill cooldowns
        var keys = new System.Collections.Generic.List<string>(skillCooldowns.Keys);
        foreach (var id in keys)
        {
            skillCooldowns[id] = Mathf.Max(0, skillCooldowns[id] - 1);
            if (skillCooldowns[id] == 0) skillCooldowns.Remove(id);
        }

        // TickDown buffs player Ä‘Ã£ Ä‘Æ°á»£c chuyá»ƒn sang BuffManager.ProcessUnitTurnTick(readyUnit) táº¡i lÃºc ProcessTurnStart

        if (SkillMenuUI.Instance != null && SkillMenuUI.Instance.gameObject.activeSelf)
            SkillMenuUI.Instance.RefreshAllCDDisplays();
    }
    // [Phase 5] ExecuteMultiHitSkill, ExecuteDelayedHit, ExecuteDragonFlameBreath
    // da chuyen vao PlayerSkills.cs / EnemySkills.cs (SkillAction subclasses).
}
