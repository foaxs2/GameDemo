using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class InventorySlot
{
    public ItemData item;
    public int quantity;

    public bool IsEmpty => item == null || quantity <= 0;

    public void Clear()
    {
        item = null;
        quantity = 0;
    }
}

/// <summary>Một dòng trong danh sách đồ test.</summary>
[Serializable]
public class TestItemEntry
{
    public ItemData item;
    [Range(1, 99)] public int quantity = 1;
}

public class InventoryManager : MonoBehaviour
{
    public static InventoryManager Instance { get; private set; }

    [Header("Cấu hình Inventory")]
    [Tooltip("Số ô tối đa của kho lưu trữ (Storage). Combat inventory không giới hạn.")]
    public int storageMaxSlots = 20;

    // Combat inventory: không giới hạn ô, item stack lên đến maxStackSize
    public List<InventorySlot> combatInventory = new List<InventorySlot>();
    // Storage inventory: giới hạn 20 ô, không stack (đồ vật phẩm loại 2)
    public List<InventorySlot> storageInventory = new List<InventorySlot>();

    [Header("Trang bị hiện tại")]
    public ItemData equippedWeapon;
    public ItemData equippedArmor;
    public ItemData equippedAccessory1;
    public ItemData equippedAccessory2;

    public event Action OnInventoryChanged;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        InitializeStorageInventory();
    }
    [Header("Đồ Test (Dùng Context Menu — Không Tự Chạy)")]
    [Tooltip("Thêm nhiều dòng, mỗi dòng kéo 1 ItemData và điền số lượng. Sau đó bấm ⟳ → Thêm Đồ Test Thủ Công.")]
    public List<TestItemEntry> testItems = new List<TestItemEntry>();

    private void InitializeStorageInventory()
    {
        storageInventory.Clear();
        for (int i = 0; i < storageMaxSlots; i++)
            storageInventory.Add(new InventorySlot());

        // Combat inventory bắt đầu rỗng, tự mở rộng khi cần
        combatInventory.Clear();
        Debug.Log($"[INVENTORY] Storage: {storageMaxSlots} ô. Combat: không giới hạn.");
    }

    // === THÊM ITEM ===
    public bool AddItem(ItemData item, int quantity = 1)
    {
        if (item == null) return false;

        if (item.itemType == ItemType.Equipment)
            return AddEquipmentToStorage(item);

        if (item.usableInCombat || item.itemType == ItemType.Consumable)
            return AddToCombatInventory(item, quantity);
        else
            return AddToStorage(item, quantity);
    }

    // Combat inventory: thêm vào slot hiện có hoặc tạo slot mới (không giới hạn)
    private bool AddToCombatInventory(ItemData item, int quantity)
    {
        // Giới hạn đặc biệt: Bình máu (HP) tối đa chỉ 3 bình
        if (item.itemType == ItemType.Consumable && item.consumableType == ConsumableType.HP)
        {
            int currentQty = GetItemQuantity(item);
            if (currentQty >= 3)
            {
                Debug.LogWarning("[INVENTORY] Bạn đã mang tối đa 3 bình máu (3/3)!");
                return false;
            }
            quantity = Mathf.Min(quantity, 3 - currentQty);
        }

        int effectiveMaxStack = item.maxStackSize > 1 ? item.maxStackSize : 99;

        // Tìm slot hiện có để gộp
        foreach (var slot in combatInventory)
        {
            if (slot.item == item && slot.quantity < effectiveMaxStack)
            {
                int space = effectiveMaxStack - slot.quantity;
                int toAdd = Mathf.Min(quantity, space);
                slot.quantity += toAdd;
                quantity -= toAdd;
                if (quantity <= 0) { OnInventoryChanged?.Invoke(); return true; }
            }
        }

        // Tạo slot mới (không giới hạn số slot)
        if (quantity > 0)
        {
            combatInventory.Add(new InventorySlot { item = item, quantity = quantity });
        }
        OnInventoryChanged?.Invoke();
        return true;
    }

    private bool AddEquipmentToStorage(ItemData item)
    {
        for (int i = 0; i < storageInventory.Count; i++)
        {
            if (storageInventory[i].IsEmpty)
            {
                storageInventory[i].item = item;
                storageInventory[i].quantity = 1;
                OnInventoryChanged?.Invoke();
                return true;
            }
        }
        Debug.LogWarning("[INVENTORY] Kho Storage đầy, không thêm được trang bị!");
        return false;
    }

    private bool AddToStorage(ItemData item, int quantity)
    {
        int effectiveMaxStack = item.maxStackSize > 1 ? item.maxStackSize : 99;

        for (int i = 0; i < storageInventory.Count; i++)
        {
            if (storageInventory[i].item == item && storageInventory[i].quantity < effectiveMaxStack)
            {
                int space = effectiveMaxStack - storageInventory[i].quantity;
                int toAdd = Mathf.Min(quantity, space);
                storageInventory[i].quantity += toAdd;
                quantity -= toAdd;
                if (quantity <= 0) { OnInventoryChanged?.Invoke(); return true; }
            }
        }
        for (int i = 0; i < storageInventory.Count; i++)
        {
            if (storageInventory[i].IsEmpty)
            {
                storageInventory[i].item = item;
                storageInventory[i].quantity = quantity;
                OnInventoryChanged?.Invoke();
                return true;
            }
        }
        Debug.LogWarning("[INVENTORY] Storage đầy!");
        return false;
    }

    // === XÓA ITEM ===
    public bool RemoveItem(ItemData item, int quantity = 1)
    {
        if (RemoveFromList(combatInventory, item, quantity)) return true;
        return RemoveFromList(storageInventory, item, quantity);
    }

    private bool RemoveFromList(List<InventorySlot> list, ItemData item, int quantity)
    {
        for (int i = 0; i < list.Count; i++)
        {
            if (list[i].item == item)
            {
                list[i].quantity -= quantity;
                if (list[i].quantity <= 0)
                {
                    if (list == combatInventory)
                        list.RemoveAt(i); // Combat: xóa slot hẳn
                    else
                        list[i].Clear();  // Storage: giữ slot rỗng
                }
                OnInventoryChanged?.Invoke();
                return true;
            }
        }
        return false;
    }

    // === KIỂM TRA ===
    public int GetItemQuantity(ItemData item)
    {
        int total = 0;
        foreach (var s in combatInventory) if (s.item == item) total += s.quantity;
        foreach (var s in storageInventory) if (s.item == item) total += s.quantity;
        return total;
    }

    public bool HasItem(ItemData item, int quantity = 1) => GetItemQuantity(item) >= quantity;

    // === TRANG BỊ ===
    public bool EquipItem(ItemData item, EquipmentSlot? targetSlot = null)
    {
        if (item.itemType != ItemType.Equipment) return false;

        EquipmentSlot slotToEquip = targetSlot ?? item.equipmentSlot;

        if (slotToEquip == EquipmentSlot.Accessory1 && equippedAccessory2 == item && GetItemQuantity(item) < 2)
            equippedAccessory2 = null;
        else if (slotToEquip == EquipmentSlot.Accessory2 && equippedAccessory1 == item && GetItemQuantity(item) < 2)
            equippedAccessory1 = null;

        switch (slotToEquip)
        {
            case EquipmentSlot.Weapon:     equippedWeapon     = item; break;
            case EquipmentSlot.Armor:      equippedArmor      = item; break;
            case EquipmentSlot.Accessory1: equippedAccessory1 = item; break;
            case EquipmentSlot.Accessory2: equippedAccessory2 = item; break;
        }

        Debug.Log($"Đã trang bị {item.itemName}");
        PlayerManager.Instance?.UpdateEquipmentStats();
        CombatManager.Instance?.ForceUpdatePlayerUI();
        OnInventoryChanged?.Invoke();
        return true;
    }

    // === TEST (ContextMenu — chỉ chạy khi right-click trong Inspector) ===
    [ContextMenu("Thêm Đồ Test Thủ Công")]
    public void AddTestItemsManual()
    {
        if (testItems == null || testItems.Count == 0)
        {
            Debug.LogWarning("[CONTEXTMENU] Danh sách 'Test Items' đang rỗng. Thêm ít nhất 1 dòng vào Inspector.");
            return;
        }

        int added = 0;
        foreach (var entry in testItems)
        {
            if (entry.item == null) continue;
            bool success = AddItem(entry.item, entry.quantity);
            if (success)
            {
                Debug.Log($"[CONTEXTMENU] ✓ Thêm {entry.quantity}x {entry.item.itemName}");
                added++;
            }
            else
                Debug.LogWarning($"[CONTEXTMENU] ✗ Không thể thêm {entry.item.itemName} (Storage có thể đầy).");
        }
        Debug.Log($"[CONTEXTMENU] Xong — đã thêm {added}/{testItems.Count} món vào inventory.");
    }
}