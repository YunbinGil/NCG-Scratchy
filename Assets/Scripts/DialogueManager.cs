using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class DialogueManager : MonoBehaviour, IPointerClickHandler
{
    public static DialogueManager Instance { get; private set; }

    [SerializeField] private GameObject dialoguePanel;
    [SerializeField] private TextMeshProUGUI dialogueText;
    [SerializeField] private float typingSpeed = 0.03f;

    private string[] lines;
    private int index;
    private Coroutine typingCoroutine;
    private bool isActive;
    private float ignoreInputUntil;
    private Action onDialogComplete;
    private bool useUnscaledTyping;
    private AudioSource phoneVoice;
    [SerializeField] private TMP_FontAsset endingDialogueFont;

    public bool IsActive => isActive;

    public bool OwnsDialogueGraphic(Graphic graphic)
    {
        return dialoguePanel != null && graphic.transform.IsChildOf(dialoguePanel.transform);
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        if (dialoguePanel != null)
            dialoguePanel.SetActive(false);
    }

    private void OnDisable()
    {
        StopPhoneVoice();
    }

    private void Update()
    {
        if (!isActive) return;
        if (Time.unscaledTime < ignoreInputUntil) return;

        if (Input.GetKeyDown(KeyCode.Space))
        {
            AdvanceOrCompleteLine();
        }
    }

    // UI 패널 영역 클릭 시 한 줄씩 진행 (EventSystem/Raycast Target 필요)
    public void OnPointerClick(PointerEventData eventData)
    {
        if (!isActive) return;
        if (Time.unscaledTime < ignoreInputUntil) return;

        AdvanceOrCompleteLine();
    }

    private void AdvanceOrCompleteLine()
    {
        if (typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine);
            typingCoroutine = null;
            dialogueText.text = lines[index];
            StopPhoneVoice();
        }
        else
        {
            NextLine();
        }
    }

    public void StartDialog(string[] texts, Action onComplete = null, bool isEnding = false, AudioSource voice = null)
    {
        if (texts == null || texts.Length == 0) return;

        StopPhoneVoice();
        phoneVoice = voice;
        lines = texts;
        index = 0;
        onDialogComplete = onComplete;
        useUnscaledTyping = isEnding;
        if (isEnding && endingDialogueFont != null)
            dialogueText.font = endingDialogueFont;
        isActive = true;
        ignoreInputUntil = Time.unscaledTime + 1f; // 전화기 클릭 직후 첫 줄 스킵 방지

        if (dialoguePanel != null)
            dialoguePanel.SetActive(true);

        ShowCurrentLine();
    }

    private void ShowCurrentLine()
    {
        if (typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine);
        }
        if (phoneVoice != null && phoneVoice.clip != null && !string.IsNullOrEmpty(lines[index]))
            phoneVoice.Play();
        typingCoroutine = StartCoroutine(TypeLine(lines[index]));
    }

    private IEnumerator TypeLine(string line)
    {
        dialogueText.text = string.Empty;
        foreach (char c in line)
        {
            dialogueText.text += c;
            yield return useUnscaledTyping ? (object)new WaitForSecondsRealtime(typingSpeed) : new WaitForSeconds(typingSpeed);
        }
        typingCoroutine = null;
        StopPhoneVoice();
    }

    private void StopPhoneVoice()
    {
        if (phoneVoice != null && phoneVoice.isPlaying)
            phoneVoice.Stop();
    }

    private void NextLine()
    {
        index++;
        if (index < lines.Length)
        {
            ShowCurrentLine();
        }
        else
        {
            EndDialog();
        }
    }

    public void DismissForEndingCall()
    {
        if (!isActive)
            return;

        if (typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine);
            typingCoroutine = null;
        }

        onDialogComplete = null;
        EndDialog();
    }

    private void EndDialog()
    {
        StopPhoneVoice();
        phoneVoice = null;
        isActive = false;
        lines = null;
        index = 0;

        if (dialogueText != null)
            dialogueText.text = string.Empty;

        if (dialoguePanel != null)
            dialoguePanel.SetActive(false);

        Action callback = onDialogComplete;
        onDialogComplete = null;
        callback?.Invoke();
    }
}