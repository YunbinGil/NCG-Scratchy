using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class Draggable : MonoBehaviour,
    IPointerDownHandler, 
    IDragHandler,
    IPointerUpHandler
{
    // [Header("드래그 가능 영역")]
    // [SerializeField]
    private RectTransform tableRect;

    public Button objectButton;

    private RectTransform objectRect;
    private bool isDragging;
    private Vector2 dragOffset;
    private Vector2 pressPosition;
    private float dragThreshold = 10f;

    public void OnPointerDown(PointerEventData eventData)
    {
        pressPosition = eventData.position;
        isDragging = false;
        transform.SetAsLastSibling();
    }

    public void OnDrag(PointerEventData eventData)
    {
        float distance = Vector2.Distance(pressPosition, eventData.position);

        if (distance >= dragThreshold)
        {
            isDragging = true;
        }

        if (isDragging)
        {
            // 오브젝트 이동 범위 설정
            float minX = tableRect.anchoredPosition.x - tableRect.rect.width / 2f + objectRect.rect.width / 2f;
            float maxX = tableRect.anchoredPosition.x + tableRect.rect.width / 2f - objectRect.rect.width / 2f;
            float minY = tableRect.anchoredPosition.y - tableRect.rect.height / 2f + objectRect.rect.height / 2f;
            float maxY = tableRect.anchoredPosition.y + tableRect.rect.height / 2f - objectRect.rect.height / 2f;

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
        isDragging = false;
    }
}