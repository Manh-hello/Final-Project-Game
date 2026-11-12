using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class LoadSceneButton : MonoBehaviour
{
    [SerializeField] string sceneName = "GamePlayMap1";          // scene game thật
    [SerializeField] string loadingScene = "LoadingMap1Scene";   // màn load hiển thị trước đó

    void Awake() => GetComponent<Button>().onClick.AddListener(
        () => SceneLoader.Load(sceneName, loadingScene));
}