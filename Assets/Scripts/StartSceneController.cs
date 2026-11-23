using UnityEngine;
using UnityEngine.SceneManagement;

public class StartSceneController : MonoBehaviour
{
    [SerializeField] private string tutorialSceneName = "Tutorial";

    public void LoadTutorialScene()
    {
        SceneManager.LoadScene(tutorialSceneName);
    }

    public void LoadOption()
    {
        
    }

    public void QuitGame()
    {
        Application.Quit();

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
}