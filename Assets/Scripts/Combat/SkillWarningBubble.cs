using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Quản lý UI bong bóng cảnh báo kỹ năng hiển thị trên đầu kẻ thù.
/// </summary>
public class SkillWarningBubble : MonoBehaviour
{
    [Header("UI References")]
    [Tooltip("Khung bong bóng cảnh báo")]
    public Image bubbleBg;
    [Tooltip("Icon hiển thị kỹ năng kẻ thù sắp sử dụng")]
    public Image skillIconImage;

    /// <summary>Hiển thị bong bóng với Icon kỹ năng tương ứng.</summary>
    public void Show(Sprite skillIcon)
    {
        if (skillIconImage != null && skillIcon != null)
        {
            skillIconImage.sprite = skillIcon;
            skillIconImage.enabled = true;
        }

        gameObject.SetActive(true);
    }

    /// <summary>Ẩn bong bóng cảnh báo.</summary>
    public void Hide()
    {
        gameObject.SetActive(false);
    }
}
