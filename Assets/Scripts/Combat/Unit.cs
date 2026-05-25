using System.Collections.Generic;
using UnityEngine;

public abstract class Unit : MonoBehaviour
{
    [Header("Chỉ số Sinh tồn chung")]
    public int maxHP;
    public int currentHP;

    [Header("Chỉ số Chiến đấu chung")]
    public float baseDefense;
    public float currentDefense;
    public float baseSpeed;
    public float currentSpeed;
    public float currentAP;

    [Header("Dữ liệu Debuff")]
    public List<DebuffInstance> debuffs = new List<DebuffInstance>();

    [Header("Dữ liệu Buff")]
    public List<BuffInstance> buffs = new List<BuffInstance>();

    [Header("Cờ Trạng Thái")]
    public bool isImmuneToStun;
    public bool immuneToBleed;
    public bool weakToPoison;
    public bool weakToBurn;
    public bool weakToBleed;

    public abstract void TakeDamage(float damage, bool isTrueDamage = false, bool ignoreFracture = false);
    public virtual void OnImmediateActionReady() { }

    public virtual float GetTotalAttack() { return 0f; }
    public virtual float GetTotalSpeed() { return 0f; }
    public virtual float GetTotalCrit() { return 0f; }
    public virtual float GetTotalEvasion() { return 0f; }
}