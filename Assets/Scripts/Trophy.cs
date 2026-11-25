using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class Trophy : MonoBehaviour, IInspectableSource
{
    [Header("데이터 (ScriptableObject)")]
    [SerializeField] private TrophyData dishData;

    [Header("UI 버튼")]
    [SerializeField] private Button DishButton;
    private GameObject stainOverlay;

    public TrophyData Data => dishData;
    private UiManager uiManager;
    private int sellPriceAtSpawn;
    private float breakChanceAtSpawn;
    public bool IsCleaned { get; private set; }
    public int Reward { get; private set; }



    private void Awake()
    {
        sellPriceAtSpawn = dishData != null ? dishData.GetSellPriceAfterClean() : 0;
        breakChanceAtSpawn = dishData != null ? dishData.GetBreakChance() : 0f;
    }

    private void Start()
    {
        uiManager = GameObject.FindWithTag("GameController").GetComponent<UiManager>();
    }

    private void OnDestroy()
    {
        if (stainOverlay != null)
        {
            Destroy(stainOverlay);
            stainOverlay = null;
        }
    }

    public void HandleClick()
    {
        if (InspectViewManager.Instance != null && InspectViewManager.Instance.IsInspecting)
            return;

        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
        {

        }

        InspectViewManager.Instance?.OpenInspect(this);
        if(IsCleaned)
            InspectViewManager.Instance?.OnDishCleaned(this);
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
    public Sprite GetBrokenSprite()
    {
        if (dishData == null) return null;
        return dishData.brokenSprite != null ? dishData.brokenSprite : dishData.tableSprite;
    }
    public float GetInspectScale()
    {
        return 1f;
    }

    public void OpenInDetailPanel(InspectDetailPanel detailPanel)
    {
        if (detailPanel == null || dishData == null) return;
        detailPanel.ShowDish(dishData, sellPriceAtSpawn, breakChanceAtSpawn);
    }
    public GameObject GetStainOverlay()
    {
        return stainOverlay;
    }

    public void SetStainOverlay(GameObject overlay)
    {
        stainOverlay = overlay;
        stainOverlay.GetComponent<Scratchable>().Dish = this;
    }

    public void CompleteScratch()
    {
        if (dishData == null) return;
        IsCleaned = true;
        Reward = Random.value < breakChanceAtSpawn
            ? -Mathf.Abs(dishData.breakPenalty)
            : sellPriceAtSpawn;
    }

    public void DestoryItem()
    {
        Destroy(gameObject);
    }
}