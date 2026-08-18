using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class EventManager : MonoBehaviour
{
    public static EventManager Instance { get; private set; }

    [Header("Tham chiếu Giao diện (UI)")]
    public GameObject canvasEvent;
    public GameObject panelEvent; 
    public Image iconEvent;       
    public TextMeshProUGUI nameEvent; 
    public TextMeshProUGUI descriptionEvent; 

    [Header("Tùy Chọn Bố Cục")]
    [Tooltip("Nếu bật, code sẽ tự động ép kích thước các nút/chữ theo mẫu. Nếu tắt, sẽ dùng 100% kích thước và vị trí bạn tự chỉnh trong Prefab.")]
    public bool autoFormatLayout = false;

    [Header("Các Nút Lựa Chọn")]
    public Button[] choiceButtons; 
    public TextMeshProUGUI[] choiceTexts; 
    public Button btnContinue;

    private EventData currentEvent;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        AutoFindReferences();
    }

    private void Start()
    {
        AutoFindReferences();
        FormatEventPanelLayout();
        if (panelEvent != null) panelEvent.SetActive(false);
        if (canvasEvent != null) canvasEvent.SetActive(false);
        if (btnContinue != null)
        {
            btnContinue.onClick.RemoveAllListeners();
            btnContinue.onClick.AddListener(ClosePanel);
            btnContinue.gameObject.SetActive(false); 
        }
    }

    /// <summary>
    /// Tự động tìm kiếm và gắn lại toàn bộ các tham chiếu UI của EventManager nếu bị thiếu/mất liên kết.
    /// </summary>
    public void AutoFindReferences()
    {
        if (canvasEvent == null)
        {
            Transform ceTrans = transform.Find("Canvas_Event");
            if (ceTrans != null) canvasEvent = ceTrans.gameObject;
            else
            {
                Canvas[] canvases = Resources.FindObjectsOfTypeAll<Canvas>();
                foreach (var c in canvases)
                {
                    if (c.gameObject.name == "Canvas_Event")
                    {
                        canvasEvent = c.gameObject;
                        break;
                    }
                }
            }
        }

        if (panelEvent == null)
        {
            if (canvasEvent != null)
            {
                Transform peTrans = canvasEvent.transform.Find("PanelEvent");
                if (peTrans != null) panelEvent = peTrans.gameObject;
            }
            if (panelEvent == null)
            {
                GameObject peObj = GameObject.Find("PanelEvent");
                if (peObj != null) panelEvent = peObj;
            }
        }

        if (panelEvent != null)
        {
            if (iconEvent == null)
            {
                Transform t = panelEvent.transform.Find("IconEvent");
                if (t != null) iconEvent = t.GetComponent<Image>();
            }

            if (nameEvent == null)
            {
                Transform t = panelEvent.transform.Find("NameEvent");
                if (t != null) nameEvent = t.GetComponent<TextMeshProUGUI>();
            }

            if (descriptionEvent == null)
            {
                Transform t = panelEvent.transform.Find("Description");
                if (t != null) descriptionEvent = t.GetComponent<TextMeshProUGUI>();
            }

            if (choiceButtons == null || choiceButtons.Length < 3 || choiceButtons[0] == null)
            {
                choiceButtons = new Button[3];
                choiceTexts = new TextMeshProUGUI[3];

                for (int i = 0; i < 3; i++)
                {
                    Transform btnTrans = panelEvent.transform.Find($"btnLuaChon{i + 1}");
                    if (btnTrans != null)
                    {
                        choiceButtons[i] = btnTrans.GetComponent<Button>();
                        choiceTexts[i] = btnTrans.GetComponentInChildren<TextMeshProUGUI>();
                    }
                }
            }

            if (btnContinue == null)
            {
                Transform contTrans = panelEvent.transform.Find("btnTiepTuc");
                if (contTrans != null)
                {
                    btnContinue = contTrans.GetComponent<Button>();
                }
            }
        }
    }

    /// <summary>
    /// Đồng bộ vị trí và độ nổi của PanelEvent để che phủ Dungeon, 
    /// đồng thời tôn trọng toàn bộ kích thước chữ/nút bạn tùy biến trong Prefab.
    /// </summary>
    public void FormatEventPanelLayout()
    {
        if (panelEvent == null) return;

        // 1. Đảm bảo Canvas chứa Event luôn có SortOrder cao hơn Canvas Dungeon để không bị che
        Canvas eventCanvas = panelEvent.GetComponentInParent<Canvas>();
        if (eventCanvas != null)
        {
            eventCanvas.overrideSorting = true;
            eventCanvas.sortingOrder = 50;
        }
        panelEvent.transform.SetAsLastSibling();

        // 2. Tìm vị trí & kích thước của RawImage_DungeonViewport để đặt PanelEvent khớp 700x700 ngay trên Dungeon
        GameObject rawImgObj = GameObject.Find("RawImage_DungeonViewport");
        RectTransform rawRect = rawImgObj != null ? rawImgObj.GetComponent<RectTransform>() : null;
        RectTransform panelRect = panelEvent.GetComponent<RectTransform>();

        if (panelRect != null)
        {
            panelRect.anchorMin = new Vector2(0.5f, 0.5f);
            panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.pivot = new Vector2(0.5f, 0.5f);

            if (rawRect != null)
            {
                panelRect.anchoredPosition = rawRect.anchoredPosition;
                panelRect.sizeDelta = rawRect.sizeDelta; // 700 x 700
            }
            else
            {
                panelRect.anchoredPosition = new Vector2(0f, 40f);
                panelRect.sizeDelta = new Vector2(700f, 700f);
            }
        }

        // 3. Đảm bảo nền tối đặc (Alpha = 1) để che sạch dungeon và chặn chuột
        Image bgImg = panelEvent.GetComponent<Image>();
        if (bgImg != null)
        {
            bgImg.color = new Color(0.08f, 0.08f, 0.10f, 1.0f); // Nền đen đục 100%
            bgImg.raycastTarget = true;
        }

        // 4. Đảm bảo khung viền trang trí "Khung" trải kín 100% PanelEvent
        Transform khungTrans = panelEvent.transform.Find("Khung");
        if (khungTrans != null)
        {
            RectTransform khungRect = khungTrans.GetComponent<RectTransform>();
            if (khungRect != null)
            {
                khungRect.anchorMin = Vector2.zero;
                khungRect.anchorMax = Vector2.one;
                khungRect.pivot = new Vector2(0.5f, 0.5f);
                khungRect.anchoredPosition = Vector2.zero;
                khungRect.sizeDelta = Vector2.zero;
            }
        }

        // Nếu KHÔNG bật autoFormatLayout thì dừng tại đây, giữ nguyên 100% thiết kế nút và chữ trong Prefab của bạn
        if (!autoFormatLayout) return;

        // 5. Căn chỉnh Icon sự kiện (ở đỉnh)
        if (iconEvent != null)
        {
            RectTransform iconRect = iconEvent.GetComponent<RectTransform>();
            if (iconRect != null)
            {
                iconRect.anchorMin = new Vector2(0.5f, 0.5f);
                iconRect.anchorMax = new Vector2(0.5f, 0.5f);
                iconRect.pivot = new Vector2(0.5f, 0.5f);
                iconRect.anchoredPosition = new Vector2(0f, 220f);
                iconRect.sizeDelta = new Vector2(90f, 90f);
            }
            iconEvent.preserveAspect = true;
        }

        // 6. Căn chỉnh Tên sự kiện
        if (nameEvent != null)
        {
            RectTransform nameRect = nameEvent.GetComponent<RectTransform>();
            if (nameRect != null)
            {
                nameRect.anchorMin = new Vector2(0.5f, 0.5f);
                nameRect.anchorMax = new Vector2(0.5f, 0.5f);
                nameRect.pivot = new Vector2(0.5f, 0.5f);
                nameRect.anchoredPosition = new Vector2(0f, 155f);
                nameRect.sizeDelta = new Vector2(620f, 40f);
            }
            nameEvent.alignment = TextAlignmentOptions.Center;
            nameEvent.fontSize = 22;
            nameEvent.color = new Color(1.0f, 0.85f, 0.4f); // Vàng gold
        }

        // 7. Căn chỉnh Mô tả sự kiện
        if (descriptionEvent != null)
        {
            RectTransform descRect = descriptionEvent.GetComponent<RectTransform>();
            if (descRect != null)
            {
                descRect.anchorMin = new Vector2(0.5f, 0.5f);
                descRect.anchorMax = new Vector2(0.5f, 0.5f);
                descRect.pivot = new Vector2(0.5f, 0.5f);
                descRect.anchoredPosition = new Vector2(0f, 45f);
                descRect.sizeDelta = new Vector2(620f, 160f);
            }
            descriptionEvent.alignment = TextAlignmentOptions.Center;
            descriptionEvent.fontSize = 17;
            descriptionEvent.textWrappingMode = TextWrappingModes.Normal;
            descriptionEvent.color = new Color(0.9f, 0.9f, 0.95f);
        }

        // 8. Căn chỉnh các Nút Lựa Chọn (xếp dọc ở nửa dưới)
        float startY = -85f;
        float spacing = 58f;
        if (choiceButtons != null)
        {
            for (int i = 0; i < choiceButtons.Length; i++)
            {
                if (choiceButtons[i] != null)
                {
                    RectTransform btnRect = choiceButtons[i].GetComponent<RectTransform>();
                    if (btnRect != null)
                    {
                        btnRect.anchorMin = new Vector2(0.5f, 0.5f);
                        btnRect.anchorMax = new Vector2(0.5f, 0.5f);
                        btnRect.pivot = new Vector2(0.5f, 0.5f);
                        btnRect.anchoredPosition = new Vector2(0f, startY - (i * spacing));
                        btnRect.sizeDelta = new Vector2(540f, 48f);
                    }
                }
            }
        }

        // 9. Căn chỉnh Nút Tiếp Tục (nếu là bẫy hoặc xem xong kết quả)
        if (btnContinue != null)
        {
            RectTransform contRect = btnContinue.GetComponent<RectTransform>();
            if (contRect != null)
            {
                contRect.anchorMin = new Vector2(0.5f, 0.5f);
                contRect.anchorMax = new Vector2(0.5f, 0.5f);
                contRect.pivot = new Vector2(0.5f, 0.5f);
                contRect.anchoredPosition = new Vector2(0f, -170f);
                contRect.sizeDelta = new Vector2(540f, 50f);
            }
        }

        // Đảm bảo PanelEvent luôn nổi lên trên cùng màn hình
        panelEvent.transform.SetAsLastSibling();
    }

    private void ClosePanel()
    {
        panelEvent.SetActive(false);
        if (canvasEvent != null) canvasEvent.SetActive(false);

        if (PlayerMovement.Instance != null)
            PlayerMovement.Instance.SetEventLock(false);
        if (UIManager.Instance != null && DungeonUIManager.Instance == null)
            UIManager.Instance.SetHUDVisible(true);
        if (DungeonUIManager.Instance != null)
            DungeonUIManager.Instance.UpdateDungeonHUD();
    }

    private void ApplyTrapPenaltyAndClose(EventData eventData)
    {
        if (eventData != null && PlayerManager.Instance != null)
        {
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
                panelEvent.SetActive(false);
                if (canvasEvent != null) canvasEvent.SetActive(false);

                if (DungeonDeathUI.Instance != null)
                    DungeonDeathUI.Instance.ShowDeathPanel();
                else
                {
                    PlayerManager.Instance.currentHP = PlayerManager.Instance.maxHP;
                    DeathContext.Pending = DeathContext.DeathType.DungeonDeath;
                    SaveSystem.Instance?.Save();
                    UnityEngine.SceneManagement.SceneManager.LoadScene("Town");
                }
                return;
            }
        }

        ClosePanel();
    }

    public void TriggerEvent(EventData eventData)
    {
        if (eventData == null) return;

        AutoFindReferences();
        if (panelEvent == null) return;

        currentEvent = eventData;
        if (canvasEvent != null) canvasEvent.SetActive(true);
        FormatEventPanelLayout();
        panelEvent.SetActive(true);
        panelEvent.transform.SetAsLastSibling();

        if (PlayerMovement.Instance != null)
            PlayerMovement.Instance.SetEventLock(true);
        if (UIManager.Instance != null && DungeonUIManager.Instance == null)
            UIManager.Instance.SetHUDVisible(false);

        if (nameEvent != null) nameEvent.text = eventData.eventName;
        if (eventData.eventIcon != null && iconEvent != null) iconEvent.sprite = eventData.eventIcon;

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

            // 3. Hoãn việc trừ chỉ số — chỉ trừ khi người chơi bấm nút Tiếp Tục / Xác Nhận
            btnContinue.onClick.RemoveAllListeners();
            btnContinue.onClick.AddListener(() => ApplyTrapPenaltyAndClose(eventData));
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