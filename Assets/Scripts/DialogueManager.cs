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

    public bool IsActive => isActive;

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
        }
        else
        {
            NextLine();
        }
    }

    public void StartDialog(string[] texts, Action onComplete = null)
    {
        if (texts == null || texts.Length == 0) return;

        lines = texts;
        index = 0;
        onDialogComplete = onComplete;
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
        typingCoroutine = StartCoroutine(TypeLine(lines[index]));
    }

    private IEnumerator TypeLine(string line)
    {
        dialogueText.text = string.Empty;
        foreach (char c in line)
        {
            dialogueText.text += c;
            yield return new WaitForSeconds(typingSpeed);
        }
        typingCoroutine = null;
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

    private void EndDialog()
    {
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