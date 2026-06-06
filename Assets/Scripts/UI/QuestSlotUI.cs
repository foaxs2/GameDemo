using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class QuestSlotUI : MonoBehaviour
{
    public Image iconImage;
    public TextMeshProUGUI targetText;
    public TextMeshProUGUI progressText;
    public TextMeshProUGUI rewardText;
    public GameObject darkOverlay;

    private QuestSaveData currentQuest;
    private QuestConfig currentConfig;
    private GuildUI parentUI;
    private Button mainButton;

    private void Awake()
    {
        mainButton = GetComponent<Button>();
    }

    public void Setup(QuestSaveData quest, GuildUI ui)
    {
        currentQuest = quest;
        parentUI = ui;
        
        currentConfig = GuildManager.Instance.GetQuestConfig(quest.questID);
        if (currentConfig == null) return;

        // Cập nhật Hình ảnh
        if (currentConfig.questIcon != null)
            iconImage.sprite = currentConfig.questIcon;

        // Cập nhật Text
        if (currentConfig.type == QuestType.Hunt)
            targetText.text = "Săn: " + currentConfig.targetName;
        else
            targetText.text = "Tìm: " + currentConfig.targetName;

        // Ghi phần thưởng
        string rText = $"{currentConfig.rewardGold}g";
        if (currentConfig.rewardExp > 0) rText += $", {currentConfig.rewardExp}exp";
        if (currentConfig.rewardItem != null) rText += $", {currentConfig.rewardItemAmount} {currentConfig.rewardItem.itemName}";
        if (currentConfig.rewardEquipment != null) rText += $", {currentConfig.rewardEquipment.itemName}";
        rewardText.text = rText;
        
        UpdateProgress();

        if (mainButton != null)
        {
            mainButton.onClick.RemoveAllListeners();
            mainButton.onClick.AddListener(OnSlotClicked);
        }
    }

    public void UpdateProgress()
    {
        if (currentQuest == null || currentConfig == null) return;

        if (currentQuest.isAccepted)
        {
            if (darkOverlay != null) darkOverlay.SetActive(true);

            bool canComplete = false;
            
            if (currentConfig.type == QuestType.Hunt)
            {
                int currentKilled = 0;
                if (PlayerManager.Instance.killedMonsters.ContainsKey(currentConfig.targetName))
                    currentKilled = PlayerManager.Instance.killedMonsters[currentConfig.targetName];
                
                int remaining = currentConfig.requiredAmount - currentKilled;
                if (remaining < 0) remaining = 0;
                
                progressText.text = $"{remaining} / {currentConfig.requiredAmount}";
                canComplete = currentKilled >= currentConfig.requiredAmount;
            }
            else if (currentConfig.type == QuestType.Gather)
            {
                int currentAmount = 0;
                ItemData item = SaveSystem.Instance.allItems != null ? System.Array.Find(SaveSystem.Instance.allItems, x => x != null && x.itemName == currentConfig.targetName) : null;
                if (item != null)
                    currentAmount = InventoryManager.Instance.GetItemQuantity(item);
                
                progressText.text = $"{currentAmount} / {currentConfig.requiredAmount}";
                canComplete = currentAmount >= currentConfig.requiredAmount;
            }

            progressText.color = canComplete ? Color.green : Color.white;
        }
        else
        {
            if (darkOverlay != null) darkOverlay.SetActive(false);
            progressText.text = $"Yêu cầu: {currentConfig.requiredAmount}";
            progressText.color = Color.white;
        }
    }

    private void OnSlotClicked()
    {
        if (currentQuest == null || currentConfig == null) return;

        if (!currentQuest.isAccepted)
        {
            parentUI.AcceptQuest(currentQuest.questID);
        }
        else
        {
            bool canComplete = false;
            if (currentConfig.type == QuestType.Hunt)
            {
                int currentKilled = 0;
                if (PlayerManager.Instance.killedMonsters.ContainsKey(currentConfig.targetName))
                    currentKilled = PlayerManager.Instance.killedMonsters[currentConfig.targetName];
                canComplete = currentKilled >= currentConfig.requiredAmount;
            }
            else if (currentConfig.type == QuestType.Gather)
            {
                int currentAmount = 0;
                ItemData item = SaveSystem.Instance.allItems != null ? System.Array.Find(SaveSystem.Instance.allItems, x => x != null && x.itemName == currentConfig.targetName) : null;
                if (item != null)
                    currentAmount = InventoryManager.Instance.GetItemQuantity(item);
                canComplete = currentAmount >= currentConfig.requiredAmount;
            }
            
            if (canComplete)
            {
                parentUI.TryCompleteQuest(currentQuest.questID);
            }
            else
            {
                parentUI.ShowCancelConfirm(currentQuest.questID);
            }
        }
    }
}
