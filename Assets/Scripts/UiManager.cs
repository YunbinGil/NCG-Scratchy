using UnityEngine;
using UnityEngine.Serialization;
using TMPro;
using UnityEngine.UI;

public class UiManager : MonoBehaviour
{
    [Header("Cursor Settings")]
    [SerializeField] private Camera targetCamera;
    [SerializeField] private SpriteRenderer cursorRenderer;
    [SerializeField] private Sprite defaultCursorSprite;
    [SerializeField] private Sprite useCursorSprite;
    [SerializeField] private Sprite scrubberSprite;
    [SerializeField] private Sprite useScrubberSprite;
    [FormerlySerializedAs("scrubCoinSprite")]
    [SerializeField] private Sprite defaultCoinSprite;
    [SerializeField] private Sprite aluminiumCoinSprite;
    [SerializeField] private Sprite titaniumCoinSprite;
    [SerializeField] private TMP_Text moneyText;
    [SerializeField] private TMP_Text levelText;
    [SerializeField] private Image levelImage;
    // [SerializeField] private Ticket[] tickets;
    // [SerializeField] private Equipment[] equips;
    private const float ClickSpriteDuration = 0.12f;

    private enum CursorMode { Default, Scrubber, Coin }

    private CursorMode cursorMode;
    private float clickSpriteUntil;
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
        UpdateCursorSprite();

        if (levelMask != null && levelMask.rectTransform.rect.width != lastMaskWidth)
            UpdateLevelMask();
    }

    private void LateUpdate()
    {
        if (cursorRenderer == null) return;
        if (targetCamera == null) targetCamera = Camera.main;
        if (targetCamera == null) return;

        Vector3 screenPos = Input.mousePosition;
        screenPos.z = Mathf.Abs(targetCamera.transform.position.z);

        Vector3 worldPos = targetCamera.ScreenToWorldPoint(screenPos);
        Transform cursorTransform = cursorRenderer.transform;
        Vector3 currentPosition = cursorTransform.position;
        Bounds spriteBounds = cursorRenderer.bounds;
        Vector3 topLeft = new Vector3(spriteBounds.min.x, spriteBounds.max.y, currentPosition.z);
        worldPos.z = currentPosition.z;
        cursorTransform.position = worldPos + (currentPosition - topLeft);
    }

    public void SetCursorSprite(Sprite newSprite)
    {
        if (cursorRenderer != null && newSprite != null && cursorRenderer.sprite != newSprite)
        {
            cursorRenderer.sprite = newSprite;
        }
    }

    private void UpdateCursorSprite()
    {
        if (cursorMode == CursorMode.Default && Input.GetMouseButtonDown(0))
            clickSpriteUntil = Time.unscaledTime + ClickSpriteDuration;

        RefreshCursorSprite();
    }

    private void RefreshCursorSprite()
    {
        Sprite sprite = cursorMode switch
        {
            CursorMode.Scrubber => Input.GetMouseButton(0) && useScrubberSprite != null
                ? useScrubberSprite : scrubberSprite,
            CursorMode.Coin => defaultCoinSprite,
            _ => Time.unscaledTime < clickSpriteUntil && useCursorSprite != null
                ? useCursorSprite : defaultCursorSprite
        };
        SetCursorSprite(sprite != null ? sprite : defaultCursorSprite);
    }

    public void SetDefaultCursor()
    {
        SetCursorMode(CursorMode.Default);
    }

    public void SetScrubberCursor()
    {
        SetCursorMode(CursorMode.Scrubber);
    }

    public void SetScrubCoinCursor()
    {
        SetCursorMode(CursorMode.Coin);
    }

    private void SetCursorMode(CursorMode mode)
    {
        cursorMode = mode;
        clickSpriteUntil = 0f;
        RefreshCursorSprite();
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