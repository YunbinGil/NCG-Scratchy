using System.Collections.Generic;
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
    void CompleteScratch();
    public int Reward { get; }
    public bool IsCleaned { get; }
}

public class InspectViewManager : MonoBehaviour
{
    public static InspectViewManager Instance { get; private set; }

    [Header("Inspect 상태에서만 켜지는 루트")]
    [SerializeField] private GameObject inspectOverlayRoot;

    [Header("배경 및 중앙 확대 이미지")]
    [SerializeField] private Button closeDimmedBackgroundButton;
    [SerializeField] private GameObject centerPreview;
    private RectTransform centerPreviewRect;
    private Image centerPreviewImage;
    private SpriteRenderer centerPreviewRenderer;
    [SerializeField] private float basePreviewScale = 1f;

    [Header("우측 상세 패널")]
    [SerializeField] private InspectDetailPanel detailPanel;
    [SerializeField] private Button resultButton;

    [Header("얼룩 오브젝트 생성 설정")]
    [SerializeField] private GameObject stainOverlayPrefab; // SpriteRenderer + Scratchable이 붙은 프리팹
    [SerializeField] private Transform stainOverlayParent; // 생성할 부모(없으면 null)

    public ViewState CurrentState { get; private set; } = ViewState.Table;
    public bool IsInspecting => CurrentState == ViewState.Inspect;

    internal IInspectableSource ActiveScratchTarget =>
        isActiveAndEnabled && IsInspecting && currentActiveOverlay != null && currentActiveOverlay.activeInHierarchy &&
        currentTarget != null && !currentTarget.IsCleaned
            ? currentTarget
            : null;

    private GameManager gameManager;
    private IInspectableSource currentTarget;
    private GameObject currentActiveOverlay;
    private List<GameObject> LotteryPreviews = new List<GameObject>(); // 실제 생성된 GO들 넣고 icons랑 stainoverlay만 바꿀
    private LotteryPreviewItem currentLotteryPreview;

    private void Awake()
    {
        centerPreviewRect = EnsureCenterPreviewRect();

        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        if (closeDimmedBackgroundButton != null)
        {
            closeDimmedBackgroundButton.onClick.RemoveListener(HandleDimmedBackgroundClick);
            closeDimmedBackgroundButton.onClick.AddListener(HandleDimmedBackgroundClick);
        }
        SetState(ViewState.Table);
    }

    private void Start()
    {
        GameObject controllerObj = GameObject.FindWithTag("GameController");
        if (controllerObj != null)
        {
            gameManager = controllerObj.GetComponent<GameManager>();
        }
    }

    private RectTransform EnsureCenterPreviewRect()
    {
        if (centerPreview != null)
        {
            if (centerPreviewImage == null)
                centerPreviewImage = centerPreview.GetComponent<Image>();

            if (centerPreviewRenderer == null)
                centerPreviewRenderer = centerPreview.GetComponent<SpriteRenderer>();

            if (centerPreviewRect == null)
                centerPreviewRect = centerPreview.GetComponent<RectTransform>();
        }

        if (centerPreviewRect == null && centerPreviewImage != null)
        {
            centerPreviewRect = centerPreviewImage.rectTransform;
        }

        return centerPreviewRect;
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

        if (currentActiveOverlay != null)
        {
            currentActiveOverlay.SetActive(false);
            currentActiveOverlay = null;
        }

        currentTarget = target;

        if (target is Trophy trophy)
        {
            OpenTrophyView(trophy);
        }
        else if (target is Lottery lottery)
        {
            OpenLotteryView(lottery);
        }

        if (currentActiveOverlay != null)
        {
            // Debug.Log($"{currentActiveOverlay.name}: {currentActiveOverlay.activeSelf}");
            currentActiveOverlay.SetActive(true);
        }

        if (detailPanel != null)
        {
            detailPanel.Show();
            target.OpenInDetailPanel(detailPanel);
        }

        if (resultButton != null)
        {
            if (target.IsCleaned)
            {
                ShowResultButton(target);
            }
            else
            {
                resultButton.gameObject.SetActive(false);
            }
        }

        SetState(ViewState.Inspect);
    }

    public void OpenTrophyView(Trophy trophy)
    {
        Sprite sprite = trophy.GetInspectSprite();
        float scale = Mathf.Max(0.1f, trophy.GetInspectScale());

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

        currentActiveOverlay = GetOrCreateOverlayForTrophy(trophy);
    }

