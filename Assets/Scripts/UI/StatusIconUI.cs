using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Gắn vào prefab StatusIconPrefab.
/// Layout prefab:
///   StatusIconPrefab (RectTransform 40x40)
///   ├── imgIcon         (Image, full 40x40) — icon buff/debuff chính
///   ├── imgStackBadge   (Image, ~14x14, góc dưới trái) — arrow sprite x1/x2/x3
///   ├── txtStackCount   (TextMeshProUGUI nhỏ, trên imgStackBadge) — số stack (ẩn nếu = 1)
///   └── txtDuration     (TextMeshProUGUI nhỏ, góc trên phải) — số lượt còn lại
/// </summary>
public class StatusIconUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] public Image            imgIcon;
    [SerializeField] public Image            imgStackBadge;   // arrow sprite ở góc dưới
    [SerializeField] public TextMeshProUGUI  txtStackCount;   // số stack (hiện khi > 1)
    [SerializeField] public TextMeshProUGUI  txtDuration;     // số lượt còn lại

    /// <summary>
    /// Cập nhật hiển thị icon.
    /// </summary>
    /// <param name="icon">Sprite icon chính</param>
    /// <param name="stacks">Số stack hiện tại</param>
    /// <param name="duration">Số lượt còn lại (0 = ẩn)</param>
    /// <param name="stackArrow">Sprite mũi tên tương ứng stack</param>
    public void Setup(Sprite icon, int stacks, int duration, Sprite stackArrow)
    {
        // Icon chính
        if (imgIcon != null)
        {
            imgIcon.sprite  = icon;
            imgIcon.enabled = icon != null;
        }

        // Stack badge (mũi tên ở góc dưới)
        if (imgStackBadge != null)
        {
            bool showBadge = stackArrow != null;
            imgStackBadge.gameObject.SetActive(showBadge);
            if (showBadge) imgStackBadge.sprite = stackArrow;
        }

        // Số stack (chỉ hiện khi stacks > 1)
        if (txtStackCount != null)
        {
            txtStackCount.gameObject.SetActive(stacks > 1);
            txtStackCount.text = stacks.ToString();
        }

        // Số lượt còn lại
        if (txtDuration != null)
        {
            txtDuration.gameObject.SetActive(duration > 0);
            txtDuration.text = duration.ToString();
        }
    }
}
