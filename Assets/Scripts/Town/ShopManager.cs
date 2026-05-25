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
        else
        {
            Destroy(gameObject);
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

        for (int i = 0; i < 12; i++)
        {
            ItemData randomItem = allPossibleItems[Random.Range(0, allPossibleItems.Count)];
            
            // Theo ý người chơi: Shop không bán bình máu (HP)
            if (randomItem.itemType == ItemType.Consumable && randomItem.consumableType == ConsumableType.HP)
            {
                // Bốc lại món khác
                for (int retry = 0; retry < 10; retry++)
                {
                    randomItem = allPossibleItems[Random.Range(0, allPossibleItems.Count)];
                    if (!(randomItem.itemType == ItemType.Consumable && randomItem.consumableType == ConsumableType.HP))
                        break;
                }
            }

            currentShopItems.Add(randomItem);
        }

        Debug.Log($"[SHOP] Đã làm mới cửa hàng. Vàng chủ shop: {shopGold}");
    }
}