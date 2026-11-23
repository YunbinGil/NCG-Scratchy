using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class Draggable : MonoBehaviour,
    IPointerDownHandler, 
    IDragHandler,
    IPointerUpHandler,
    IPointerEnterHandler
{
    private RectTransform tableRect;

    public Button objectButton;

    private RectTransform objectRect;
    private bool isDragging;
    [SerializeField] private float highlightAngle = 10f;
    [SerializeField] private GameObject dragSprite;
    private Quaternion defaultRotation;
    private bool isTilted;
    private Vector2 pressPosition;
    private float dragThreshold = 10f;

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (isDragging)
            return;

        isTilted = !isTilted;
        objectRect.localRotation = isTilted
            ? defaultRotation * Quaternion.Euler(0f, 0f, -highlightAngle)
            : defaultRotation;
    }

    private void SetDragging(bool dragging)
    {
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
        }

        if (isDragging)
        {
            Vector2 boundsCenter = tableRect != null ? tableRect.anchoredPosition : dragBounds.rect.center - ((Vector2)objectRect.localPosition - objectRect.anchoredPosition);

            // 오브젝트 이동 범위 설정
            float minX = boundsCenter.x - dragBounds.rect.width / 2f + objectRect.rect.width / 2f;
            float maxX = boundsCenter.x + dragBounds.rect.width / 2f - objectRect.rect.width / 2f;
            float minY = boundsCenter.y - dragBounds.rect.height / 2f + objectRect.rect.height / 2f;
            float maxY = boundsCenter.y + dragBounds.rect.height / 2f - objectRect.rect.height / 2f;

            // 이동할 위치 계산
            Vector2 nextPosition = objectRect.anchoredPosition + eventData.delta / objectRect.lossyScale.x;

            // 테이블 범위로 제한
            nextPosition.x = Mathf.Clamp(nextPosition.x, minX, maxX);
            nextPosition.y = Mathf.Clamp(nextPosition.y, minY, maxY);

            // 최종 위치 적용
            objectRect.anchoredPosition = nextPosition;
        }
    }

    private void Awake()
    {
        objectButton = GetComponent<Button>();
        objectRect = GetComponent<RectTransform>();
        defaultRotation = objectRect.localRotation;
        SetDragging(false);
    }

    public void SetTableRect(RectTransform table)
    {
        tableRect = table;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (!isDragging)
        {
            Debug.Log("버튼 클릭");
        }
        SetDragging(false);
    }
}