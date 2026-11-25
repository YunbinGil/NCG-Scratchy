using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class UiManager : MonoBehaviour
{
    [Header("Cursor Settings")]
    [SerializeField] private Camera targetCamera;
    [SerializeField] private SpriteRenderer cursorRenderer;
    [SerializeField] private Sprite defaultCursorSprite;
    [SerializeField] private Sprite scrubberSprite;
    [SerializeField] private Sprite scrubCoinSprite;
    [SerializeField] private TMP_Text moneyText;
    [SerializeField] private TMP_Text levelText;
    [SerializeField] private Image levelImage;
    // [SerializeField] private Ticket[] tickets;
    // [SerializeField] private Equipment[] equips;
    private GameManager gameManager;
    private RectMask2D levelMask;
    private float experienceRatio;
    private float lastMaskWidth = -1f;

    private void Awake()
    {
        gameManager = GameObject.FindWithTag("GameController").GetComponent<GameManager>();
        if (targetCamera == null) targetCamera = Camera.main;
        SetDefaultCursor();

        if (levelImage != null)
        {
            levelMask = levelImage.GetComponentInParent<RectMask2D>();
            levelImage.enabled = false;
        }
    }

    private void Update()
    {
        FollowMouse();

        if (levelMask != null && levelMask.rectTransform.rect.width != lastMaskWidth)
            UpdateLevelMask();
    }

    private void FollowMouse()
    {
        if (cursorRenderer == null) return;
        if (targetCamera == null) targetCamera = Camera.main;
        if (targetCamera == null) return;

        Vector3 screenPos = Input.mousePosition;
        screenPos.z = Mathf.Abs(targetCamera.transform.position.z);

        Vector3 worldPos = targetCamera.ScreenToWorldPoint(screenPos);
        worldPos.z = cursorRenderer.transform.position.z;
        cursorRenderer.transform.position = worldPos;
    }

    public void SetCursorSprite(Sprite newSprite)
    {
        if (cursorRenderer != null && newSprite != null)
        {
            cursorRenderer.sprite = newSprite;
        }
    }

    public void SetDefaultCursor()
    {
        SetCursorSprite(defaultCursorSprite);
    }

    public void SetScrubberCursor()
    {
        SetCursorSprite(scrubberSprite);
    }
    public void SetScrubCoinCursor()
    {
        SetCursorSprite(scrubCoinSprite);
    }
    public void SetMoneyText(int amount)
    {
        if (moneyText != null)
            moneyText.text = amount.ToString();
    }
    public void SetLevelText(int exp, int goalExp)
    {
        if (levelText != null)
            Localization.Bind(levelText, "status.experience", "{0} / {1}", exp, goalExp);

        experienceRatio = goalExp > 0 ? Mathf.Clamp01((float)exp / goalExp) : 0f;
        UpdateLevelMask();
    }

    private void UpdateLevelMask()
    {
        if (levelMask == null || levelImage == null)
            return;

        lastMaskWidth = levelMask.rectTransform.rect.width;
        Vector4 padding = levelMask.padding;
        padding.z = lastMaskWidth * (1f - experienceRatio);
        levelMask.padding = padding;
        levelImage.enabled = experienceRatio > 0f;
    }
}