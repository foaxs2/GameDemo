using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

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
    // Thêm biến này để kéo ItemDescriptionHeader vào
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
            // Do Canvas giờ là con của GameManager, GameObject dư thừa sẽ bị GameManager tự hủy.
            // Nên ta không cần chuyển giao tham chiếu (reference) thủ công nữa vì nó sẽ làm gãy UI.
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
        if (slotPrefab == null || combatSlotsParent == null) return;

        // Tạo 20 slots cho combat
        for (int i = 0; i < InventoryManager.Instance.maxSlots; i++)
        {
            GameObject slot = Instantiate(slotPrefab, combatSlotsParent);
            combatSlotObjects.Add(slot);

            // Gán sự kiện click
            Button slotButton = slot.GetComponent<Button>();
            if (slotButton != null)
            {
                int index = i;
                slotButton.onClick.AddListener(() => OnSlotClicked(index, true));
            }
        }

        // Tạo 20 slots cho storage (tương tự)
        for (int i = 0; i < InventoryManager.Instance.maxSlots; i++)
        {
            GameObject slot = Instantiate(slotPrefab, storageSlotsParent);
            storageSlotObjects.Add(slot);
            Button slotButton = slot.GetComponent<Button>();
            if (slotButton != null)
            {
                int index = i;
                slotButton.onClick.AddListener(() => OnSlotClicked(index, false));
            }
        }
        UpdateSelection();
    }

    private void Update()
    {
        if (!isInventoryOpen) return;
        HandleKeyboardNavigation();

        if (PlayerManager.Instance != null)
        {
            if (goldText != null) goldText.text = $"Vàng: {PlayerManager.Instance.gold}";
            if (foodText != null) foodText.text = $"Lương thực: {PlayerManager.Instance.food}/{PlayerManager.Instance.maxFood}";
        }
    }

    void HandleKeyboardNavigation()
    {
        var currentSlots = isInCombatInventory ? combatSlotObjects : storageSlotObjects;
        int activeSlotCount = 0;

        // Đếm số slot đang active (có item)
        for (int i = 0; i < currentSlots.Count; i++)
        {
            if (currentSlots[i].activeSelf)
                activeSlotCount++;
        }

        // === THOÁT (X / I): PHẢI KIỂM TRA TRƯỚC activeSlotCount ===
        // Nếu không kiểm tra trước, khi hết item (activeSlotCount==0)
        // sẽ return sớm và không bao giờ xử lý được phím X → bị kẹt!
        if (Input.GetKeyDown(KeyCode.X) || Input.GetKeyDown(KeyCode.I))
        {
            ToggleInventory();
            return;
        }

        // Nếu không có slot nào active (không có item) thì dừng nav còn lại
        if (activeSlotCount == 0) return;

        // Di chuyển giữa Combat và Storage Inventory
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            isInCombatInventory = !isInCombatInventory;
            selectedSlotIndex = 0;
            UpdateSelection();
            UpdateDescription();
            return;
        }

        // Di chuyển LÊN/XUỐNG (2 slots mỗi hàng)
        if (Input.GetKeyDown(KeyCode.UpArrow))
        {
            selectedSlotIndex -= 2; // 2 slots mỗi hàng
            if (selectedSlotIndex < 0) selectedSlotIndex = 0;

            // Tìm slot active gần nhất phía trên
            while (selectedSlotIndex >= 0 && !currentSlots[selectedSlotIndex].activeSelf)
                selectedSlotIndex--;

            if (selectedSlotIndex < 0) selectedSlotIndex = 0;
            UpdateSelection();
            UpdateDescription();
            return;
        }

        if (Input.GetKeyDown(KeyCode.DownArrow))
        {
            selectedSlotIndex += 2; // 2 slots mỗi hàng
            int maxIndex = currentSlots.Count - 1;
            if (selectedSlotIndex > maxIndex) selectedSlotIndex = maxIndex;

            // Tìm slot active gần nhất phía dưới
            while (selectedSlotIndex <= maxIndex && !currentSlots[selectedSlotIndex].activeSelf)
                selectedSlotIndex++;

            if (selectedSlotIndex > maxIndex) selectedSlotIndex = maxIndex;
            UpdateSelection();
            UpdateDescription();
            return;
        }

        // Di chuyển TRÁI/PHẢI
        if (Input.GetKeyDown(KeyCode.LeftArrow))
        {
            selectedSlotIndex--;
            if (selectedSlotIndex < 0)
            {
                selectedSlotIndex = 0; // Giữ ở slot đầu tiên, không mất selection
            }
            else
            {
                // Tìm slot active gần nhất bên trái
                while (selectedSlotIndex >= 0 && !currentSlots[selectedSlotIndex].activeSelf)
                    selectedSlotIndex--;

                if (selectedSlotIndex < 0) selectedSlotIndex = 0;
            }
            UpdateSelection();
            UpdateDescription();
            return;
        }

        if (Input.GetKeyDown(KeyCode.RightArrow))
        {
            selectedSlotIndex++;
            int maxIndex = currentSlots.Count - 1;
            if (selectedSlotIndex > maxIndex)
            {
                selectedSlotIndex = maxIndex; // Giữ ở slot cuối cùng, không mất selection
            }
            else
            {
                // Tìm slot active gần nhất bên phải
                while (selectedSlotIndex <= maxIndex && !currentSlots[selectedSlotIndex].activeSelf)
                    selectedSlotIndex++;

                if (selectedSlotIndex > maxIndex) selectedSlotIndex = maxIndex;
            }
            UpdateSelection();
            UpdateDescription();
            return;
        }

        // Xác nhận dùng item (phím Z)
        if (Input.GetKeyDown(KeyCode.Z))
        {
            UseSelectedItem();
            return;
        }

        // Thoát (X/I) đã được xử lý ở đầu hàm — không cần xử lý lại ở đây
    }

    // === HÀM MỚI: LÀM SÁNG SLOT ĐƯỢC CHỌN ===
    void UpdateSelection()
    {
        var slots = isInCombatInventory ? combatSlotObjects : storageSlotObjects;

        for (int i = 0; i < slots.Count; i++)
        {
            GameObject slot = slots[i];
            Image slotBackground = slot.GetComponent<Image>();

            if (slotBackground != null)
            {
                // Nếu là slot đang chọn thì màu Trắng, không thì màu Xám
                slotBackground.color = (i == selectedSlotIndex) ? selectedColor : normalColor;
            }

            // Xử lý viền SelectionBorder nếu có
            Image border = slot.transform.Find("SelectionBorder")?.GetComponent<Image>();
            if (border != null) border.enabled = (i == selectedSlotIndex);
        }
    }

    void UpdateDescription()
    {
        var slots = isInCombatInventory ? InventoryManager.Instance.combatInventory : InventoryManager.Instance.storageInventory;

        if (selectedSlotIndex >= 0 && selectedSlotIndex < slots.Count)
        {
            var slot = slots[selectedSlotIndex];

            if (itemDescriptionText != null)
            {
                if (slot.IsEmpty)
                {
                    // Ẩn mô tả khi slot trống
                    itemDescriptionText.text = "";
                    itemDescriptionText.gameObject.SetActive(false);
                }
                else
                {
                    // Hiện mô tả khi có item
                    itemDescriptionText.gameObject.SetActive(true);
                    itemDescriptionText.text = slot.item.description;
                }
            }

            // Đã bỏ phần hiển thị itemNameText ở panel Description theo ý muốn
        }
    }

    public void UpdateInventoryDisplay()
    {
        UpdateCombatSlots();
        UpdateStorageSlots();
        UpdateSelection();
        UpdateDescription();
    }

    private void UpdateCombatSlots()
    {
        var slots = InventoryManager.Instance.combatInventory;
        for (int i = 0; i < combatSlotObjects.Count; i++)
        {
            if (i >= slots.Count || slots[i].IsEmpty)
            {
                combatSlotObjects[i].SetActive(false);
            }
            else
            {
                combatSlotObjects[i].SetActive(true);
                UpdateSlotUI(combatSlotObjects[i], slots[i], i);
            }
        }
    }

    private void UpdateStorageSlots()
    {
        var slots = InventoryManager.Instance.storageInventory;
        for (int i = 0; i < storageSlotObjects.Count; i++)
        {
            if (i >= slots.Count || slots[i].IsEmpty)
            {
                storageSlotObjects[i].SetActive(false);
            }
            else
            {
                storageSlotObjects[i].SetActive(true);
                UpdateSlotUI(storageSlotObjects[i], slots[i], i);
            }
        }
    }

    // === SỬA LỖI ICON KHÔNG HIỆN ===
    private void UpdateSlotUI(GameObject slotObj, InventorySlot slot, int index)
    {
        Debug.Log($"[SLOT {index}] === BẮT ĐẦU UPDATE ===");
        Debug.Log($"[SLOT {index}] Item: {(slot.item != null ? slot.item.itemName : "NULL")}");

        // 1. Tìm Image Icon
        Image iconImage = slotObj.transform.Find("ItemIcon")?.GetComponent<Image>();

        // 2. Tìm Text Tên Item (PHẢI TÌM ĐÚNG TÊN)
        TextMeshProUGUI nameText = slotObj.transform.Find("ItemNameText")?.GetComponent<TextMeshProUGUI>();

        // 3. Tìm Text Số Lượng
        TextMeshProUGUI quantityText = slotObj.transform.Find("QuantityText")?.GetComponent<TextMeshProUGUI>();

        Debug.Log($"[SLOT {index}] IconImage: {(iconImage != null ? "FOUND" : "NULL")}");
        Debug.Log($"[SLOT {index}] NameText: {(nameText != null ? "FOUND" : "NULL")}");

        if (slot.IsEmpty)
        {
            // Ẩn khi slot trống
            if (iconImage != null)
            {
                iconImage.enabled = false;
                iconImage.sprite = null;
            }
            if (nameText != null) nameText.text = "";  // XÓA TEXT
            if (quantityText != null) quantityText.text = "";
        }
        else
        {
            // Hiện khi có item
            if (iconImage != null && slot.item != null && slot.item.icon != null)
            {
                iconImage.sprite = slot.item.icon;
                iconImage.enabled = true;
                iconImage.color = Color.white; // Force màu trắng
            }

            if (nameText != null && slot.item != null)
            {
                nameText.text = slot.item.itemName;  // GÁN TÊN ITEM VÀO ĐÂY
                nameText.color = Color.white;
                nameText.gameObject.SetActive(true); // Đảm bảo text được bật
            }

            if (quantityText != null && slot.item != null)
            {
                if (slot.item.isStackable && slot.quantity > 1)
                    quantityText.text = $"x{slot.quantity}";
                else
                    quantityText.text = "";
            }
        }
    }

    private void OnSlotClicked(int slotIndex, bool isCombat)
    {
        selectedSlotIndex = slotIndex;
        isInCombatInventory = isCombat;
        UpdateSelection();
        UpdateDescription();
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
                // Tự động thoát menu vật phẩm sau khi dùng item thành công
                if (success)
                    ToggleInventory();
            }
        }
    }

    public void ToggleInventory()
    {
        isInventoryOpen = !isInventoryOpen;

        // Ưu tiên ẩn/hiện rootCanvas (Canvas gốc), nếu không có thì dùng inventoryPanel
        GameObject targetRoot = (rootCanvas != null) ? rootCanvas : inventoryPanel;

        if (targetRoot != null)
            targetRoot.SetActive(isInventoryOpen);

        // Đảm bảo inventoryPanel cũng đồng bộ
        if (inventoryPanel != null && inventoryPanel != targetRoot)
            inventoryPanel.SetActive(isInventoryOpen);

        if (isInventoryOpen)
        {
            // Force rebuild layout
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
                CombatManager.Instance.actionMenu.SetActive(false);

            Debug.Log("[INVENTORY] Đã mở túi đồ. Dùng phím mũi tên để di chuyển, Z để dùng item, X hoặc I để đóng.");
        }
        else
        {
            // Khi đóng inventory, ẩn description
            if (itemDescriptionText != null)
            {
                itemDescriptionText.gameObject.SetActive(false);
                itemDescriptionText.text = "";
            }
            if (CombatManager.Instance != null)
            {
                // Dùng Coroutine để tránh bị dính phím Z/X cùng 1 frame với CombatManager
                StartCoroutine(ResumeCombatNextFrame());
            }
        }
    }

    private System.Collections.IEnumerator ResumeCombatNextFrame()
    {
        yield return null; // Chờ sang frame tiếp theo để Input clear
        if (CombatManager.Instance != null)
        {
            CombatManager.Instance.actionMenu.SetActive(true);
            CombatManager.Instance.ResumeCombat(); // Tiếp tục chiến đấu
        }
    }
}
