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

    private string[] _lines;
    private int _index;
    private Coroutine _typingCoroutine;
    private bool _isActive;
    private float _ignoreInputUntil;
    private Action _onDialogComplete;

    public bool IsActive => _isActive;

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
        if (!_isActive) return;
        if (Time.unscaledTime < _ignoreInputUntil) return;

        if (Input.GetKeyDown(KeyCode.Space))
        {
            AdvanceOrCompleteLine();
        }
    }

    // UI 패널 영역 클릭 시 한 줄씩 진행 (EventSystem/Raycast Target 필요)
    public void OnPointerClick(PointerEventData eventData)
    {
        if (!_isActive) return;
        if (Time.unscaledTime < _ignoreInputUntil) return;

        AdvanceOrCompleteLine();
    }

    private void AdvanceOrCompleteLine()
    {
        if (_typingCoroutine != null)
        {
            StopCoroutine(_typingCoroutine);
            _typingCoroutine = null;
            dialogueText.text = _lines[_index];
        }
        else
        {
            NextLine();
        }
    }

    public void StartDialog(string[] texts, Action onComplete = null)
    {
        if (texts == null || texts.Length == 0) return;

        _lines = texts;
        _index = 0;
        _onDialogComplete = onComplete;
        _isActive = true;
        _ignoreInputUntil = Time.unscaledTime + 1f; // 전화기 클릭 직후 첫 줄 스킵 방지

        if (dialoguePanel != null)
            dialoguePanel.SetActive(true);

        ShowCurrentLine();
    }

    private void ShowCurrentLine()
    {
        if (_typingCoroutine != null)
        {
            StopCoroutine(_typingCoroutine);
        }
        _typingCoroutine = StartCoroutine(TypeLine(_lines[_index]));
    }

    private IEnumerator TypeLine(string line)
    {
        dialogueText.text = string.Empty;
        foreach (char c in line)
        {
            dialogueText.text += c;
            yield return new WaitForSeconds(typingSpeed);
        }
        _typingCoroutine = null;
    }

    private void NextLine()
    {
        _index++;
        if (_index < _lines.Length)
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
        _isActive = false;
        _lines = null;
        _index = 0;

        if (dialogueText != null)
            dialogueText.text = string.Empty;

        if (dialoguePanel != null)
            dialoguePanel.SetActive(false);

        Action callback = _onDialogComplete;
        _onDialogComplete = null;
        callback?.Invoke();
    }
}