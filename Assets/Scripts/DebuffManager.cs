using System;
using UnityEngine;

public class DebuffManager : MonoBehaviour
{
    public static DebuffManager Instance { get; private set; }

    public event Action<Unit, DebuffType, int> OnDebuffApplied;
    public event Action<Unit, DebuffType> OnDebuffRemoved;
    public event Action<Unit, DebuffType, float> OnDebuffTick;

    private void Awake()
    {
        if (Instance != null && Instance != this) Destroy(gameObject);
        else Instance = this;
    }

    public bool CheckImmunity(Unit target, DebuffType type)
    {
        if (type == DebuffType.Stun && target.isImmuneToStun) return true;
        if (type == DebuffType.Bleed && target.immuneToBleed) return true;
        return false;
    }

    public void AddDebuff(Unit target, DebuffType type, int duration, int stacks = 1, float baseBleedDamage = 0f)
    {
        if (CheckImmunity(target, type)) return;

        DebuffInstance existingDebuff = GetDebuff(target, type);

        if (existingDebuff != null)
        {
            switch (type)
            {
                case DebuffType.Stun:
                case DebuffType.Fracture:
                    existingDebuff.Duration = duration;
                    break;
                case DebuffType.Poison:
                    if (existingDebuff.Duration > 1) existingDebuff.Stacks = Mathf.Min(3, existingDebuff.Stacks + 1);
                    existingDebuff.Duration = duration;
                    break;
                case DebuffType.Burn:
                    existingDebuff.Stacks = Mathf.Min(3, existingDebuff.Stacks + stacks);
                    existingDebuff.Duration = duration;
                    break;
                case DebuffType.Bleed:
                    existingDebuff.Duration = duration;
                    existingDebuff.StoredDamage = baseBleedDamage;
                    break;
            }
            OnDebuffApplied?.Invoke(target, type, existingDebuff.Stacks);
        }
        else
        {
            DebuffInstance newDebuff = new DebuffInstance(type, duration, stacks, baseBleedDamage);
            target.debuffs.Add(newDebuff);
            OnDebuffApplied?.Invoke(target, type, stacks);
        }
        UpdateUnitStats(target);
    }

    public void RemoveDebuff(Unit target, DebuffType type)
    {
        int index = target.debuffs.FindIndex(d => d.Type == type);
        if (index >= 0)
        {
            target.debuffs.RemoveAt(index);
            OnDebuffRemoved?.Invoke(target, type);
            UpdateUnitStats(target);
        }
    }

    public void ClearAllDebuffs(Unit target)
    {
        for (int i = target.debuffs.Count - 1; i >= 0; i--)
        {
            DebuffType type = target.debuffs[i].Type;
            target.debuffs.RemoveAt(i);
            OnDebuffRemoved?.Invoke(target, type);
        }
        UpdateUnitStats(target);
    }

    public bool HasDebuff(Unit target, DebuffType type)
    {
        for (int i = 0; i < target.debuffs.Count; i++)
        {
            if (target.debuffs[i].Type == type) return true;
        }
        return false;
    }

    private DebuffInstance GetDebuff(Unit target, DebuffType type)
    {
        for (int i = 0; i < target.debuffs.Count; i++)
        {
            if (target.debuffs[i].Type == type) return target.debuffs[i];
        }
        return null;
    }

    public void ProcessTurnStart(Unit unit)
    {
        bool isStunned = HasDebuff(unit, DebuffType.Stun);

        for (int i = unit.debuffs.Count - 1; i >= 0; i--)
        {
            DebuffInstance debuff = unit.debuffs[i];
            bool debuffExpired = false;

            switch (debuff.Type)
            {
                case DebuffType.Poison:
                    float poisonDmg = CalculatePoisonDamage(unit, debuff.Stacks);
                    int durationDecay = 1;
                    if (isStunned)
                    {
                        poisonDmg *= 2f;
                        durationDecay = 2;
                    }
                    unit.TakeDamage(poisonDmg, true);
                    OnDebuffTick?.Invoke(unit, DebuffType.Poison, poisonDmg);
                    debuff.Duration -= durationDecay;
                    if (debuff.Duration <= 0) debuffExpired = true;
                    break;

                case DebuffType.Burn:
                    float burnDmg = CalculateBurnDamage(unit, debuff.Stacks);
                    unit.TakeDamage(burnDmg, true);
                    OnDebuffTick?.Invoke(unit, DebuffType.Burn, burnDmg);
                    debuff.Duration--;
                    if (debuff.Duration <= 0) debuffExpired = true;
                    break;

                case DebuffType.Bleed:
                    float bleedDmg = CalculateBleedDamage(unit, debuff.StoredDamage);
                    unit.TakeDamage(bleedDmg, false, true);
                    OnDebuffTick?.Invoke(unit, DebuffType.Bleed, bleedDmg);
                    debuff.Duration--;
                    if (debuff.Duration <= 0) debuffExpired = true;
                    break;
            }

            if (debuffExpired)
            {
                DebuffType expiredType = debuff.Type;
                unit.debuffs.RemoveAt(i);
                OnDebuffRemoved?.Invoke(unit, expiredType);
            }
        }
        UpdateUnitStats(unit);
    }

    public void ProcessTurnEnd(Unit unit)
    {
        for (int i = unit.debuffs.Count - 1; i >= 0; i--)
        {
            DebuffInstance debuff = unit.debuffs[i];
            bool debuffExpired = false;

            if (debuff.Type == DebuffType.Stun || debuff.Type == DebuffType.Fracture)
            {
                debuff.Duration--;
                if (debuff.Duration <= 0)
                {
                    debuffExpired = true;
                    if (debuff.Type == DebuffType.Stun && unit.currentAP >= 100f) unit.OnImmediateActionReady();
                }
            }

            if (debuffExpired)
            {
                DebuffType expiredType = debuff.Type;
                unit.debuffs.RemoveAt(i);
                OnDebuffRemoved?.Invoke(unit, expiredType);
            }
        }
        UpdateUnitStats(unit);
    }

    private float CalculatePoisonDamage(Unit target, int stacks)
    {
        float damage = stacks switch { 1 => 3f, 2 => 5f, _ => 8f };
        if (target.weakToPoison) damage += (target.currentHP * 0.05f);
        return Mathf.Max(0, damage);
    }

    private float CalculateBurnDamage(Unit target, int stacks)
    {
        float damage = stacks switch { 1 => 1f, 2 => 3f, _ => 5f };
        if (target.weakToBurn) damage += (target.currentHP * 0.05f);
        return Mathf.Max(0, damage);
    }

    private float CalculateBleedDamage(Unit target, float baseAttackDamage)
    {
        float hpPercentage = target.weakToBleed ? 0.12f : 0.08f;
        return (target.maxHP * hpPercentage) + baseAttackDamage;
    }

    public void UpdateUnitStats(Unit unit)
    {
        int defReduction = 0;
        float spdMultiplier = 1f;

        for (int i = 0; i < unit.debuffs.Count; i++)
        {
            DebuffInstance d = unit.debuffs[i];
            if (d.Type == DebuffType.Burn) defReduction += d.Stacks switch { 1 => 1, 2 => 2, _ => 4 };
            else if (d.Type == DebuffType.Fracture) spdMultiplier -= 0.20f;
        }

        unit.currentDefense = Mathf.Max(0, unit.baseDefense - defReduction);
        unit.currentSpeed = unit.baseSpeed * spdMultiplier;
    }

    public float GetDamageTakenMultiplier(Unit target)
    {
        if (HasDebuff(target, DebuffType.Fracture)) return 1.30f;
        return 1.0f;
    }
}