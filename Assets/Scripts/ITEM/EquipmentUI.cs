using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;

public class EquipmentUI : MonoBehaviour
{
    public static EquipmentUI Instance { get; private set; }

    private enum EquipmentUIState { SelectingCategory, SelectingItem }
    private EquipmentUIState currentState = EquipmentUIState.SelectingCategory;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    [Header("Tham chiếu Canvas Tổng")]
    public GameObject rootCanvas;

    [Header("Top - Mô tả")]
    public TextMeshProUGUI txtDescription;

    [Header("Left - Chỉ số")]
    public TextMeshProUGUI txtStats;

    [Header("Right - 4 Ô trang bị")]
    public Button[] categoryButtons;
    public TextMeshProUGUI[] categoryNames;
    public Image[] categoryIcons;

    [Header("Bottom - Kho đồ (ScrollView)")]
    public Transform bottomContent;
    public GameObject equipSlotPrefab;
    public ScrollRect bottomScrollRect;
    public float scrollBottomPadding = 100f;

    private int selectedCategoryIndex = 0;
    private int selectedItemIndex = 0;
    private List<ItemData> filteredItems = new List<ItemData>();
    private List<GameObject> spawnedSlots = new List<GameObject>();

    private void OnEnable()
    {
        currentState = EquipmentUIState.SelectingCategory;
        selectedCategoryIndex = 0;
        selectedItemIndex = 0;

        if (EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(null);

        if (PlayerManager.Instance != null)
            PlayerManager.Instance.UpdateEquipmentStats();

        if (txtDescription != null)
            txtDescription.gameObject.SetActive(true);

        SetupCategoryButtonMouseEvents();

        UpdatePlayerStatsUI();
        UpdateEquippedDisplay();
        RefreshBottomList((EquipmentSlot)selectedCategoryIndex);
        ApplyCategoryColors();
        UpdateDescriptionForCategory();
    }

    private void Update()
    {
        if (Input.GetMouseButtonDown(1) || Input.GetKeyDown(KeyCode.X))
        {
            HandleBackAction();
            return;
        }

        if (currentState == EquipmentUIState.SelectingCategory)
            HandleCategoryNavigation();
        else
            HandleItemNavigation();

        ApplyCategoryColors();
    }

    private void HandleBackAction()
    {
        if (currentState == EquipmentUIState.SelectingItem)
        {
            currentState = EquipmentUIState.SelectingCategory;
            ClearItemHighlights();
            UpdateDescriptionForCategory();
        }
        else
        {
            ExitEquipmentMenu();
        }
    }

    private void ExitEquipmentMenu()
    {
        if (rootCanvas != null) rootCanvas.SetActive(false);
        else gameObject.SetActive(false);

        if (InventoryUI.Instance != null && InventoryUI.Instance.inventoryPanel != null
            && InventoryUI.Instance.inventoryPanel.activeSelf)
            InventoryUI.Instance.ToggleInventory();

        if (CombatManager.Instance != null)
        {
            CombatManager.Instance.UI?.ShowActionMenu(true);
            CombatManager.Instance.ResumeCombat();
        }
    }

    private void HandleCategoryNavigation()
    {
        bool changed = false;

        if (Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.W))
        {
            selectedCategoryIndex = Mathf.Max(0, selectedCategoryIndex - 1);
            changed = true;
        }
        if (Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.S))
        {
            selectedCategoryIndex = Mathf.Min(categoryButtons.Length - 1, selectedCategoryIndex + 1);
            changed = true;
        }

        if (changed)
        {
            RefreshBottomList((EquipmentSlot)selectedCategoryIndex);
            UpdateEquippedDisplay();
            UpdateDescriptionForCategory();
        }

