using UnityEngine;

public class ItemTestHelper : MonoBehaviour
{
    [Header("Danh sách trang bị để Test")]
    public ItemData[] allTestEquipment; // Mảng chứa tất cả vũ khí, giáp, phụ kiện

    [Header("Vật phẩm tiêu hao")]
    public ItemData binhMau;
    public ItemData thuocAnThan;
    public ItemData banhMi;

    void Update()
    {
        // Phím 1, 2, 3 giữ nguyên để thêm vật phẩm tiêu hao [cite: 17]
        if (Input.GetKeyDown(KeyCode.Alpha1)) InventoryManager.Instance.AddItem(binhMau, 3);
        if (Input.GetKeyDown(KeyCode.Alpha2)) InventoryManager.Instance.AddItem(thuocAnThan, 5);
        if (Input.GetKeyDown(KeyCode.Alpha3)) InventoryManager.Instance.AddItem(banhMi, 10);

        // PHÍM T: THÊM TẤT CẢ TRANG BỊ TRONG MẢNG VÀO TÚI ĐỒ
        if (Input.GetKeyDown(KeyCode.T))
        {
            if (allTestEquipment != null)
            {
                foreach (ItemData item in allTestEquipment)
                {
                    InventoryManager.Instance.AddItem(item, 1);
                }
                Debug.Log("✅ Đã thêm trang bị test!");

                // THÊM DÒNG NÀY: Nếu menu trang bị đang mở thì ép nó vẽ lại đồ ngay
                if (EquipmentUI.Instance != null && EquipmentUI.Instance.gameObject.activeInHierarchy)
                {
                    EquipmentUI.Instance.RefreshAll();
                }
            }
        }

        if (Input.GetKeyDown(KeyCode.Y))
        {
            if (PlayerManager.Instance != null)
            {
                PlayerManager.Instance.gold += 1000;
                Debug.Log("<color=yellow>💰 Đã buff 1000 Vàng!</color>");
            }
        }

        // PHÍM U: CỘNG 1000 EXP
        if (Input.GetKeyDown(KeyCode.U))
        {
            if (PlayerManager.Instance != null)
            {
                // Dùng hàm AddExp để trigger luôn logic LevelUp (tăng máu, cho điểm chỉ số)
                PlayerManager.Instance.AddExp(1000);
                Debug.Log("<color=cyan>🌟 Đã buff 1000 EXP!</color>");
            }
        }
    }
}