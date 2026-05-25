using UnityEngine;

[System.Serializable]
public class EventChoice
{
    public string choiceText;
    [Range(0f, 100f)] 
    public float baseSuccessChance = 50f; 

    [Header("--- PHẦN THƯỞNG (Nếu thành công) ---")]
    public int rewardGold;
    public int rewardExp;
    public int rewardHP;
    public int rewardFood;
    public int rewardSanity;
    public ItemData rewardItem; 
    
    public string buffStatType; 
    public int buffStatAmount;
    
    [TextArea(2, 3)] public string successMessage;

    [Header("--- HÌNH PHẠT (Nếu thất bại) ---")]
    public int penaltyHP;
    public int penaltyFood;
    public int penaltySanity;
    public int penaltyGold;
    [TextArea(2, 3)] public string failMessage;
}

[CreateAssetMenu(fileName = "New Event", menuName = "RPG/Dungeon Event")]
public class EventData : ScriptableObject
{
    public string eventName;
    public Sprite eventIcon;
    [TextArea(5, 10)] public string description;
    
    [Header("--- SỰ KIỆN LỰA CHỌN ---")]
    public EventChoice[] choices;

    [Header("--- CẠM BẪY (Chỉ hoạt động khi Choices = 0) ---")]
    public int trapPenaltyHP;
    public int trapPenaltyFood;
    public int trapPenaltySanity;
    public int trapPenaltyGold;
}