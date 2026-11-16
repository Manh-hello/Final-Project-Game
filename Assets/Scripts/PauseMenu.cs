using UnityEngine;

public class PauseMenu : MonoBehaviour
{
    [SerializeField] GameObject panel;

    public void Pause()
    {
        panel.SetActive(true);
        Time.timeScale = 0f;
    }

    public void Resume()
    {
        panel.SetActive(false);
        Time.timeScale = 1f;
    }

    public void QuitToHub()
    {
        Time.timeScale = 1f; // bắt buộc trả lại, nếu không scene sau bị đứng
        SceneLoader.Load("MainMenu");
    }
}