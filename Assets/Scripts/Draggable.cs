using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class Draggable : MonoBehaviour,
    IPointerDownHandler,
    IDragHandler,
    IEndDragHandler,
    IPointerUpHandler,
    IPointerEnterHandler
{
    private RectTransform tableRect;

    public Button objectButton;

    private RectTransform objectRect;
    private Canvas parentCanvas;
    private CanvasGroup canvasGroup;
    private bool blocksRaycastsBeforeDrag;
    private Camera eventCamera;
    private bool isDragging;
    [SerializeField] private float highlightAngle = 10f;
    [SerializeField] private GameObject dragSprite;
    private Quaternion defaultRotation;
    private bool isTilted;
    private Vector2 pressPosition;
    private float dragThreshold = 10f;
    private IInspectableSource itemSource;
    private bool HasItemAudio => itemSource is Lottery or Trophy;

    private void Awake()
    {
        itemSource = GetComponent<IInspectableSource>();
        objectButton = GetComponent<Button>();
        objectRect = GetComponent<RectTransform>();
        parentCanvas = GetComponentInParent<Canvas>();
        if (!TryGetComponent(out canvasGroup))
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        defaultRotation = objectRect.localRotation;
        SetDragging(false);
    }

    public void SetTableRect(RectTransform table)
    {
        tableRect = table;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (isDragging)
            return;

        if (HasItemAudio)
            GameAudio.Play(SoundEffect.Dish);

        isTilted = !isTilted;
        objectRect.localRotation = isTilted
            ? defaultRotation * Quaternion.Euler(0f, 0f, -highlightAngle)
            : defaultRotation;
    }

    private void SetDragging(bool dragging)
    {
        if (dragging && !isDragging)
        {
            blocksRaycastsBeforeDrag = canvasGroup.blocksRaycasts;
            canvasGroup.blocksRaycasts = false;
        }
        else if (!dragging && isDragging)
        {
            canvasGroup.blocksRaycasts = blocksRaycastsBeforeDrag;
        }

        isDragging = dragging;
        if (dragSprite != null)
            dragSprite.SetActive(dragging);
    }

    private void OnDisable()
    {
        SetDragging(false);
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        pressPosition = eventData.position;
        eventCamera = eventData.pressEventCamera != null
            ? eventData.pressEventCamera
            : (parentCanvas != null && parentCanvas.renderMode != RenderMode.ScreenSpaceOverlay
                ? parentCanvas.worldCamera
                : null);

        SetDragging(false);
        transform.SetAsLastSibling();
    }

    public void OnDrag(PointerEventData eventData)
    {
        RectTransform dragBounds = tableRect != null ? tableRect : objectRect.parent as RectTransform;
        if (dragBounds == null)
            return;

        float distance = Vector2.Distance(pressPosition, eventData.position);
        if (!isDragging && distance >= dragThreshold)
        {
            SetDragging(true);
            if (HasItemAudio)
                GameAudio.Play(SoundEffect.Dish);
        }

        if (!isDragging)
            return;

        Camera cam = eventData.pressEventCamera != null ? eventData.pressEventCamera : eventCamera;

        // 포인터 위치를 dragBounds 로컬 좌표로 변환
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(dragBounds, eventData.position, cam, out Vector2 pointerLocal))
            return;

        // dragBounds 내부에서 오브젝트 중심이 갈 수 있는 로컬 범위 계산
        float halfW = Mathf.Max(0f, (dragBounds.rect.width - objectRect.rect.width) * 0.5f);
        float halfH = Mathf.Max(0f, (dragBounds.rect.height - objectRect.rect.height) * 0.5f);

        float minX = dragBounds.rect.center.x - halfW;
        float maxX = dragBounds.rect.center.x + halfW;
        float minY = dragBounds.rect.center.y - halfH;
        float maxY = dragBounds.rect.center.y + halfH;

        Vector2 clampedLocal = new Vector2(
            Mathf.Clamp(pointerLocal.x, minX, maxX),
            Mathf.Clamp(pointerLocal.y, minY, maxY)
        );

        // dragBounds 로컬 좌표를 월드 좌표로 변환해 적용
        objectRect.position = dragBounds.TransformPoint(clampedLocal);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (!isDragging)
        {
            if (HasItemAudio && eventData.button == PointerEventData.InputButton.Left)
                GameAudio.Play(SoundEffect.Dish);
            itemSource?.HandleClick();
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        SetDragging(false);

        if (TryGetComponent(out LotteryDropToBotByCollider dropByCollider))
        {
            dropByCollider.TryHandleDropToBot();
        }
    }
}
