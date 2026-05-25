using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class EnemyUI : MonoBehaviour
{
    public EnemyStats stats;
    public Image icon;
    public Slider hpBar;
    public Slider apBar;

    [Header("SEN ≤ 4 — Ẩn Danh Tính")]
    [SerializeField] private Sprite unknownSprite;
    private Sprite normalSprite;

    private bool _isDying = false;

    public void Setup(EnemyStats enemyData)
    {
        stats = enemyData;
        normalSprite = enemyData.enemySprite;
        icon.sprite = normalSprite;
        icon.color = Color.white;
        _isDying = false;
        hpBar.maxValue = enemyData.maxHP;
        apBar.maxValue = 100f;

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

    private void StartDeathAnimation()
    {
        if (_isDying) return;
        _isDying = true;
        StartCoroutine(DeathRoutine());
    }

    private IEnumerator DeathRoutine()
    {
        float duration = 0.5f;
        float elapsed  = 0f;

        // Giữ màu gốc + vị trí gốc
        Color startColor = icon.color;
        Vector3 startPos = transform.localPosition;

        // Tắt thanh HP/AP để gọn màn hình
        if (hpBar != null) hpBar.gameObject.SetActive(false);
        if (apBar != null) apBar.gameObject.SetActive(false);

        // Animation: chìm xuống + fade out
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            // Slide xuống 40px
            transform.localPosition = startPos + new Vector3(0f, -40f * t, 0f);

            // Fade out toàn bộ CanvasGroup hoặc đổi alpha của icon
            float alpha = 1f - t;
            icon.color = new Color(startColor.r, startColor.g, startColor.b, alpha);

            yield return null;
        }

        // Ẩn hoàn toàn sau animation
        gameObject.SetActive(false);
    }

    /// <summary>Gọi từ CombatManager.UpdatePlayerUI() để cập nhật icon theo SEN.</summary>
    public void RefreshSENState()
    {
        if (stats == null || stats.currentHP <= 0 || _isDying) return;

        bool insane = PlayerManager.Instance != null && PlayerManager.Instance.sen <= 4;

        icon.sprite = (insane && unknownSprite != null) ? unknownSprite : (normalSprite != null ? normalSprite : stats.enemySprite);
        icon.color = Color.white;
    }
}