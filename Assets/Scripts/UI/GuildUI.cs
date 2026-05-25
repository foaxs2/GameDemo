using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections.Generic;

public class GuildUI : MonoBehaviour
{
    public static GuildUI Instance { get; private set; }

    [Header("UI Panels")]
    public GameObject guildPanel;
    public Transform questBoardContainer;
    public GameObject questSlotPrefab;

    [Header("Player Info")]
    public TextMeshProUGUI goldText;
    public TextMeshProUGUI hpPotionsText;
    public TextMeshProUGUI foodText;

    [Header("Buttons")]
    public Button btnClaimSalary;
    public TextMeshProUGUI salaryText;
    public Button btnBuyHP;
    public Button btnBuyFood;
    public Button btnRefreshBoard;
    public Button btnClose;

    [Header("Shop Config")]
    public int hpPotionCost = 10;
    public int foodCost = 30;
    [Tooltip("Kéo ItemData của thuốc hồi máu vào đây (Bắt buộc)")]
    public ItemData hpPotionItem;

    [Header("Cancel Confirm Panel")]
    public GameObject cancelConfirmPanel;
    public Button btnConfirmYes;
    public Button btnConfirmNo;
    private string pendingCancelQuestID;

    private List<QuestSlotUI> activeSlots = new List<QuestSlotUI>();

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        btnClaimSalary.onClick.AddListener(ClaimSalary);
        btnBuyHP.onClick.AddListener(BuyHP);
        btnBuyFood.onClick.AddListener(BuyFood);
        if(btnRefreshBoard != null) btnRefreshBoard.onClick.AddListener(RefreshQuestBoard);
        btnClose.onClick.AddListener(CloseGuild);

        if (btnConfirmYes != null) btnConfirmYes.onClick.AddListener(ConfirmCancelQuest);
        if (btnConfirmNo != null) btnConfirmNo.onClick.AddListener(() => { if (cancelConfirmPanel != null) cancelConfirmPanel.SetActive(false); });

        if (cancelConfirmPanel != null) cancelConfirmPanel.SetActive(false);
        if (guildPanel != null) guildPanel.SetActive(false);
    }

    public void OpenGuild()
    {
        guildPanel.SetActive(true);
        UpdatePlayerInfo();
        UpdateSalaryButton();
        PopulateQuestBoard();
    }

    public void CloseGuild()
    {
        guildPanel.SetActive(false);
    }

    private void UpdatePlayerInfo()
    {
        if (PlayerManager.Instance == null) return;

        goldText.text = PlayerManager.Instance.gold.ToString();
        foodText.text = $"{PlayerManager.Instance.food}/{PlayerManager.Instance.maxFood}";

        if (hpPotionItem != null)
        {
            int count = InventoryManager.Instance.GetItemQuantity(hpPotionItem);
            hpPotionsText.text = $"{count} / 3"; 
        }
    }

    private void UpdateSalaryButton()
    {
        if (GuildManager.Instance == null) return;

        if (GuildManager.Instance.HasSalary())
        {
            btnClaimSalary.interactable = true;
            salaryText.text = "40G";
        }
        else
        {
            btnClaimSalary.interactable = false;
            salaryText.text = "Đã Nhận";
        }
    }

    private void ClaimSalary()
    {
        if (GuildManager.Instance.TryClaimSalary())
        {
            UpdatePlayerInfo();
            UpdateSalaryButton();
            FloatingTextManager.Instance?.SpawnText(PlayerManager.Instance.transform.position, "+40 Gold", Color.yellow);
        }
    }

    private void BuyHP()
    {
        if (hpPotionItem == null) return;

        int currentCount = InventoryManager.Instance.GetItemQuantity(hpPotionItem);
        if (currentCount >= 3)
        {
            Debug.Log("[GUILD] Đã đầy bình máu (3/3).");
            return;
        }

        if (PlayerManager.Instance.gold >= hpPotionCost)
        {
            PlayerManager.Instance.gold -= hpPotionCost;
            InventoryManager.Instance.AddItem(hpPotionItem, 1);
            UpdatePlayerInfo();
            Debug.Log("[GUILD] Đã mua 1 bình máu.");
        }
        else
        {
            Debug.Log("[GUILD] Không đủ vàng mua bình máu.");
        }
    }

    private void BuyFood()
    {
        if (PlayerManager.Instance.food >= PlayerManager.Instance.maxFood)
        {
            Debug.Log("[GUILD] Lương thực đã đầy!");
            return;
        }

        if (PlayerManager.Instance.gold >= foodCost)
        {
            PlayerManager.Instance.gold -= foodCost;
            int healAmount = Mathf.FloorToInt(PlayerManager.Instance.maxFood * 0.5f);
            PlayerManager.Instance.food += healAmount;
            if (PlayerManager.Instance.food > PlayerManager.Instance.maxFood) 
                PlayerManager.Instance.food = PlayerManager.Instance.maxFood;
                
            UpdatePlayerInfo();
            Debug.Log("[GUILD] Đã mua Lương thực.");
        }
        else
        {
            Debug.Log("[GUILD] Không đủ vàng mua Lương thực.");
        }
    }

    private void PopulateQuestBoard()
    {
        if (GuildManager.Instance == null) return;

        foreach (Transform child in questBoardContainer)
        {
            Destroy(child.gameObject);
        }
        activeSlots.Clear();

        var quests = GuildManager.Instance.GetActiveQuests();

        if (quests.Count == 0)
        {
            GuildManager.Instance.RefreshBoard();
            quests = GuildManager.Instance.GetActiveQuests();
        }

        foreach (var q in quests)
        {
            GameObject go = Instantiate(questSlotPrefab, questBoardContainer);
            QuestSlotUI slotUI = go.GetComponent<QuestSlotUI>();
            if (slotUI != null)
            {
                slotUI.Setup(q, this);
                activeSlots.Add(slotUI);
            }
        }
    }

    private void RefreshQuestBoard()
    {
        GuildManager.Instance.RefreshBoard();
        PopulateQuestBoard();
    }

    public void AcceptQuest(string questID)
    {
        GuildManager.Instance.AcceptQuest(questID);
        RefreshAllSlots();
    }

    public void ShowCancelConfirm(string questID)
    {
        pendingCancelQuestID = questID;
        if (cancelConfirmPanel != null) cancelConfirmPanel.SetActive(true);
        else Debug.LogWarning("[GUILD] Chưa gắn Cancel Confirm Panel trong Inspector!");
    }

    private void ConfirmCancelQuest()
    {
        CancelQuest(pendingCancelQuestID);
        if (cancelConfirmPanel != null) cancelConfirmPanel.SetActive(false);
    }

    public void CancelQuest(string questID)
    {
        GuildManager.Instance.CancelQuest(questID);
        PopulateQuestBoard(); 
    }

    public void TryCompleteQuest(string questID)
    {
        if (GuildManager.Instance.CompleteQuest(questID))
        {
            UpdatePlayerInfo();
            PopulateQuestBoard();
        }
        else
        {
            Debug.Log("[GUILD] Chưa đủ điều kiện trả nhiệm vụ!");
        }
    }

    private void RefreshAllSlots()
    {
        foreach (var slot in activeSlots)
        {
            slot.UpdateProgress();
        }
    }
}
