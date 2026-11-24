using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class PausePanelController : MonoBehaviour
{
    [Header("세팅 패널 & 버튼")]
    [SerializeField] private GameObject settingPanel;
    [SerializeField] private Button settingToggleButton;
    [SerializeField] private Button continueButton;
    [SerializeField] private Button mainMenuButton;
    [SerializeField] private Button quitGameButton;


    [Header("열렸을 때 정지 여부")]
    [SerializeField] private bool pauseTimeWhenOpen = true;

    private void Awake()
    {
        if (settingToggleButton != null)
            settingToggleButton.onClick.AddListener(TogglePanel);

        if (continueButton != null)
            continueButton.onClick.AddListener(ContinueGame);

        if (mainMenuButton != null)
            mainMenuButton.onClick.AddListener(GoToMainMenu);

        if (quitGameButton != null)
            quitGameButton.onClick.AddListener(QuitGame);

        SetPanelActive(false);
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            TogglePanel();
        }
    }

    public void TogglePanel()
    {
        if (settingPanel == null)
        {
            return;
        }

        SetPanelActive(!settingPanel.activeSelf);
    }

    public void ContinueGame()
    {
        SetPanelActive(false);
    }

    public void GoToMainMenu()
    {
        Time.timeScale = 1f;
        SetPanelActive(false);
    }

    public void QuitGame()
    {
        Time.timeScale = 1f;

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private void SetPanelActive(bool isOpen)
    {
        if (settingPanel != null)
            settingPanel.SetActive(isOpen);

        if (pauseTimeWhenOpen)
        {
            Time.timeScale = isOpen ? 0f : 1f;
        }
    }
}