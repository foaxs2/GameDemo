using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;

/// <summary>
/// [Phase 2 Refactor] Chịu trách nhiệm TOÀN BỘ giao diện UI trong màn chiến đấu.
/// CombatManager chỉ cần gọi các hàm public của class này, không cần biết chi tiết UI.
///
/// Cách gắn vào Unity:
///   1. Add Component script này vào cùng GameObject chứa CombatManager.
///   2. Kéo lại tất cả các ô trống UI (HP bar, panel, text...) vào đây trong Inspector.
/// </summary>
public class CombatUIManager : MonoBehaviour
{
    public static CombatUIManager Instance { get; private set; }

    // ─────────────────────────────────────────────────────────────────────────
    //  UI NGƯỜI CHƠI
    // ─────────────────────────────────────────────────────────────────────────
    [Header("UI Người chơi")]
    public Image playerIcon;
    public TextMeshProUGUI playerHPText;
    public TextMeshProUGUI playerAPText;
    public TextMeshProUGUI playerSanityText;
    public Slider playerHPBar;
    public Slider playerAPBar;
    public Slider playerSanityBar;

    // ─────────────────────────────────────────────────────────────────────────
    //  UI ĐIỀU KHIỂN & BẢNG THÔNG TIN KẺ THÙU
    // ─────────────────────────────────────────────────────────────────────────
    [Header("UI Điều khiển & Bảng thông tin")]
    public GameObject actionMenu;
    public GameObject enemyInfoPanel;
    public TextMeshProUGUI infoStatsText;
    public TextMeshProUGUI infoSkillText;
    public GameObject[] actionButtons;

    // ─────────────────────────────────────────────────────────────────────────
    //  HỆ THỐNG NHẮM MỤC TIÊU (Arrow)
    // ─────────────────────────────────────────────────────────────────────────
    [Header("Hệ thống Nhắm Mục Tiêu")]
    public GameObject targetArrow;

    // ─────────────────────────────────────────────────────────────────────────
    //  UI PHỤ
    // ─────────────────────────────────────────────────────────────────────────
    [Header("Tham chiếu Trang Bị & UI Phụ")]
    public GameObject equipmentCanvas;

    [Header("Hệ thống Kỹ năng")]
    public GameObject skillMenuPanel;

    // ─────────────────────────────────────────────────────────────────────────
    //  CÁC PANEL KẾT TRẬN
    // ─────────────────────────────────────────────────────────────────────────
    [Header("Bảng Chiến Thắng (Victory Panel)")]
    public GameObject victoryPanel;
    public TextMeshProUGUI txtVictoryExp;
    public TextMeshProUGUI txtVictoryGold;

    [Header("Bảng Tử Vong (Death Panel)")]
    [SerializeField] private GameObject deathPanel;
    [SerializeField] private GameObject madnessPanel;

    [Header("Boss Warning UI")]
    [SerializeField] private TextMeshProUGUI bossWarningText;

    [Header("Buff/Debuff Icon Container")]
    [SerializeField] private StatusIconContainer playerStatusContainer;

