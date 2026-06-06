using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SkillTreeUI : MonoBehaviour
{
    [Header("Danh sách Kỹ năng có thể học")]
    public SkillData[] availableSkills;

    [Header("Bảng Thông Tin (Popup)")]
    public GameObject skillDetailPanel;
    public Button btnCloseDetail;

    public Image detailIcon;
    public TextMeshProUGUI txtDetailName;
    public TextMeshProUGUI txtDetailLevel;
    public TextMeshProUGUI txtDetailDescCurrent;
    public TextMeshProUGUI txtDetailDescNext;

    public Button btnUpgrade;
    public TextMeshProUGUI txtUpgradeBtn;

    [Header("Tài chính")] // THÊM MỚI: Dùng để hiển thị tiền đang có
    public TextMeshProUGUI txtPlayerGold;

    private SkillData currentSelectedSkill;

    void Start()
    {
        skillDetailPanel.SetActive(false);
        if (btnCloseDetail != null)
            btnCloseDetail.onClick.AddListener(() => skillDetailPanel.SetActive(false));

        btnUpgrade.onClick.AddListener(OnUpgradeButtonClicked);
    }

    public void OpenSkillDetail(SkillData skill)
    {
        currentSelectedSkill = skill;
        skillDetailPanel.SetActive(true);
        RefreshDetailPanel();
    }

    private void RefreshDetailPanel()
    {
        if (currentSelectedSkill == null) return;

        // Cập nhật hiển thị số Vàng hiện tại
        if (txtPlayerGold != null && PlayerManager.Instance != null)
        {
            txtPlayerGold.text = $"{PlayerManager.Instance.gold}";
        }

        SkillData skill = currentSelectedSkill;
        int currentLevel = SkillManager.Instance.GetSkillLevel(skill.skillID);

        detailIcon.sprite = skill.icon;
        txtDetailName.text = skill.skillName;
        txtDetailLevel.text = $"Cấp độ: {currentLevel}/{skill.maxLevel}";

        // 1. XỬ LÝ MÔ TẢ CẤP HIỆN TẠI
        if (currentLevel == 0)
        {
            txtDetailDescCurrent.text = "Chưa học.";
        }
        else
        {
            txtDetailDescCurrent.text = $"[Cấp {currentLevel}]: {GetFormattedDescription(skill, currentLevel - 1, false)}";
        }

        // 2. XỬ LÝ MÔ TẢ CẤP TIẾP THEO (Tô màu xanh lá cây)
        if (currentLevel >= skill.maxLevel)
        {
            txtDetailDescNext.text = "Kỹ năng đã đạt cấp độ tối đa.";
            txtUpgradeBtn.text = "Nâng cấp (MAX)";
            btnUpgrade.interactable = false;
        }
        else
        {
            txtDetailDescNext.text = $"[Cấp {currentLevel + 1}]:\n{GetFormattedDescription(skill, currentLevel, true)}";

            int cost = SkillManager.Instance.GetUpgradeCost(skill);
            txtUpgradeBtn.text = currentLevel == 0 ? $"Học ({cost}G)" : $"Nâng cấp ({cost}G) Cấp {currentLevel}";

            btnUpgrade.interactable = PlayerManager.Instance.gold >= cost; // Chỉ cho bấm nếu đủ tiền
        }
    }

    // HÀM MỚI: Tự động ghép số vào mô tả và tô màu
    private string GetFormattedDescription(SkillData skill, int levelIndex, bool highlight)
    {
        if (levelIndex < 0 || levelIndex >= skill.maxLevel) return skill.description;

        float dmg = skill.damageBonusPerLevel[levelIndex];
        float effect = skill.effectChancePerLevel[levelIndex];

        // Nếu highlight = true (cho cấp tiếp theo), bọc thẻ màu xanh vào. Nếu không thì in số bình thường.
        string dmgStr = highlight ? $"<color=green>{dmg}</color>" : dmg.ToString();
        string effectStr = highlight ? $"<color=green>{effect}</color>" : effect.ToString();

        string finalDesc = skill.description;

        // Tìm chữ {0} thay bằng Sát thương, tìm {1} thay bằng Hiệu ứng
        if (finalDesc.Contains("{0}")) finalDesc = finalDesc.Replace("{0}", dmgStr);
        if (finalDesc.Contains("{1}")) finalDesc = finalDesc.Replace("{1}", effectStr);

        return finalDesc;
    }

    private void OnUpgradeButtonClicked()
    {
        if (SkillManager.Instance.UpgradeSkill(currentSelectedSkill))
        {
            RefreshDetailPanel();
        }
    }
}