        if (Input.GetKeyDown(KeyCode.Z) || Input.GetKeyDown(KeyCode.Return)
            || Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D))
        {
            if (filteredItems.Count > 0) EnterItemSelection();
        }
    }

    private void HandleItemNavigation()
    {
        bool moved = false;

        if (Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.W))
        {
            selectedItemIndex = Mathf.Max(0, selectedItemIndex - 1);
            moved = true;
        }
        if (Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.S))
        {
            selectedItemIndex = Mathf.Min(filteredItems.Count - 1, selectedItemIndex + 1);
            moved = true;
        }

        if (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.A))
        {
            HandleBackAction();
            return;
        }

        if (moved)
        {
            UpdateItemHighlights();
            UpdateDescriptionForItem();
            ScrollToSelectedItem();
        }

        if (Input.GetKeyDown(KeyCode.Z) || Input.GetKeyDown(KeyCode.Return))
        {
            EquipSelectedItem();
        }
    }

    private void EnterItemSelection()
    {
        currentState = EquipmentUIState.SelectingItem;
        selectedItemIndex = 0;
        UpdateItemHighlights();
        UpdateDescriptionForItem();
        ScrollToSelectedItem();
    }

    private void ClearItemHighlights()
    {
        for (int i = 0; i < spawnedSlots.Count; i++)
        {
            Transform border = spawnedSlots[i].transform.Find("SelectionBorder");
            if (border != null) border.gameObject.SetActive(false);
        }
    }

    private void UpdateItemHighlights()
    {
        for (int i = 0; i < spawnedSlots.Count; i++)
        {
            Transform border = spawnedSlots[i].transform.Find("SelectionBorder");
            if (border != null)
                border.gameObject.SetActive(i == selectedItemIndex);
        }
    }

    private void EquipSelectedItem()
    {
        if (selectedItemIndex < 0 || selectedItemIndex >= filteredItems.Count) return;
        EquipmentSlot targetSlot = (EquipmentSlot)selectedCategoryIndex;
        InventoryManager.Instance.EquipItem(filteredItems[selectedItemIndex], targetSlot);

        currentState = EquipmentUIState.SelectingCategory;
        UpdatePlayerStatsUI();
        UpdateEquippedDisplay();
        RefreshBottomList((EquipmentSlot)selectedCategoryIndex);
        UpdateDescriptionForCategory();
    }

    private void UpdateDescriptionForCategory()
    {
        if (txtDescription == null) return;
        txtDescription.gameObject.SetActive(true);

        string catName = "";
        ItemData equipped = null;

        // Tự động quét xem đang đứng ở ô nào và lấy đúng món đồ đang mặc
        switch (selectedCategoryIndex)
        {
            case 0: catName = "Vũ Khí"; equipped = InventoryManager.Instance.equippedWeapon; break;
            case 1: catName = "Giáp"; equipped = InventoryManager.Instance.equippedArmor; break;
            case 2: catName = "Phụ Kiện 1"; equipped = InventoryManager.Instance.equippedAccessory1; break;
            case 3: catName = "Phụ Kiện 2"; equipped = InventoryManager.Instance.equippedAccessory2; break;
        }

        if (equipped != null)
        {
            // Chỉ dùng thẻ <b> để in đậm, KHÔNG ép màu chữ để giữ nguyên màu bạn set trong Inspector
            txtDescription.text = $"<b>[{catName}]</b> {equipped.itemName}\n{equipped.description}";
        }
        else
        {
            txtDescription.text = $"<b>[{catName}]</b> Chưa trang bị\nClick chuột để chọn trang bị.";
        }
    }

    private void UpdateDescriptionForItem()
    {
        if (txtDescription == null) return;
        txtDescription.gameObject.SetActive(true);

        if (selectedItemIndex >= 0 && selectedItemIndex < filteredItems.Count)
        {
            var item = filteredItems[selectedItemIndex];
            // Hiển thị mô tả món đồ đang chọn trong danh sách dưới
            txtDescription.text = $"<b>{item.itemName}</b>\n{item.description}";
        }
    }

    private ItemData GetEquippedForCategory(int index)
    {
        if (InventoryManager.Instance == null) return null;
        switch (index)
        {
            case 0: return InventoryManager.Instance.equippedWeapon;
            case 1: return InventoryManager.Instance.equippedArmor;
            case 2: return InventoryManager.Instance.equippedAccessory1;
            case 3: return InventoryManager.Instance.equippedAccessory2;
            default: return null;
        }
    }

    private void UpdatePlayerStatsUI()
    {
        if (txtStats == null || PlayerManager.Instance == null) return;

        var p = PlayerManager.Instance;

        string atkText = BonusText(p.equipmentDamageBonus, false);
        string defText = BonusText(p.equipmentDefenseBonus, false);
        string spdText = BonusText(p.equipmentSpeedBonus, false);
        string critText = BonusText(p.equipmentCritBonus, true);
        string evaText = BonusText(p.equipmentEvasionBonus, true);

        float totalDef = p.currentDefense + p.equipmentDefenseBonus;

        txtStats.text = $"<color=red>FOAX</color>\n\n" +
                        $"Công Kích: {p.GetTotalAttack():F1} {atkText}\n" +
                        $"Phòng Ngự: {totalDef:F1} {defText}\n" +
                        $"Tốc Độ: {p.GetTotalSpeed():F1} {spdText}\n" +
                        $"Chí Mạng: {p.GetTotalCrit():F1}% {critText}\n" +
                        $"Né Tránh: {p.GetTotalEvasion():F1}% {evaText}";
    }

    private string BonusText(float bonus, bool isPercent)
    {
        string suffix = isPercent ? "%" : "";
        if (bonus > 0) return $"<color=green>(+{bonus}{suffix})</color>";
        if (bonus < 0) return $"<color=red>({bonus}{suffix})</color>";
        return "";
    }

    private void UpdateEquippedDisplay()
    {
        var inv = InventoryManager.Instance;
        SetSlotUI(0, inv.equippedWeapon);
        SetSlotUI(1, inv.equippedArmor);
        SetSlotUI(2, inv.equippedAccessory1);
        SetSlotUI(3, inv.equippedAccessory2);
    }

    private void SetSlotUI(int index, ItemData item)
    {
        if (categoryNames != null && index < categoryNames.Length && categoryNames[index] != null)
            categoryNames[index].text = (item != null) ? item.itemName : "Rỗng";
        if (categoryIcons != null && index < categoryIcons.Length && categoryIcons[index] != null)
        {
            categoryIcons[index].sprite = (item != null) ? item.icon : null;
            categoryIcons[index].enabled = (item != null);
        }
    }

    private void ApplyCategoryColors()
    {
        if (categoryButtons == null) return;
        for (int i = 0; i < categoryButtons.Length; i++)
        {
            if (categoryButtons[i] == null) continue;
            bool isSelected = (i == selectedCategoryIndex);
            Color targetColor = isSelected ? Color.yellow : Color.white;

            categoryButtons[i].transition = Selectable.Transition.None;
            Image img = categoryButtons[i].GetComponent<Image>();
            if (img != null) img.color = targetColor;
        }
    }

    private void RefreshBottomList(EquipmentSlot filterType)
    {
        foreach (var obj in spawnedSlots) Destroy(obj); // SỬA LẠI THÀNH Destroy THAY VÌ DestroyImmediate
        spawnedSlots.Clear();
        filteredItems.Clear();

        List<InventorySlot> allSlots = new List<InventorySlot>();
        allSlots.AddRange(InventoryManager.Instance.storageInventory);
        allSlots.AddRange(InventoryManager.Instance.combatInventory);

        int spawnedIndex = 0;
        foreach (var slot in allSlots)
        {
            if (slot.IsEmpty || slot.item == null) continue;

            bool match;
            if (filterType == EquipmentSlot.Accessory1 || filterType == EquipmentSlot.Accessory2)
                match = (slot.item.equipmentSlot == EquipmentSlot.Accessory1
                      || slot.item.equipmentSlot == EquipmentSlot.Accessory2);
            else
                match = (slot.item.equipmentSlot == filterType);

            if (slot.item.itemType == ItemType.Equipment && match)
            {
                filteredItems.Add(slot.item);
                GameObject newSlot = Instantiate(equipSlotPrefab, bottomContent);
                spawnedSlots.Add(newSlot);

                // --- SỬA LỖI CS0571 Ở ĐÂY: DÙNG DẤU BẰNG (=) ĐỂ GÁN GIÁ TRỊ ---
                var nameText = newSlot.transform.Find("ItemNameText")?.GetComponent<TextMeshProUGUI>();
                var qtyText = newSlot.transform.Find("QuantityText")?.GetComponent<TextMeshProUGUI>();
                var icon = newSlot.transform.Find("ItemIcon")?.GetComponent<Image>();

                if (nameText != null) nameText.text = slot.item.itemName;
                if (qtyText != null) qtyText.text = $"x{slot.quantity}";
                if (icon != null) icon.sprite = slot.item.icon;

                Transform border = newSlot.transform.Find("SelectionBorder");
                if (border != null) border.gameObject.SetActive(false);

                int capturedIndex = spawnedIndex;
                AddSlotMouseEvents(newSlot, capturedIndex);
                spawnedIndex++;
            }
        }

        Canvas.ForceUpdateCanvases();
        if (bottomContent is RectTransform contentRect)
            LayoutRebuilder.ForceRebuildLayoutImmediate(contentRect);
    }

    private void ScrollToSelectedItem()
    {
        if (bottomScrollRect == null || spawnedSlots.Count == 0) return;
        if (selectedItemIndex < 0 || selectedItemIndex >= spawnedSlots.Count) return;

        RectTransform contentRect = bottomScrollRect.content;
        RectTransform viewportRect = bottomScrollRect.viewport;
        RectTransform itemRect = spawnedSlots[selectedItemIndex].GetComponent<RectTransform>();

        float contentHeight = contentRect.rect.height;
        float viewportHeight = viewportRect.rect.height;
        if (contentHeight <= viewportHeight) return;

        float itemTop = -itemRect.anchoredPosition.y;
        float itemBottom = itemTop + itemRect.rect.height + scrollBottomPadding;
        float maxScroll = contentHeight - viewportHeight;
        float curOffset = (1f - bottomScrollRect.verticalNormalizedPosition) * maxScroll;
        float newOffset = curOffset;

        if (itemBottom > curOffset + viewportHeight) newOffset = itemBottom - viewportHeight;
        else if (itemTop < curOffset) newOffset = itemTop;

        bottomScrollRect.verticalNormalizedPosition = Mathf.Clamp01(1f - (newOffset / maxScroll));
    }

    private void AddSlotMouseEvents(GameObject slotObj, int index)
    {
        EventTrigger trigger = slotObj.GetComponent<EventTrigger>();
        if (trigger == null) trigger = slotObj.AddComponent<EventTrigger>();
        trigger.triggers.Clear();

        var hoverEntry = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
        hoverEntry.callback.AddListener((_) =>
        {
            if (currentState != EquipmentUIState.SelectingItem) return;
            selectedItemIndex = index;
            UpdateItemHighlights();
            UpdateDescriptionForItem();
        });
        trigger.triggers.Add(hoverEntry);

        var clickEntry = new EventTrigger.Entry { eventID = EventTriggerType.PointerClick };
        clickEntry.callback.AddListener((data) =>
        {
            PointerEventData ped = (PointerEventData)data;
            if (ped.button == PointerEventData.InputButton.Left)
            {
                selectedItemIndex = index;
                currentState = EquipmentUIState.SelectingItem;
                UpdateItemHighlights();
                UpdateDescriptionForItem();
                EquipSelectedItem();
            }
        });
        trigger.triggers.Add(clickEntry);

        // Chuyển tiếp các sự kiện Drag và Scroll lên bottomScrollRect để có thể kéo chuột và cuộn chuột
        if (bottomScrollRect != null)
        {
            var scrollEntry = new EventTrigger.Entry { eventID = EventTriggerType.Scroll };
            scrollEntry.callback.AddListener((data) => { bottomScrollRect.OnScroll((PointerEventData)data); });
            trigger.triggers.Add(scrollEntry);

            var beginDragEntry = new EventTrigger.Entry { eventID = EventTriggerType.BeginDrag };
            beginDragEntry.callback.AddListener((data) => { bottomScrollRect.OnBeginDrag((PointerEventData)data); });
            trigger.triggers.Add(beginDragEntry);

            var dragEntry = new EventTrigger.Entry { eventID = EventTriggerType.Drag };
            dragEntry.callback.AddListener((data) => { bottomScrollRect.OnDrag((PointerEventData)data); });
            trigger.triggers.Add(dragEntry);

            var endDragEntry = new EventTrigger.Entry { eventID = EventTriggerType.EndDrag };
            endDragEntry.callback.AddListener((data) => { bottomScrollRect.OnEndDrag((PointerEventData)data); });
            trigger.triggers.Add(endDragEntry);

            var initDragEntry = new EventTrigger.Entry { eventID = EventTriggerType.InitializePotentialDrag };
            initDragEntry.callback.AddListener((data) => { bottomScrollRect.OnInitializePotentialDrag((PointerEventData)data); });
            trigger.triggers.Add(initDragEntry);
        }
    }

    private void SetupCategoryButtonMouseEvents()
    {
        if (categoryButtons == null) return;
        for (int i = 0; i < categoryButtons.Length; i++)
        {
            if (categoryButtons[i] == null) continue;
            int capturedIndex = i;

            EventTrigger trigger = categoryButtons[i].GetComponent<EventTrigger>();
            if (trigger == null) trigger = categoryButtons[i].gameObject.AddComponent<EventTrigger>();
            trigger.triggers.Clear();

            var hoverEntry = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
            hoverEntry.callback.AddListener((_) =>
            {
                if (currentState == EquipmentUIState.SelectingCategory)
                {
                    selectedCategoryIndex = capturedIndex;
                    RefreshBottomList((EquipmentSlot)selectedCategoryIndex);
                    UpdateEquippedDisplay();
                    UpdateDescriptionForCategory();
                }
            });
            trigger.triggers.Add(hoverEntry);

            var clickEntry = new EventTrigger.Entry { eventID = EventTriggerType.PointerClick };
            clickEntry.callback.AddListener((data) =>
            {
                PointerEventData ped = (PointerEventData)data;
                if (ped.button == PointerEventData.InputButton.Left)
                {
                    selectedCategoryIndex = capturedIndex;
                    RefreshBottomList((EquipmentSlot)selectedCategoryIndex);
                    UpdateEquippedDisplay();
                    if (filteredItems.Count > 0)
                        EnterItemSelection();
                    else
                        UpdateDescriptionForCategory();
                }
            });
            trigger.triggers.Add(clickEntry);
        }
    }

    public void RefreshAll()
    {
        UpdatePlayerStatsUI();
        UpdateEquippedDisplay();
        RefreshBottomList((EquipmentSlot)selectedCategoryIndex);

        if (currentState == EquipmentUIState.SelectingItem)
            UpdateDescriptionForItem();
        else
            UpdateDescriptionForCategory();
    }
}