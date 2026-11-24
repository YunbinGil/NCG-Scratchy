using TMPro;
using UnityEngine;
using UnityEngine.UI;

public enum ViewState
{
    Table,   // 기본 테이블뷰
    Inspect  // 오브젝트 확대 + 우측 상세 패널 표시 상태
}

public interface IInspectableSource
{
    Sprite GetInspectSprite();
    float GetInspectScale();
    void OpenInDetailPanel(InspectDetailPanel detailPanel);
    void DestoryItem();
    void HandleClick();
    public int Reward { get; }
    public bool IsCleaned { get; }
}

public class InspectViewManager : MonoBehaviour
{
    public static InspectViewManager Instance { get; private set; }

    [Header("Inspect 상태에서만 켜지는 루트")]
    [SerializeField] private GameObject inspectOverlayRoot;

    // [주석 처리됨] 기존 Background 버튼 및 영역 제한 필드
    // [SerializeField] private Button dimmedBackgroundButton;
    // [SerializeField] private RectTransform tableArea;

    [Header("배경 및 중앙 확대 이미지")]
    [SerializeField] private Button closeDimmedBackgroundButton;
    [SerializeField] private RectTransform centerPreviewRect;
    [SerializeField] private Image centerPreviewImage;
    [SerializeField] private SpriteRenderer centerPreviewRenderer;
    [SerializeField] private float basePreviewScale = 1f;

    [Header("우측 상세 패널")]
    [SerializeField] private InspectDetailPanel detailPanel;
    [SerializeField] private Button resultButton;
    [Header("얼룩 오브젝트 생성 설정")]
    [SerializeField] private GameObject stainOverlayPrefab; // SpriteRenderer + Scratchable이 붙은 프리팹
    [SerializeField] private Transform stainOverlayParent; // 생성할 부모(없으면 null)

    public ViewState CurrentState { get; private set; } = ViewState.Table;
    public bool IsInspecting => CurrentState == ViewState.Inspect;
    private UiManager uiManager;
    private GameManager gameManager;
    private Dish currentInspectDish;
    private GameObject currentActiveOverlay;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        // [주석 처리됨] 기존 Background 버튼 리스너 연결
        // if (dimmedBackgroundButton != null)
        // {
        //     dimmedBackgroundButton.onClick.RemoveListener(HandleBackgroundClick);
        //     dimmedBackgroundButton.onClick.AddListener(HandleBackgroundClick);
        // }

