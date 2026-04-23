using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

/// <summary>
/// Gắn vào mỗi Button trong SkillMenuPanel.
/// Hiển thị icon kỹ năng + số CD còn lại (nếu đang hồi chiêu).
/// Tooltip hiện khi hover chuột hoặc keyboard select.
/// Highlight border sáng khi hover hoặc selected.
/// </summary>
public class SkillButtonUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    [HideInInspector] public SkillData skillData;
    [HideInInspector] public SkillMenuUI parentMenu;

    [Header("UI References")]
    public Image iconImage;
    public Image highlightBorder;

    [Tooltip("Panel che tối + số CD khi kỹ năng đang hồi chiêu (ẩn mặc định)")]
    public GameObject cdOverlay;
    public TextMeshProUGUI cdText;

    void Awake()
    {
        Button btn = GetComponent<Button>();
        if (btn != null)
            btn.onClick.AddListener(OnLeftClick);
    }

    public void Setup(SkillData data, SkillMenuUI menu)
    {
        skillData = data;
        parentMenu = menu;

        if (iconImage != null)
        {
            if (data != null && data.icon != null)
            {
                iconImage.sprite = data.icon;
                iconImage.color = Color.white;
            }
            else
                iconImage.color = new Color(0.35f, 0.35f, 0.35f, 1f);
        }

        RefreshCDDisplay();
        SetHighlight(false);
    }

    /// <summary>Cập nhật hiển thị CD overlay dựa trên CombatManager.skillCooldowns</summary>
    public void RefreshCDDisplay()
    {
        if (skillData == null) return;

        int remaining = 0;
        if (CombatManager.Instance != null && CombatManager.Instance.skillCooldowns != null)
            CombatManager.Instance.skillCooldowns.TryGetValue(skillData.skillID, out remaining);

        bool onCD = remaining > 0;

        // Làm xám icon khi đang CD
        if (iconImage != null)
            iconImage.color = (onCD) ? new Color(0.3f, 0.3f, 0.3f, 1f) : Color.white;

        // Hiện/ẩn overlay và cập nhật số
        if (cdOverlay != null) 
        {
            cdOverlay.SetActive(onCD);
            if (onCD && cdText != null)
            {
                cdText.text = remaining.ToString();
                // Đảm bảo Text luôn ở trên cùng
                cdText.transform.SetAsLastSibling();
            }
        }
    }

    /// <summary>Bật/tắt highlight viền (keyboard selection hoặc hover)</summary>
    public void SetHighlight(bool active)
    {
        if (highlightBorder != null)
            highlightBorder.gameObject.SetActive(active);

        if (active && parentMenu != null && skillData != null)
            parentMenu.ShowTooltip(skillData);
    }

    // ─── Mouse Events ───────────────────────────────────────────────

    public void OnPointerEnter(PointerEventData eventData)
    {
        // Hover: bật highlight viền + tooltip
        if (highlightBorder != null)
            highlightBorder.gameObject.SetActive(true);

        if (parentMenu != null && skillData != null)
            parentMenu.ShowTooltip(skillData);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        // Rời chuột: tắt highlight nếu không phải ô keyboard đang chọn
        bool isKeyboardSelected = parentMenu != null && parentMenu.IsCurrentlySelected(this);
        if (highlightBorder != null)
            highlightBorder.gameObject.SetActive(isKeyboardSelected);

        if (parentMenu != null && !isKeyboardSelected)
            parentMenu.HideTooltip();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        // Chuột phải trong bảng kỹ năng = đóng bảng
        if (eventData.button == PointerEventData.InputButton.Right)
            CombatManager.Instance?.CancelSkillMenu();
    }

    // ─── Click ──────────────────────────────────────────────────────

    private void OnLeftClick()
    {
        if (CombatManager.Instance == null || skillData == null) return;

        // Kiểm tra CD trước khi cho dùng
        int remaining = 0;
        CombatManager.Instance.skillCooldowns.TryGetValue(skillData.skillID, out remaining);
        if (remaining > 0) return; // Đang hồi chiêu → không làm gì

        CombatManager.Instance.OnSkillButton(skillData);
    }
}
