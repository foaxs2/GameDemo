using UnityEngine;
using TMPro;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance;

    public TextMeshProUGUI hpText;
    public TextMeshProUGUI goldText;
    public TextMeshProUGUI senText;
    public TextMeshProUGUI foodText;

    void Awake()
    {
        if (Instance == null)
        {   
            //Đưa nội dung vào DDOL để lưu dữ liệu
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
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