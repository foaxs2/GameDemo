using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance;

    [Header("HUD Stats")]
    public TextMeshProUGUI hpText;
    public TextMeshProUGUI goldText;
    public TextMeshProUGUI senText;
    public TextMeshProUGUI foodText;

    [Header("EXP & Level (Tuỳ chọn)")]
    public TextMeshProUGUI levelText;   // Hiện "Cấp: X"
    public TextMeshProUGUI expText;     // Hiện "EXP: X / Y"

    [Header("EXP Slider & Điểm Kỹ Năng Mới")]
    public UnityEngine.UI.Slider expSlider;        // Slider trượt EXP màu trắng xám
    public TextMeshProUGUI skillPointsText;       // Hiển thị số Điểm kỹ năng đang có

    private Canvas myCanvas;

    // Các biến lưu trữ tiền tố lấy từ Editor
    private string hpPrefix = "HP: ";
    private string goldPrefix = "Vàng: ";
    private string senPrefix = "Sen: ";
    private string foodPrefix = "Lương Thực: ";
    private string levelPrefix = "Cấp: ";
    private string expPrefix = "EXP: ";

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            myCanvas = GetComponent<Canvas>();
            SceneManager.sceneLoaded += OnSceneLoaded;
        }
        else if (Instance != this)
        {
            // Huỷ toàn bộ GameObject trùng lặp (kèm theo Canvas phụ) để tránh lỗi chồng chéo UI
            Destroy(gameObject);
        }
    }

    void Start()
    {
        ExtractPrefixes();
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (myCanvas != null)
            myCanvas.enabled = (scene.name != "Combat" && scene.name != "Town");
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void ExtractPrefixes()
    {
        hpPrefix = GetTextPrefix(hpText, "HP: ");
        goldPrefix = GetTextPrefix(goldText, "Vàng: ");
        senPrefix = GetTextPrefix(senText, "Sen: ");
        foodPrefix = GetTextPrefix(foodText, "Lương Thực: ");
        levelPrefix = GetTextPrefix(levelText, "Cấp: ");
        expPrefix = GetTextPrefix(expText, "EXP: ");
    }

    private string GetTextPrefix(TextMeshProUGUI tmpText, string defaultPrefix)
    {
        if (tmpText == null || string.IsNullOrEmpty(tmpText.text)) return defaultPrefix;

        // Tìm vị trí chữ số đầu tiên trong chuỗi và cắt chuỗi lấy phần tiền tố phía trước
        System.Text.RegularExpressions.Match match = System.Text.RegularExpressions.Regex.Match(tmpText.text, @"\d");
        if (match.Success)
        {
            string prefix = tmpText.text.Substring(0, match.Index);
            // Loại bỏ các ký tự xuống dòng (nếu có) để ép text hiển thị trên cùng 1 hàng ngang
            prefix = prefix.Replace("\r", "").Replace("\n", "");
            return prefix;
        }

        return tmpText.text.Replace("\r", "").Replace("\n", "");
    }

    void Update()
    {
        if (PlayerManager.Instance == null) return;
        var p = PlayerManager.Instance;

        if (hpText != null)
            hpText.text = hpPrefix + p.currentHP + "/" + p.maxHP;

        if (goldText != null)
            goldText.text = goldPrefix + p.gold;

        if (senText != null)
            senText.text = senPrefix + p.sen + "/" + p.maxSen;

        if (foodText != null)
            foodText.text = foodPrefix + p.food + "/" + p.maxFood;

        if (levelText != null)
            levelText.text = levelPrefix + p.level;

        if (expText != null)
            expText.text = expPrefix + p.currentExp + " / " + p.expToNextLevel;

        if (expSlider != null)
        {
            expSlider.maxValue = p.expToNextLevel;
            expSlider.value = p.currentExp;
        }

        if (skillPointsText != null)
        {
            skillPointsText.text = p.unspentStatPoints.ToString();
        }
    }

    /// <summary>Gọi từ EventManager để ẩn/hiện HUD khi panel sự kiện đang mở.</summary>
    public void SetHUDVisible(bool visible)
    {
        if (myCanvas != null)
            myCanvas.enabled = visible;
    }
}