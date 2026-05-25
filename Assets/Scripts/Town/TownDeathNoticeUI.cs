using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Attach vào object trong scene Town.
/// Khi scene Town load, đọc DeathContext.Pending và hiện bảng thông báo tương ứng.
/// </summary>
public class TownDeathNoticeUI : MonoBehaviour
{
    [Header("Bảng Thông Báo Chết (HP = 0)")]
    [SerializeField] private GameObject panelDeath;
    [SerializeField] private TextMeshProUGUI txtDeathMessage;
    [SerializeField] private Button btnDeathConfirm;

    [Header("Bảng Thông Báo Phát Điên (SEN = 0)")]
    [SerializeField] private GameObject panelMadness;
    [SerializeField] private TextMeshProUGUI txtMadnessMessage;
    [SerializeField] private Button btnMadnessConfirm;

    private void Start()
    {
        // Ẩn cả 2 bảng mặc định
        if (panelDeath   != null) panelDeath.SetActive(false);
        if (panelMadness != null) panelMadness.SetActive(false);

        // Gán sự kiện nút xác nhận
        if (btnDeathConfirm   != null) btnDeathConfirm.onClick.AddListener(CloseDeathPanel);
        if (btnMadnessConfirm != null) btnMadnessConfirm.onClick.AddListener(CloseMadnessPanel);

        // Kiểm tra loại death được truyền vào
        DeathContext.DeathType deathType = DeathContext.Consume();

        switch (deathType)
        {
            case DeathContext.DeathType.CombatDeath:
                ShowDeath("Bạn đã tỉnh lại...\nMáu của bạn đã được hồi phục.");
                break;

            case DeathContext.DeathType.CombatMadness:
                ShowMadness("Bạn đã trở về thành với bộ dạng rách rưới.\nLý trí của bạn đã tan vỡ trong trận chiến.");
                break;

            case DeathContext.DeathType.DungeonDeath:
                ShowDeath("Bạn đã tỉnh lại...\nMáu của bạn đã được hồi phục.");
                break;

            case DeathContext.DeathType.DungeonMadness:
                ShowMadness("Bạn đã trở về thành với bộ dạng rách rưới.\nLý trí của bạn đã sụp đổ trong hầm ngục.");
                break;

            // None: không làm gì
        }
    }

    private void ShowDeath(string message)
    {
        if (panelDeath == null) return;
        if (txtDeathMessage != null) txtDeathMessage.text = message;
        panelDeath.SetActive(true);
    }

    private void ShowMadness(string message)
    {
        if (panelMadness == null) return;
        if (txtMadnessMessage != null) txtMadnessMessage.text = message;
        panelMadness.SetActive(true);
    }

    private void CloseDeathPanel()
    {
        if (panelDeath != null) panelDeath.SetActive(false);
    }

    private void CloseMadnessPanel()
    {
        if (panelMadness != null) panelMadness.SetActive(false);
    }
}
