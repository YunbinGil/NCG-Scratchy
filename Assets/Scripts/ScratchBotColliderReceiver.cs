using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
public class ScratchBotColliderReceiver : MonoBehaviour
{
    [SerializeField] private ScratchBot scratchBot;
    [SerializeField] private BoxCollider2D botCollider;

    private RectTransform rectTransform;

    private void Awake()
    {
        if (scratchBot == null)
            scratchBot = GetComponent<ScratchBot>();

        if (botCollider == null)
            botCollider = GetComponent<BoxCollider2D>();

        rectTransform = GetComponent<RectTransform>();

        if (botCollider != null)
            botCollider.isTrigger = true;

        SyncColliderSize();
    }

    public void SyncColliderSize()
    {
        if (rectTransform == null)
            rectTransform = GetComponent<RectTransform>();

        if (botCollider == null)
            botCollider = GetComponent<BoxCollider2D>();

        if (rectTransform == null || botCollider == null)
            return;

        Vector2 size = rectTransform.rect.size;
        if (size.x > 0.01f && size.y > 0.01f)
        {
            botCollider.size = size;
            botCollider.offset = Vector2.zero;
        }
    }

    public bool TryAbsorbIfOverlapping(
        GameObject lotteryObj,
        BoxCollider2D lotteryCollider)
    {
        if (scratchBot == null)
            scratchBot = GetComponent<ScratchBot>();

        if (scratchBot == null || lotteryObj == null)
            return false;

        if (!scratchBot.CanAbsorbMore())
            return false;

        SyncColliderSize();

        if (botCollider == null || !botCollider.enabled)
            return false;

        Physics2D.SyncTransforms();

        bool overlapped = false;

        // 1. 두 BoxCollider2D의 실제 2D 겹침 검사
        if (lotteryCollider != null && lotteryCollider.enabled &&
            botCollider.size.x > 0f && botCollider.size.y > 0f &&
            lotteryCollider.size.x > 0f && lotteryCollider.size.y > 0f)
        {
            ColliderDistance2D distance = Physics2D.Distance(botCollider, lotteryCollider);
            overlapped = distance.isOverlapped;
        }

        // 2. 복권 중심점 또는 4개 꼭짓점이 botCollider 안에 들어오는지 검사
        if (!overlapped)
        {
            RectTransform lotteryRect = lotteryObj.GetComponent<RectTransform>();
            if (lotteryRect != null)
            {
                Vector3 center = lotteryRect.TransformPoint(lotteryRect.rect.center);
                if (botCollider.OverlapPoint(center))
                {
                    overlapped = true;
                }
                else
                {
                    Vector3[] corners = new Vector3[4];
                    lotteryRect.GetWorldCorners(corners);
                    for (int i = 0; i < corners.Length; i++)
                    {
                        if (botCollider.OverlapPoint(corners[i]))
                        {
                            overlapped = true;
                            break;
                        }
                    }
                }
            }
        }

        if (!overlapped)
            return false;

        return scratchBot.TryAbsorbLottery(lotteryObj);
    }
}