        if (closeDimmedBackgroundButton != null)
        {
            closeDimmedBackgroundButton.onClick.RemoveListener(HandleDimmedBackgroundClick);
            closeDimmedBackgroundButton.onClick.AddListener(HandleDimmedBackgroundClick);
        }
        SetState(ViewState.Table);
    }

    // [주석 처리됨] 기존 Background 버튼 클릭 핸들러
    // private void HandleBackgroundClick()
    // {
    //     Vector2 screenPos = Input.mousePosition;
    //     Camera eventCam = GetEventCamera();
    //
    //     if (tableArea != null && RectTransformUtility.RectangleContainsScreenPoint(tableArea, screenPos, eventCam))
    //     {
    //         return;
    //     }
    //
    //     CloseInspect();
    // }
    private void Start()
    {
        uiManager = GameObject.FindWithTag("GameController").GetComponent<UiManager>();
        gameManager = GameObject.FindWithTag("GameController").GetComponent<GameManager>();
    }
    private void HandleDimmedBackgroundClick()
    {
        RectTransform previewArea = GetCenterPreviewRectTransform();
        if (previewArea != null && previewArea.gameObject.activeInHierarchy)
        {
            Vector2 screenPos = Input.mousePosition;
            Camera eventCam = GetEventCamera(previewArea);

            // CenterPreviewImage 영역 안을 클릭한 경우 닫지 않음
            if (RectTransformUtility.RectangleContainsScreenPoint(previewArea, screenPos, eventCam))
            {
                return;
            }
        }

        CloseInspect();
    }

    private RectTransform GetCenterPreviewRectTransform()
    {
        if (centerPreviewRect != null)
            return centerPreviewRect;

        if (centerPreviewImage != null)
            return centerPreviewImage.rectTransform;

        return null;
    }

    private Camera GetEventCamera(RectTransform targetRect = null)
    {
        Canvas canvas = null;

        if (targetRect != null)
            canvas = targetRect.GetComponentInParent<Canvas>();

        if (canvas == null && closeDimmedBackgroundButton != null)
            canvas = closeDimmedBackgroundButton.GetComponentInParent<Canvas>();

        if (canvas == null)
            canvas = GetComponentInParent<Canvas>();

        if (canvas == null || canvas.renderMode == RenderMode.ScreenSpaceOverlay)
            return null;

        return canvas.worldCamera != null ? canvas.worldCamera : Camera.main;
    }

    public void OpenInspect(IInspectableSource target)
    {
        if (target == null) return;

        Sprite sprite = target.GetInspectSprite();
        float scale = Mathf.Max(0.1f, target.GetInspectScale());

        if (centerPreviewImage != null)
        {
            centerPreviewImage.sprite = sprite;
            centerPreviewImage.enabled = sprite != null;
            centerPreviewImage.rectTransform.localScale = Vector3.one * (basePreviewScale * scale);
        }

        if (centerPreviewRenderer != null)
        {
            centerPreviewRenderer.sprite = sprite;
            centerPreviewRenderer.enabled = sprite != null;
            centerPreviewRenderer.transform.localScale = Vector3.one * (basePreviewScale * scale);
        }

        if (detailPanel != null)
        {
            detailPanel.Show();
            target.OpenInDetailPanel(detailPanel);
        }

        if (currentActiveOverlay != null)
        {
            currentActiveOverlay.SetActive(false);
            currentActiveOverlay = null;
        }

        currentInspectDish = target as Dish;
        if (currentInspectDish != null)
        {
            currentActiveOverlay = GetOrCreateOverlayForDish(currentInspectDish);
            if (currentActiveOverlay != null)
            {
                currentActiveOverlay.SetActive(true);
            }
        }
        if (target.IsCleaned)
        {
            ShowResultButton(target);
        }
        else
        {
            resultButton.gameObject.SetActive(false);
        }
        SetState(ViewState.Inspect);
    }

    public void CloseInspect()
    {
        if (detailPanel != null)
        {
            detailPanel.Hide();
        }

        if (centerPreviewImage != null)
        {
            centerPreviewImage.sprite = null;
            centerPreviewImage.enabled = false;
        }

        if (centerPreviewRenderer != null)
        {
            centerPreviewRenderer.sprite = null;
            centerPreviewRenderer.enabled = false;
        }
        if (currentActiveOverlay != null)
        {
            currentActiveOverlay.SetActive(false);
            currentActiveOverlay = null;
        }
        currentInspectDish = null;

        SetState(ViewState.Table);
        uiManager.SetDefaultCursor();
    }

    public void SetState(ViewState nextState)
    {
        CurrentState = nextState;
        bool isInspect = nextState == ViewState.Inspect;

        if (inspectOverlayRoot != null)
        {
            inspectOverlayRoot.SetActive(isInspect);
        }
    }

    private GameObject GetOrCreateOverlayForDish(Dish dish)
    {
        if (dish == null) return null;

        GameObject overlay = dish.GetStainOverlay();
        if (overlay == null && stainOverlayPrefab != null)
        {
            overlay = Instantiate(stainOverlayPrefab, stainOverlayParent);
            overlay.name = $"{dish.gameObject.name}_StainOverlay";
            overlay.GetComponent<Scratchable>().Dish = dish;

            SpriteRenderer sr = overlay.GetComponentInChildren<SpriteRenderer>();
            if (sr != null)
            {
                sr.sprite = dish.GetRustedSprite();
            }

            dish.SetStainOverlay(overlay);
        }

        return overlay;
    }

    public void OnDishCleaned(Dish dish)
    {
        if (dish == null) return;

        Sprite resultSprite = dish.Reward > 0 ? dish.GetInspectSprite() : dish.GetBrokenSprite();
        if (centerPreviewImage != null)
        {
            centerPreviewImage.sprite = resultSprite;
            centerPreviewImage.enabled = resultSprite != null;
        }
        if (centerPreviewRenderer != null)
        {
            centerPreviewRenderer.sprite = resultSprite;
            centerPreviewRenderer.enabled = resultSprite != null;
        }
        PhoneEventController.Instance?.TriggerBrokenOrLossCall();
        ShowResultButton(dish);        
    }
    private void ShowResultButton(IInspectableSource target)
    {
        resultButton.onClick.RemoveAllListeners();
        resultButton.onClick.AddListener(() =>
        {
            gameManager.AddMoney(target.Reward);
            gameManager.AddExp(target.Reward);
            CloseInspect();
            target.DestoryItem();
        });
        resultButton.GetComponentInChildren<TMP_Text>().text = $"${target.Reward}";
        resultButton.gameObject.SetActive(true);
    }
}