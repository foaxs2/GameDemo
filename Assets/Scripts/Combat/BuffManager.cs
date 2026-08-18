using System;
using System.Collections.Generic;
using UnityEngine;

public class BuffManager : MonoBehaviour
{
    public static BuffManager Instance { get; private set; }

    public event Action<Unit, BuffType, int> OnBuffApplied;
    public event Action<Unit, BuffType>      OnBuffRemoved;
    public event Action<Unit, BuffType>      OnBuffTick;    // Fire khi duration giảm nhưng buff vẫn còn

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    // ─── ADD ─────────────────────────────────────────────────────────────

    /// <summary>Thêm buff thông thường. Nếu đã có cùng loại → stack + refresh duration. Giới hạn tối đa là 3 stacks.</summary>
    public void AddBuff(Unit target, BuffType type, float value, int duration, int maxStacks = 3)
    {
        if (target == null) return;
        maxStacks = Mathf.Min(3, maxStacks);
        BuffInstance existing = GetBuff(target, type);
        if (existing != null)
        {
            existing.Stacks         = Mathf.Min(maxStacks, existing.Stacks + 1);
            existing.RemainingTurns = duration;
            OnBuffApplied?.Invoke(target, type, existing.Stacks);
        }
        else
        {
            target.buffs.Add(new BuffInstance(type, value, duration, maxStacks));
            OnBuffApplied?.Invoke(target, type, 1);
        }
    }

    /// <summary>Tẩm vũ khí — độc nhất. Ghi đè loại cũ nếu khác.</summary>
    public void AddWeaponCoating(Unit target, WeaponCoatingType coatingType, float chance, int duration)
    {
        if (target == null) return;
        // Xóa coating cũ (nếu có) — chỉ 1 loại tại 1 thời điểm
        RemoveBuff(target, BuffType.WeaponCoating);
        target.buffs.Add(new BuffInstance(coatingType, chance, duration));
        OnBuffApplied?.Invoke(target, BuffType.WeaponCoating, 1);
    }

    // ─── REMOVE ──────────────────────────────────────────────────────────

    public void RemoveBuff(Unit target, BuffType type)
    {
        if (target == null) return;
        int idx = target.buffs.FindIndex(b => b.Type == type);
        if (idx >= 0)
        {
            target.buffs.RemoveAt(idx);
            OnBuffRemoved?.Invoke(target, type);
        }
    }

    public void ClearAllBuffs(Unit target)
    {
        if (target == null) return;
        for (int i = target.buffs.Count - 1; i >= 0; i--)
        {
            BuffType t = target.buffs[i].Type;
            target.buffs.RemoveAt(i);
            OnBuffRemoved?.Invoke(target, t);
        }
    }

    // ─── TICK ────────────────────────────────────────────────────────────

    /// <summary>Trừ 1 lượt của 1 loại buff cụ thể (dùng khi bị đánh hoặc xả stack).</summary>
    public void ConsumeBuffTurn(Unit target, BuffType type)
    {
        if (target == null) return;
        int idx = target.buffs.FindIndex(b => b.Type == type);
        if (idx >= 0)
        {
            target.buffs[idx].RemainingTurns--;
            if (target.buffs[idx].RemainingTurns <= 0)
            {
                target.buffs.RemoveAt(idx);
                OnBuffRemoved?.Invoke(target, type);
            }
            else
            {
                OnBuffTick?.Invoke(target, type);
            }
        }
    }

    /// <summary>Gọi khi unit bắt đầu lượt hành động. Trừ 1 lượt, xóa khi hết.</summary>
    public void ProcessUnitTurnTick(Unit unit)
    {
        if (unit == null) return;
        for (int i = unit.buffs.Count - 1; i >= 0; i--)
        {
            unit.buffs[i].RemainingTurns--;
            if (unit.buffs[i].RemainingTurns <= 0)
            {
                BuffType expired = unit.buffs[i].Type;
                unit.buffs.RemoveAt(i);
                OnBuffRemoved?.Invoke(unit, expired);
            }
            else
            {
                // Fire tick để StatusIconContainer cập nhật số lượt hiển thị
                OnBuffTick?.Invoke(unit, unit.buffs[i].Type);
            }
        }
    }

    // ─── QUERY ───────────────────────────────────────────────────────────

    public BuffInstance GetBuff(Unit target, BuffType type)
    {
        if (target == null) return null;
        for (int i = 0; i < target.buffs.Count; i++)
            if (target.buffs[i].Type == type) return target.buffs[i];
        return null;
    }

    public bool HasBuff(Unit target, BuffType type) => GetBuff(target, type) != null;

    /// <summary>Trả về tổng giá trị buff dựa trên số stacks (tầng 1-2 cộng dồn tuyến tính, từ tầng 3 trở đi cộng thêm gấp đôi giá trị gốc).</summary>
    public float GetBuffValue(Unit target, BuffType type)
    {
        var b = GetBuff(target, type);
        if (b == null) return 0f;
        return b.Stacks <= 2 ? b.Value * b.Stacks : b.Value * 2 * (b.Stacks - 1);
    }

    public int GetBuffStacks(Unit target, BuffType type)
    {
        var b = GetBuff(target, type);
        return b != null ? b.Stacks : 0;
    }

    public WeaponCoatingType GetWeaponCoating(Unit target)
    {
        var b = GetBuff(target, BuffType.WeaponCoating);
        return b?.CoatingType ?? WeaponCoatingType.None;
    }

    public float GetWeaponCoatingChance(Unit target)
    {
        var b = GetBuff(target, BuffType.WeaponCoating);
        return b?.CoatingChance ?? 0f;
    }
}
