using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using UnityEngine.EventSystems;

public class InventoryUI : MonoBehaviour
{
    public static InventoryUI Instance { get; private set; }
    [Header("UI References")]
    public GameObject inventoryPanel;
    public GameObject rootCanvas; // Kéo Canvas_Inventory gốc vào đây để ẩn hoàn toàn
    public Transform combatSlotsParent;
    public Transform storageSlotsParent;
    public GameObject slotPrefab;

    [Header("Description & Info")]
    public TextMeshProUGUI itemDescriptionText;
    public Image itemIconPreview;

    [Header("Text Display")]
    public TextMeshProUGUI goldText;
    public TextMeshProUGUI foodText;

    [Header("Selection Colors")]
    public Color selectedColor = Color.white; // Màu khi chọn (Trắng sáng)
    public Color normalColor = new Color(0.5f, 0.5f, 0.5f, 0.5f); // Màu thường (Xám mờ)

    private List<GameObject> combatSlotObjects = new List<GameObject>();
    private List<GameObject> storageSlotObjects = new List<GameObject>();

    private int selectedSlotIndex = 0;
    private bool isInCombatInventory = true;
    private bool isInventoryOpen = false;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        InitializeSlots();

        InventoryManager.Instance.OnInventoryChanged -= UpdateInventoryDisplay;
        InventoryManager.Instance.OnInventoryChanged += UpdateInventoryDisplay;

        if (inventoryPanel != null)
            inventoryPanel.SetActive(false);

