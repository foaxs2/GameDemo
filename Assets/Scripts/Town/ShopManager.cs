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

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            RefreshShop(); // Tạo hàng hóa lần đầu khi mới vào game
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // Hàm tung ngẫu nhiên lại shop khi về làng
    public void RefreshShop()
    {
        // Random vàng của chủ shop từ 70 - 250
        shopGold = Random.Range(70, 251);

        currentShopItems.Clear();

        // Nếu danh sách tổng rỗng thì báo lỗi
        if (allPossibleItems == null || allPossibleItems.Count == 0)
        {
            Debug.LogWarning("Chưa có item nào trong kho dữ liệu của ShopManager!");
            return;
        }

        // Bốc ngẫu nhiên 12 món đồ (không stack)
        for (int i = 0; i < 12; i++)
        {
            ItemData randomItem = allPossibleItems[Random.Range(0, allPossibleItems.Count)];
            currentShopItems.Add(randomItem);
        }

        Debug.Log($"[SHOP] Đã làm mới cửa hàng. Vàng chủ shop: {shopGold}");
    }
}