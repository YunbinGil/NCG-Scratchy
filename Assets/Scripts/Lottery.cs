using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class Lottery : MonoBehaviour, IInspectableSource
{
    [Header("데이터 (ScriptableObject)")]
    [SerializeField] private LotteryData lotteryData;

    [Header("UI 버튼")]
    [SerializeField] private Button lotteryButton;
    [SerializeField] private Image image;
    
    private GameObject stainOverlay;

    public LotteryData Data => lotteryData;

    public int Reward => throw new System.NotImplementedException();
    //TODO: rotteryData, rotteryIconData 확률에 따라 결과계산 후 Reward반영

    public bool IsCleaned { get; } = false;
    //TODO: 다 긁어서 결과 나오면 true로 변경
    public void Awake()
    {
        image.sprite = lotteryData.tableSprite;
    }

    public void HandleClick()
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

    public void DestoryItem()
    {
        Destroy(gameObject);
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
    private void OnDestroy()
    {
        if (stainOverlay != null)
        {
            Destroy(stainOverlay);
            stainOverlay = null;
        }
    }

    public void CompleteScratch()
    {
        throw new System.NotImplementedException();
    }
}