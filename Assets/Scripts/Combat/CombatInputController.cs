using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

/// <summary>
/// [Phase 4 Refactor] Chịu trách nhiệm TOÀN BỘ việc đọc phím bấm và chuột trong màn chiến đấu.
/// - Điều hướng Action Menu bằng phím mũi tên / W-S
/// - Chuyển đổi mục tiêu (Tab/A-D) khi đang nhắm
/// - Xác nhận hành động (Z / Enter / Click chuột trái)
/// - Hủy hành động (X / Chuột phải)
///
/// Cách gắn vào Unity:
///   Add Component script này vào cùng GameObject chứa CombatManager.
///   Không cần kéo thả gì thêm — script tự lấy tham chiếu từ CombatManager.Instance.
/// </summary>
public class CombatInputController : MonoBehaviour
{
    public static CombatInputController Instance { get; private set; }

    // ─────────────────────────────────────────────────────────────────────────
    //  STATE (đã chuyển từ CombatManager)
    // ─────────────────────────────────────────────────────────────────────────
    public bool IsTargetingMode { get; private set; } = false;
    public EnemyStats CurrentTarget { get; private set; }

    private int selectedButtonIndex = 0;
    private SkillData pendingSkill;

    // Tham chiếu nhanh — được điền trong Awake/Start
    private CombatManager CM   => CombatManager.Instance;
    private CombatUIManager UI => CM?.UI;

