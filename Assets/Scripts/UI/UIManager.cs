using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance;

    public TextMeshProUGUI hpText;
    public TextMeshProUGUI goldText;
    public TextMeshProUGUI senText;
    public TextMeshProUGUI foodText;

    private Canvas myCanvas;

    void Awake()
    {
        if (Instance == null)
        {   
            //Đưa nội dung vào DDOL để lưu dữ liệu
            Instance = this;
            DontDestroyOnLoad(gameObject);
            
            myCanvas = GetComponent<Canvas>();
            SceneManager.sceneLoaded += OnSceneLoaded;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (myCanvas != null)
        {
            // Tự động tắt bảng HUD khi đang ở màn Combat hoặc Town, hiện lại ở Dungeon
            myCanvas.enabled = (scene.name != "Combat" && scene.name != "Town");
        }
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void Update()
    {
        if (PlayerManager.Instance != null)
        {
            if (hpText != null)
                hpText.text = "HP: " + PlayerManager.Instance.currentHP + "/" + PlayerManager.Instance.maxHP;

            if (goldText != null)
                goldText.text = "Vàng: " + PlayerManager.Instance.gold;

            if (senText != null)
                senText.text = "Sen: " + PlayerManager.Instance.sen + "/" + PlayerManager.Instance.maxSen;

            if (foodText != null)
                foodText.text = "Lương Thực: " + PlayerManager.Instance.food + "/" + PlayerManager.Instance.maxFood;
        }
    }
}