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
    int Reward { get; }
    bool IsCleaned { get; }
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

    [Header("우측 상세 패널")]
    [SerializeField] private InspectDetailPanel detailPanel;
    [SerializeField] private Button resultButton;

    [Header("스크래치 오디오")]
    [SerializeField] private AudioSource scratchSound;
    [SerializeField] private AudioSource getCoinSound;
    private float scratchSoundUntil;

    [Header("얼룩 오브젝트 생성 설정")]
    [SerializeField] private GameObject stainOverlayPrefab; // SpriteRenderer + Scratchable이 붙은 프리팹
    [SerializeField] private Transform stainOverlayParent;  // 생성할 부모(없으면 null)

    [Header("트로피 프리뷰 설정")]
    [SerializeField] private GameObject trophyPreviewPrefab;
    private GameObject trophyPreviewInstance;

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
    private readonly List<GameObject> LotteryPreviews = new List<GameObject>(); // 실제 생성된 GO들 넣고 icons랑 stainoverlay만 바꿀
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

    internal void ReportManualScratch(IInspectableSource item)
    {
        if (scratchSound == null || scratchSound.clip == null || Time.timeScale <= 0f ||
            item == null || !ReferenceEquals(ActiveScratchTarget, item))
            return;

        scratchSoundUntil = Time.unscaledTime + 0.08f;
        if (!scratchSound.isPlaying)
            scratchSound.Play();
    }

    internal void PlayScratchComplete()
    {
        if (getCoinSound != null && getCoinSound.clip != null)
            getCoinSound.PlayOneShot(getCoinSound.clip);
    }

    private void LateUpdate()
    {
        if (scratchSound != null && scratchSound.isPlaying &&
            (Time.timeScale <= 0f || ActiveScratchTarget == null ||
             Time.unscaledTime >= scratchSoundUntil || Input.GetMouseButtonUp(0)))
            scratchSound.Stop();
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
            lottery.ApplyLuckBeforeInspect();
            OpenLotteryView(lottery);
        }

        if (currentActiveOverlay != null)
        {
            currentActiveOverlay.SetActive(true);
        }

        if (detailPanel != null)
        {
            detailPanel.Show();
            target.OpenInDetailPanel(detailPanel);
        }

        if (resultButton != null)
        {
            bool shouldShowResult = target is Lottery lottery
                ? (lottery.HasMatched || lottery.IsCleaned)
                : target.IsCleaned;

            if (shouldShowResult)
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

    private void HidePreview()
    {
        if (currentLotteryPreview != null)
        {
            currentLotteryPreview.gameObject.SetActive(false);
        }
        if (trophyPreviewInstance != null)
        {
            trophyPreviewInstance.SetActive(false);
        }
    }

    private void HideCenterPreviewVisuals()
    {
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
    }

    private void ApplySpriteToTrophyPreview(Sprite sprite)
    {
        if (trophyPreviewInstance != null)
        {
            Image previewImg = trophyPreviewInstance.GetComponentInChildren<Image>(true);
            if (previewImg != null)
            {
                previewImg.sprite = sprite;
                previewImg.enabled = sprite != null;
            }

            SpriteRenderer previewSr = trophyPreviewInstance.GetComponentInChildren<SpriteRenderer>(true);
            if (previewSr != null)
            {
                previewSr.sprite = sprite;
                previewSr.enabled = sprite != null;
            }
        }

        if (centerPreviewImage != null)
        {
            centerPreviewImage.sprite = sprite;
            centerPreviewImage.enabled = sprite != null;
        }

        if (centerPreviewRenderer != null)
        {
            centerPreviewRenderer.sprite = sprite;
            centerPreviewRenderer.enabled = sprite != null;
        }
    }

    public void OpenTrophyView(Trophy trophy)
    {
        if (trophy == null) return;

        HidePreview();
        HideCenterPreviewVisuals();

        if (trophyPreviewInstance == null && trophyPreviewPrefab != null)
        {
            trophyPreviewInstance = Instantiate(trophyPreviewPrefab, centerPreviewRect, false);
            trophyPreviewInstance.name = trophyPreviewPrefab.name;
        }

        if (trophyPreviewInstance != null)
        {
            trophyPreviewInstance.SetActive(true);
            trophyPreviewInstance.transform.localScale = trophyPreviewPrefab != null
                ? trophyPreviewPrefab.transform.localScale
                : Vector3.one;
        }

        ApplySpriteToTrophyPreview(trophy.GetInspectSprite());
        currentActiveOverlay = GetOrCreateOverlayForTrophy(trophy);
    }

    public void OpenLotteryView(Lottery lottery)
    {
        if (lottery == null || lottery.Data == null || lottery.Data.lotteryViewPrefab == null)
        {
            Debug.LogWarning("OpenLotteryView: lottery 또는 lotteryPrefab이 null입니다.");
            return;
        }

        HidePreview();
        HideCenterPreviewVisuals();

        GameObject lotteryPrefab = lottery.Data.lotteryViewPrefab;
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

        if (currentLotteryPreview != null)
        {
            float itemScale = Mathf.Max(0.1f, lottery.GetInspectScale());
            targetInstance.transform.localScale = lotteryPrefab.transform.localScale * itemScale;
            targetInstance.transform.localPosition = currentLotteryPreview.Offset;
        }

        targetInstance.SetActive(true);

        currentActiveOverlay = GetOrCreateOverlayForLottery(lottery);
        if (currentActiveOverlay != null)
        {
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

        HideCenterPreviewVisuals();
        HidePreview();

        if (currentActiveOverlay != null)
        {
            currentActiveOverlay.SetActive(false);
            currentActiveOverlay = null;
        }

        currentTarget = null;

        SetState(ViewState.Table);
    }

    public void SetState(ViewState nextState)
    {
        if (nextState == ViewState.Table && scratchSound != null)
            scratchSound.Stop();

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
        RectTransform parentRect = trophyPreviewInstance != null
            ? trophyPreviewInstance.GetComponent<RectTransform>()
            : null;

        overlay = parentRect != null
            ? Instantiate(stainOverlayPrefab, parentRect, false)
            : Instantiate(stainOverlayPrefab, stainOverlayParent, false);

        overlay.name = $"{trophy.gameObject.name}_StainOverlay";

        Scratchable scratchable = overlay.GetComponent<Scratchable>();
        if (scratchable != null)
        {
            scratchable.CurrentItem = trophy;
        }

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
            ShowResultButton(target);
        }
        else if (target is Lottery lottery)
        {
            OnLotteryCleaned(lottery);
            if (lottery.HasMatched || lottery.IsCleaned)
            {
                ShowResultButton(lottery);
            }
        }
    }

    public void OnTrophyCleaned(Trophy trophy)
    {
        Sprite resultSprite = trophy.Reward > 0 ? trophy.GetInspectSprite() : trophy.GetBrokenSprite();
        ApplySpriteToTrophyPreview(resultSprite);

        if (trophy.Reward < 0)
            PhoneEventController.Instance?.TriggerBrokenOrLossCall();
    }

    public void OnLotteryCleaned(Lottery lottery)
    {
    }

    private void ShowResultButton(IInspectableSource target)
    {
        if (resultButton == null) return;

        resultButton.onClick.RemoveAllListeners();
        resultButton.onClick.AddListener(() =>
        {
            if (gameManager != null)
            {
                gameManager.AddMoney(target.Reward);
                gameManager.AddExp(target.Reward);
            }
            CloseInspect();
            target.DestoryItem();
        });

        GameAudio.RegisterButton(resultButton);
        TMP_Text label = resultButton.GetComponentInChildren<TMP_Text>();
        if (label != null)
            label.text = $"${target.Reward}";

        resultButton.gameObject.SetActive(true);
    }
    public void OnLotteryMatched(Lottery lottery)
    {
        if (lottery == null) return;

        currentTarget = lottery;

        if (IsInspecting)
        {
            ShowResultButton(lottery);
        }
    }
}