    // ─────────────────────────────────────────────────────────────────────────
    //  LIFECYCLE
    // ─────────────────────────────────────────────────────────────────────────

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(this); return; }
        Instance = this;
    }

    /// <summary>
    /// Gọi từ CombatManager.Start() để khởi tạo các thành phần UI.
    /// </summary>
    public void Initialize()
    {
        // ── Auto-tìm actionButtons nếu chưa gán trong Inspector ──
        if (actionButtons == null || actionButtons.Length == 0 || actionButtons[0] == null)
        {
            actionButtons = new GameObject[5];
            if (actionMenu != null)
            {
                Transform t = actionMenu.transform;
                if (t.childCount >= 5)
                {
                    for (int i = 0; i < 5; i++)
                        actionButtons[i] = t.GetChild(i).gameObject;
                }
            }
        }

        // ── Auto-tìm StatusIconContainer ──
        if (playerStatusContainer == null)
        {
            var go = GameObject.Find("PlayerStatusIcons");
            if (go != null) playerStatusContainer = go.GetComponent<StatusIconContainer>();
            if (playerStatusContainer == null)
                Debug.LogWarning("[CombatUIManager] Không tìm thấy PlayerStatusIcons! Hãy kéo vào field playerStatusContainer trong Inspector.");
        }
        playerStatusContainer?.SetUnit(PlayerManager.Instance);

        // ── Ẩn các UI ban đầu ──
        if (targetArrow  != null) targetArrow.SetActive(false);
        if (skillMenuPanel != null) skillMenuPanel.SetActive(false);
        if (actionMenu   != null) actionMenu.SetActive(false);
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  CẬP NHẬT UI NGƯỜI CHƠI
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>Cập nhật toàn bộ thanh HP, Sanity và refresh icon kẻ thù theo SEN.</summary>
    public void UpdatePlayerUI()
    {
        if (PlayerManager.Instance == null) return;

        playerHPBar.maxValue = PlayerManager.Instance.maxHP;
        playerHPBar.value    = PlayerManager.Instance.currentHP;
        playerHPText.text    = PlayerManager.Instance.currentHP + "/" + PlayerManager.Instance.maxHP;

        if (playerSanityBar != null)
        {
            playerSanityBar.maxValue = PlayerManager.Instance.maxSen;
            playerSanityBar.value    = PlayerManager.Instance.sen;
        }
        if (playerSanityText != null) playerSanityText.text = PlayerManager.Instance.sen.ToString();

        // Cập nhật icon kẻ thù theo SEN (SEN ≤ 4 → icon ẩn danh)
        foreach (var ui in FindObjectsByType<EnemyUI>(FindObjectsSortMode.None))
            ui.RefreshSENState();
    }

    /// <summary>Cập nhật thanh AP và text phần trăm.</summary>
    public void UpdateAPUI()
    {
        if (playerAPBar == null || PlayerManager.Instance == null) return;
        playerAPBar.value = PlayerManager.Instance.currentAP;
        playerAPText.text = Mathf.FloorToInt(PlayerManager.Instance.currentAP) + "%";
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  ACTION MENU & BUTTON SELECTION
    // ─────────────────────────────────────────────────────────────────────────

    public void ShowActionMenu(bool show) => actionMenu?.SetActive(show);

    /// <summary>Tô màu nút đang được chọn trong action menu.</summary>
    public void UpdateButtonSelection(int selectedIndex)
    {
        if (actionButtons == null) return;
        for (int i = 0; i < actionButtons.Length; i++)
        {
            if (actionButtons[i] == null) continue;
            Button btn = actionButtons[i].GetComponent<Button>();
            if (btn == null) continue;

            btn.transition = Selectable.Transition.None;
            Image img = actionButtons[i].GetComponent<Image>();
            if (img != null) img.color = (i == selectedIndex) ? Color.yellow : Color.white;

            TextMeshProUGUI label = actionButtons[i].GetComponentInChildren<TextMeshProUGUI>();
            if (label != null) label.color = Color.black;
        }
    }

    /// <summary>
    /// Gắn sự kiện hover (PointerEnter) cho từng nút trong action menu.
    /// Khi hover sẽ callback lên CombatManager để cập nhật selectedButtonIndex.
    /// </summary>
    public void SetupActionButtonHoverEvents(System.Action<int> onHoverCallback)
    {
        if (actionButtons == null) return;
        for (int i = 0; i < actionButtons.Length; i++)
        {
            if (actionButtons[i] == null) continue;
            int capturedIndex = i;

            EventTrigger trigger = actionButtons[i].GetComponent<EventTrigger>();
            if (trigger == null) trigger = actionButtons[i].AddComponent<EventTrigger>();
            trigger.triggers.RemoveAll(e => e.eventID == EventTriggerType.PointerEnter);

            var entry = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
            entry.callback.AddListener((_) =>
            {
                if (actionMenu == null || !actionMenu.activeSelf) return;
                onHoverCallback?.Invoke(capturedIndex);
            });
            trigger.triggers.Add(entry);
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  TARGET ARROW
    // ─────────────────────────────────────────────────────────────────────────

    public void ShowTargetArrow(bool show)
    {
        if (targetArrow != null) targetArrow.SetActive(show);
    }

    /// <summary>Di chuyển targetArrow lên trên EnemyUI của mục tiêu.</summary>
    public void MoveArrowToTarget(EnemyStats target)
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

    // ─────────────────────────────────────────────────────────────────────────
    //  FLOATING TEXT HELPERS
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>Hiện chữ "Miss!" màu cyan tại vị trí của Unit (Player hoặc Enemy).</summary>
    public void ShowMissText(Unit target)
    {
        if (FloatingTextManager.Instance == null) return;
        Transform uiTransform = GetPlayerHPBarTransform();
        if (target is EnemyStats targetEnemy)
            uiTransform = GetEnemyUITransform(targetEnemy);
        FloatingTextManager.Instance.SpawnText(uiTransform.position, "Miss!", Color.cyan);
    }

    /// <summary>Trả về Transform của HP bar player (dùng để neo floating text phía player).</summary>
    public Transform GetPlayerHPBarTransform() => playerHPBar != null ? playerHPBar.transform : null;

    /// <summary>Trả về Transform của PlayerIcon (dùng để phát Aura buff hoặc hiệu ứng bao quanh Player).</summary>
    public Transform GetPlayerIconTransform()
    {
        if (playerIcon != null) return playerIcon.transform;
        // Tự tìm con tên PlayerIcon trong PlayerStatus nếu chưa gán Inspector
        if (playerHPBar != null && playerHPBar.transform.parent != null)
        {
            Transform iconTf = playerHPBar.transform.parent.Find("PlayerIcon");
            if (iconTf != null) return iconTf;
        }
        return GetPlayerHPBarTransform();
    }

    private Coroutine _playerFlashRoutine = null;

    /// <summary>
    /// Hiệu ứng nháy đỏ PlayerIcon khi người chơi bị quái đánh trúng (tương tự FlashHit của quái).
    /// </summary>
    public void FlashPlayerHit(float duration = 0.4f)
    {
        Image icon = playerIcon;
        if (icon == null)
        {
            Transform t = GetPlayerIconTransform();
            if (t != null) icon = t.GetComponent<Image>();
        }
        if (icon == null) return;

        if (_playerFlashRoutine != null) StopCoroutine(_playerFlashRoutine);
        _playerFlashRoutine = StartCoroutine(PlayerHitRoutine(icon, duration));
    }

    private IEnumerator PlayerHitRoutine(Image icon, float duration)
    {
        icon.color = new Color(1f, 0.05f, 0.05f, 1f);
        yield return new WaitForSeconds(duration);
        if (icon != null) icon.color = Color.white;
        _playerFlashRoutine = null;
    }

    /// <summary>Trả về Transform icon của một EnemyStats trong Hierarchy UI.</summary>
    public Transform GetEnemyUITransform(EnemyStats target)
    {
        foreach (var ui in FindObjectsByType<EnemyUI>(FindObjectsSortMode.None))
            if (ui.stats == target) return ui.icon.transform;
        return playerHPBar != null ? playerHPBar.transform : transform;
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  BẢNG THÔNG TIN KẺ THÙ
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>Bật/tắt bảng thông tin kẻ thù, điền số liệu hoặc ẩn nếu SEN ≤ 4.</summary>
    public void ToggleEnemyInfo(EnemyStats stats, bool isTargetingMode)
    {
        if (isTargetingMode) return;
        enemyInfoPanel.SetActive(!enemyInfoPanel.activeSelf);
        if (!enemyInfoPanel.activeSelf || stats == null) return;

        // SEN ≤ 4: ẩn thông tin kẻ thù
        if (PlayerManager.Instance != null && PlayerManager.Instance.sen <= 4)
        {
            infoStatsText.text = "???\n???\n???";
            infoSkillText.text = "";
            return;
        }

        infoStatsText.text = $"HP: {stats.maxHP}\nATK: {stats.attack}\nDEF: {stats.currentDefense}\nSPD: {stats.currentSpeed}\nCRIT: {stats.critChance}%\nEVA: {stats.evasion}%";
        if (stats.maxCooldown > 0)
            infoSkillText.text = $"<color=yellow>Kỹ năng: {stats.skillName}</color>\n{stats.skillDescription}\n(Hồi chiêu: {stats.maxCooldown} lượt)";
        else
            infoSkillText.text = "Kẻ thù này không có kỹ năng đặc biệt.";
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  CÁC PANEL KẾT TRẬN
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>Điền số liệu EXP/Gold và bật victoryPanel.</summary>
    public void ShowVictoryPanel(int exp, int gold)
    {
        if (txtVictoryExp != null) txtVictoryExp.text = exp.ToString();
        if (txtVictoryGold != null) txtVictoryGold.text = gold.ToString();
        if (victoryPanel != null) victoryPanel.SetActive(true);
    }

    /// <summary>Hiện bảng tử vong. Trả về false nếu panel chưa được gán.</summary>
    public bool ShowDeathPanel()
    {
        if (deathPanel != null) { deathPanel.SetActive(true); return true; }
        Debug.LogWarning("[CombatUIManager] deathPanel chưa được gán trong Inspector!");
        return false;
    }

    /// <summary>Hiện bảng phát điên. Trả về false nếu panel chưa được gán.</summary>
    public bool ShowMadnessPanelUI()
    {
        if (madnessPanel != null) { madnessPanel.SetActive(true); return true; }
        Debug.LogWarning("[CombatUIManager] madnessPanel chưa được gán trong Inspector!");
        return false;
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  BOSS WARNING
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>Hiện cảnh báo boss và tự ẩn sau 2 giây.</summary>
    public void ShowBossWarning(string message)
    {
        if (bossWarningText == null) return;
        bossWarningText.text = message;
        bossWarningText.gameObject.SetActive(true);
        StopCoroutine(nameof(HideBossWarning));
        StartCoroutine(nameof(HideBossWarning));
    }

    private IEnumerator HideBossWarning()
    {
        yield return new WaitForSeconds(2f);
        if (bossWarningText != null) bossWarningText.gameObject.SetActive(false);
    }

    /// <summary>Hiện floating text tại vị trí thanh HP của player.</summary>
    public void ShowFloatingText(string text, Color color)
    {
        if (FloatingTextManager.Instance == null || playerHPBar == null) return;
        FloatingTextManager.Instance.SpawnText(playerHPBar.transform.position, text, color);
    }
}
