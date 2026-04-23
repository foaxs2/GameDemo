using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SkillMenuUI : MonoBehaviour
{
    public static SkillMenuUI Instance { get; private set; }

    [Header("Spawning")]
    public GameObject skillButtonPrefab;
    public Transform skillButtonContainer;

    [Header("Tooltip")]
    public GameObject tooltipPanel;
    public TextMeshProUGUI tooltipNameText;
    public TextMeshProUGUI tooltipLevelText;
    public TextMeshProUGUI tooltipDescText;

    private List<SkillButtonUI> spawnedButtons = new List<SkillButtonUI>();
    private int selectedIndex = 0;

    void Awake()
    {
        if (Instance == null) Instance = this;
    }

    void Update()
    {
        if (!gameObject.activeSelf) return;
        HandleNavigation();
    }

    public void Open()
    {
        if (Instance == null) Instance = this;
        gameObject.SetActive(true);
        transform.SetAsLastSibling();
        RefreshButtons();

        selectedIndex = 0;
        if (spawnedButtons.Count > 0)
            UpdateSelection();
        else
            HideTooltip();
    }

    public void Close()
    {
        HideTooltip();
        gameObject.SetActive(false);
    }

    void RefreshButtons()
    {
        foreach (var btn in spawnedButtons)
            if (btn != null) Destroy(btn.gameObject);
        spawnedButtons.Clear();

        if (SkillManager.Instance == null || SkillManager.Instance.allSkills == null) return;

        foreach (var skill in SkillManager.Instance.allSkills)
        {
            if (skill == null) continue;
            int level = SkillManager.Instance.GetSkillLevel(skill.skillID);
            if (level <= 0) continue;

            GameObject obj = Instantiate(skillButtonPrefab, skillButtonContainer);
            SkillButtonUI ui = obj.GetComponent<SkillButtonUI>();
            if (ui != null)
            {
                ui.Setup(skill, this);
                spawnedButtons.Add(ui);
            }
        }
        LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)skillButtonContainer);
    }

    /// <summary>Làm mới số CD trên tất cả các button (gọi sau mỗi lượt)</summary>
    public void RefreshAllCDDisplays()
    {
        foreach (var btn in spawnedButtons)
            if (btn != null) btn.RefreshCDDisplay();
    }

    void HandleNavigation()
    {
        // X luôn thoát dù có skill hay không
        if (Input.GetKeyDown(KeyCode.X))
        {
            CombatManager.Instance?.CancelSkillMenu();
            return;
        }

        if (spawnedButtons.Count == 0) return;

        int cols = GetColumnCount();
        bool selectionChanged = false;

        if (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D))
        {
            selectedIndex = (selectedIndex + 1) % spawnedButtons.Count;
            selectionChanged = true;
        }
        if (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.A))
        {
            selectedIndex = (selectedIndex - 1 + spawnedButtons.Count) % spawnedButtons.Count;
            selectionChanged = true;
        }
        if (Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.S))
        {
            selectedIndex = Mathf.Min(selectedIndex + cols, spawnedButtons.Count - 1);
            selectionChanged = true;
        }
        if (Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.W))
        {
            selectedIndex = Mathf.Max(selectedIndex - cols, 0);
            selectionChanged = true;
        }

        if (selectionChanged) UpdateSelection();

        if (Input.GetKeyDown(KeyCode.Z) || Input.GetKeyDown(KeyCode.Return))
            ConfirmSelected();
    }

    int GetColumnCount()
    {
        if (skillButtonContainer == null) return 4;
        GridLayoutGroup grid = skillButtonContainer.GetComponent<GridLayoutGroup>();
        if (grid != null && grid.constraint == GridLayoutGroup.Constraint.FixedColumnCount)
            return grid.constraintCount;
        return 4;
    }

    void UpdateSelection()
    {
        for (int i = 0; i < spawnedButtons.Count; i++)
            spawnedButtons[i].SetHighlight(i == selectedIndex);
    }

    void ConfirmSelected()
    {
        if (selectedIndex < 0 || selectedIndex >= spawnedButtons.Count) return;
        var btn = spawnedButtons[selectedIndex];

        // Kiểm tra CD
        int remaining = 0;
        CombatManager.Instance?.skillCooldowns.TryGetValue(btn.skillData.skillID, out remaining);
        if (remaining > 0) return;

        CombatManager.Instance?.OnSkillButton(btn.skillData);
    }

    // ─── Tooltip ─────────────────────────────────────────────────────

    public void ShowTooltip(SkillData skill)
    {
        if (tooltipPanel == null || skill == null) return;
        tooltipPanel.SetActive(true);

        if (tooltipNameText != null)
            tooltipNameText.text = skill.skillName;

        int lv = SkillManager.Instance != null ? SkillManager.Instance.GetSkillLevel(skill.skillID) : 0;

        if (tooltipLevelText != null)
        {
            string cdInfo = skill.cooldown > 0 ? $"  |  CD: {skill.cooldown} lượt" : "";
            tooltipLevelText.text = $"Lv.{lv} / {skill.maxLevel}{cdInfo}";
        }

        // Hiện mô tả với số liệu cụ thể của cấp hiện tại (giống màn hình học kỹ năng)
        if (tooltipDescText != null)
            tooltipDescText.text = BuildDescription(skill, lv);
    }

    /// <summary>
    /// Xây dựng mô tả kỹ năng với số liệu cụ thể theo cấp,
    /// giống giao diện học kỹ năng (thay {0} bằng giá trị thực).
    /// </summary>
    string BuildDescription(SkillData skill, int level)
    {
        if (level <= 0 || skill.damageBonusPerLevel == null) return skill.description;

        int idx = Mathf.Clamp(level - 1, 0, skill.damageBonusPerLevel.Length - 1);
        float dmg = skill.damageBonusPerLevel[idx];
        float eff = (skill.effectChancePerLevel != null && idx < skill.effectChancePerLevel.Length)
            ? skill.effectChancePerLevel[idx] : 0f;

        // Thay {0} = dmgBonus, {1} = effectChance trong chuỗi description
        string desc = skill.description
            .Replace("{0}", dmg.ToString("0.##"))
            .Replace("{1}", eff.ToString("0.##"));

        // Nếu không có placeholder, thêm thông tin vào cuối
        if (!skill.description.Contains("{0}") && !skill.description.Contains("{1}"))
        {
            if (dmg > 0) desc += $"\n<color=#FFD700>+ {dmg:0.##} sát thương</color>";
            if (eff > 0) desc += $"\n<color=#FF8C69>+ {eff:0.##}% hiệu ứng</color>";
        }

        if (skill.buffDuration > 0)
            desc += $"\n<color=#98FB98>Tồn tại: {skill.buffDuration} lượt</color>";

        return desc;
    }

    public void HideTooltip()
    {
        if (tooltipPanel != null) tooltipPanel.SetActive(false);
    }

    public bool IsCurrentlySelected(SkillButtonUI btn)
        => selectedIndex >= 0 && selectedIndex < spawnedButtons.Count && spawnedButtons[selectedIndex] == btn;
}
