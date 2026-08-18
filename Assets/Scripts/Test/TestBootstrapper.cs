using UnityEngine;

[DefaultExecutionOrder(-100)]
public class TestBootstrapper : MonoBehaviour
{
    [Header("Prefab GameManager (chứa PlayerManager, InventoryManager...)")]
    public GameObject gameManagerPrefab;
    
    void Awake()
    {
        if (PlayerManager.Instance == null && gameManagerPrefab != null)
        {
            Instantiate(gameManagerPrefab);
            Debug.Log("[TEST] Đã khởi tạo GameManager từ prefab cho test scene.");

            // Tự động nạp file lưu riêng biệt của TEST MODE nếu có
            if (SaveSystem.Instance != null && SaveSystem.HasSave())
            {
                SaveSystem.Instance.Load();
            }
        }
    }
}
