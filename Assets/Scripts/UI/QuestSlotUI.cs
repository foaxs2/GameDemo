using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class QuestSlotUI : MonoBehaviour
{
    [Header("UI Cơ Bản (Dùng chung)")]
    public Image iconImage;
    public TextMeshProUGUI targetText;
    public TextMeshProUGUI progressText;
    public TextMeshProUGUI rewardText;
    public GameObject darkOverlay;

    [Header("Tùy Chọn Cho Thanh Ngang (Dungeon Row)")]
    public TextMeshProUGUI txtGoldReward; // Số vàng riêng (ví dụ: "90")
    public TextMeshProUGUI txtExpReward;  // Số EXP riêng (ví dụ: "25")

    [Header("Tùy Chọn Quà Tặng Kèm (Vật Phẩm / Trang Bị Gộp Chung 1 Ô)")]
    [Tooltip("Icon quà tặng kèm (Vật Phẩm hoặc Trang Bị, tự ẩn nếu không có quà)")]
    public Image imgRewardItemIcon;
    [Tooltip("Text số lượng quà tặng (chỉ hiển thị 'x1', 'x2'..., tự ẩn nếu không có quà)")]
    public TextMeshProUGUI txtRewardItemQty;

    [Header("Nút Nhận Thưởng Khi Xong Quest")]
    public Button claimButton;

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

        // Cập nhật Hình ảnh Icon nhiệm vụ
        if (iconImage != null && currentConfig.questIcon != null)
            iconImage.sprite = currentConfig.questIcon;

        // Cập nhật Text Vàng & EXP riêng nếu có
        if (txtGoldReward != null) txtGoldReward.text = currentConfig.rewardGold.ToString();
        if (txtExpReward != null) txtExpReward.text = currentConfig.rewardExp > 0 ? currentConfig.rewardExp.ToString() : "";

        // Cập nhật Quà Tặng Kèm (Vật phẩm hoặc Trang bị gộp chung vào 1 Icon + Text)
        ItemData bonusItem = currentConfig.rewardItem != null ? currentConfig.rewardItem : currentConfig.rewardEquipment;
        int bonusAmount = currentConfig.rewardItem != null ? Mathf.Max(1, currentConfig.rewardItemAmount) : (currentConfig.rewardEquipment != null ? 1 : 0);

        if (imgRewardItemIcon != null)
        {
            if (bonusItem != null && bonusItem.icon != null)
            {
                imgRewardItemIcon.gameObject.SetActive(true);
                imgRewardItemIcon.sprite = bonusItem.icon;
                if (txtRewardItemQty != null)
                {
                    txtRewardItemQty.gameObject.SetActive(true);
                    txtRewardItemQty.text = $"x{bonusAmount}";
                }
            }
            else
            {
                imgRewardItemIcon.gameObject.SetActive(false);
                if (txtRewardItemQty != null) txtRewardItemQty.gameObject.SetActive(false);
            }
        }
        else if (txtRewardItemQty != null)
        {
            txtRewardItemQty.gameObject.SetActive(false);
        }

        // Ghi phần thưởng gộp (cho Prefab cũ ở Town)
        if (rewardText != null)
        {
            string rText = $"{currentConfig.rewardGold}g";
            if (currentConfig.rewardExp > 0) rText += $", {currentConfig.rewardExp}exp";
            if (currentConfig.rewardItem != null) rText += $", {currentConfig.rewardItemAmount} {currentConfig.rewardItem.itemName}";
            if (currentConfig.rewardEquipment != null) rText += $", {currentConfig.rewardEquipment.itemName}";
            rewardText.text = rText;
        }
        
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

        string typePrefix = currentConfig.type == QuestType.Hunt ? "Săn" : "Tìm";

        if (targetText != null) targetText.text = $"{typePrefix}: {currentConfig.targetName}";

        if (currentQuest.isAccepted)
        {
            bool canComplete = false;
            int currentAmount = 0;
            
            if (currentConfig.type == QuestType.Hunt)
            {
                if (PlayerManager.Instance != null && PlayerManager.Instance.killedMonsters.ContainsKey(currentConfig.targetName))
                    currentAmount = PlayerManager.Instance.killedMonsters[currentConfig.targetName];
                
                int displayKilled = Mathf.Min(currentAmount, currentConfig.requiredAmount);
                if (progressText != null) progressText.text = $"{displayKilled} / {currentConfig.requiredAmount}";
                canComplete = currentAmount >= currentConfig.requiredAmount;
            }
            else if (currentConfig.type == QuestType.Gather)
            {
                if (InventoryManager.Instance != null)
                    currentAmount = InventoryManager.Instance.GetItemQuantity(currentConfig.targetName);
                
                int displayAmount = Mathf.Min(currentAmount, currentConfig.requiredAmount);
                if (progressText != null) progressText.text = $"{displayAmount} / {currentConfig.requiredAmount}";
                canComplete = currentAmount >= currentConfig.requiredAmount;
            }

            // Khi hoàn thành: Bật darkOverlay và hiện đầy đủ nút claimButton cùng Text bên trong
            if (canComplete)
            {
                EnsureClaimButton();

                if (darkOverlay != null)
                {
                    darkOverlay.SetActive(true);
                    darkOverlay.transform.SetAsLastSibling();
                }

                if (claimButton != null)
                {
                    claimButton.gameObject.SetActive(true);
                    claimButton.transform.SetAsLastSibling();

                    // Kích hoạt tất cả các component Text/TextMeshPro bên trong nút claim
                    var tmps = claimButton.GetComponentsInChildren<TextMeshProUGUI>(true);
                    foreach (var tmp in tmps)
                    {
                        tmp.gameObject.SetActive(true);
                        tmp.enabled = true;
                    }

                    var texts = claimButton.GetComponentsInChildren<Text>(true);
                    foreach (var txt in texts)
                    {
                        txt.gameObject.SetActive(true);
                        txt.enabled = true;
                    }

                    claimButton.onClick.RemoveAllListeners();
                    claimButton.onClick.AddListener(OnClaimButtonClicked);
                }
            }
            else
            {
                if (darkOverlay != null) darkOverlay.SetActive(false);
                if (claimButton != null) claimButton.gameObject.SetActive(false);
            }
        }
        else
        {
            if (darkOverlay != null) darkOverlay.SetActive(false);
            if (claimButton != null) claimButton.gameObject.SetActive(false);
            if (targetText != null) targetText.text = $"{typePrefix}: {currentConfig.targetName}";
            if (progressText != null) progressText.text = $"Yêu cầu: {currentConfig.requiredAmount}";
        }
    }

    private void EnsureClaimButton()
    {
        if (claimButton != null) return;

        // 1. Tìm theo tên trong root hoặc dưới darkOverlay
        Transform t = transform.Find("btnClaim");
        if (t == null && darkOverlay != null) t = darkOverlay.transform.Find("btnClaim");
        if (t == null) t = transform.Find("ClaimButton");
        if (t == null && darkOverlay != null) t = darkOverlay.transform.Find("ClaimButton");
        if (t != null)
        {
            claimButton = t.GetComponent<Button>();
            if (claimButton != null) return;
        }

        // 2. Tìm trong toàn bộ con (kể cả inactive)
        Button[] buttons = GetComponentsInChildren<Button>(true);
        foreach (var b in buttons)
        {
            if (b != null && b != mainButton)
            {
                claimButton = b;
                return;
            }
        }
    }

    public void OnClaimButtonClicked()
    {
        if (currentQuest == null || currentConfig == null || GuildManager.Instance == null) return;

        bool success = GuildManager.Instance.CompleteQuest(currentQuest.questID);
        if (success)
        {
            Debug.Log($"[QUEST] Đã hoàn thành và nhận thưởng nhiệm vụ: {currentConfig.targetName} (+{currentConfig.rewardGold} Vàng, +{currentConfig.rewardExp} EXP)!");

            if (FloatingTextManager.Instance != null && PlayerMovement.Instance != null)
            {
                FloatingTextManager.Instance.SpawnText(PlayerMovement.Instance.transform.position, $"Hoàn thành Quest: +{currentConfig.rewardGold} Vàng!", Color.yellow);
            }

            if (parentUI != null)
            {
                parentUI.RefreshBoard();
            }

            if (DungeonUIManager.Instance != null)
            {
                DungeonUIManager.Instance.UpdateAllDungeonHUD();
            }

            if (SaveSystem.IsTestSceneActive())
            {
                SaveSystem.Instance?.Save();
            }
        }
    }

    private void OnSlotClicked()
    {
        if (currentQuest == null || currentConfig == null) return;

        if (!currentQuest.isAccepted)
        {
            if (parentUI != null) parentUI.AcceptQuest(currentQuest.questID);
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
                OnClaimButtonClicked();
            }
            else
            {
                if (parentUI != null) parentUI.ShowCancelConfirm(currentQuest.questID);
            }
        }
    }
}
