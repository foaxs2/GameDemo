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

public class InventoryManager : MonoBehaviour
{
    public static InventoryManager Instance { get; private set; }

    [Header("Cấu hình Inventory")]
    public int maxSlots = 20;

    // 2 loại inventory
    public List<InventorySlot> combatInventory = new List<InventorySlot>();  // Item dùng được trong combat
    public List<InventorySlot> storageInventory = new List<InventorySlot>(); // Item chỉ dùng ở town

    [Header("Trang bị hiện tại")]
    public ItemData equippedWeapon;
    public ItemData equippedArmor;
    public ItemData equippedAccessory1;
    public ItemData equippedAccessory2;

    public event Action OnInventoryChanged;
    [Header("Đồ tặng sẵn để Test")]
    public ItemData testWeapon;
    public ItemData testArmor;
    public ItemData basicPotion;

    void Start()
    {
        // Tặng đồ cho người chơi ngay khi vào game
        AddItem(testWeapon, 1);
        AddItem(testArmor, 1);
        if (basicPotion != null) AddItem(basicPotion, 3);
    }
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            // Nếu đã tồn tại bản thể cũ, ta HỦY TOÀN BỘ CỤC DƯ THỪA NÀY,
            // tránh việc tạo ra các trường hợp "GameManager (1)" chết trôi
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        InitializeInventories();
    }

    void InitializeInventories()
    {
        combatInventory.Clear();
        storageInventory.Clear();

        for (int i = 0; i < maxSlots; i++)
        {
            combatInventory.Add(new InventorySlot());
            storageInventory.Add(new InventorySlot());
        }

        Debug.Log($"[INVENTORY] Đã khởi tạo {maxSlots} slots.");
    }

    // === THÊM ITEM ===
    public bool AddItem(ItemData item, int quantity = 1)
    {
        if (item == null) return false;

        // Trang bị KHÔNG bao giờ gom stack - mỗi piece chiếm 1 slot riêng
        if (item.itemType == ItemType.Equipment)
        {
            return AddEquipmentToInventory(storageInventory, item);
        }

        // Vật phẩm tiêu hao: thêm vào combat inventory nếu dùng được trong combat
        if (item.usableInCombat || item.itemType == ItemType.Consumable)
        {
            return AddToInventory(combatInventory, item, quantity);
        }
        else
        {
            return AddToInventory(storageInventory, item, quantity);
        }
    }

    // Hàm riêng cho Equipment: mỗi item chiếm 1 slot dù trùng tên
    private bool AddEquipmentToInventory(List<InventorySlot> inventory, ItemData item)
    {
        for (int i = 0; i < inventory.Count; i++)
        {
            if (inventory[i].IsEmpty)
            {
                inventory[i].item = item;
                inventory[i].quantity = 1;
                Debug.Log($"Đã thêm trang bị [{item.itemName}] vào slot {i}");
                OnInventoryChanged?.Invoke();
                return true;
            }
        }
        Debug.LogWarning("Kho đồ đầy, không thể thêm trang bị!");
        return false;
    }

    private bool AddToInventory(List<InventorySlot> inventory, ItemData item, int quantity)
    {
        // Luôn cho phép gộp đồ, nếu maxStackSize = 0 thì mặc định là 99
        int effectiveMaxStack = item.maxStackSize > 1 ? item.maxStackSize : 99;

        // Tìm slot có item giống để cộng dồn
        for (int i = 0; i < inventory.Count; i++)
        {
            if (inventory[i].item == item && inventory[i].quantity < effectiveMaxStack)
            {
                int spaceLeft = effectiveMaxStack - inventory[i].quantity;
                int toAdd = Mathf.Min(quantity, spaceLeft);
                inventory[i].quantity += toAdd;
                quantity -= toAdd;

                if (quantity <= 0)
                {
                    OnInventoryChanged?.Invoke();
                    return true;
                }
            }
        }

        // Tìm slot trống
        for (int i = 0; i < inventory.Count; i++)
        {
            if (inventory[i].IsEmpty)
            {
                inventory[i].item = item;
                inventory[i].quantity = quantity;
                Debug.Log($"Đã thêm {item.itemName} x{quantity} vào inventory");
                OnInventoryChanged?.Invoke();
                return true;
            }
        }

        Debug.LogWarning("Inventory đầy!");
        return false;
    }

    // === XÓA ITEM ===
    public bool RemoveItem(ItemData item, int quantity = 1)
    {
        // Thử xóa từ combat inventory trước
        if (RemoveFromInventory(combatInventory, item, quantity))
            return true;

        // Nếu không có, thử xóa từ storage inventory
        return RemoveFromInventory(storageInventory, item, quantity);
    }

    private bool RemoveFromInventory(List<InventorySlot> inventory, ItemData item, int quantity)
    {
        for (int i = 0; i < inventory.Count; i++)
        {
            if (inventory[i].item == item)
            {
                inventory[i].quantity -= quantity;

                if (inventory[i].quantity <= 0)
                {
                    inventory[i].Clear();
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
        foreach (var slot in combatInventory)
            if (slot.item == item) total += slot.quantity;

        foreach (var slot in storageInventory)
            if (slot.item == item) total += slot.quantity;

        return total;
    }

    public bool HasItem(ItemData item, int quantity = 1)
    {
        return GetItemQuantity(item) >= quantity;
    }

    // === TRANG BỊ ===
    public bool EquipItem(ItemData item, EquipmentSlot? targetSlot = null)
    {
        if (item.itemType != ItemType.Equipment) return false;

        EquipmentSlot slotToEquip = targetSlot ?? item.equipmentSlot;

        if (slotToEquip == EquipmentSlot.Accessory1 && equippedAccessory2 == item && GetItemQuantity(item) < 2)
        {
            equippedAccessory2 = null; // Tự động tháo ở ô 2 ra
        }
        // Tương tự ngược lại cho ô 2
        else if (slotToEquip == EquipmentSlot.Accessory2 && equippedAccessory1 == item && GetItemQuantity(item) < 2)
        {
            equippedAccessory1 = null; // Tự động tháo ở ô 1 ra
        }

        switch (slotToEquip)
        {
            case EquipmentSlot.Weapon:
                equippedWeapon = item;
                break;
            case EquipmentSlot.Armor:
                equippedArmor = item;
                break;
            case EquipmentSlot.Accessory1:
                equippedAccessory1 = item;
                break;
            case EquipmentSlot.Accessory2:
                equippedAccessory2 = item;
                break;
        }

        Debug.Log($"Đã trang bị {item.itemName}");
        if (PlayerManager.Instance != null)
        {
            PlayerManager.Instance.UpdateEquipmentStats();
        }
        
        // Ngay lập tức bắt thanh máu trong Combat cập nhật thay vì chờ bị đánh
        if (CombatManager.Instance != null)
        {
            CombatManager.Instance.ForceUpdatePlayerUI();
        }

        OnInventoryChanged?.Invoke();
        return true;
    }

    // === DEBUG: THÊM ITEM TEST ===
    [ContextMenu("Add Test Items")]
    public void AddTestItems()
    {
        // Tạo item test (sau này sẽ load từ ScriptableObject)
        Debug.Log("Thêm item test... (cần tạo ScriptableObject trước)");
    }
}