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
            if (gameObject.GetComponent<Canvas>() != null || gameObject.GetComponent<Camera>() != null)
            {
                Destroy(this);
            }
            else
            {
                Destroy(gameObject);
            }
        }
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

    void Update()
    {
        if (PlayerManager.Instance == null) return;
        var p = PlayerManager.Instance;

        if (hpText != null)
            hpText.text = "HP: " + p.currentHP + "/" + p.maxHP;

        if (goldText != null)
            goldText.text = "Vàng: " + p.gold;

        if (senText != null)
            senText.text = "Sen: " + p.sen + "/" + p.maxSen;

        if (foodText != null)
            foodText.text = "Lương Thực: " + p.food + "/" + p.maxFood;

        if (levelText != null)
            levelText.text = "Cấp: " + p.level;

        if (expText != null)
            expText.text = "EXP: " + p.currentExp + " / " + p.expToNextLevel;

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