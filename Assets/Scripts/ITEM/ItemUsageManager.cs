using UnityEngine;

public class ItemUsageManager : MonoBehaviour
{
    public static ItemUsageManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    public bool UseItem(ItemData item)
    {
        if (item == null) return false;

        if (item.itemType == ItemType.Consumable)
        {
            return UseConsumable(item);
        }
        else if (item.itemType == ItemType.Equipment)
        {
            Debug.Log("Không thể sử dụng trực tiếp trang bị. Hãy trang bị nó!");
            return false;
        }

        return false;
    }

    private bool UseConsumable(ItemData item)
    {
        var player = PlayerManager.Instance;
        bool used = false;

        switch (item.consumableType)
        {
            case ConsumableType.HP:
                int healAmount = item.healAmount + Mathf.FloorToInt(player.maxHP * item.healPercentage);
                player.currentHP = Mathf.Min(player.maxHP, player.currentHP + healAmount);
                Debug.Log($"Sử dụng {item.itemName}: Hồi {healAmount} HP");
                used = true;
                break;

            case ConsumableType.SEN:
                player.AddSanity(item.healAmount);
                Debug.Log($"Sử dụng {item.itemName}: Hồi {item.healAmount} SEN");
                used = true;
                break;

            case ConsumableType.Food:
                if (item.foodRestoreAmount > 0)
                {
                    player.food = Mathf.Min(player.maxFood, player.food + item.foodRestoreAmount);
                }
                
                if (item.healAmount > 0)
                {
                    player.currentHP = Mathf.Min(player.maxHP, player.currentHP + item.healAmount);
                }
                Debug.Log($"Sử dụng {item.itemName}: Hồi {item.foodRestoreAmount} Food và {item.healAmount} HP");
                used = true;
                break;

            case ConsumableType.Buff_ATK:
                ApplyBuff(BuffType.ATK_Up, item.buffValue, item.buffDuration, item.maxStacks);
                Debug.Log($"Sử dụng {item.itemName}: +{item.buffValue} ATK trong {item.buffDuration} lượt");
                used = true;
                break;

            case ConsumableType.Buff_DEF:
                ApplyBuff(BuffType.DEF_Up, item.buffValue, item.buffDuration, item.maxStacks);
                Debug.Log($"Sử dụng {item.itemName}: +{item.buffValue} DEF trong {item.buffDuration} lượt");
                used = true;
                break;

            case ConsumableType.Buff_SPD:
                ApplyBuff(BuffType.SPD_Up, item.buffValue, item.buffDuration, item.maxStacks);
                Debug.Log($"Sử dụng {item.itemName}: +{item.buffValue} SPD trong {item.buffDuration} lượt");
                used = true;
                break;

            case ConsumableType.Buff_CRIT:
                ApplyBuff(BuffType.CRIT_Up, item.buffValue, item.buffDuration, item.maxStacks);
                Debug.Log($"Sử dụng {item.itemName}: +{item.buffValue}% CRIT trong {item.buffDuration} lượt");
                used = true;
                break;

            case ConsumableType.Buff_EVA:
                ApplyBuff(BuffType.EVA_Up, item.buffValue, item.buffDuration, item.maxStacks);
                Debug.Log($"Sử dụng {item.itemName}: +{item.buffValue}% EVA trong {item.buffDuration} lượt");
                used = true;
                break;

            case ConsumableType.WeaponCoating:
                if (BuffManager.Instance != null)
                    BuffManager.Instance.AddWeaponCoating(PlayerManager.Instance,
                        item.coatingType, item.coatingChance, item.buffDuration);
                Debug.Log($"Sử dụng {item.itemName}: Tẩm {item.coatingType} {item.coatingChance}% trong {item.buffDuration} lượt");
                used = true;
                break;

            case ConsumableType.Cleanse:
                if (DebuffManager.Instance != null)
                    DebuffManager.Instance.ClearAllDebuffs(PlayerManager.Instance);
                Debug.Log($"Sử dụng {item.itemName}: Giải toàn bộ debuff");
                used = true;
                break;

            default:
                Debug.Log($"Chưa implement consumable type: {item.consumableType}");
                break;
        }

        if (used)
        {
            // Trừ 1 item khỏi inventory
            InventoryManager.Instance.RemoveItem(item, 1);

            // Cập nhật UI Inventory
            if (InventoryUI.Instance != null)
                InventoryUI.Instance.UpdateInventoryDisplay();

            // FIX: Cập nhật HP bar ngay sau khi dùng item
            // ItemUsageManager thay đổi currentHP nhưng không gọi UpdatePlayerUI
            // nên thanh HP không refresh dù HP đã thực sự thay đổi
            if (CombatManager.Instance != null)
                CombatManager.Instance.ForceUpdatePlayerUI();
        }

        return used;
    }

    private void ApplyBuff(BuffType buffType, float value, int duration, int maxStacks)
    {
        if (BuffManager.Instance == null) return;
        int stacks = maxStacks > 0 ? maxStacks : 3;
        int dur    = duration  > 0 ? duration  : 5;
        BuffManager.Instance.AddBuff(PlayerManager.Instance, buffType, value, dur, stacks);
    }
}