using System.Collections.Generic;
using UnityEngine;

public class FloatingTextManager : MonoBehaviour
{
    // --- SINGLETON ---
    public static FloatingTextManager Instance { get; private set; }

    [Header("Cấu hình Pool")]
    [SerializeField] private GameObject textPrefab; // Kéo Prefab DamageText ở Bước 2 vào đây
    [SerializeField] private Transform worldCanvasParent; // Kéo WorldSpaceCanvas ở Bước 2 vào đây
    [SerializeField] private int poolSize = 10; // Số lượng chữ tạo sẵn lúc đầu trận

    // Danh sách "Bể chứa" đối tượng
    private List<GameObject> pool = new List<GameObject>();

    private void Awake()
    {
        if (Instance != null && Instance != this) Destroy(gameObject);
        else Instance = this;
    }

    private void Start()
    {
        if (worldCanvasParent == null)
        {
            Debug.LogError("Chưa gán WorldSpaceCanvas cho FloatingTextManager!");
            return;
        }

        // Tạo sẵn các đối tượng lúc đầu trận để tránh lag khi combat
        for (int i = 0; i < poolSize; i++)
        {
            CreateNewTextObject();
        }
    }

    // Hàm phụ: Tạo 1 object mới, ẩn đi và thêm vào pool
    private GameObject CreateNewTextObject()
    {
        GameObject obj = Instantiate(textPrefab, worldCanvasParent); // Tạo dưới parent Canvas
        obj.SetActive(false); // Ẩn đi ngay lập tức
        pool.Add(obj); // Thêm vào pool
        return obj;
    }

    public void SpawnText(Vector3 position, string content, Color color)
    {
        GameObject textObj = null;

        // 1. Tìm đối tượng đang ẩn (đang nhàn rỗi) trong Pool
        for (int i = 0; i < pool.Count; i++)
        {
            if (!pool[i].activeInHierarchy)
            {
                textObj = pool[i];
                break; // Tìm thấy rồi, thoát vòng lặp
            }
        }

        // 2. Nếu không tìm thấy đối tượng nào nhàn rỗi (pool bị quá tải)
        if (textObj == null)
        {
            // Tạo thêm object mới (mở rộng pool) để đáp ứng
            textObj = CreateNewTextObject();
        }

        // 3. Kích hoạt và thiết lập chữ
        textObj.transform.position = position; // Gán vị trí xuất phát
        textObj.SetActive(true);

        // Lấy component script và gán nội dung/màu
        textObj.GetComponent<FloatingText>().SetText(content, color);
    }
}