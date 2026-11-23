using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class Dish : MonoBehaviour, IInspectableSource
{
    [Header("데이터 (ScriptableObject)")]
    [SerializeField] private DishData dishData;

    [Header("UI 버튼")]
    [SerializeField] private Button DishButton;

    public DishData Data => dishData;

    private void Awake()
    {
        if (DishButton == null)
            DishButton = GetComponent<Button>();

        if (DishButton != null)
        {
            DishButton.onClick.RemoveListener(HandleClick);
            DishButton.onClick.AddListener(HandleClick);
        }
    }

    private void OnDestroy()
    {
        if (DishButton != null)
            DishButton.onClick.RemoveListener(HandleClick);
    }

    private void HandleClick()
    {
        if (InspectViewManager.Instance != null && InspectViewManager.Instance.IsInspecting)
            return;

        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
        {
            
        }

        InspectViewManager.Instance?.OpenInspect(this);
    }

    public Sprite GetInspectSprite()
    {
        if (dishData == null) return null;
        return dishData.inspectSprite != null ? dishData.inspectSprite : dishData.tableSprite;
    }

    public float GetInspectScale()
    {
        return 1f;
    }

    public void OpenInDetailPanel(InspectDetailPanel detailPanel)
    {
        if (detailPanel == null || dishData == null) return;
        detailPanel.ShowDish(dishData);
    }
}