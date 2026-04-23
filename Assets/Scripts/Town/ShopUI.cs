using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ShopUI : MonoBehaviour
{
    [Header("Tiền Bạc")]
    public TextMeshProUGUI txtShopGold;
    public TextMeshProUGUI txtPlayerGold;

    [Header("Khu vực hiển thị (Content)")]
    public Transform shopContent;
    public Transform playerContent;

    [Header("Prefab Ô vật phẩm")]
    public GameObject shopSlotPrefab;

    [Header("Nút Tương Tác")]
    public Button btnBuy;
    public Button btnSell;
    public Button btnClose;

    // Quản lý Double-Click & Lựa chọn
    private float doubleClickThreshold = 0.3f;
    private float lastClickTime = 0f;
    private int selectedIndex = -1;
    private bool isSelectingShop = true; // true: đang chọn đồ của shop, false: đồ của player

    // Danh sách lưu lại các ô đã sinh ra để quản lý viền sáng
    private List<GameObject> shopUIObjects = new List<GameObject>();
    private List<GameObject> playerUIObjects = new List<GameObject>();

    // Lưu lại danh sách đồ của người chơi (dạng dẹp, bỏ qua ô rỗng)
    private List<InventorySlot> currentPlayerItems = new List<InventorySlot>();

    void Start()
    {
        btnBuy.onClick.AddListener(BuyItem);
        btnSell.onClick.AddListener(SellItem);
        btnClose.onClick.AddListener(() => gameObject.SetActive(false));
    }

    public void OpenShop()
    {
        gameObject.SetActive(true);
        selectedIndex = -1; // Reset lựa chọn
        RefreshAll();
    }

    private void RefreshAll()
    {
        if (ShopManager.Instance == null || PlayerManager.Instance == null) return;

        txtShopGold.text = ShopManager.Instance.shopGold.ToString();
        txtPlayerGold.text = PlayerManager.Instance.gold.ToString();

        LoadShopItems();
        LoadPlayerItems();
        UpdateSelectionVisuals();
    }

    private void LoadShopItems()
    {
        foreach (var obj in shopUIObjects) Destroy(obj);
        shopUIObjects.Clear();

        List<ItemData> shopItems = ShopManager.Instance.currentShopItems;
        for (int i = 0; i < shopItems.Count; i++)
        {
            int index = i; // Copy giá trị cho biến cục bộ để dùng trong lambda
            ItemData item = shopItems[i];

            GameObject newSlot = Instantiate(shopSlotPrefab, shopContent);
            shopUIObjects.Add(newSlot);

            SetupSlotInfo(newSlot, item, item.buyPrice, "");

            // Bắt sự kiện Click
            newSlot.GetComponent<Button>().onClick.AddListener(() => OnSlotClicked(index, true));
        }
    }

    private void LoadPlayerItems()
    {
        foreach (var obj in playerUIObjects) Destroy(obj);
        playerUIObjects.Clear();
        currentPlayerItems.Clear();

        // Gộp đồ từ Combat và Storage
        var inv = InventoryManager.Instance;
        List<InventorySlot> allSlots = new List<InventorySlot>();
        allSlots.AddRange(inv.combatInventory);
        allSlots.AddRange(inv.storageInventory);

        int indexCounter = 0;
        foreach (var slot in allSlots)
        {
            if (!slot.IsEmpty)
            {
                currentPlayerItems.Add(slot);
                int index = indexCounter;

                GameObject newSlot = Instantiate(shopSlotPrefab, playerContent);
                playerUIObjects.Add(newSlot);

                // Hiển thị số lượng (x2, x3...)
                string qtyText = slot.quantity > 1 ? $"x{slot.quantity}" : "";
                SetupSlotInfo(newSlot, slot.item, slot.item.sellPrice, qtyText);

                newSlot.GetComponent<Button>().onClick.AddListener(() => OnSlotClicked(index, false));
                indexCounter++;
            }
        }
    }

    private void SetupSlotInfo(GameObject slotObj, ItemData item, int price, string qtyText)
    {
        slotObj.transform.Find("ItemIcon").GetComponent<Image>().sprite = item.icon;

        // Hiện Tên + Số Lượng
        TextMeshProUGUI nameTxt = slotObj.transform.Find("ItemNameText").GetComponent<TextMeshProUGUI>();
        nameTxt.text = $"{item.itemName} {qtyText}";

        // Hiện Giá
        TextMeshProUGUI priceTxt = slotObj.transform.Find("PriceText").GetComponent<TextMeshProUGUI>();
        if (priceTxt != null) priceTxt.text = price.ToString() + "G";

        // Tắt viền vàng
        Transform border = slotObj.transform.Find("SelectionBorder");
        if (border != null) border.gameObject.SetActive(false);
    }

    // LOGIC XỬ LÝ CLICK VÀ DOUBLE CLICK
    private void OnSlotClicked(int index, bool isShop)
    {
        if (Time.time - lastClickTime < doubleClickThreshold && selectedIndex == index && isSelectingShop == isShop)
        {
            // DOUBLE CLICK: Mua hoặc Bán ngay lập tức
            if (isShop) BuyItem();
            else SellItem();
        }
        else
        {
            // SINGLE CLICK: Chỉ đánh dấu sáng viền
            selectedIndex = index;
            isSelectingShop = isShop;
            lastClickTime = Time.time;
            UpdateSelectionVisuals();
        }
    }

    private void UpdateSelectionVisuals()
    {
        // Quét bên Shop
        for (int i = 0; i < shopUIObjects.Count; i++)
        {
            Transform border = shopUIObjects[i].transform.Find("SelectionBorder");
            if (border != null) border.gameObject.SetActive(isSelectingShop && selectedIndex == i);
        }

        // Quét bên Người chơi
        for (int i = 0; i < playerUIObjects.Count; i++)
        {
            Transform border = playerUIObjects[i].transform.Find("SelectionBorder");
            if (border != null) border.gameObject.SetActive(!isSelectingShop && selectedIndex == i);
        }

        // Bật tắt nút Mua/Bán
        btnBuy.interactable = (isSelectingShop && selectedIndex >= 0);
        btnSell.interactable = (!isSelectingShop && selectedIndex >= 0);
    }

    private void BuyItem()
    {
        if (!isSelectingShop || selectedIndex < 0 || selectedIndex >= ShopManager.Instance.currentShopItems.Count) return;

        ItemData itemToBuy = ShopManager.Instance.currentShopItems[selectedIndex];

        // 1. Kiểm tra tiền người chơi
        if (PlayerManager.Instance.gold < itemToBuy.buyPrice)
        {
            Debug.LogWarning("Không đủ vàng để mua!");
            return;
        }

        // 2. Thêm vào túi đồ (Hàm AddItem tự lo việc Stack nếu đồ giống nhau)
        if (InventoryManager.Instance.AddItem(itemToBuy, 1))
        {
            // 3. Trừ tiền người chơi, cộng tiền Shop
            PlayerManager.Instance.gold -= itemToBuy.buyPrice;
            ShopManager.Instance.shopGold += itemToBuy.buyPrice;

            // 4. Xóa món đó khỏi Shop (Vì shop 20 món riêng biệt)
            ShopManager.Instance.currentShopItems.RemoveAt(selectedIndex);

            selectedIndex = -1; // Reset lựa chọn
            RefreshAll();
            Debug.Log($"Đã mua {itemToBuy.itemName}");
        }
    }

    private void SellItem()
    {
        if (isSelectingShop || selectedIndex < 0 || selectedIndex >= currentPlayerItems.Count) return;

        InventorySlot slotToSell = currentPlayerItems[selectedIndex];
        ItemData itemToSell = slotToSell.item;

        // 1. Kiểm tra tiền của Shop có đủ để thu mua không
        if (ShopManager.Instance.shopGold < itemToSell.sellPrice)
        {
            Debug.LogWarning("Chủ shop đã cạn tiền, không thể mua thêm đồ của bạn!");
            return;
        }
        //Chan bao nếu món đồ không thể bán
        if (!itemToSell.canBeSold)
        {
            return;
        }
        // 2. Trừ đồ trong túi người chơi
        if (InventoryManager.Instance.RemoveItem(itemToSell, 1))
        {
            // 3. Trừ tiền Shop, Cộng tiền người chơi
            ShopManager.Instance.shopGold -= itemToSell.sellPrice;
            PlayerManager.Instance.gold += itemToSell.sellPrice;

            // 4.Quăng món đồ vừa bán vào danh sách của Shop để lỡ bán nhầm có thể mua lại
            ShopManager.Instance.currentShopItems.Add(itemToSell);

            selectedIndex = -1;
            RefreshAll();
            Debug.Log($"Đã bán {itemToSell.itemName}");
        }
    }
}