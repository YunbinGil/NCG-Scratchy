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

    [Header("UI References")]
    [SerializeField] private TMP_Text moneyText;
    [SerializeField] private TMP_Text levelText;
    [SerializeField] private Image levelImage;

    private const float ClickSpriteDuration = 0.12f;

    private enum CursorMode { Default, Scrubber, Coin }

    private CursorMode cursorMode;
    private float clickSpriteUntil;
    private Vector3 cursorBaseScale;
    private GameManager gameManager;
    private RectMask2D levelMask;
    private float experienceRatio;
    private float lastMaskWidth = -1f;

    private void Awake()
    {
        if (cursorRenderer != null)
            cursorBaseScale = cursorRenderer.transform.localScale;

        GameObject controllerObj = GameObject.FindWithTag("GameController");
        if (controllerObj != null)
        {
            gameManager = controllerObj.GetComponent<GameManager>();
        }

        if (targetCamera == null)
        {
            targetCamera = Camera.main;
        }

        if (levelImage != null)
        {
            levelMask = levelImage.GetComponentInParent<RectMask2D>();
            levelImage.enabled = false;
        }
    }

    private void Update()
    {
        if (levelMask != null && levelMask.rectTransform.rect.width != lastMaskWidth)
            UpdateLevelMask();
    }

    private void LateUpdate()
    {
        if (cursorRenderer == null) return;
        UpdateCursorAppearance();
        if (targetCamera == null) targetCamera = Camera.main;
        if (targetCamera == null) return;

        Vector3 screenPos = Input.mousePosition;
        screenPos.z = Mathf.Abs(targetCamera.transform.position.z);

        Vector3 worldPos = targetCamera.ScreenToWorldPoint(screenPos);
        Transform cursorTransform = cursorRenderer.transform;
        Vector3 currentPosition = cursorTransform.position;
        Bounds spriteBounds = cursorRenderer.bounds;
        Vector3 cursorAnchor = cursorMode == CursorMode.Default
            ? new Vector3(spriteBounds.min.x, spriteBounds.max.y, currentPosition.z)
            : spriteBounds.center;
        cursorAnchor.z = currentPosition.z;
        worldPos.z = currentPosition.z;
        cursorTransform.position = worldPos + (currentPosition - cursorAnchor);
    }

    private void UpdateCursorAppearance()
    {
        IInspectableSource target = InspectViewManager.Instance?.ActiveScratchTarget;
        CursorMode nextMode = target switch
        {
            Lottery => CursorMode.Coin,
            Trophy => CursorMode.Scrubber,
            _ => CursorMode.Default
        };

        if (nextMode != cursorMode)
        {
            cursorMode = nextMode;
            clickSpriteUntil = 0f;
        }

        if (cursorMode == CursorMode.Default && Input.GetMouseButtonDown(0))
            clickSpriteUntil = Time.unscaledTime + ClickSpriteDuration;

        float scale = cursorMode == CursorMode.Coin ? gameManager?.CoinSizeMultiplier ?? 1f : 1f;
        cursorRenderer.transform.localScale = cursorBaseScale * scale;
        RefreshCursorSprite();
    }

    private void RefreshCursorSprite()
    {
        Sprite sprite = cursorMode switch
        {
            CursorMode.Scrubber => Input.GetMouseButton(0) && useScrubberSprite != null
                ? useScrubberSprite : scrubberSprite,
            CursorMode.Coin => GetCoinSprite(),
            _ => Time.unscaledTime < clickSpriteUntil && useCursorSprite != null
                ? useCursorSprite : defaultCursorSprite
        };

        if (sprite == null)
            sprite = defaultCursorSprite;
        if (sprite != null && cursorRenderer.sprite != sprite)
            cursorRenderer.sprite = sprite;
    }

    private Sprite GetCoinSprite()
    {
        int coinLevel = gameManager?.Upgrades?.GetLevel(UpgradeType.Kind) ?? 0;
        return coinLevel switch
        {
            2 => titaniumCoinSprite != null ? titaniumCoinSprite : defaultCoinSprite,
            1 => aluminiumCoinSprite != null ? aluminiumCoinSprite : defaultCoinSprite,
            _ => defaultCoinSprite
        };
    }

    public void SetMoneyText(int amount)
    {
        if (moneyText != null)
        {
            moneyText.text = amount.ToString();
        }
    }

    public void SetLevelText(int exp, int goalExp)
    {
        if (levelText != null)
        {
            Localization.Bind(levelText, "status.experience", "{0} / {1}", exp, goalExp);
        }

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