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
    private GameObject stainOverlay;

    public DishData Data => dishData;
    private UiManager uiManager;

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
    private void Start()
    {
        uiManager = GameObject.FindWithTag("GameController").GetComponent<UiManager>();
    }

    private void OnDestroy()
    {
        if (DishButton != null)
            DishButton.onClick.RemoveListener(HandleClick);
        
        if (stainOverlay != null)
        {
            Destroy(stainOverlay);
            stainOverlay = null;
        }
    }

    private void HandleClick()
    {
        if (InspectViewManager.Instance != null && InspectViewManager.Instance.IsInspecting)
            return;

        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
        {
            
        }

        InspectViewManager.Instance?.OpenInspect(this);
        uiManager.SetScrubberCursor();
    }

    public Sprite GetInspectSprite()
    {
        if (dishData == null) return null;
        return dishData.inspectSprite != null ? dishData.inspectSprite : dishData.tableSprite;
    }
    public Sprite GetRustedSprite()
    {
        if (dishData == null) return null;
        return dishData.stainSprite != null ? dishData.stainSprite : dishData.tableSprite;
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
    public GameObject GetStainOverlay()
    {
        return stainOverlay;
    }

    public void SetStainOverlay(GameObject overlay)
    {
        stainOverlay = overlay;
    }
}