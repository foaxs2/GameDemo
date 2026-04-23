using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;

public class TownInteraction : MonoBehaviour
{
    public string locationName;
    public TrainingUI trainingUI;
    public ShopUI shopUI;
    void Update()
    {
        if (Mouse.current == null) return;
        // Ngan viecj click khi con tro chuot o tren UI
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
        {
            return;
        }

        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            Vector2 mousePosition = Mouse.current.position.ReadValue();
            Vector2 worldPosition = Camera.main.ScreenToWorldPoint(mousePosition);
            RaycastHit2D hit = Physics2D.Raycast(worldPosition, Vector2.zero);

            if (hit.collider != null && hit.collider.gameObject == gameObject)
            {
                Debug.Log("Bạn đã click vào: " + locationName);

                // 1. MỞ KHU VỰC HUẤN LUYỆN
                if (locationName == "Khu vực huấn luyện" && trainingUI != null)
                {
                    trainingUI.OpenUI();
                }

                // 2. TƯƠNG TÁC VỚI BẢN ĐỒ ĐỂ XUỐNG DUNGEON
                if (locationName == "Bản đồ")
                {
                    Debug.Log($"Bắt đầu thám hiểm Dungeon! Tầng hiện tại: {PlayerMovement.currentFloor}");
                    SceneManager.LoadScene("Dungeon"); // Thay tên cho khớp với tên Scene của bạn
                }
                //3 Tuowng tacs cuawr hangf
                if (locationName == "Cửa hàng" && shopUI != null)
                {
                    shopUI.OpenShop();
                }
            }
        }
    }
}