using UnityEngine;
using TMPro; // Bắt buộc phải có để dùng TextMeshPro

public class FloatingText : MonoBehaviour
{
    [Header("Cấu hình Hiệu ứng")]
    [SerializeField] private TextMeshProUGUI textMesh;
    [SerializeField] private float moveSpeed = 2f;
    [SerializeField] private float fadeDuration = 1f;

    private float timer; // Bộ đếm thời gian
    private Color originalColor;
    private Vector3 randomOffset;

    private void Awake()
    {
        // Tự động lấy component nếu chưa kéo vào Inspector
        if (textMesh == null) textMesh = GetComponent<TextMeshProUGUI>();
        originalColor = textMesh.color;
    }
    public void SetText(string content, Color color)
    {
        textMesh.text = content;
        textMesh.color = color; // Gán màu (Vàng cho chí mạng, Trắng thường, Đỏ dính độc...)
        originalColor = color;

        timer = 0f; // Reset bộ đếm

        // Tạo độ lệch ngẫu nhiên nhẹ ở vị trí xuất phát (X: -0.2 đến 0.2, Y: -0.1 đến 0.1)
        randomOffset = new Vector3(Random.Range(0.5f, 1f), Random.Range(-0.1f, 0.1f), 0f);
        transform.localPosition += randomOffset; // Áp dụng độ lệch
    }

    private void Update()
    {
        // 1. Hiệu ứng bay lên
        transform.Translate(Vector3.up * moveSpeed * Time.deltaTime, Space.World);

        // 2. Hiệu ứng mờ dần (Fade Out)
        timer += Time.deltaTime;
        float alphaPercentage = 1f - (timer / fadeDuration); // Tính % alpha còn lại

        Color newColor = originalColor;
        newColor.a = alphaPercentage; // Cập nhật độ trong suốt mới
        textMesh.color = newColor;

        // 3. Tự động trả về Pool khi hết thời gian
        if (timer >= fadeDuration)
        {
            // Trả vị trí về 0 (relative với parent) để lần sau dùng lại không bị lệch
            transform.localPosition -= randomOffset;
            gameObject.SetActive(false); // Ẩn đối tượng đi (trả về pool)
        }
    }
}