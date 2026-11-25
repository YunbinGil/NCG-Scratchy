using UnityEngine;
using UnityEngine.EventSystems;

[RequireComponent(typeof(BoxCollider2D))]
public class LotteryDropToBotByCollider : MonoBehaviour, IEndDragHandler
{
    [SerializeField] private BoxCollider2D lotteryCollider;
    [SerializeField] private ScratchBotColliderReceiver botReceiver;

    private RectTransform rectTransform;
    private int lastHandledFrame = -1;

    private void Awake()
    {
        if (lotteryCollider == null)
            lotteryCollider = GetComponent<BoxCollider2D>();

        rectTransform = GetComponent<RectTransform>();

        if (lotteryCollider != null)
            lotteryCollider.isTrigger = true;

        SyncColliderSize();
    }

    public void SyncColliderSize()
    {
        if (rectTransform == null)
            rectTransform = GetComponent<RectTransform>();

        if (lotteryCollider == null)
            lotteryCollider = GetComponent<BoxCollider2D>();

        if (rectTransform == null || lotteryCollider == null)
            return;

        Vector2 size = rectTransform.rect.size;
        if (size.x > 0.01f && size.y > 0.01f)
        {
            lotteryCollider.size = size;
            lotteryCollider.offset = rectTransform.rect.center;
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        TryHandleDropToBot();
    }

    public void TryHandleDropToBot()
    {
        if (lastHandledFrame == Time.frameCount)
            return;
        lastHandledFrame = Time.frameCount;

        SyncColliderSize();

        if (lotteryCollider == null)
            return;

        if (botReceiver == null)
        {
            botReceiver = FindFirstObjectByType<ScratchBotColliderReceiver>();
        }

        if (botReceiver == null)
            return;

        Physics2D.SyncTransforms();
        botReceiver.TryAbsorbIfOverlapping(gameObject, lotteryCollider);
    }
}