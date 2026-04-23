using UnityEngine;

public enum SkillTargetType { Random, SingleTarget, SelfBuff }

[CreateAssetMenu(fileName = "New Skill", menuName = "RPG/Skill Data")]
public class SkillData : ScriptableObject
{
    public string skillID; 
    public string skillName;
    public Sprite icon;
    [TextArea] public string description;
    public SkillTargetType targetType;

    [Header("Cấu hình Nâng cấp")]
    public int maxLevel = 5;
    public int baseCost = 100;

    [Header("Hồi chiêu (CD)")]
    [Tooltip("Số lượt phải chờ sau khi dùng kỹ năng này. 0 = không có CD.")]
    public int cooldown = 0;

    [Header("Thời gian tồn tại Buff (chỉ dùng cho SelfBuff)")]
    [Tooltip("Số lượt buff tồn tại sau khi kích hoạt. 0 = vô thời hạn trong trận.")]
    public int buffDuration = 0;

    [Header("Thông số tùy chỉnh (Điền % hoặc Sát thương cho từng cấp)")]
    // Độ dài của mảng này phải bằng maxLevel. Element 0 = Cấp 1.
    public float[] damageBonusPerLevel;
    public float[] effectChancePerLevel; // Dùng cho tỷ lệ (VD: 30% chí mạng, 20% chảy máu)
}