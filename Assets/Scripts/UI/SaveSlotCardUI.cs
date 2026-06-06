using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SaveSlotCardUI : MonoBehaviour
{
    [Header("Card Title")]
    public TextMeshProUGUI txtSlotTitle; // Hiện "Khung Lưu 1", "Khung Lưu 2", "Khung Lưu 3"

    [Header("Save Info Elements")]
    public TextMeshProUGUI txtPlayerName;   // Hiện tên người chơi (hoặc "Slot Trống")
    public TextMeshProUGUI txtPlayerStats;  // Hiện "Cấp X" hoặc "Nhấp để tạo nhân vật"
    public TextMeshProUGUI txtPlayTime;     // Hiện "Thời gian chơi: 1g 30p" hoặc ẩn đi

    [Header("Visual Elements")]
    public Image imgCharacterIcon;          // Icon nhân vật
    public Sprite spriteDefaultIcon;        // Sprite mặc định khi slot trống
    public Sprite[] spritesCharacterIcons;  // Array chứa các icon nhân vật để bốc theo index

    [Header("Action Buttons")]
    public Button btnCardAction;            // Nút nhấn vào cả card để load/tạo mới
    public Button btnDeleteSlot;            // Nút nhỏ màu đỏ để xóa save slot

    private int currentSlotIndex = -1;
    private StartSceneManager manager;

    public void SetupCard(int slotIndex, bool hasSave, StartSceneManager parentManager)
    {
        currentSlotIndex = slotIndex;
        manager = parentManager;

        // Set tiêu đề slot
        if (txtSlotTitle != null) txtSlotTitle.text = $"Khung Lưu {slotIndex}";

        // Xóa các Listener cũ tránh kích hoạt trùng lặp
        btnCardAction.onClick.RemoveAllListeners();
        btnDeleteSlot.onClick.RemoveAllListeners();

        // Gán sự kiện nhấn vào Card chính
        btnCardAction.onClick.AddListener(() => manager.SelectSlot(slotIndex));

        if (hasSave)
        {
            // Lấy dữ liệu metadata từ file save
            PlayerSaveData meta = SaveSystem.GetPlayerMetadata(slotIndex);

            if (meta != null)
            {
                if (txtPlayerName != null) txtPlayerName.text = string.IsNullOrEmpty(meta.playerName) ? "Anh Hùng Vô Danh" : meta.playerName;
                if (txtPlayerStats != null) txtPlayerStats.text = $"Cấp độ: {meta.level} | HP: {meta.currentHP}/{meta.maxHP}";
                if (txtPlayTime != null) txtPlayTime.text = FormatPlayTime(meta.playTime);

                // Setup Icon
                if (imgCharacterIcon != null)
                {
                    if (spritesCharacterIcons != null && spritesCharacterIcons.Length > 0)
                    {
                        int idx = Mathf.Clamp(meta.characterIconIndex, 0, spritesCharacterIcons.Length - 1);
                        imgCharacterIcon.sprite = spritesCharacterIcons[idx];
                    }
                    imgCharacterIcon.gameObject.SetActive(true);
                }

                // Hiện nút xóa
                if (btnDeleteSlot != null)
                {
                    btnDeleteSlot.gameObject.SetActive(true);
                    btnDeleteSlot.onClick.AddListener(() => manager.RequestDeleteSlot(slotIndex));
                }
            }
            else
            {
                // File save lỗi hoặc rỗng
                SetupAsEmpty();
            }
        }
        else
        {
            // Không có save -> Setup trống
            SetupAsEmpty();
        }
    }

    private void SetupAsEmpty()
    {
        if (txtPlayerName != null) txtPlayerName.text = "<color=#999999>Slot Trống</color>";
        if (txtPlayerStats != null) txtPlayerStats.text = "Tạo nhân vật";
        if (txtPlayTime != null) txtPlayTime.text = "";

        if (imgCharacterIcon != null)
        {
            if (spriteDefaultIcon != null)
            {
                imgCharacterIcon.sprite = spriteDefaultIcon;
                imgCharacterIcon.gameObject.SetActive(true);
            }
            else
            {
                imgCharacterIcon.gameObject.SetActive(false); // Ẩn đi nếu không có sprite default
            }
        }

        if (btnDeleteSlot != null)
        {
            btnDeleteSlot.gameObject.SetActive(false); // Ẩn nút xóa khi slot trống
        }
    }

    private string FormatPlayTime(float seconds)
    {
        int hrs = Mathf.FloorToInt(seconds / 3600);
        int mins = Mathf.FloorToInt((seconds % 3600) / 60);
        int secs = Mathf.FloorToInt(seconds % 60);

        if (hrs > 0)
        {
            return $"{hrs} giờ {mins} phút";
        }
        else if (mins > 0)
        {
            return $"{mins} phút {secs} giây";
        }
        return $"{secs} giây";
    }
}
