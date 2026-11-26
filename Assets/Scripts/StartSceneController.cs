using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class StartSceneController : MonoBehaviour
{
    private const string LoadFlagKey = "ShouldLoadSavedData";

    [SerializeField] private string gameSceneName = "Game";
    [SerializeField] private Button loadGameButton;

    private void Start()
    {
        if (loadGameButton != null)
        {
            loadGameButton.interactable = LocalJsonSaveSystem.HasSaveFile();
        }
    }

    public void NewGameScene()
    {
        PlayerPrefs.SetInt(LoadFlagKey, 0);
        PlayerPrefs.Save();
        PausePanelController.ClearUpgradeSnapshotForNewGame();

        if (!string.IsNullOrEmpty(gameSceneName))
        {
            SceneManager.LoadScene(gameSceneName);
        }
    }

    public void LoadGameScene()
    {
        if (!LocalJsonSaveSystem.HasSaveFile())
        {
            Debug.Log("불러올 저장 데이터가 없습니다.");
            return;
        }

        PlayerPrefs.SetInt(LoadFlagKey, 1);
        PlayerPrefs.Save();

        if (!string.IsNullOrEmpty(gameSceneName))
        {
            SceneManager.LoadScene(gameSceneName);
        }
    }

    public void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}