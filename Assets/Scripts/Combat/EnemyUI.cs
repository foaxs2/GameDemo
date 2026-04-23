using UnityEngine;
using UnityEngine.UI;

public class EnemyUI : MonoBehaviour
{
    public EnemyStats stats; // Quái vật mà Slot này đại diện
    public Image icon;
    public Slider hpBar;
    public Slider apBar;

    public void Setup(EnemyStats enemyData)
    {
        stats = enemyData;
        icon.sprite = enemyData.enemySprite;
        hpBar.maxValue = enemyData.maxHP;
        apBar.maxValue = 100f; // Max AP
    }

    void Update()
    {
        if (stats == null) return;

        hpBar.value = stats.currentHP;
        apBar.value = stats.currentAP;

        // Nếu quái chết, tự động làm mờ ảnh
        if (stats.currentHP <= 0)
        {
            icon.color = new Color(0.3f, 0.3f, 0.3f, 1f); // Đổi sang màu xám
        }
    }
}