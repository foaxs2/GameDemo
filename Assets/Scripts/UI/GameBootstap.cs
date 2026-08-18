using UnityEngine;
public class GameBootstrap : MonoBehaviour {
    void Start() {
        string scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        // Chỉ chạy khởi tạo ở các Scene mở đầu game thật, không chạy ở Dungeon/Combat/Test
        if (scene != "Start" && scene != "Town") return;
        
        // Phòng hờ rò rỉ dữ liệu tĩnh nếu tính năng Domain Reload bị tắt trong Unity
        CombatManager.isTestMode = false;
        CombatManager.overrideEnemyPrefabs = null;
        CombatManager.autoWinCombat = false;
        CombatManager.returnSceneName = "Dungeon";

        if (SaveSystem.HasSave())
            SaveSystem.Instance?.Load();
        else
            SaveSystem.Instance?.InitializeNewGame();
    }
}