    public void OpenLotteryView(Lottery lottery)
    {
        if (lottery == null || lottery.Data == null || lottery.Data.lotteryPrefab == null)
        {
            Debug.LogWarning("OpenLotteryView: lottery 또는 lotteryPrefab이 null입니다.");
            return;
        }

        GameObject lotteryPrefab = lottery.Data.lotteryPrefab;
        GameObject targetInstance = null;

        foreach (var previewObj in LotteryPreviews)
        {
            if (previewObj != null && previewObj.name.StartsWith(lotteryPrefab.name))
            {
                targetInstance = previewObj;
                break;
            }
        }

        if (targetInstance == null)
        {
            targetInstance = Instantiate(lotteryPrefab, centerPreviewRect, false);
            LotteryPreviewItem previewItem = targetInstance.GetComponent<LotteryPreviewItem>();
            if (previewItem == null)
            {
                Debug.LogWarning($"{targetInstance.name}에 LotteryPreviewItem 컴포넌트가 없습니다.");
                return;
            }

            currentLotteryPreview = previewItem;
            targetInstance.name = lotteryPrefab.name;
            LotteryPreviews.Add(targetInstance);
        }
        else
        {
            currentLotteryPreview = targetInstance.GetComponent<LotteryPreviewItem>();
        }

        float itemScale = Mathf.Max(0.1f, lottery.GetInspectScale());
        Vector3 finalScale = lotteryPrefab.transform.localScale * (itemScale);

        if (currentLotteryPreview != null)
        {
            targetInstance.transform.localScale = finalScale;
            targetInstance.transform.localPosition = currentLotteryPreview.Offset;
        }

        targetInstance.SetActive(true);

        currentActiveOverlay = GetOrCreateOverlayForLottery(lottery);
        if (currentActiveOverlay != null)
        {
            currentActiveOverlay.transform.localScale = Vector3.one;
            currentActiveOverlay.SetActive(true);
        }
    }
    private GameObject GetOrCreateOverlayForLottery(Lottery lottery)
    {
        if (lottery == null || currentLotteryPreview == null) return null;

        GameObject overlay = lottery.GetStainOverlay();
        if (overlay == null && currentLotteryPreview.IconAreasPrefab != null)
        {
            overlay = Instantiate(currentLotteryPreview.IconAreasPrefab, currentLotteryPreview.transform);
            overlay.name = $"{lottery.name}_StainOverlay";
        }

        if (overlay != null)
        {
            Scratchable[] scratchables = SetupOverlayIconAreas(lottery, overlay, out IconArea[] areas);
            lottery.SetStainOverlay(overlay, scratchables);
            ApplyIconsToOverlay(lottery, areas);
        }

        return overlay;
    }

    private Scratchable[] SetupOverlayIconAreas(Lottery lottery, GameObject overlay, out IconArea[] areas)
    {
        areas = overlay.GetComponentsInChildren<IconArea>(true);

        var scratchableList = new List<Scratchable>(areas.Length);
        for (int i = 0; i < areas.Length; i++)
        {
            IconArea area = areas[i];
            if (area == null) continue;

            area.SlotIndex = i;
            if (area.Scratchable != null)
            {
                area.Scratchable.CurrentItem = lottery;
                scratchableList.Add(area.Scratchable);
            }
        }

        return scratchableList.ToArray();
    }

    private void ApplyIconsToOverlay(Lottery lottery, IconArea[] areas)
    {
        if (lottery == null || lottery.currentLotteryIcons == null || areas == null) return;

        int count = Mathf.Min(areas.Length, lottery.currentLotteryIcons.Count);
        for (int i = 0; i < count; i++)
        {
            LotteryIconData iconData = lottery.currentLotteryIcons[i];
            if (areas[i] != null && iconData != null)
            {
                areas[i].SetIconSprite(iconData.iconSprite);
            }
        }
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

        if (currentTarget is Lottery)
        {
            if (currentLotteryPreview != null)
            {
                currentLotteryPreview.gameObject.SetActive(false);
            }
        }

        currentTarget = null;

        SetState(ViewState.Table);
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

    private GameObject GetOrCreateOverlayForTrophy(Trophy trophy)
    {
        if (trophy == null) return null;

        GameObject overlay = trophy.GetStainOverlay();
        if (overlay == null && stainOverlayPrefab != null)
        {
            overlay = Instantiate(stainOverlayPrefab, stainOverlayParent, false);
            overlay.name = $"{trophy.gameObject.name}_StainOverlay";
            overlay.GetComponent<Scratchable>().CurrentItem = trophy;

            SpriteRenderer sr = overlay.GetComponentInChildren<SpriteRenderer>();
            if (sr != null)
            {
                sr.sprite = trophy.GetRustedSprite();
            }

            trophy.SetStainOverlay(overlay);
        }
        return overlay;
    }

    public void OnCleaned(IInspectableSource target)
    {
        if (target == null) return;

        currentTarget = target;

        if (target is Trophy trophy)
        {
            OnTrophyCleaned(trophy);
        }
        else if (target is Lottery lottery)
        {
            OnLotteryCleaned(lottery);
        }

        ShowResultButton(target);
    }

    public void OnTrophyCleaned(Trophy trophy)
    {
        Sprite resultSprite = trophy.Reward > 0 ? trophy.GetInspectSprite() : trophy.GetBrokenSprite();
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
        if (trophy.Reward < 0)
            PhoneEventController.Instance?.TriggerBrokenOrLossCall();
    }

    public void OnLotteryCleaned(Lottery lottery)
    {

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