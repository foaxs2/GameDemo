using System.Collections.Generic;
using UnityEngine;

public class SkillManager : MonoBehaviour
{
    public static SkillManager Instance;

    [Header("Tất cả Kỹ năng trong Game")]
    [Tooltip("Kéo toàn bộ SkillData ScriptableObject vào đây — SkillMenuUI sẽ tự lọc theo level")]
    public SkillData[] allSkills;

    // Dictionary lưu ID kỹ năng và Cấp độ hiện tại (0 = chưa học)
    public Dictionary<string, int> learnedSkills = new Dictionary<string, int>();

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else if (Instance != this)
        {
            if (gameObject.GetComponent<Canvas>() != null || gameObject.GetComponent<Camera>() != null)
            {
                Destroy(this);
            }
            else
            {
                Destroy(gameObject);
            }
        }
    }

    public int GetSkillLevel(string skillID)
    {
        if (learnedSkills.TryGetValue(skillID, out int level)) return level;
        return 0;
    }

    public int GetUpgradeCost(SkillData skill)
    {
        int currentLevel = GetSkillLevel(skill.skillID);
        if (currentLevel >= skill.maxLevel) return 0;

        // Công thức: Lần 1 = gốc, Lần 2 = gốc * 1.5, Lần 3 = Lần 2 * 1.5 (Làm tròn xuống)
        float cost = skill.baseCost;
        for (int i = 0; i < currentLevel; i++)
        {
            cost = Mathf.Floor(cost * 1.5f);
        }

        // Áp dụng giảm 25% học kĩ năng khi có sự kiện SkillDiscount
        if (TownEventManager.Instance != null && TownEventManager.Instance.CurrentEvent == TownEvent.SkillDiscount)
        {
            cost = Mathf.RoundToInt(cost * 0.75f);
        }

        return (int)cost;
    }

    public bool UpgradeSkill(SkillData skill)
    {
        int cost = GetUpgradeCost(skill);
        int currentLevel = GetSkillLevel(skill.skillID);

        if (currentLevel >= skill.maxLevel) return false;
        if (PlayerManager.Instance.gold < cost) return false;

        // Trừ tiền và tăng cấp
        PlayerManager.Instance.gold -= cost;
        learnedSkills[skill.skillID] = currentLevel + 1;
        Debug.Log($"Đã nâng cấp {skill.skillName} lên cấp {currentLevel + 1}");
        return true;
    }
}