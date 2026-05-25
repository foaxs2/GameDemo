using UnityEngine;
public class GameBootstrap : MonoBehaviour {
    void Start() {
        if (SaveSystem.HasSave())
            SaveSystem.Instance?.Load();
        else
            SaveSystem.Instance?.InitializeNewGame();
    }
}