﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;

public class CombatManager : MonoBehaviour
{
    public static CombatManager Instance { get; private set; }

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

    // Proxy properties cho EncounterSpawner truy cáº­p targeting state
    public bool IsTargetingMode => Input != null ? Input.IsTargetingMode : false;
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

    private void Awake()
    {
        if (SceneManager.GetActiveScene().name != "Combat")
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
        // [Phase 2] Auto-tÃ¬m CombatUIManager náº¿u chÆ°a gÃ¡n
        if (UI == null) UI = GetComponent<CombatUIManager>();
        if (UI == null) UI = gameObject.AddComponent<CombatUIManager>();

        // [Phase 3] Auto-tÃ¬m EncounterSpawner náº¿u chÆ°a gÃ¡n
        if (Spawner == null) Spawner = GetComponent<EncounterSpawner>();
        if (Spawner == null) Spawner = gameObject.AddComponent<EncounterSpawner>();

        Random.State oldState = Random.state;
        int encounterSeed = PlayerMovement.currentMapSeed + (PlayerMovement.combatEnemyPosition.x * 37) + (PlayerMovement.combatEnemyPosition.y * 101);
        Random.InitState(encounterSeed);

        Spawner.RunEncounter();

        PlayerMovement.isBossFight = false;
        Random.state = oldState;

        // [Phase 4] Auto-tÃ¬m CombatInputController náº¿u chÆ°a gÃ¡n
        if (Input == null) Input = GetComponent<CombatInputController>();
        if (Input == null) Input = gameObject.AddComponent<CombatInputController>();

        // [Phase 5] Auto-tÃ¬m SkillExecutor náº¿u chÆ°a gÃ¡n
        if (Executor == null) Executor = GetComponent<SkillExecutor>();
        if (Executor == null) Executor = gameObject.AddComponent<SkillExecutor>();

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
            else EnemyTurn((EnemyStats)readyUnit);
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

    // â”€â”€â”€ [Phase 5] Public bridges cho SkillAction â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
    public bool   CheckBattleEndPublic()              => CheckBattleEnd();
    public EnemyStats GetRandomAliveEnemyPublic()     => GetRandomAliveEnemy();
    public void   InvokeApplyOnHitEffects(EnemyStats t, float raw) => ApplyOnHitEffects(t, raw);
    public void   InvokeEnemyNormalAttack(EnemyStats a)            => ExecuteEnemyNormalAttack(a);
    public void   UpdatePlayerUIPublic()              => UpdatePlayerUI();
    public void   CheckPlayerDeathPublic()            => CheckPlayerDeath();

    bool CheckBattleEnd()
    {
        if (activeEnemies == null || activeEnemies.Count == 0) return false;

        // â”€â”€ Sinh ÄÃ n check: trÆ°á»›c khi káº¿t luáº­n allDead â”€â”€
        if (BossPassiveManager.Instance != null && BossPassiveManager.Instance.CheckSpawnOnAllyDeath(this))
        {
            // [Phase 3] DÃ¹ng Spawner Ä‘á»ƒ spawn Nhá»‡n Hang thay tháº¿
            GameObject nhenHangPrefab = Spawner?.FindNormalPrefabByName("nhenhang");
            if (nhenHangPrefab != null)
            {
                Spawner.SpawnSpecificEnemy(nhenHangPrefab);
                Spawner.SpawnSpecificEnemy(nhenHangPrefab);
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
            foreach (var enemy in activeEnemies)
            {
                earnedExp += enemy.expDrop;
                earnedGold += enemy.goldDrop;
                
                // Cáº­p nháº­t sá»‘ lÆ°á»£ng quÃ¡i diá»‡t Ä‘Æ°á»£c cho há»‡ thá»‘ng Nhiá»‡m Vá»¥ Guild
                if (PlayerManager.Instance != null)
                {
                    string mName = enemy.enemyName;
                    if (!PlayerManager.Instance.killedMonsters.ContainsKey(mName))
                        PlayerManager.Instance.killedMonsters[mName] = 0;
                    PlayerManager.Instance.killedMonsters[mName]++;
                }
            }

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
            BuffManager.Instance?.ClearAllBuffs(PlayerManager.Instance); // XÃ³a buff khi tháº¯ng
            PlayerManager.Instance.AddExp(earnedExp);
            PlayerManager.Instance.gold += earnedGold;
            // Track floor gold/EXP earned for death penalty
            PlayerMovement.floorGoldEarned += earnedGold;
            PlayerMovement.floorExpEarned  += earnedExp;
        }
        PlayerMovement.defeatedEnemiesTiles.Add(PlayerMovement.combatEnemyPosition);
        SceneManager.LoadScene("Dungeon");
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

    void EnemyTurn(EnemyStats attacker)
    {
        bool isBoss = attacker.hasEnrageOnLowHP || attacker.hasRoarPassive ||
                      attacker.hasPassivePoison || attacker.hasSpawnOnAllyDeath;

        // â”€â”€ Boss Passives: Enrage + Roar (trÆ°á»›c khi hÃ nh Ä‘á»™ng) â”€â”€
        if (isBoss)
        {
            BossPassiveManager.Instance?.CheckAllBossPassives(attacker);
            BossPassiveManager.Instance?.IncrementBossTurnCounter(attacker);
        }

        if (attacker.currentCooldown > 0) attacker.currentCooldown--;

        // Hiá»‡n cáº£nh bÃ¡o TRÆ¯á»šC 1 LÆ¯á»¢T khi boss chuáº©n bá»‹ tung chiÃªu (CD == 1)
        if (isBoss && attacker.maxCooldown > 0 && attacker.currentCooldown == 1
            && !string.IsNullOrEmpty(attacker.bossSkillWarningText))
            UI.ShowBossWarning(attacker.bossSkillWarningText);

        bool usedSkill = false;
        if (attacker.maxCooldown > 0 && attacker.currentCooldown == 0)
        {
            // Boss luÃ´n dÃ¹ng skill 100%; quÃ¡i thÆ°á»ng 70%
            float skillChance = isBoss ? 100f : 70f;
            if (Random.Range(0f, 100f) <= skillChance)
            {
                // KhÃ´ng hiá»‡n cáº£nh bÃ¡o á»Ÿ Ä‘Ã¢y ná»¯a (cáº£nh bÃ¡o Ä‘Ã£ hiá»‡n lÆ°á»£t trÆ°á»›c)
                ExecuteEnemySkill(attacker);
                usedSkill = true;
            }
            else attacker.currentCooldown = attacker.maxCooldown;
        }

        if (!usedSkill) ExecuteEnemyNormalAttack(attacker);

        UI.UpdatePlayerUI();
        CheckPlayerDeath();

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

    // [Phase 2] MoveArrowToTarget Ä‘Ã£ chuyá»ƒn vÃ o CombatUIManager.

    private void ExecutePlayerAttack(EnemyStats target)
    {
        Input?.ResetTargeting();

        // [Phase 1] Gá»i CombatCalculator thay vÃ¬ tá»± tÃ­nh
        var result = CombatCalculator.CalculateNormalAttack(PlayerManager.Instance, target);

        if (result.missed)
        {
            UI.ShowMissText(target);
        }
        else
        {
            target.currentHP -= result.finalDamage;
            if (target.currentHP < 0) target.currentHP = 0;

            Color dmgColor = result.isCrit ? Color.yellow : Color.white;
            Transform targetTransform = UI.GetEnemyUITransform(target);
            FloatingTextManager.Instance?.SpawnText(targetTransform.position, result.finalDamage.ToString(), dmgColor);

            ApplyOnHitEffects(target, result.rawDamage);
        }

        // Countdown duration debuff vÃ  buff (chá»‰ khi Attack, khÃ´ng khi dÃ¹ng Ä‘á»“)
        DebuffManager.Instance?.ProcessTurnDurationTick(PlayerManager.Instance);
        BuffManager.Instance?.ProcessUnitTurnTick(PlayerManager.Instance);

        playerTurnDamageTicked = false; // Reset cho lÆ°á»£t tiáº¿p theo
        PlayerManager.Instance.currentAP -= maxAP;
        TickDownSkillCooldowns();
        isCombatPaused = false;
    }

    public void OnDefendButton()
    {
        UI.ShowActionMenu(false);
        PlayerManager.Instance.isDefending = true;

        // Hiá»ƒn thá»‹ icon PhÃ²ng Thá»§: DEFâ†‘ vÃ  EVAâ†‘ trong 1 lÆ°á»£t
        // value=0 vÃ¬ isDefending flag Ä‘Ã£ xá»­ lÃ½ bonus DEF/EVA thá»±c táº¿
        if (BuffManager.Instance != null)
        {
            BuffManager.Instance.AddBuff(PlayerManager.Instance, BuffType.DEF_Up, 0f, 1, 1);
            BuffManager.Instance.AddBuff(PlayerManager.Instance, BuffType.EVA_Up, 0f, 1, 1);
        }

        // Countdown duration debuff vÃ  buff (chá»‰ khi Defend, khÃ´ng khi dÃ¹ng Ä‘á»“)
        DebuffManager.Instance?.ProcessTurnDurationTick(PlayerManager.Instance);
        BuffManager.Instance?.ProcessUnitTurnTick(PlayerManager.Instance);

        playerTurnDamageTicked = false; // Reset cho lÆ°á»£t tiáº¿p theo
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
            SceneManager.LoadScene("Dungeon");
        }
        else
        {
            UI.ShowFloatingText("Cháº¡y tháº¥t báº¡i!", Color.red);
            PlayerManager.Instance.currentAP = 0;
            playerWaitTurns = 0;
            foreach (var e in activeEnemies) if (e.currentHP > 0) playerWaitTurns++;
            isCombatPaused = false;
        }
    }

    // [Phase 2] ToggleEnemyInfo Ä‘Ã£ chuyá»ƒn vÃ o CombatUIManager.

    void ExecuteEnemyNormalAttack(EnemyStats attacker)
    {
        var result = CombatCalculator.CalculateEnemyAttack(attacker, PlayerManager.Instance);
        if (result.missed) { UI.ShowMissText(PlayerManager.Instance); return; }

        PlayerManager.Instance.TakeDamage(result.rawDamage, false, false);

        Color dmgColor = result.isCrit ? Color.yellow : Color.white;
        FloatingTextManager.Instance?.SpawnText(UI.GetPlayerHPBarTransform().position, result.displayDamage.ToString(), dmgColor);

        if (result.isCrit && UnityEngine.Random.Range(0f, 100f) <= 12f) PlayerManager.Instance.ReduceSanity(1);
        BossPassiveManager.Instance?.ApplyPassiveEffects(attacker);
    }

    void ExecuteEnemySkill(EnemyStats attacker)
    {
        // [Phase 5] Delegate to SkillExecutor
        if (Executor != null)
        {
            bool isAsync = Executor.RunEnemySkill(attacker.skillID, attacker);
            // Async skills (FlameBreath) tá»± giáº£i phÃ³ng isCombatPaused qua coroutine
            return;
        }
        // Fallback náº¿u Executor chÆ°a sáºµn (khÃ´ng nÃªn xáº£y ra)
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
        BuffManager.Instance?.ClearAllBuffs(PlayerManager.Instance); // XÃ³a buff khi thua
        // Apply death penalty: lose half gold & EXP earned this floor
        PlayerMovement.ApplyDeathPenalty();
        // Há»“i full HP, SEN giá»¯ nguyÃªn
        PlayerManager.Instance.currentHP = PlayerManager.Instance.maxHP;
        DeathContext.Pending = DeathContext.DeathType.CombatDeath;
        SaveSystem.Instance?.Save();
        SceneManager.LoadScene("Town");
    }

    public void OnMadnessReturnTown()
    {
        BuffManager.Instance?.ClearAllBuffs(PlayerManager.Instance); // XÃ³a buff khi thua
        // Apply death penalty: lose half gold & EXP earned this floor
        PlayerMovement.ApplyDeathPenalty();
        // HP giá»¯ nguyÃªn nhÆ° lÃºc á»Ÿ trong combat, SEN = 2
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

        target.currentHP -= result.finalDamage;
        if (target.currentHP < 0) target.currentHP = 0;

        Color dmgColor = result.isCrit ? Color.yellow : Color.white;
        Transform targetTransform = UI.GetEnemyUITransform(target);
        FloatingTextManager.Instance?.SpawnText(targetTransform.position, result.finalDamage.ToString(), dmgColor);

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

    /// <summary>[Phase 5] HoÃ n táº¥t lÆ°á»£t player sau khi skill Ä‘á»“ng bá»™ káº¿t thÃºc.</summary>
    public void FinishPlayerTurnAfterSkill()
    {
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
