using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class ScreenButton : MonoBehaviour // chuyển scene khi nhấn nút
{
    [SerializeField] ScreenId target;

    static string SceneOf(ScreenId id) => id switch
    {
        ScreenId.Hub => "MainMenu",
        ScreenId.Profile => "ProfileScene",
        ScreenId.Quest => "QuestScene",
        ScreenId.Achievement => "AchievementScene",
        ScreenId.PlayerManagement => "PlayerScene",
        ScreenId.Inventory => "InventoryScene",
        ScreenId.LoadingMap1 => "LoadingMap1Scene",
        _ => null // màn chưa có scene (Settings, Bestiary...)
    };

    void Awake() => GetComponent<Button>().onClick.AddListener(Go);

    void Go()
    {
        string scene = SceneOf(target);
        if (scene == null) { Debug.LogWarning($"Chưa có scene cho {target}"); return; }
        if (SceneManager.GetActiveScene().name == scene) return; // đang ở scene đó rồi
        SceneManager.LoadScene(scene);
    }
}