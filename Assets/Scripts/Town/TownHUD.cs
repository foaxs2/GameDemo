using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class TownHUD : MonoBehaviour
{
    [Header("HUD Text Fields")]
    public TextMeshProUGUI txtTownName;
    public TextMeshProUGUI txtEvent; // THÊM MỚI: Hiển thị Sự Kiện Town
    public TextMeshProUGUI txtHP;
    public TextMeshProUGUI txtGold;
    public TextMeshProUGUI txtSen;
    public TextMeshProUGUI txtFood;
    public TextMeshProUGUI txtLevel;
    public TextMeshProUGUI txtSkillPoints;

    [Header("EXP Slider")]
    public Slider expSlider;

    void Start()
    {
        if (txtTownName != null) 
        {
            txtTownName.text = "Thị trấn";
        }
    }

    void Update()
    {
        // Đảm bảo PlayerManager đã tồn tại trước khi lấy dữ liệu
        if (PlayerManager.Instance == null) return;
        var p = PlayerManager.Instance;

        // Cập nhật các thông số nhân vật lên UI
        if (txtHP != null) 
            txtHP.text = $"HP: {p.currentHP}/{p.maxHP}";

        if (txtGold != null) 
            txtGold.text = $"{p.gold}";

        if (txtSen != null) 
            txtSen.text = $"Sen: {p.sen}/{p.maxSen}";

        if (txtFood != null) 
            txtFood.text = $"Đói {p.food}/{p.maxFood}";

        if (txtLevel != null) 
            txtLevel.text = $"LV: {p.level}";

        if (txtSkillPoints != null) 
            txtSkillPoints.text = $"{p.unspentStatPoints} DKN";

        // Cập nhật sự kiện Town
        if (txtEvent != null)
        {
            if (TownEventManager.Instance != null)
            {
                TownEvent current = TownEventManager.Instance.CurrentEvent;
                if (current == TownEvent.None)
                {
                    txtEvent.text = "Bình yên";
                    txtEvent.color = Color.black;
                }
                else
                {
                    txtEvent.text = TownEventManager.Instance.GetEventName(current);
                    txtEvent.color = Color.green; // Tô màu xanh lá nổi bật cho sự kiện
                }
            }
            else
            {
                txtEvent.text = "Bình yên";
                txtEvent.color = Color.black;
            }
        }

        // Cập nhật thanh EXP
        if (expSlider != null)
        {
            expSlider.maxValue = p.expToNextLevel;
            expSlider.value = p.currentExp;
        }
    }
}
