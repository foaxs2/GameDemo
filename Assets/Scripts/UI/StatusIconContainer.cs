using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Gắn lên một GameObject có GridLayoutGroup.
/// Quản lý danh sách icon buff/debuff cho 1 Unit.
/// Buff hiển thị TRƯỚC, debuff hiển thị SAU.
/// Tự refresh khi nhận event từ BuffManager / DebuffManager.
/// </summary>
public class StatusIconContainer : MonoBehaviour
{
    [SerializeField] private GameObject  statusIconPrefab;
    [SerializeField] private IconLibrary iconLibrary;

    private Unit              _trackedUnit;
    private readonly List<GameObject> _pool = new List<GameObject>();

    // ─── SETUP ───────────────────────────────────────────────────────────

    public void SetUnit(Unit unit)
    {
        _trackedUnit = unit;
        Refresh();
    }

    // ─── EVENT SUBSCRIPTIONS ─────────────────────────────────────────────
    // Dùng Start/OnDestroy thay vì OnEnable/OnDisable để tránh race condition:
    // OnEnable có thể chạy TRƯỚC BuffManager.Awake() → Instance = null → mất subscription

    void Start()
    {
        if (BuffManager.Instance != null)
        {
            BuffManager.Instance.OnBuffApplied += HandleBuffChanged;
            BuffManager.Instance.OnBuffRemoved += HandleBuffRemoved;
            BuffManager.Instance.OnBuffTick    += HandleBuffTick;    // Cập nhật số lượt khi tick
        }
        else Debug.LogWarning("[StatusIconContainer] BuffManager.Instance is null at Start!");

        if (DebuffManager.Instance != null)
        {
            DebuffManager.Instance.OnDebuffApplied += HandleDebuffChanged;
            DebuffManager.Instance.OnDebuffRemoved += HandleDebuffRemoved;
            DebuffManager.Instance.OnDebuffTick    += HandleDebuffTick;  // Cập nhật số lượt khi tick
        }
        else Debug.LogWarning("[StatusIconContainer] DebuffManager.Instance is null at Start!");

        if (_trackedUnit != null) Refresh();
    }

    void OnDestroy()
    {
        if (BuffManager.Instance != null)
        {
            BuffManager.Instance.OnBuffApplied -= HandleBuffChanged;
            BuffManager.Instance.OnBuffRemoved -= HandleBuffRemoved;
            BuffManager.Instance.OnBuffTick    -= HandleBuffTick;
        }
        if (DebuffManager.Instance != null)
        {
            DebuffManager.Instance.OnDebuffApplied -= HandleDebuffChanged;
            DebuffManager.Instance.OnDebuffRemoved -= HandleDebuffRemoved;
            DebuffManager.Instance.OnDebuffTick    -= HandleDebuffTick;
        }
    }

    private void HandleBuffChanged(Unit u, BuffType t, int s)     { if (u == _trackedUnit) Refresh(); }
    private void HandleBuffRemoved(Unit u, BuffType t)             { if (u == _trackedUnit) Refresh(); }
    private void HandleBuffTick(Unit u, BuffType t)                { if (u == _trackedUnit) Refresh(); } // Đếm ngược số lượt
    private void HandleDebuffChanged(Unit u, DebuffType t, int s)  { if (u == _trackedUnit) Refresh(); }
    private void HandleDebuffRemoved(Unit u, DebuffType t)         { if (u == _trackedUnit) Refresh(); }
    private void HandleDebuffTick(Unit u, DebuffType t, float dmg) { if (u == _trackedUnit) Refresh(); } // Đếm ngược số lượt

    // ─── REFRESH ─────────────────────────────────────────────────────────

    public void Refresh()
    {
        if (_trackedUnit == null || iconLibrary == null || statusIconPrefab == null) return;

        // Ẩn hết pool
        foreach (var go in _pool) go.SetActive(false);

        int index = 0;

        // === BUFFS TRƯỚC ===
        foreach (var buff in _trackedUnit.buffs)
        {
            Sprite icon = iconLibrary.GetBuffIcon(buff.Type, buff.CoatingType);
            if (icon == null)
            {
                Debug.LogWarning($"[StatusIconContainer] Sprite NULL cho BuffType={buff.Type} (Coating={buff.CoatingType}). Hãy gán sprite trong IconLibrary!");
                continue;
            }
            ShowAtIndex(index++, icon, buff.Stacks, buff.RemainingTurns,
                        iconLibrary.GetStackArrow(buff.Stacks));
        }

        // === DEBUFFS SAU ===
        foreach (var debuff in _trackedUnit.debuffs)
        {
            Sprite icon = iconLibrary.GetDebuffIcon(debuff.Type);
            if (icon == null)
            {
                Debug.LogWarning($"[StatusIconContainer] Sprite NULL cho DebuffType={debuff.Type}. Hãy gán sprite trong IconLibrary!");
                continue;
            }
            ShowAtIndex(index++, icon, debuff.Stacks, debuff.Duration,
                        iconLibrary.GetStackArrow(debuff.Stacks));
        }
    }

    private void ShowAtIndex(int index, Sprite icon, int stacks, int duration, Sprite arrow)
    {
        // Mở rộng pool khi cần
        while (_pool.Count <= index)
            _pool.Add(Instantiate(statusIconPrefab, transform));

        _pool[index].SetActive(true);
        var ui = _pool[index].GetComponent<StatusIconUI>();
        ui?.Setup(icon, stacks, duration, arrow);
    }
}
