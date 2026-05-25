using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class EventManager : MonoBehaviour
{
    public static EventManager Instance { get; private set; }

    [Header("Tham chiếu Giao diện (UI)")]
    public GameObject panelEvent; 
    public Image iconEvent;       
    public TextMeshProUGUI nameEvent; 
    public TextMeshProUGUI descriptionEvent; 

    [Header("Các Nút Lựa Chọn")]
    public Button[] choiceButtons; 
    public TextMeshProUGUI[] choiceTexts; 
    public Button btnContinue;

    private EventData currentEvent;

    private void Awake()
    {
        if (Instance == null) Instance = this;
    }

    private void Start()
    {
        if (panelEvent != null) panelEvent.SetActive(false);
        if (btnContinue != null)
        {
            btnContinue.onClick.AddListener(ClosePanel);
            btnContinue.gameObject.SetActive(false); 
        }
    }

    private void ClosePanel()
    {
        panelEvent.SetActive(false);
        if (PlayerMovement.Instance != null)
            PlayerMovement.Instance.SetEventLock(false);
        if (UIManager.Instance != null)
            UIManager.Instance.SetHUDVisible(true);
    }

    public void TriggerEvent(EventData eventData)
    {
        if (eventData == null || panelEvent == null) return;

        currentEvent = eventData;
        panelEvent.SetActive(true);

        if (PlayerMovement.Instance != null)
            PlayerMovement.Instance.SetEventLock(true);
        if (UIManager.Instance != null)
            UIManager.Instance.SetHUDVisible(false);

        nameEvent.text = eventData.eventName;
        if (eventData.eventIcon != null) iconEvent.sprite = eventData.eventIcon;

        // PHÂN LOẠI: LÀ BẪY HAY LÀ SỰ KIỆN LỰA CHỌN?
        if (eventData.choices == null || eventData.choices.Length == 0)
        {
            // === XỬ LÝ CẠM BẪY ===
            // 1. Tắt tất cả nút lựa chọn, bật nút Tiếp tục
            foreach (var btn in choiceButtons) btn.gameObject.SetActive(false);
            btnContinue.gameObject.SetActive(true);

            // 2. Gom các chỉ số bị phạt thành 1 dòng text
            string penaltyDetail = "";
            if (eventData.trapPenaltyHP > 0) penaltyDetail += $"-{eventData.trapPenaltyHP} HP, ";
            if (eventData.trapPenaltyFood > 0) penaltyDetail += $"-{eventData.trapPenaltyFood} Food, ";
            if (eventData.trapPenaltySanity > 0) penaltyDetail += $"-{eventData.trapPenaltySanity} Sen, ";
            if (eventData.trapPenaltyGold > 0) penaltyDetail += $"-{eventData.trapPenaltyGold} Vàng, ";

            if (penaltyDetail.EndsWith(", ")) penaltyDetail = penaltyDetail.Substring(0, penaltyDetail.Length - 2);
            string penaltyLine = !string.IsNullOrEmpty(penaltyDetail) ? $"\n\n<color=orange>Hậu quả: {penaltyDetail}</color>" : "";

            descriptionEvent.text = eventData.description + penaltyLine;

            // 3. Trừ thẳng chỉ số người chơi
            if (eventData.trapPenaltyHP > 0) PlayerManager.Instance.TakeDamage(eventData.trapPenaltyHP, true, true);
            if (eventData.trapPenaltyFood > 0)
            {
                PlayerManager.Instance.food -= eventData.trapPenaltyFood;
                if (PlayerManager.Instance.food < 0) PlayerManager.Instance.food = 0;
            }
            if (eventData.trapPenaltySanity > 0) PlayerManager.Instance.ReduceSanity(eventData.trapPenaltySanity);
            if (eventData.trapPenaltyGold > 0)
            {
                PlayerManager.Instance.gold -= eventData.trapPenaltyGold;
                if (PlayerManager.Instance.gold < 0) PlayerManager.Instance.gold = 0;
            }

            // Kiểm tra chết sau bẫy: HP = 0 → Dungeon Death
            if (PlayerManager.Instance.currentHP <= 0)
            {
                if (DungeonDeathUI.Instance != null)
                    DungeonDeathUI.Instance.ShowDeathPanel();
                else
                {
                    // Fallback nếu không có DungeonDeathUI trong scene
                    PlayerManager.Instance.currentHP = PlayerManager.Instance.maxHP;
                    DeathContext.Pending = DeathContext.DeathType.DungeonDeath;
                    SaveSystem.Instance?.Save();
                    UnityEngine.SceneManagement.SceneManager.LoadScene("Town");
                }
                return;
            }

        }
        else
        {
            // === XỬ LÝ SỰ KIỆN LỰA CHỌN BÌNH THƯỜNG ===
            descriptionEvent.text = eventData.description;
            btnContinue.gameObject.SetActive(false);

            for (int i = 0; i < choiceButtons.Length; i++)
            {
                if (i < eventData.choices.Length)
                {
                    choiceButtons[i].gameObject.SetActive(true);
                    choiceTexts[i].text = eventData.choices[i].choiceText;

                    int choiceIndex = i; 
                    choiceButtons[i].onClick.RemoveAllListeners();
                    choiceButtons[i].onClick.AddListener(() => OnChoiceSelected(choiceIndex));
                }
                else
                {
                    choiceButtons[i].gameObject.SetActive(false);
                }
            }
        }
    }

    private void OnChoiceSelected(int choiceIndex)
    {
        EventChoice choice = currentEvent.choices[choiceIndex];

        float bonusFromSanity = PlayerManager.Instance.sen * 3f;
        float finalChance = Mathf.Clamp(choice.baseSuccessChance + bonusFromSanity, 0f, 100f);

        float roll = Random.Range(0f, 100f);
        bool isSuccess = roll <= finalChance;

        foreach (var btn in choiceButtons) btn.gameObject.SetActive(false);
        btnContinue.gameObject.SetActive(true);

        if (isSuccess)
        {
            string rewardDetail = "";
            if (choice.rewardGold > 0) rewardDetail += $"+{choice.rewardGold} Vàng, ";
            if (choice.rewardExp > 0) rewardDetail += $"+{choice.rewardExp} EXP, ";
            if (choice.rewardHP > 0) rewardDetail += $"+{choice.rewardHP} HP, ";
            if (choice.rewardFood > 0) rewardDetail += $"+{choice.rewardFood} Food, ";
            if (choice.rewardSanity > 0) rewardDetail += $"+{choice.rewardSanity} Sen, ";
            if (choice.rewardItem != null) rewardDetail += $"+1 {choice.rewardItem.itemName}, ";
            if (!string.IsNullOrEmpty(choice.buffStatType) && choice.buffStatAmount > 0)
                rewardDetail += $"+{choice.buffStatAmount} {choice.buffStatType.ToUpper()}, ";

            if (rewardDetail.EndsWith(", ")) rewardDetail = rewardDetail.Substring(0, rewardDetail.Length - 2);
            string rewardLine = !string.IsNullOrEmpty(rewardDetail) ? $"\n<color=yellow>Nhận: {rewardDetail}</color>" : "";

            descriptionEvent.text = $"<color=green>THÀNH CÔNG!</color>{rewardLine}\n\n{choice.successMessage}";

            if (choice.rewardGold > 0) PlayerManager.Instance.gold += choice.rewardGold;
            if (choice.rewardExp > 0) PlayerManager.Instance.AddExp(choice.rewardExp);
            // Track cho hình phạt chết: cộng phần thưởng vàng/EXP từ sự kiện vào bộ đếm tầng
            if (choice.rewardGold > 0) PlayerMovement.floorGoldEarned += choice.rewardGold;
            if (choice.rewardExp  > 0) PlayerMovement.floorExpEarned  += choice.rewardExp;
            if (choice.rewardHP > 0)
            {
                PlayerManager.Instance.currentHP += choice.rewardHP;
                if (PlayerManager.Instance.currentHP > PlayerManager.Instance.maxHP) PlayerManager.Instance.currentHP = PlayerManager.Instance.maxHP;
            }
            if (choice.rewardFood > 0)
            {
                PlayerManager.Instance.food += choice.rewardFood;
                if (PlayerManager.Instance.food > PlayerManager.Instance.maxFood) PlayerManager.Instance.food = PlayerManager.Instance.maxFood;
            }
            if (choice.rewardSanity > 0) PlayerManager.Instance.AddSanity(choice.rewardSanity);

            if (choice.rewardItem != null && InventoryManager.Instance != null)
                InventoryManager.Instance.AddItem(choice.rewardItem, 1);

            if (!string.IsNullOrEmpty(choice.buffStatType) && choice.buffStatAmount > 0)
            {
                switch (choice.buffStatType.ToUpper())
                {
                    case "STR": PlayerManager.Instance.str += choice.buffStatAmount; break;
                    case "DEX": PlayerManager.Instance.dex += choice.buffStatAmount; break;
                    case "VIT":
                        PlayerManager.Instance.vit += choice.buffStatAmount;
                        PlayerManager.Instance.UpdateMaxHP(); 
                        break;
                    case "AGL":
                        PlayerManager.Instance.agl += choice.buffStatAmount;
                        PlayerManager.Instance.currentSpeed = PlayerManager.Instance.GetTotalSpeed(); 
                        break;
                }
            }
        }
        else
        {
            string penaltyDetail = "";
            if (choice.penaltyHP > 0) penaltyDetail += $"-{choice.penaltyHP} HP, ";
            if (choice.penaltyFood > 0) penaltyDetail += $"-{choice.penaltyFood} Food, ";
            if (choice.penaltySanity > 0) penaltyDetail += $"-{choice.penaltySanity} Sen, ";
            if (choice.penaltyGold > 0) penaltyDetail += $"-{choice.penaltyGold} Vàng, ";

            if (penaltyDetail.EndsWith(", ")) penaltyDetail = penaltyDetail.Substring(0, penaltyDetail.Length - 2);
            string penaltyLine = !string.IsNullOrEmpty(penaltyDetail) ? $"\n<color=orange>Phạt: {penaltyDetail}</color>" : "";

            descriptionEvent.text = $"<color=red>THẤT BẠI...</color>{penaltyLine}\n\n{choice.failMessage}";

            if (choice.penaltyHP > 0) PlayerManager.Instance.TakeDamage(choice.penaltyHP, true, true);
            if (choice.penaltyFood > 0)
            {
                PlayerManager.Instance.food -= choice.penaltyFood;
                if (PlayerManager.Instance.food < 0) PlayerManager.Instance.food = 0;
            }
            if (choice.penaltySanity > 0) PlayerManager.Instance.ReduceSanity(choice.penaltySanity);
            if (choice.penaltyGold > 0)
            {
                PlayerManager.Instance.gold -= choice.penaltyGold;
                if (PlayerManager.Instance.gold < 0) PlayerManager.Instance.gold = 0;
            }
        }
    }
}