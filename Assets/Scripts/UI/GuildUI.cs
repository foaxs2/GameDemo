using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections.Generic;
using UnityEngine.EventSystems;

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
    public TextMeshProUGUI senText; // Hiển thị số sen đang có / Tổng sen (Ví dụ: 2/10)

    [Header("Buttons")]
    public Button btnClaimSalary;
    public TextMeshProUGUI salaryText;
    public Button btnBuyHP;
    public Button btnBuyFood;
    public Button btnBuyWine; // Nút mua rượu
    public Button btnRefreshBoard;
    public Button btnClose;

    [Header("Shop Config")]
    public int hpPotionCost = 10;
    public int foodCost = 30;
    public int wineCost = 15; // Giá rượu có thể chỉnh được (mặc định 15)
    [Tooltip("Kéo ItemData của thuốc hồi máu vào đây (Bắt buộc)")]
    public ItemData hpPotionItem;

    [Header("Price Overlays")]
    public GameObject hpPriceOverlay;
    public TextMeshProUGUI hpPriceOverlayText;
    public GameObject foodPriceOverlay;
    public TextMeshProUGUI foodPriceOverlayText;
    public GameObject winePriceOverlay;
    public TextMeshProUGUI winePriceOverlayText;

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
        if (btnBuyWine != null) btnBuyWine.onClick.AddListener(BuyWine);
        if(btnRefreshBoard != null) btnRefreshBoard.onClick.AddListener(RefreshQuestBoard);
        btnClose.onClick.AddListener(CloseGuild);

        if (btnConfirmYes != null) btnConfirmYes.onClick.AddListener(ConfirmCancelQuest);
        if (btnConfirmNo != null) btnConfirmNo.onClick.AddListener(() => { if (cancelConfirmPanel != null) cancelConfirmPanel.SetActive(false); });

        if (cancelConfirmPanel != null) cancelConfirmPanel.SetActive(false);
        if (guildPanel != null) guildPanel.SetActive(false);

        // Gắn sự kiện Hover cho các nút mua đồ
        AddHoverListeners(btnBuyHP, hpPriceOverlay, hpPriceOverlayText, GetHPPotionCost());
        AddHoverListeners(btnBuyFood, foodPriceOverlay, foodPriceOverlayText, GetFoodCost());
        AddHoverListeners(btnBuyWine, winePriceOverlay, winePriceOverlayText, GetWineCost());
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

        if (senText != null)
        {
            senText.text = $"{PlayerManager.Instance.sen}/{PlayerManager.Instance.maxSen}";
        }

        // Cập nhật giá hiển thị động trên các lớp phủ khi có sự kiện Town
        if (hpPriceOverlayText != null) hpPriceOverlayText.text = $"{GetHPPotionCost()}G";
        if (foodPriceOverlayText != null) foodPriceOverlayText.text = $"{GetFoodCost()}G";
        if (winePriceOverlayText != null) winePriceOverlayText.text = $"{GetWineCost()}G";
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

        int cost = GetHPPotionCost();
        if (PlayerManager.Instance.gold >= cost)
        {
            PlayerManager.Instance.gold -= cost;
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

        int cost = GetFoodCost();
        if (PlayerManager.Instance.gold >= cost)
        {
            PlayerManager.Instance.gold -= cost;
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

    private void BuyWine()
    {
        if (PlayerManager.Instance == null) return;

        // Kiểm tra nếu sen đã đầy
        if (PlayerManager.Instance.sen >= PlayerManager.Instance.maxSen)
        {
            Debug.Log("[GUILD] Chỉ số Sen đã đầy!");
            return;
        }

        int cost = GetWineCost();
        // Kiểm tra nếu đủ tiền mua rượu
        if (PlayerManager.Instance.gold >= cost)
        {
            PlayerManager.Instance.gold -= cost;
            PlayerManager.Instance.AddSanity(2); // Hồi phục 2 sen
            UpdatePlayerInfo();
            Debug.Log("[GUILD] Đã mua Rượu, hồi phục 2 Sen.");
        }
        else
        {
            Debug.Log("[GUILD] Không đủ vàng để mua Rượu!");
        }
    }

    private void AddHoverListeners(Button button, GameObject overlay, TextMeshProUGUI overlayText, int cost)
    {
        if (button == null || overlay == null) return;

        // Cập nhật text giá tiền hiển thị trên lớp phủ
        if (overlayText != null)
        {
            overlayText.text = $"{cost}G";
        }

        // Mặc định ẩn lớp phủ giá tiền đi
        overlay.SetActive(false);

        // Lấy hoặc tự động thêm EventTrigger vào Button
        EventTrigger trigger = button.gameObject.GetComponent<EventTrigger>();
        if (trigger == null)
        {
            trigger = button.gameObject.AddComponent<EventTrigger>();
        }

        // Hover chuột vào (PointerEnter) -> Hiện lớp phủ
        EventTrigger.Entry entryEnter = new EventTrigger.Entry();
        entryEnter.eventID = EventTriggerType.PointerEnter;
        entryEnter.callback.AddListener((data) => { overlay.SetActive(true); });
        trigger.triggers.Add(entryEnter);

        // Rê chuột ra ngoài (PointerExit) -> Ẩn lớp phủ
        EventTrigger.Entry entryExit = new EventTrigger.Entry();
        entryExit.eventID = EventTriggerType.PointerExit;
        entryExit.callback.AddListener((data) => { overlay.SetActive(false); });
        trigger.triggers.Add(entryExit);
    }

    public int GetHPPotionCost()
    {
        int cost = hpPotionCost;
        if (TownEventManager.Instance != null && TownEventManager.Instance.CurrentEvent == TownEvent.ShopDiscount)
        {
            cost = Mathf.RoundToInt(cost * 0.75f);
        }
        return cost;
    }

    public int GetFoodCost()
    {
        int cost = foodCost;
        if (TownEventManager.Instance != null && TownEventManager.Instance.CurrentEvent == TownEvent.ShopDiscount)
        {
            cost = Mathf.RoundToInt(cost * 0.75f);
        }
        return cost;
    }

    public int GetWineCost()
    {
        if (TownEventManager.Instance != null && TownEventManager.Instance.CurrentEvent == TownEvent.FreeWine)
        {
            return 0; // Miễn phí rượu
        }
        return wineCost;
    }
}
