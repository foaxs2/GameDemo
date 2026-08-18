using System.Collections.Generic;
using UnityEngine;

public class ShopManager : MonoBehaviour
{
    public static ShopManager Instance;

    [Header("Tài sản của Chủ Shop")]
    public int shopGold = 0;
    public List<ItemData> currentShopItems = new List<ItemData>();

    [Header("Kho Dữ Liệu (Kéo tất cả Item vào đây)")]
    public List<ItemData> allPossibleItems;

    [Header("Cấu hình Gold Shop (Chỉnh được trong Inspector)")]
    [SerializeField] private int shopGoldMin = 70;
    [SerializeField] private int shopGoldMax = 250;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            RefreshShop();
        }
        else if (Instance != this)
        {
            if (gameObject.GetComponent<Canvas>() != null || gameObject.GetComponent<Camera>() != null)
            {
                Destroy(this);
            }
            else
            {
                Destroy(gameObject);
            }
        }
    }

    /// <summary>Tung ngẫu nhiên lại shop. Gọi khi về làng.</summary>
    public void RefreshShop()
    {
        shopGold = Random.Range(shopGoldMin, shopGoldMax + 1);
        currentShopItems.Clear();

        if (allPossibleItems == null || allPossibleItems.Count == 0)
        {
            Debug.LogWarning("[ShopManager] Chưa có item nào trong kho dữ liệu!");
            return;
        }

        // Lọc danh sách item hợp lệ để bán trong Shop:
        // - Không null
        // - buyPrice > 0 (không xuất hiện vật phẩm 0G)
        // - canBeSold == true
        // - Không phải Bình máu HP (Shop không bán bình máu theo thiết kế)
        List<ItemData> validItems = new List<ItemData>();
        foreach (var item in allPossibleItems)
        {
            if (item != null && item.buyPrice > 0 && item.canBeSold)
            {
                if (item.itemType == ItemType.Consumable && item.consumableType == ConsumableType.HP)
                    continue;

                validItems.Add(item);
            }
        }

        if (validItems.Count == 0)
        {
            Debug.LogWarning("[ShopManager] Không tìm thấy vật phẩm nào hợp lệ (buyPrice > 0) trong kho dữ liệu!");
            return;
        }

        for (int i = 0; i < 12; i++)
        {
            // Vật phẩm ngoài loại Consumable không được trùng nhau trong shop
            List<ItemData> availableCandidates = validItems.FindAll(item => 
                item.itemType == ItemType.Consumable || !currentShopItems.Contains(item)
            );

            // Nếu không còn vật phẩm duy nhất nào khả dụng, dùng lại validItems làm fallback
            if (availableCandidates.Count == 0)
                availableCandidates = validItems;

            ItemData randomItem = availableCandidates[Random.Range(0, availableCandidates.Count)];
            currentShopItems.Add(randomItem);
        }

        Debug.Log($"[SHOP] Đã làm mới cửa hàng ({currentShopItems.Count} món). Vàng chủ shop: {shopGold}");
    }
}