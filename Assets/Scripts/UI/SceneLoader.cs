using UnityEngine.SceneManagement;

public static class SceneLoader
{
    public static string TargetScene { get; private set; }

    // Dùng màn load mặc định
    public static void Load(string sceneName) => Load(sceneName, "LoadingScene");

    // Dùng màn load riêng cho từng map
    public static void Load(string sceneName, string loadingScene)
    {
        TargetScene = sceneName;
        SceneManager.LoadScene(loadingScene);
    }
}