using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class EnemyUI : MonoBehaviour
{
    public EnemyStats stats;
    public Image icon;
    public Slider hpBar;
    public Slider apBar;

    [Header("Bong bóng cảnh báo Kỹ năng")]
    public SkillWarningBubble warningBubble;

    [Header("SEN ≤ 4 — Ẩn Danh Tính")]
    [SerializeField] private Sprite unknownSprite;
    private Sprite normalSprite;

    [Header("Flash Effect & Glow — Ánh sáng nền & Nháy màu")]
    [SerializeField] private Image bgGlow;
    private Coroutine _flashRoutine = null;

    private bool _isDying = false;
    private bool _isWarningVisible = false;
    private Sprite _currentWarningIcon = null;

    void Awake()
    {
        InitGlowObject();
    }

    /// <summary>
    /// Tạo hoặc tìm Image Glow nằm đúng vị trí và kích thước của EnemyIcon,
    /// đảm bảo ánh sáng chỉ bao phủ quanh khu vực icon, không tràn xuống toàn bộ slot.
    /// </summary>
    private void InitGlowObject()
    {
        if (bgGlow != null) return;
        if (icon == null) return;

        Transform existing = (icon.transform.parent != null) ? icon.transform.parent.Find("IconGlow") : null;
        if (existing != null)
        {
            bgGlow = existing.GetComponent<Image>();
            return;
        }

        GameObject glowObj = new GameObject("IconGlow", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        glowObj.transform.SetParent(icon.transform.parent != null ? icon.transform.parent : transform, false);
        glowObj.transform.SetSiblingIndex(icon.transform.GetSiblingIndex()); // Đặt ngay phía sau EnemyIcon

        RectTransform rt = glowObj.GetComponent<RectTransform>();
        RectTransform iconRt = icon.rectTransform;
        rt.anchorMin = iconRt.anchorMin;
        rt.anchorMax = iconRt.anchorMax;
        rt.anchoredPosition = iconRt.anchoredPosition;
        rt.sizeDelta = iconRt.sizeDelta; // Khớp 100% kích thước EnemyIcon (180x150)
        rt.pivot = iconRt.pivot;

        bgGlow = glowObj.GetComponent<Image>();
        bgGlow.raycastTarget = false;
        bgGlow.color = Color.clear;
    }

    public void Setup(EnemyStats enemyData)
    {
        stats = enemyData;
        normalSprite = enemyData.enemySprite;
        icon.sprite = normalSprite;
        icon.color = Color.white;
        InitGlowObject();
        if (bgGlow != null) bgGlow.color = Color.clear;
        _isDying = false;
        hpBar.maxValue = enemyData.maxHP;
        apBar.maxValue = 100f;

        if (warningBubble != null) warningBubble.Hide();

        // Bước 7: Gán enemy unit cho StatusIconContainer của prefab này
        var container = GetComponentInChildren<StatusIconContainer>(true);
        if (container != null)
            container.SetUnit(enemyData);
    }

    void Update()
    {
        if (stats == null || _isDying) return;

        hpBar.value = stats.currentHP;
        apBar.value = stats.currentAP;

        if (stats.currentHP <= 0)
            StartDeathAnimation();
    }

    /// <summary>
    /// Hiệu ứng sáng khi kẻ thù chuẩn bị tấn công:
    /// Ánh sáng nền trắng sáng + màu ấm trên icon (đậm hơn 20%), thời gian hiển thị 0.35s (+0.2s).
    /// </summary>
    public IEnumerator FlashAttack(float flashDuration = 0.35f)
    {
        if (icon == null || _isDying) yield break;

        icon.color = new Color(1f, 1f, 0.25f, 1f);
        if (bgGlow != null) bgGlow.color = new Color(1f, 1f, 1f, 1f);

        yield return new WaitForSeconds(flashDuration);

        if (!_isDying)
        {
            icon.color = Color.white;
            if (bgGlow != null) bgGlow.color = Color.clear;
        }
    }

    /// <summary>
    /// Hiệu ứng nháy đỏ khi quái bị Player tấn công trúng đích (phong cách Darkest Dungeon).
    /// Ánh sáng đỏ đậm hơn 20%, thời gian hiển thị 0.4s (+0.2s).
    /// </summary>
    public void FlashHit(float duration = 0.4f)
    {
        if (icon == null || _isDying) return;
        if (_flashRoutine != null) StopCoroutine(_flashRoutine);
        _flashRoutine = StartCoroutine(HitRoutine(duration));
    }

    private IEnumerator HitRoutine(float duration)
    {
        icon.color = new Color(1f, 0.05f, 0.05f, 1f);
        if (bgGlow != null) bgGlow.color = new Color(1f, 0.05f, 0.05f, 1f);

        yield return new WaitForSeconds(duration);

        if (!_isDying)
        {
            icon.color = Color.white;
            if (bgGlow != null) bgGlow.color = Color.clear;
        }
        _flashRoutine = null;
    }

    /// <summary>
    /// Bật/tắt bong bóng cảnh báo kỹ năng. Tự động ẩn nếu SEN ≤ 4.
    /// </summary>
    public void SetSkillWarning(bool visible, Sprite skillIcon = null)
    {
        _isWarningVisible = visible;
        if (skillIcon != null) _currentWarningIcon = skillIcon;

        if (warningBubble == null) return;

        bool isInsane = PlayerManager.Instance != null && PlayerManager.Instance.sen <= 4;

        if (visible && _currentWarningIcon != null && !isInsane && !_isDying)
        {
            warningBubble.Show(_currentWarningIcon);
        }
        else
        {
            warningBubble.Hide();
        }
    }

    private void StartDeathAnimation()
    {
        if (_isDying) return;
        _isDying = true;

        if (warningBubble != null) warningBubble.Hide();
        StartCoroutine(DeathRoutine());
    }

    private IEnumerator DeathRoutine()
    {
        // ═══ GIAI ĐOẠN 1: Flash đỏ đậm 0.4s (+0.2s) ═══
        if (icon != null)
        {
            icon.color = new Color(1f, 0.05f, 0.05f, 1f);
            if (bgGlow != null) bgGlow.color = new Color(1f, 0.05f, 0.05f, 1f);
            yield return new WaitForSeconds(0.4f);
        }

        // ═══ GIAI ĐOẠN 2: Slide xuống + Fade out ═══
        float duration = 0.5f;
        float elapsed  = 0f;

        Color startColor = icon.color;
        Vector3 startPos = transform.localPosition;

        // Tắt thanh HP/AP để gọn màn hình
        if (hpBar != null) hpBar.gameObject.SetActive(false);
        if (apBar != null) apBar.gameObject.SetActive(false);
        if (bgGlow != null) bgGlow.color = Color.clear;

        // Animation: chìm xuống + fade out
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            // Slide xuống 40px
            transform.localPosition = startPos + new Vector3(0f, -40f * t, 0f);

            // Fade out
            float alpha = 1f - t;
            icon.color = new Color(startColor.r, startColor.g, startColor.b, alpha);

            yield return null;
        }

        // Ẩn hoàn toàn sau animation
        gameObject.SetActive(false);
    }

    /// <summary>Gọi từ CombatManager.UpdatePlayerUI() để cập nhật icon và bong bóng theo SEN.</summary>
    public void RefreshSENState()
    {
        if (stats == null || stats.currentHP <= 0 || _isDying) return;

        bool insane = PlayerManager.Instance != null && PlayerManager.Instance.sen <= 4;

        icon.sprite = (insane && unknownSprite != null) ? unknownSprite : (normalSprite != null ? normalSprite : stats.enemySprite);
        icon.color = Color.white;

        // Cập nhật lại hiển thị bong bóng cảnh báo theo trạng thái SEN
        SetSkillWarning(_isWarningVisible, _currentWarningIcon);
    }
}