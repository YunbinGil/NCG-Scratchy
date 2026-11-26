using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class Trophy : MonoBehaviour, IInspectableSource
{
    [Header("데이터 (ScriptableObject)")]
    [SerializeField] private TrophyData trophyData;

    [Header("UI 버튼")]
    [SerializeField] private Button TrophyButton;
    private GameObject stainOverlay;

    public TrophyData Data => trophyData;
    private int sellPriceAtSpawn;
    private float breakChanceAtSpawn;
    public bool IsCleaned { get; private set; }
    public int Reward { get; private set; }



    private void Awake()
    {
        sellPriceAtSpawn = trophyData != null ? trophyData.GetSellPriceAfterClean() : 0;
        breakChanceAtSpawn = trophyData != null ? trophyData.GetBreakChance() : 0f;
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
            InspectViewManager.Instance?.OnCleaned(this);
    }

    public Sprite GetInspectSprite()
    {
        if (trophyData == null) return null;
        return trophyData.inspectSprite != null ? trophyData.inspectSprite : trophyData.tableSprite;
    }
    public Sprite GetRustedSprite()
    {
        if (trophyData == null) return null;
        return trophyData.stainSprite != null ? trophyData.stainSprite : trophyData.tableSprite;
    }
    public Sprite GetBrokenSprite()
    {
        if (trophyData == null) return null;
        return trophyData.brokenSprite != null ? trophyData.brokenSprite : trophyData.tableSprite;
    }
    public float GetInspectScale()
    {
        return 1f;
    }

    public void OpenInDetailPanel(InspectDetailPanel detailPanel)
    {
        if (detailPanel == null || trophyData == null) return;
        detailPanel.ShowDish(trophyData, sellPriceAtSpawn, breakChanceAtSpawn);
    }
    public GameObject GetStainOverlay()
    {
        return stainOverlay;
    }

    public void SetStainOverlay(GameObject overlay)
    {
        stainOverlay = overlay;
        stainOverlay.GetComponent<Scratchable>().CurrentItem = this;
    }

    public void CompleteScratch()
    {
        if (trophyData == null) return;
        IsCleaned = true;
        Reward = Random.value < breakChanceAtSpawn
            ? -Mathf.Abs(trophyData.breakPenalty)
            : sellPriceAtSpawn;
    }

    public void DestoryItem()
    {
        Destroy(gameObject);
    }
}