using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UIShake : MonoBehaviour
{
    public static UIShake Instance { get; private set; }

    private class ShakeChildInfo
    {
        public RectTransform rect;
        public Vector2 initialPos;
    }

    private List<ShakeChildInfo> shakeChildren = new List<ShakeChildInfo>();
    private Camera targetCamera;
    private Vector3 initialCameraPos;
    private Coroutine currentShakeCoroutine;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else if (Instance != this) { Destroy(gameObject); return; }
        EnsureTargets();
    }

    private void EnsureTargets()
    {
        if (shakeChildren.Count > 0) return;

        // 1. Tìm Canvas chính
        Canvas targetCanvas = null;
        GameObject combatCanvasGO = GameObject.Find("CanvasCombat");
        if (combatCanvasGO != null)
            targetCanvas = combatCanvasGO.GetComponent<Canvas>();

        if (targetCanvas == null && CombatUIManager.Instance != null)
        {
            targetCanvas = CombatUIManager.Instance.GetComponentInParent<Canvas>();
            if (targetCanvas == null && CombatUIManager.Instance.playerHPBar != null)
                targetCanvas = CombatUIManager.Instance.playerHPBar.GetComponentInParent<Canvas>();
        }

        if (targetCanvas == null)
        {
            foreach (var c in FindObjectsByType<Canvas>(FindObjectsSortMode.None))
            {
                string cName = c.gameObject.name;
                if (cName.Contains("Equipment") || cName.Contains("Inventory") || cName.Contains("Event"))
                    continue;
                targetCanvas = c;
                break;
            }
        }

        // Thu thập tất cả các phần tử UI con cấp 1 của Canvas (trừ TestMenu)
        if (targetCanvas != null)
        {
            foreach (Transform child in targetCanvas.transform)
            {
                if (child.name == "TestMenu") continue;
                RectTransform rt = child.GetComponent<RectTransform>();
                if (rt != null)
                {
                    shakeChildren.Add(new ShakeChildInfo { rect = rt, initialPos = rt.anchoredPosition });
                }
            }
        }

        // Đồng thời lấy tham chiếu Camera.main để rung phụ trợ
        targetCamera = Camera.main;
        if (targetCamera != null)
            initialCameraPos = targetCamera.transform.localPosition;
    }

    /// <summary>
    /// Rung toàn bộ màn hình combat (tất cả các panel UI + Camera).
    /// </summary>
    public void Shake(float duration = 0.2f, float magnitude = 14f)
    {
        EnsureTargets();
        if (shakeChildren.Count == 0 && targetCamera == null) return;

        if (currentShakeCoroutine != null) StopCoroutine(currentShakeCoroutine);
        currentShakeCoroutine = StartCoroutine(DoShake(duration, magnitude));
    }

    private IEnumerator DoShake(float duration, float magnitude)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            float x = Random.Range(-1f, 1f) * magnitude;
            float y = Random.Range(-1f, 1f) * magnitude;
            Vector2 offset = new Vector2(x, y);

            for (int i = 0; i < shakeChildren.Count; i++)
            {
                if (shakeChildren[i].rect != null)
                    shakeChildren[i].rect.anchoredPosition = shakeChildren[i].initialPos + offset;
            }

            if (targetCamera != null)
                targetCamera.transform.localPosition = initialCameraPos + new Vector3(x * 0.05f, y * 0.05f, 0);

            elapsed += Time.deltaTime;
            yield return null;
        }

        ResetAllPositions();
        currentShakeCoroutine = null;
    }

    private void ResetAllPositions()
    {
        for (int i = 0; i < shakeChildren.Count; i++)
        {
            if (shakeChildren[i].rect != null)
                shakeChildren[i].rect.anchoredPosition = shakeChildren[i].initialPos;
        }

        if (targetCamera != null)
            targetCamera.transform.localPosition = initialCameraPos;
    }

    void OnDestroy()
    {
        ResetAllPositions();
    }
}
