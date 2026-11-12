using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LoadingScreen : MonoBehaviour
{
    [SerializeField] Image fill;
    [SerializeField] TMP_Text loadingText;
    [SerializeField] TMP_Text tipText;
    [SerializeField] float minDuration = 1.5f; // tải nhanh quá vẫn hiện tối thiểu chừng này giây
    [SerializeField] float fillSpeed = 0.8f;   // tốc độ thanh chạy mỗi giây
    [SerializeField] string fallbackScene = "Gameplay"; // dùng khi chạy thử scene Loading trực tiếp
    [SerializeField] string[] tips =
    {
        "Mẹo: Tiến hóa thú ở Hub để mở thêm kỹ năng.",
        "Mẹo: Mỗi vùng đất có loại quái và phần thưởng riêng.",
        "Mẹo: Hoàn thành nhiệm vụ để nhận đá tiến hóa.",
    };

    IEnumerator Start()
    {
        if (tipText && tips.Length > 0)
            tipText.text = tips[Random.Range(0, tips.Length)];

        string target = string.IsNullOrEmpty(SceneLoader.TargetScene) ? fallbackScene : SceneLoader.TargetScene;
        var op = SceneManager.LoadSceneAsync(target);
        op.allowSceneActivation = false;

        float shown = 0f, elapsed = 0f;
        while (true)
        {
            elapsed += Time.unscaledDeltaTime;
            // op.progress chỉ chạy tới 0.9 khi allowSceneActivation = false
            float real = Mathf.Clamp01(op.progress / 0.9f);
            float timeLimit = Mathf.Clamp01(elapsed / minDuration);
            float goal = Mathf.Min(real, timeLimit);

            shown = Mathf.MoveTowards(shown, goal, fillSpeed * Time.unscaledDeltaTime);
            fill.fillAmount = shown;
            loadingText.text = $"Đang tải... {Mathf.RoundToInt(shown * 100)}%";

            if (shown >= 1f) break;
            yield return null;
        }

        yield return new WaitForSecondsRealtime(0.2f);
        op.allowSceneActivation = true;
    }
}