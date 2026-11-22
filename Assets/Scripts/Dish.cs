using UnityEngine;
using UnityEngine.EventSystems;

public class Dish : MonoBehaviour, IInspectableSource, IPointerDownHandler, IPointerUpHandler
{
    [Header("데이터 (ScriptableObject)")]
    [SerializeField] private DishData dishData;

    [Header("클릭 판정")]
    [SerializeField] private float clickMoveThreshold = 10f;

    private Vector2 _downPos;
    private bool _isTracking;

    public DishData Data => dishData;

    public Sprite GetInspectSprite()
    {
        if (dishData == null) return null;
        return dishData.inspectSprite != null ? dishData.inspectSprite : dishData.tableSprite;
    }

    public float GetInspectScale()
    {
        return dishData != null ? dishData.inspectScale : 1f;
    }

    public void OpenInDetailPanel(InspectDetailPanel detailPanel)
    {
        if (detailPanel == null) return;
        detailPanel.ShowDish(dishData);
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