        UpdateInventoryDisplay();
    }

    void InitializeSlots()
    {
        if (slotPrefab == null) return;

        // Combat inventory: không giới hạn slot — spawn động khi UpdateInventoryDisplay
        // Không pre-spawn ở đây, UpdateCombatSlots sẽ tạo slot khi cần

        // Storage inventory: giới hạn cố định
        if (storageSlotsParent != null)
        {
            int storageCount = InventoryManager.Instance != null
                ? InventoryManager.Instance.storageMaxSlots
                : 20;

            for (int i = 0; i < storageCount; i++)
            {
                GameObject slot = Instantiate(slotPrefab, storageSlotsParent);
                storageSlotObjects.Add(slot);

                Button slotButton = slot.GetComponent<Button>();
                if (slotButton != null)
                {
                    int index = i;
                    slotButton.onClick.AddListener(() => OnSlotClicked(index, false));
                }
                AddSlotHoverEvent(slot, i, false);
            }
        }
        UpdateSelection();
    }

    void AddSlotHoverEvent(GameObject slot, int index, bool isCombat)
    {
        EventTrigger trigger = slot.GetComponent<EventTrigger>();
        if (trigger == null) trigger = slot.AddComponent<EventTrigger>();
        trigger.triggers.Clear();

        // CHỈ XỬ LÝ HOVER: Di chuột vào -> Làm sáng ô và hiện mô tả
        var hoverEntry = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
        hoverEntry.callback.AddListener((_) =>
        {
            if (!isInventoryOpen) return;
            var dataSlots = isCombat ? InventoryManager.Instance.combatInventory : InventoryManager.Instance.storageInventory;
            if (index >= dataSlots.Count || dataSlots[index].IsEmpty) return;

            selectedSlotIndex = index;
            isInCombatInventory = isCombat;
            UpdateSelection(); // Kích hoạt UI làm sáng ô
            UpdateDescription();
        });
        trigger.triggers.Add(hoverEntry);
    }

    private void Update()
    {
        if (!isInventoryOpen) return;

        if (Input.GetMouseButtonDown(1))
        {
            ToggleInventory();
            return;
        }

        HandleKeyboardNavigation();

        if (PlayerManager.Instance != null)
        {
            if (goldText != null) goldText.text = $"Vàng: {PlayerManager.Instance.gold}";
            if (foodText != null) foodText.text = $"Lương thực: {PlayerManager.Instance.food}/{PlayerManager.Instance.maxFood}";
        }
    }

    void HandleKeyboardNavigation()
    {
        var dataSlots = isInCombatInventory
            ? InventoryManager.Instance.combatInventory
            : InventoryManager.Instance.storageInventory;

        int itemCount = 0;
        for (int i = 0; i < dataSlots.Count; i++)
            if (!dataSlots[i].IsEmpty) itemCount++;

        if (Input.GetKeyDown(KeyCode.X) || Input.GetKeyDown(KeyCode.I))
        {
            ToggleInventory();
            return;
        }

        if (itemCount == 0) return;

        if (Input.GetKeyDown(KeyCode.Tab))
        {
            isInCombatInventory = !isInCombatInventory;
            selectedSlotIndex = 0;
            UpdateSelection();
            UpdateDescription();
            return;
        }

        bool HasItem(int idx) =>
            idx >= 0 && idx < dataSlots.Count && !dataSlots[idx].IsEmpty;

        if (Input.GetKeyDown(KeyCode.UpArrow))
        {
            int next = selectedSlotIndex - 2;
            while (next >= 0 && !HasItem(next)) next--;
            if (next >= 0) selectedSlotIndex = next;
            UpdateSelection(); UpdateDescription();
            return;
        }

        if (Input.GetKeyDown(KeyCode.DownArrow))
        {
            int next = selectedSlotIndex + 2;
            while (next < dataSlots.Count && !HasItem(next)) next++;
            if (next < dataSlots.Count) selectedSlotIndex = next;
            UpdateSelection(); UpdateDescription();
            return;
        }

        if (Input.GetKeyDown(KeyCode.LeftArrow))
        {
            int next = selectedSlotIndex - 1;
            while (next >= 0 && !HasItem(next)) next--;
            if (next >= 0) selectedSlotIndex = next;
            UpdateSelection(); UpdateDescription();
            return;
        }

        if (Input.GetKeyDown(KeyCode.RightArrow))
        {
            int next = selectedSlotIndex + 1;
            while (next < dataSlots.Count && !HasItem(next)) next++;
            if (next < dataSlots.Count) selectedSlotIndex = next;
            UpdateSelection(); UpdateDescription();
            return;
        }

        if (Input.GetKeyDown(KeyCode.Z))
        {
            UseSelectedItem();
            return;
        }
    }

    void UpdateSelection()
    {
        // 1. DỌN SẠCH: Tắt toàn bộ viền và reset màu nền của CẢ 2 ngăn
        foreach (var slot in combatSlotObjects)
        {
            if (slot != null)
            {
                slot.GetComponent<Image>().color = normalColor;
                Transform border = slot.transform.Find("SelectionBorder");
                if (border != null) border.gameObject.SetActive(false);
            }
        }

        foreach (var slot in storageSlotObjects)
        {
            if (slot != null)
            {
                slot.GetComponent<Image>().color = normalColor;
                Transform border = slot.transform.Find("SelectionBorder");
                if (border != null) border.gameObject.SetActive(false);
            }
        }

        // 2. BẬT SÁNG: Chỉ bật viền và đổi màu cho ĐÚNG Ô đang được chuột chỉ vào
        var activeSlots = isInCombatInventory ? combatSlotObjects : storageSlotObjects;

        if (selectedSlotIndex >= 0 && selectedSlotIndex < activeSlots.Count)
        {
            GameObject activeSlot = activeSlots[selectedSlotIndex];
            if (activeSlot != null)
            {
                // Bật sáng nền
                activeSlot.GetComponent<Image>().color = selectedColor;

                // Bật sáng viền
                Transform border = activeSlot.transform.Find("SelectionBorder");
                if (border != null) border.gameObject.SetActive(true);
            }
        }
    }

    void UpdateDescription()
    {
        if (itemDescriptionText == null) return;

        var slots = isInCombatInventory
            ? InventoryManager.Instance.combatInventory
            : InventoryManager.Instance.storageInventory;

        if (selectedSlotIndex >= 0 && selectedSlotIndex < slots.Count)
        {
            var slot = slots[selectedSlotIndex];

            if (slot.IsEmpty)
            {
                itemDescriptionText.gameObject.SetActive(true);
                itemDescriptionText.text = "";
            }
            else
            {
                itemDescriptionText.gameObject.SetActive(true);
                // ĐÃ XÓA ÉP MÀU, CHỈ CÒN THỂ BOLD CHO TÊN VẬT PHẨM
                itemDescriptionText.text = $"<b>{slot.item.itemName}</b>\n{slot.item.description}";
            }
        }
    }

    public void UpdateInventoryDisplay()
    {
        UpdateCombatSlots();
        UpdateStorageSlots();
        UpdateSelection();
        UpdateDescription();

        if (combatSlotsParent != null)
            UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(combatSlotsParent.GetComponent<RectTransform>());
        if (storageSlotsParent != null)
            UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(storageSlotsParent.GetComponent<RectTransform>());
    }

    private void UpdateCombatSlots()
    {
        if (combatSlotsParent == null || slotPrefab == null) return;
        var slots = InventoryManager.Instance.combatInventory;

        // Đảm bảo đủ slot object cho số item hiện có
        while (combatSlotObjects.Count < slots.Count)
        {
            int idx = combatSlotObjects.Count;
            GameObject newSlot = Instantiate(slotPrefab, combatSlotsParent);
            combatSlotObjects.Add(newSlot);

            Button btn = newSlot.GetComponent<Button>();
            if (btn != null)
            {
                int capturedIdx = idx;
                btn.onClick.AddListener(() => OnSlotClicked(capturedIdx, true));
            }
            AddSlotHoverEvent(newSlot, idx, true);
        }

        for (int i = 0; i < combatSlotObjects.Count; i++)
        {
            bool hasItem = (i < slots.Count && !slots[i].IsEmpty);
            combatSlotObjects[i].SetActive(hasItem);
            if (hasItem) UpdateSlotUI(combatSlotObjects[i], slots[i], i);
        }
    }

    private void UpdateStorageSlots()
    {
        var slots = InventoryManager.Instance.storageInventory;
        for (int i = 0; i < storageSlotObjects.Count; i++)
        {
            bool hasItem = (i < slots.Count && !slots[i].IsEmpty);
            storageSlotObjects[i].SetActive(hasItem);

            if (hasItem)
                UpdateSlotUI(storageSlotObjects[i], slots[i], i);
        }
    }

    private void UpdateSlotUI(GameObject slotObj, InventorySlot slot, int index)
    {
        Image iconImage = slotObj.transform.Find("ItemIcon")?.GetComponent<Image>();
        TextMeshProUGUI nameText = slotObj.transform.Find("ItemNameText")?.GetComponent<TextMeshProUGUI>();
        TextMeshProUGUI quantityText = slotObj.transform.Find("QuantityText")?.GetComponent<TextMeshProUGUI>();

        if (slot.IsEmpty)
        {
            if (iconImage != null) { iconImage.enabled = false; iconImage.sprite = null; }
            if (nameText != null) nameText.text = "";
            if (quantityText != null) quantityText.text = "";
        }
        else
        {
            if (iconImage != null && slot.item?.icon != null)
            {
                iconImage.sprite = slot.item.icon;
                iconImage.enabled = true;
                iconImage.color = Color.white;
            }
            if (nameText != null && slot.item != null)
            {
                nameText.text = slot.item.itemName;
                nameText.color = Color.white;
                nameText.gameObject.SetActive(true);
            }
            if (quantityText != null && slot.item != null)
                quantityText.text = (slot.item.isStackable && slot.quantity > 1) ? $"x{slot.quantity}" : "";
        }
    }

    private void OnSlotClicked(int slotIndex, bool isCombat)
    {
        selectedSlotIndex = slotIndex;
        isInCombatInventory = isCombat;
        UpdateSelection();
        UpdateDescription();

        UseSelectedItem();
    }

    void UseSelectedItem()
    {
        var slots = isInCombatInventory ? InventoryManager.Instance.combatInventory : InventoryManager.Instance.storageInventory;
        if (selectedSlotIndex >= 0 && selectedSlotIndex < slots.Count)
        {
            var slot = slots[selectedSlotIndex];
            if (!slot.IsEmpty && ItemUsageManager.Instance != null)
            {
                bool success = ItemUsageManager.Instance.UseItem(slot.item);
                if (success)
                    ToggleInventory();
            }
        }
    }

    public void ToggleInventory()
    {
        isInventoryOpen = !isInventoryOpen;

        GameObject targetRoot = (rootCanvas != null) ? rootCanvas : inventoryPanel;

        if (targetRoot != null)
            targetRoot.SetActive(isInventoryOpen);

        if (inventoryPanel != null && inventoryPanel != targetRoot)
            inventoryPanel.SetActive(isInventoryOpen);

        if (isInventoryOpen)
        {
            Canvas.ForceUpdateCanvases();
            RectTransform panelRect = inventoryPanel != null ? inventoryPanel.GetComponent<RectTransform>() : null;
            if (panelRect != null)
                UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(panelRect);

            UpdateInventoryDisplay();
            selectedSlotIndex = 0;
            isInCombatInventory = true;
            UpdateSelection();
            UpdateDescription();

            if (CombatManager.Instance != null)
                CombatManager.Instance.UI?.ShowActionMenu(false);

            Debug.Log("[INVENTORY] Đã mở túi đồ. Dùng phím mũi tên để di chuyển, Z để dùng item, X hoặc I để đóng.");
        }
        else
        {
            if (itemDescriptionText != null)
            {
                itemDescriptionText.gameObject.SetActive(false);
                itemDescriptionText.text = "";
            }
            if (CombatManager.Instance != null)
            {
                StartCoroutine(ResumeCombatNextFrame());
            }
        }
    }

    private System.Collections.IEnumerator ResumeCombatNextFrame()
    {
        yield return null;
        if (CombatManager.Instance != null)
        {
            CombatManager.Instance.UI?.ShowActionMenu(true);
            CombatManager.Instance.ResumeCombat();
        }
    }
}