    // ─────────────────────────────────────────────────────────────────────────
    //  LIFECYCLE
    // ─────────────────────────────────────────────────────────────────────────
    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(this); return; }
        Instance = this;
    }

    private void Start()
    {
        // Khởi tạo hover events trên action buttons
        UI?.SetupActionButtonHoverEvents(idx =>
        {
            selectedButtonIndex = idx;
            UI?.UpdateButtonSelection(idx);
        });
        UI?.UpdateButtonSelection(selectedButtonIndex);
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  UPDATE — gọi bởi CombatManager.Update() hoặc tự chạy
    // ─────────────────────────────────────────────────────────────────────────
    private void Update()
    {
        if (CM == null) return;

        // Chuột phải — luôn nhận dù combat đang pause
        if (Input.GetMouseButtonDown(1)) HandleRightClick();

        // Chuột trái — chỉ khi đang nhắm mục tiêu
        if (Input.GetMouseButtonDown(0) && IsTargetingMode) ConfirmTargeting();

        // Điều hướng bàn phím
        if (UI?.actionMenu != null && UI.actionMenu.activeSelf)
            HandleActionMenuKeyboard();
        else if (IsTargetingMode)
            HandleTargetingKeyboard();
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  ACTION MENU — keyboard navigation
    // ─────────────────────────────────────────────────────────────────────────
    private void HandleActionMenuKeyboard()
    {
        if (Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.W))
        {
            selectedButtonIndex--;
            if (selectedButtonIndex < 0) selectedButtonIndex = UI.actionButtons.Length - 1;
            UI.UpdateButtonSelection(selectedButtonIndex);
        }

        if (Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.S))
        {
            selectedButtonIndex++;
            if (selectedButtonIndex >= UI.actionButtons.Length) selectedButtonIndex = 0;
            UI.UpdateButtonSelection(selectedButtonIndex);
        }

        if (Input.GetKeyDown(KeyCode.Z) || Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Space))
            OnActionButtonConfirmed();
    }

    private void OnActionButtonConfirmed()
    {
        if (UI?.actionButtons == null) return;
        if (selectedButtonIndex >= 0 && selectedButtonIndex < UI.actionButtons.Length)
        {
            Button btn = UI.actionButtons[selectedButtonIndex]?.GetComponent<Button>();
            btn?.onClick.Invoke();
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  TARGETING — keyboard navigation
    // ─────────────────────────────────────────────────────────────────────────
    private void HandleTargetingKeyboard()
    {
        if (Input.GetKeyDown(KeyCode.LeftArrow)  || Input.GetKeyDown(KeyCode.A)) SelectPreviousEnemy();
        if (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D)) SelectNextEnemy();
        if (Input.GetKeyDown(KeyCode.Z) || Input.GetKeyDown(KeyCode.Return)) ConfirmTargeting();
        if (Input.GetKeyDown(KeyCode.X)) CancelTargeting();
    }

    private void SelectNextEnemy()
    {
        if (CM.activeEnemies.Count <= 1) return;
        int cur  = CM.activeEnemies.IndexOf(CurrentTarget);
        int next = (cur + 1) % CM.activeEnemies.Count;
        int att  = 0;
        while (CM.activeEnemies[next].currentHP <= 0 && att < CM.activeEnemies.Count)
        { next = (next + 1) % CM.activeEnemies.Count; att++; }
        if (CM.activeEnemies[next].currentHP > 0)
        { CurrentTarget = CM.activeEnemies[next]; UI?.MoveArrowToTarget(CurrentTarget); }
    }

    private void SelectPreviousEnemy()
    {
        if (CM.activeEnemies.Count <= 1) return;
        int cur  = CM.activeEnemies.IndexOf(CurrentTarget);
        int prev = (cur - 1 + CM.activeEnemies.Count) % CM.activeEnemies.Count;
        int att  = 0;
        while (CM.activeEnemies[prev].currentHP <= 0 && att < CM.activeEnemies.Count)
        { prev = (prev - 1 + CM.activeEnemies.Count) % CM.activeEnemies.Count; att++; }
        if (CM.activeEnemies[prev].currentHP > 0)
        { CurrentTarget = CM.activeEnemies[prev]; UI?.MoveArrowToTarget(CurrentTarget); }
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  CONFIRM / CANCEL
    // ─────────────────────────────────────────────────────────────────────────
    public void ConfirmTargeting()
    {
        if (CurrentTarget == null || CurrentTarget.currentHP <= 0) return;

        if (pendingSkill != null)
        {
            int skillLevel  = SkillManager.Instance.GetSkillLevel(pendingSkill.skillID);
            float dmgBonus  = pendingSkill.damageBonusPerLevel[skillLevel - 1];
            float effBonus  = pendingSkill.effectChancePerLevel[skillLevel - 1];
            CM.ExecuteSkillFromInput(CurrentTarget, pendingSkill, dmgBonus, effBonus);
        }
        else
        {
            CM.ExecutePlayerAttackFromInput(CurrentTarget);
        }
    }

    public void CancelTargeting()
    {
        IsTargetingMode = false;
        pendingSkill    = null;
        UI?.ShowTargetArrow(false);
        UI?.ShowActionMenu(true);
    }

    private void HandleRightClick()
    {
        if (!CM) return;
        bool combatPaused = CM.IsCombatPaused;
        if (combatPaused && !IsTargetingMode &&
            (SkillMenuUI.Instance == null || !SkillMenuUI.Instance.gameObject.activeSelf)) return;

        if (IsTargetingMode) CancelTargeting();
        else if (SkillMenuUI.Instance != null && SkillMenuUI.Instance.gameObject.activeSelf)
            CM.CancelSkillMenu();
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  PUBLIC API — được gọi từ CombatManager khi bắt đầu lượt player / dùng skill
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>Bắt đầu chế độ nhắm mục tiêu thông thường (nút Attack).</summary>
    public void BeginTargeting(EnemyStats initialTarget)
    {
        IsTargetingMode = true;
        pendingSkill    = null;
        CurrentTarget   = initialTarget;
        UI?.MoveArrowToTarget(CurrentTarget);
        if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);
    }

    /// <summary>Bắt đầu chế độ nhắm mục tiêu cho một skill cụ thể.</summary>
    public void BeginSkillTargeting(EnemyStats initialTarget, SkillData skill)
    {
        IsTargetingMode = true;
        pendingSkill    = skill;
        CurrentTarget   = initialTarget;
        UI?.MoveArrowToTarget(CurrentTarget);
        if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);
    }

    /// <summary>Đặt mục tiêu từ bên ngoài (dùng bởi EncounterSpawner hover event).</summary>
    public void SetTarget(EnemyStats target)
    {
        CurrentTarget = target;
    }

    /// <summary>Reset về trạng thái ban đầu sau khi hành động kết thúc.</summary>
    public void ResetTargeting()
    {
        IsTargetingMode = false;
        pendingSkill    = null;
        UI?.ShowTargetArrow(false);
    }

    /// <summary>Lấy mục tiêu hiện tại (compatibility với CombatManager).</summary>
    public EnemyStats GetCurrentTarget() => CurrentTarget;

    /// <summary>Lấy pendingSkill hiện tại.</summary>
    public SkillData GetPendingSkill() => pendingSkill;
}
