using UnityEngine;
using UnityEngine.EventSystems;

public class Lottery : MonoBehaviour, IInspectableSource, IPointerDownHandler, IPointerUpHandler
{
    [Header("데이터 (ScriptableObject)")]
    [SerializeField] private LotteryData lotteryData;

    [Header("클릭 판정")]
    [SerializeField] private float clickMoveThreshold = 10f;

    private Vector2 _downPos;
    private bool _isTracking;

    public LotteryData Data => lotteryData;

    public Sprite GetInspectSprite()
    {
        if (lotteryData == null) return null;
        return lotteryData.inspectSprite != null ? lotteryData.inspectSprite : lotteryData.tableSprite;
    }

    public float GetInspectScale()
    {
        return 1f;
    }

    public void OpenInDetailPanel(InspectDetailPanel detailPanel)
    {
        if (detailPanel == null || lotteryData == null) return;
        detailPanel.ShowLottery(lotteryData);
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (InspectViewManager.Instance != null && InspectViewManager.Instance.IsInspecting)
            return;

        _downPos = eventData.position;
        _isTracking = true;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (!_isTracking) return;
        _isTracking = false;

        if (InspectViewManager.Instance != null && InspectViewManager.Instance.IsInspecting)
            return;

        if (Vector2.Distance(_downPos, eventData.position) <= clickMoveThreshold)
        {
            InspectViewManager.Instance?.OpenInspect(this);
        }
    }
}