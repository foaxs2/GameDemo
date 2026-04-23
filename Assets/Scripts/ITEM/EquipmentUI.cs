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
    public GameObject rootCanvas; // Kéo Canvas_Equipment vào đây

    [Header("Top - Mô tả")]
    public TextMeshProUGUI txtDescription;

    [Header("Left - Chỉ số")]
    public TextMeshProUGUI txtStats;

    [Header("Right - 4 Ô trang bị")]
    public Button[] categoryButtons;
    public TextMeshProUGUI[] categoryNames;
    public Image[] categoryIcons;

    [Header("Bottom - Kho đồ")]
    public Transform bottomContent;
    public GameObject equipSlotPrefab;
    public ScrollRect bottomScrollRect; // Kéo Scroll View vào đây
    [Tooltip("Đệm phính để item cuối không bị khuất (pixel)")]
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
        // Làm mới không bị tranh chấp nút x/z
        if (EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(null);
        // Tính lại toàn bộ chỉ số trang bị để stats panel hiển thị đúng ngay khi mở
        if (PlayerManager.Instance != null)
            PlayerManager.Instance.UpdateEquipmentStats();
        RefreshAll();
    }

    private void Update()
    {
        // LUÔN LUÔN cho phép phím X thoát menu bất kể trạng thái nào để tránh bị kẹt
        if (Input.GetKeyDown(KeyCode.X))
        {
            if (currentState == EquipmentUIState.SelectingItem)
            {
                // Nếu đang chọn đồ ở dưới, bấm X để quay lại chọn 4 ô trên
                currentState = EquipmentUIState.SelectingCategory;
                UpdateVisualSelection();
            }
            else
            {
                // Nếu đang ở 4 ô trên (kể cả khi không có item nào), bấm X để thoát hẳn
                ExitEquipmentMenu();
            }
            return;
        }

        if (currentState == EquipmentUIState.SelectingCategory) HandleCategoryNavigation();
        else HandleItemNavigation();

        ApplyCategoryColors();
    }

    private void HandleCategoryNavigation()
    {
        if (Input.GetKeyDown(KeyCode.UpArrow)) { selectedCategoryIndex = Mathf.Max(0, selectedCategoryIndex - 1); RefreshAll(); }
        if (Input.GetKeyDown(KeyCode.DownArrow)) { selectedCategoryIndex = Mathf.Min(3, selectedCategoryIndex + 1); RefreshAll(); }

        if (Input.GetKeyDown(KeyCode.Z)) // Bấm Z lần 1: Chọn loại trang bị
        {
            if (filteredItems.Count > 0)
            {
                currentState = EquipmentUIState.SelectingItem;
                selectedItemIndex = 0; // Luôn bắt đầu từ món đồ đầu tiên
                UpdateVisualSelection(); // ÉP UI CẬP NHẬT ĐỂ MÓN ĐỒ SÁNG LÊN NGAY
                Debug.Log($"Đã chọn danh mục {selectedCategoryIndex}, nhảy xuống món đồ đầu tiên.");
            }
        }
        // X đã được xử lý trong Update() – không cần xử lý lại ở đây
    }

    private void HandleItemNavigation()
    {
        bool moved = false;

        if (Input.GetKeyDown(KeyCode.LeftArrow)) { selectedItemIndex = Mathf.Max(0, selectedItemIndex - 1); moved = true; }
        if (Input.GetKeyDown(KeyCode.RightArrow)) { selectedItemIndex = Mathf.Min(filteredItems.Count - 1, selectedItemIndex + 1); moved = true; }
        if (Input.GetKeyDown(KeyCode.UpArrow)) { selectedItemIndex = Mathf.Max(0, selectedItemIndex - 2); moved = true; }
        if (Input.GetKeyDown(KeyCode.DownArrow)) { selectedItemIndex = Mathf.Min(filteredItems.Count - 1, selectedItemIndex + 2); moved = true; }

        if (Input.GetKeyDown(KeyCode.Z)) // BẤM Z LẦN 2: MẶC ĐỒ
        {
            EquipmentSlot targetSlot = (EquipmentSlot)selectedCategoryIndex;
            InventoryManager.Instance.EquipItem(filteredItems[selectedItemIndex], targetSlot);
            currentState = EquipmentUIState.SelectingCategory;
            RefreshAll();
        }

        // X đã được xử lý trong Update() – không cần xử lý lại ở đây

        if (moved)
        {
            UpdateVisualSelection();
            ScrollToSelectedItem();
        }
    }

    // Tự cuộn ScrollView để item đang chọn luôn hiển thị trong khung nhìn
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

        // Vị trí tính từ đỉnh Content xuống (giá trị dương)
        float itemTop = -itemRect.anchoredPosition.y;
        float itemBottom = itemTop + itemRect.rect.height + scrollBottomPadding;

        float maxScroll = contentHeight - viewportHeight;
        // Mức cuộn hiện tại (pixel đại số ẩn phía trên)
        float currentOffset = (1f - bottomScrollRect.verticalNormalizedPosition) * maxScroll;

        float newOffset = currentOffset;

        // Item nằm DƯỚI khung nhìn → cuộn xuống
        if (itemBottom > currentOffset + viewportHeight)
        {
            newOffset = itemBottom - viewportHeight;
        }
        // Item nằm TRÊN khung nhìn → cuộn lên
        else if (itemTop < currentOffset)
        {
            newOffset = itemTop;
        }

        bottomScrollRect.verticalNormalizedPosition = Mathf.Clamp01(1f - (newOffset / maxScroll));
    }

    // Gán màu trực tiếp lên Image mỗi frame - cách duy nhất đảm bảo chiến thắng EventSystem
    private void ApplyCategoryColors()
    {
        if (categoryButtons == null) return;
        for (int i = 0; i < categoryButtons.Length; i++)
        {
            if (categoryButtons[i] == null) continue;
            bool isSelected = (currentState == EquipmentUIState.SelectingCategory && i == selectedCategoryIndex);
            Color targetColor = isSelected ? Color.yellow : Color.white;

            // Tắt transition để Unity không can thiệp
            categoryButtons[i].transition = Selectable.Transition.None;

            // Ghi đè trực tiếp lên Image
            Image img = categoryButtons[i].GetComponent<Image>();
            if (img != null) img.color = targetColor;
        }
    }

    private void ExitEquipmentMenu()
    {
        if (rootCanvas != null) rootCanvas.SetActive(false);
        else this.gameObject.SetActive(false);

        // Đảm bảo tắt luôn cả túi đồ nếu nó đang mở
        if (InventoryUI.Instance != null && InventoryUI.Instance.inventoryPanel.activeSelf)
            InventoryUI.Instance.ToggleInventory();

        if (CombatManager.Instance != null)
        {
            CombatManager.Instance.actionMenu.SetActive(true);
            CombatManager.Instance.ResumeCombat(); // Tiếp tục chiến đấu
        }
    }

    public void RefreshAll()
    {
        UpdatePlayerStatsUI();
        UpdateEquippedDisplay();
        RefreshBottomList((EquipmentSlot)selectedCategoryIndex);
        UpdateVisualSelection();
    }

    private void UpdatePlayerStatsUI()
    {
        if (txtStats == null)
        {
            Debug.LogError("[EquipmentUI] txtStats chưa được gán trong Inspector! Kéo PlayerStatsText vào trường txtStats.");
            return;
        }
        if (PlayerManager.Instance == null)
        {
            Debug.LogWarning("[EquipmentUI] PlayerManager.Instance là null khi mở Equipment.");
            return;
        }
        var p = PlayerManager.Instance;

        string atkText = p.equipmentDamageBonus > 0 ? $"<color=green>(+{p.equipmentDamageBonus})</color>" : (p.equipmentDamageBonus < 0 ? $"<color=red>({p.equipmentDamageBonus})</color>" : "");
        string defText = p.equipmentDefenseBonus > 0 ? $"<color=green>(+{p.equipmentDefenseBonus})</color>" : (p.equipmentDefenseBonus < 0 ? $"<color=red>({p.equipmentDefenseBonus})</color>" : "");
        string spdText = p.equipmentSpeedBonus > 0 ? $"<color=green>(+{p.equipmentSpeedBonus})</color>" : (p.equipmentSpeedBonus < 0 ? $"<color=red>({p.equipmentSpeedBonus})</color>" : "");
        string critText = p.equipmentCritBonus > 0 ? $"<color=green>(+{p.equipmentCritBonus}%)</color>" : (p.equipmentCritBonus < 0 ? $"<color=red>({p.equipmentCritBonus}%)</color>" : "");
        string evaText = p.equipmentEvasionBonus > 0 ? $"<color=green>(+{p.equipmentEvasionBonus}%)</color>" : (p.equipmentEvasionBonus < 0 ? $"<color=red>({p.equipmentEvasionBonus}%)</color>" : "");

        float totalDef = p.currentDefense + p.equipmentDefenseBonus;

        // THÊM TÊN NHÂN VẬT VÀ CHỈ SỐ
        txtStats.text = $"<color=red>FOAX</color>\n\n" +
                        $"Công Kích: {p.GetTotalAttack():F1} {atkText}\n" +
                        $"Phòng Ngự: {totalDef:F1} {defText}\n" +
                        $"Tốc Độ: {p.GetTotalSpeed():F1} {spdText}\n" +
                        $"Chí Mạng: {p.GetTotalCrit():F1}% {critText}\n" +
                        $"Né Tránh: {p.GetTotalEvasion():F1}% {evaText}";

        Debug.Log($"[EquipmentUI] Stats đã cập nhật: ATK={p.GetTotalAttack():F1}, DEF={totalDef:F1}");
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
        categoryNames[index].text = (item != null) ? item.itemName : "Rỗng";
        categoryIcons[index].sprite = (item != null) ? item.icon : null;
        categoryIcons[index].enabled = (item != null);
    }

    private void RefreshBottomList(EquipmentSlot filterType)
    {
        // Dùng DestroyImmediate để xóa slot cũ NGAY LẬP TỨC (không chờ cuối frame như Destroy)
        // đảm bảo khi ForceRebuildLayoutImmediate chạy, Content không còn slot cũ nữa
        foreach (var obj in spawnedSlots) DestroyImmediate(obj);
        spawnedSlots.Clear();
        filteredItems.Clear();

        List<InventorySlot> allSlots = new List<InventorySlot>();
        allSlots.AddRange(InventoryManager.Instance.storageInventory);
        allSlots.AddRange(InventoryManager.Instance.combatInventory);

        foreach (var slot in allSlots)
        {
            if (slot.IsEmpty || slot.item == null) continue;

            bool match = false;
            // Nếu slot hiện tại đang xem là Phụ Kiện, hiển thị toàn bộ trang bị thuộc loại Phụ Kiện (1 hoặc 2)
            if (filterType == EquipmentSlot.Accessory1 || filterType == EquipmentSlot.Accessory2)
            {
                match = (slot.item.equipmentSlot == EquipmentSlot.Accessory1 || slot.item.equipmentSlot == EquipmentSlot.Accessory2);
            }
            else
            {
                match = (slot.item.equipmentSlot == filterType);
            }

            if (!slot.IsEmpty && slot.item.itemType == ItemType.Equipment && match)
            {
                filteredItems.Add(slot.item);
                GameObject newSlot = Instantiate(equipSlotPrefab, bottomContent);
                spawnedSlots.Add(newSlot);

                newSlot.transform.Find("ItemNameText").GetComponent<TextMeshProUGUI>().text = slot.item.itemName;
                newSlot.transform.Find("QuantityText").GetComponent<TextMeshProUGUI>().text = $"x{slot.quantity}";
                newSlot.transform.Find("ItemIcon").GetComponent<Image>().sprite = slot.item.icon;

                // Mặc định tắt viền vàng của slot mới sinh ra
                Transform border = newSlot.transform.Find("SelectionBorder");
                if (border != null) border.gameObject.SetActive(false);
            }
        }

        // Ép Unity tính lại chiều cao Content ngay lập tức
        // mà không cần chờ đến frame kế tiếp, tránh ScrollView bị sai khoảng cuộn
        Canvas.ForceUpdateCanvases();
        if (bottomContent is RectTransform contentRect)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(contentRect);
        }
    }

    private void UpdateVisualSelection()
    {
        // 1. Category buttons — bỏ qua, đã xử lý trong ApplyCategoryColors() mỗi frame

        // 2. Làm nổi bật món đồ trong kho đồ bên dưới
        for (int i = 0; i < spawnedSlots.Count; i++)
        {
            Transform border = spawnedSlots[i].transform.Find("SelectionBorder");
            if (border != null)
            {
                border.gameObject.SetActive(currentState == EquipmentUIState.SelectingItem && i == selectedItemIndex);
            }
        }

        // 3. Cập nhật mô tả và tên ở trên cùng
        if (currentState == EquipmentUIState.SelectingItem && filteredItems.Count > selectedItemIndex)
        {
            txtDescription.text = filteredItems[selectedItemIndex].description;
        }
    }

}
