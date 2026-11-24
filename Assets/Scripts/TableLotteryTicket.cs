using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class TableLotteryTicket : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IInspectableSource
{
    [Header("3칸 UI 이미지")]
    [SerializeField] private Image[] slotImages = new Image[3];

    [Header("확대 보기용 설정")]
    [SerializeField] private Sprite inspectSprite;
    [SerializeField] private float inspectScale = 1f;
    [SerializeField] private float dragThreshold = 10f;

    private Button button;
    private RectTransform rectTransform;
    private Canvas parentCanvas;
    private LotteryRollResult rollResult;

    private Vector2 pointerDownPos;
    private Vector2 dragOffset;
    private bool isDragging;

    private void Awake()
    {
        button = GetComponent<Button>();
        rectTransform = GetComponent<RectTransform>();
        parentCanvas = GetComponentInParent<Canvas>();

        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(OnTicketClicked);
    }

    public void Initialize(LotteryRollResult result, RectTransform tableParent)
    {
        rollResult = result;
        if (tableParent != null)
        {
            rectTransform.SetParent(tableParent, false);
        }

        for (int i = 0; i < slotImages.Length; i++)
        {
            if (slotImages[i] != null && result != null && i < result.Slots.Length)
            {
                slotImages[i].sprite = result.Slots[i].icon;
                slotImages[i].enabled = result.Slots[i].icon != null;
            }
        }

        rectTransform.SetAsLastSibling();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        pointerDownPos = eventData.position;
        isDragging = false;

        // 클릭/드래그 시 다른 복권보다 맨 위에 표시
        rectTransform.SetAsLastSibling();

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            rectTransform.parent as RectTransform,
            eventData.position,
            eventData.pressEventCamera,
            out Vector2 localPoint
        );
        dragOffset = rectTransform.anchoredPosition - localPoint;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!isDragging && Vector2.Distance(eventData.position, pointerDownPos) >= dragThreshold)
        {
            isDragging = true;
        }

        if (!isDragging) return;

        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
            rectTransform.parent as RectTransform,
            eventData.position,
            eventData.pressEventCamera,
            out Vector2 localPoint))
        {
            rectTransform.anchoredPosition = localPoint + dragOffset;
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        // 드래그 직후 프레임에 Button.onClick이 같이 튀는 것 방지
        if (isDragging)
        {
            button.interactable = false;
            Invoke(nameof(ReEnableButton), 0.05f);
        }
        isDragging = false;
    }

    private void ReEnableButton()
    {
        if (button != null) button.interactable = true;
    }

    private void OnTicketClicked()
    {
        if (isDragging) return;

        rectTransform.SetAsLastSibling();
        if (InspectViewManager.Instance != null)
        {
            InspectViewManager.Instance.OpenInspect(this);
        }
    }

    // IInspectableSource 구현
    public Sprite GetInspectSprite() => inspectSprite;
    public float GetInspectScale() => inspectScale;

    public void DestoryItem()
    {
        Destroy(gameObject);
    }

    public void HandleClick()
    {
        // 클릭 시 필요한 동작 (필요 없으면 비워두어도 컴파일은 됩니다)
    }

    public int Reward => 0; // 실제 보상 값 필드가 있으면 그 값을 반환

    public bool IsCleaned => false; // 복권에 청소 상태가 없다면 적절한 bool 값 반환
    public void OpenInDetailPanel(InspectDetailPanel detailPanel)
    {
        // detailPanel 쪽에 rollResult(각 칸 아이콘/확률/매칭 보상/총 당첨금)를 전달해 표시하도록 연결
        // 예: detailPanel.BindLottery(rollResult);
    }
}