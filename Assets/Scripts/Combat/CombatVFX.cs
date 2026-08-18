using System.Collections.Generic;
using UnityEngine;

public enum VFXType
{
    SlashNormal,
    SlashHeavy,
    AuraBuff,
    HitHeavy,
    FlameBreath
}

public class CombatVFX : MonoBehaviour
{
    public static CombatVFX Instance { get; private set; }
    private RectTransform vfxContainer;
    private Dictionary<VFXType, GameObject> vfxPrefabs = new Dictionary<VFXType, GameObject>();
    private Dictionary<VFXType, float> vfxDurations = new Dictionary<VFXType, float>()
    {
        { VFXType.SlashNormal, 0.4f },
        { VFXType.SlashHeavy,  0.6f },
        { VFXType.AuraBuff,    0.8f },
        { VFXType.HitHeavy,    0.5f },
        { VFXType.FlameBreath, 1.2f },
    };

    void Awake()
    {
        if (Instance == null) Instance = this;
        else if (Instance != this) { Destroy(gameObject); return; }

        EnsureContainerRef();
        PreloadPrefabs();
    }

    private void EnsureContainerRef()
    {
        if (vfxContainer != null) return;

        // 1. Tìm theo tên "CanvasCombat"
        GameObject combatCanvasGO = GameObject.Find("CanvasCombat");
        if (combatCanvasGO != null)
        {
            vfxContainer = combatCanvasGO.GetComponent<RectTransform>();
            return;
        }

        // 2. Tìm qua CombatUIManager nếu có
        if (CombatUIManager.Instance != null)
        {
            Canvas c = CombatUIManager.Instance.GetComponentInParent<Canvas>();
            if (c == null && CombatUIManager.Instance.playerHPBar != null)
                c = CombatUIManager.Instance.playerHPBar.GetComponentInParent<Canvas>();
            if (c != null)
            {
                vfxContainer = c.GetComponent<RectTransform>();
                return;
            }
        }

        // 3. Tìm canvas trong scene không phải của GameManager
        foreach (var c in FindObjectsByType<Canvas>(FindObjectsSortMode.None))
        {
            string cName = c.gameObject.name;
            if (cName.Contains("Equipment") || cName.Contains("Inventory") || cName.Contains("Event"))
                continue;
            vfxContainer = c.GetComponent<RectTransform>();
            break;
        }
    }

    private void PreloadPrefabs()
    {
        LoadVFX(VFXType.SlashNormal, "VFX/slash_normal");
        LoadVFX(VFXType.SlashHeavy,  "VFX/slash_heavy");
        LoadVFX(VFXType.AuraBuff,    "VFX/aura_buff");
        LoadVFX(VFXType.HitHeavy,    "VFX/hit_heavy");
        LoadVFX(VFXType.FlameBreath, "VFX/flame_breath");
    }

    private void LoadVFX(VFXType type, string path)
    {
        GameObject prefab = Resources.Load<GameObject>(path);
        if (prefab != null)
            vfxPrefabs[type] = prefab;
        else
            Debug.LogWarning($"[CombatVFX] Chưa tìm thấy prefab tại Resources/{path}. " +
                             "Đặt prefab vào Assets/Resources/VFX/ với đúng tên.");
    }

    /// <summary>
    /// Phát VFX tại đúng vị trí UI của mục tiêu kèm góc xoay tùy chỉnh (zRotation).
    /// </summary>
    public GameObject PlayVFX(VFXType type, RectTransform targetUI, float zRotation = 0f)
    {
        if (!vfxPrefabs.TryGetValue(type, out GameObject prefab) || prefab == null)
            return null;
        if (vfxContainer == null) EnsureContainerRef();
        if (vfxContainer == null || targetUI == null)
            return null;

        GameObject instance = Instantiate(prefab, vfxContainer);
        RectTransform rt = instance.GetComponent<RectTransform>();
        if (rt != null)
        {
            rt.position = targetUI.position;
            rt.localEulerAngles = new Vector3(0f, 0f, zRotation);
        }
        else
        {
            Debug.LogWarning($"[CombatVFX] Prefab {type} thiếu RectTransform — " +
                             "kiểm tra prefab có phải UI Image không.");
        }

        float duration = vfxDurations.TryGetValue(type, out float d) ? d : 1.0f;
        Destroy(instance, duration);
        return instance;
    }
}
