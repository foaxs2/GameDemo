using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

/// <summary>
/// Singleton quản lý bảng thông báo chết / phát điên trong scene Dungeon.
/// Attach vào một GameObject trong Dungeon scene.
/// </summary>
public class DungeonDeathUI : MonoBehaviour
{
    public static DungeonDeathUI Instance { get; private set; }

    [Header("Bảng Chết (HP = 0)")]
    [SerializeField] private GameObject deathPanel;
    [SerializeField] private TextMeshProUGUI txtDeathTitle;
    [SerializeField] private Button btnDeathReturn;

    [Header("Bảng Phát Điên (SEN = 0)")]
    [SerializeField] private GameObject madnessPanel;
    [SerializeField] private TextMeshProUGUI txtMadnessTitle;
    [SerializeField] private Button btnMadnessReturn;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }

        if (deathPanel   != null) deathPanel.SetActive(false);
        if (madnessPanel != null) madnessPanel.SetActive(false);

        if (btnDeathReturn   != null) btnDeathReturn.onClick.AddListener(OnDeathReturnTown);
        if (btnMadnessReturn != null) btnMadnessReturn.onClick.AddListener(OnMadnessReturnTown);
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    /// <summary>Hiện bảng "Bạn Đã Chết" — khóa di chuyển, chờ người chơi bấm nút.</summary>
    public void ShowDeathPanel()
    {
        // Khóa di chuyển
        if (PlayerMovement.Instance != null)
            PlayerMovement.Instance.SetDeathLock(true);

        if (deathPanel != null)
            deathPanel.SetActive(true);
        else
            Debug.LogWarning("[DungeonDeathUI] deathPanel chưa được gán trong Inspector!");
    }

    /// <summary>Hiện bảng "Bạn Đã Phát Điên" — khóa di chuyển, chờ người chơi bấm nút.</summary>
    public void ShowMadnessPanel()
    {
        // Khóa di chuyển
        if (PlayerMovement.Instance != null)
            PlayerMovement.Instance.SetDeathLock(true);

        if (madnessPanel != null)
            madnessPanel.SetActive(true);
        else
            Debug.LogWarning("[DungeonDeathUI] madnessPanel chưa được gán trong Inspector!");
    }

    // Nút "Trở Về Thị Trấn" trong bảng chết
    private void OnDeathReturnTown()
    {
        // Áp hình phạt: mất nửa vàng & EXP kiếm được trong tầng
        PlayerMovement.ApplyDeathPenalty();
        PlayerManager.Instance.currentHP = PlayerManager.Instance.maxHP; // hồi full HP
        DeathContext.Pending = DeathContext.DeathType.DungeonDeath;
        SaveSystem.Instance?.Save();
        SceneManager.LoadScene("Town");
    }

    // Nút "Trở Về Thị Trấn" trong bảng phát điên
    private void OnMadnessReturnTown()
    {
        // Áp hình phạt: mất nửa vàng & EXP kiếm được trong tầng
        PlayerMovement.ApplyDeathPenalty();
        PlayerManager.Instance.sen = 2;        // SEN = 2, HP giữ nguyên
        DeathContext.Pending = DeathContext.DeathType.DungeonMadness;
        SaveSystem.Instance?.Save();
        SceneManager.LoadScene("Town");
    }
}
