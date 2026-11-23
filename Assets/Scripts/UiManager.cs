using UnityEngine;

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
    [SerializeField] private GameManager gameManager;

    private void Awake()
    {
        gameManager = GameObject.FindWithTag("GameController").GetComponent<GameManager>();
        if (targetCamera == null) targetCamera = Camera.main;
        SetDefaultCursor();
    }

    private void Update()
    {
        FollowMouse();
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
    public void SetMoneyText(int amount){
        moneyText.text = amount.ToString();
    }
    public void SetLevelText(int goalExp){
        levelText.text = $"{gameManager.Exp} / {goalExp}";
    }
}