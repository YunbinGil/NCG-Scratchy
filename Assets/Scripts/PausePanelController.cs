using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class PausePanelController : MonoBehaviour
{
    [Header("세팅 패널 & 기본 버튼")]
    [SerializeField] private GameObject settingPanel;
    [SerializeField] private Button settingToggleButton;
    [SerializeField] private Button continueButton;
    [SerializeField] private Button mainMenuButton;
    [SerializeField] private Button quitGameButton;

    [Header("진행도 초기화 (3초 홀드 -> 최종 확인 팝업)")]
    [SerializeField] private Button holdResetButton;
    [SerializeField] private Image holdFillImage; // Fill Method: Horizontal, Fill Origin: Left
    [SerializeField] private float holdDuration = 3f;
    [SerializeField] private TMP_Text tapHintText; // 짧게 눌렀을 때 뜰 힌트 TMP
    [SerializeField] private GameObject finalConfirmGroup; // 확인 팝업 패널
    [SerializeField] private TMP_Text confirmPromptText; // 팝업 안 안내 문구 TMP
    [SerializeField] private Button finalConfirmResetButton;
    [SerializeField] private Button cancelConfirmButton;

    [Header("문구 설정")]
    [SerializeField] private string tapHintMessage = "버튼을 길게 누르세요";
    [SerializeField] private string confirmPromptMessage = "저장 파일이 삭제됩니다. 계속하시겠습니까?";

    [Header("씬 이름 설정")]
    [SerializeField] private string titleSceneName = "StartScenes";
    [SerializeField] private string gameSceneName = "Game";

    [Header("연동 대상")]
    [SerializeField] private PhoneCallTrigger phoneCallTrigger;

    [Header("열렸을 때 정지 여부")]
    [SerializeField] private bool pauseTimeWhenOpen = true;

    private bool isHoldingReset;
    private bool hasCompletedHold;
    private float holdTimer;

    private void Awake()
    {
        if (phoneCallTrigger == null)
            phoneCallTrigger = FindFirstObjectByType<PhoneCallTrigger>();

        if (settingToggleButton != null)
            settingToggleButton.onClick.AddListener(TogglePanel);

        if (continueButton != null)
            continueButton.onClick.AddListener(ContinueGame);

        if (mainMenuButton != null)
            mainMenuButton.onClick.AddListener(GoToTitleScene);

        if (quitGameButton != null)
            quitGameButton.onClick.AddListener(QuitGame);

        if (finalConfirmResetButton != null)
            finalConfirmResetButton.onClick.AddListener(ConfirmResetAndRestart);

        if (cancelConfirmButton != null)
            cancelConfirmButton.onClick.AddListener(CancelResetConfirm);

        ResetHoldState(clearMessage: true);
        SetPanelActive(false);
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            TogglePanel();
            return;
        }

        if (!isHoldingReset || hasCompletedHold)
            return;

        holdTimer += Time.unscaledDeltaTime;
        float progress = Mathf.Clamp01(holdTimer / Mathf.Max(0.01f, holdDuration));

        if (holdFillImage != null)
            holdFillImage.fillAmount = progress;

        if (progress >= 1f)
        {
            hasCompletedHold = true;
            isHoldingReset = false;

            if (tapHintText != null)
                tapHintText.text = string.Empty;

            if (confirmPromptText != null)
                confirmPromptText.text = confirmPromptMessage;

            if (finalConfirmGroup != null)
                finalConfirmGroup.SetActive(true);
        }
    }

    public void BeginResetHold()
    {
        if (holdResetButton != null && !holdResetButton.interactable)
            return;

        hasCompletedHold = false;
        isHoldingReset = true;
        holdTimer = 0f;

        if (holdFillImage != null)
            holdFillImage.fillAmount = 0f;

        if (finalConfirmGroup != null)
            finalConfirmGroup.SetActive(false);

        if (tapHintText != null)
            tapHintText.text = string.Empty;

        if (confirmPromptText != null)
            confirmPromptText.text = string.Empty;
    }

    public void ReleaseResetHold()
    {
        if (!isHoldingReset && !hasCompletedHold)
            return;

        if (!hasCompletedHold)
        {
            isHoldingReset = false;
            holdTimer = 0f;

            if (holdFillImage != null)
                holdFillImage.fillAmount = 0f;

            if (tapHintText != null)
                tapHintText.text = tapHintMessage;
        }
    }

    public void CancelResetConfirm()
    {
        ResetHoldState(clearMessage: true);
    }

    public void ConfirmResetAndRestart()
    {
        LocalJsonSaveSystem.DeleteSaveFile();
        ResetHoldState(clearMessage: true);

        Time.timeScale = 1f;
        if (phoneCallTrigger != null)
            phoneCallTrigger.SetPaused(false);

        PlayerPrefs.SetInt("ShouldLoadSavedData", 0);
        PlayerPrefs.Save();

        if (!string.IsNullOrEmpty(gameSceneName))
        {
            SceneManager.LoadScene(gameSceneName);
        }
    }

    private void ResetHoldState(bool clearMessage)
    {
        isHoldingReset = false;
        hasCompletedHold = false;
        holdTimer = 0f;

        if (holdFillImage != null)
            holdFillImage.fillAmount = 0f;

        if (finalConfirmGroup != null)
            finalConfirmGroup.SetActive(false);

        if (clearMessage)
        {
            if (tapHintText != null)
                tapHintText.text = string.Empty;

            if (confirmPromptText != null)
                confirmPromptText.text = string.Empty;
        }
    }

    public void TogglePanel()
    {
        if (settingPanel == null)
            return;

        SetPanelActive(!settingPanel.activeSelf);
    }

    public void ContinueGame()
    {
        SetPanelActive(false);
    }

    public void SaveGame()
    {
        LocalJsonSaveSystem saveSystem = LocalJsonSaveSystem.Instance != null
            ? LocalJsonSaveSystem.Instance
            : FindFirstObjectByType<LocalJsonSaveSystem>();

        if (saveSystem == null)
        {
            Debug.LogWarning("[PausePanelController] LocalJsonSaveSystem을 찾지 못해 저장할 수 없습니다.");
            return;
        }

        saveSystem.SaveToFile();
    }

    public void GoToTitleScene()
    {
        SaveGame();

        Time.timeScale = 1f;
        SetPanelActive(false);

        if (!string.IsNullOrEmpty(titleSceneName))
        {
            SceneManager.LoadScene(titleSceneName);
        }
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

        if (phoneCallTrigger != null)
            phoneCallTrigger.SetPaused(isOpen);

        if (!isOpen)
            ResetHoldState(clearMessage: true);

        if (pauseTimeWhenOpen)
        {
            Time.timeScale = isOpen ? 0f : 1f;
        }
    }
}