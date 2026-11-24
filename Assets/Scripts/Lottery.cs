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

    public LotteryData Data => lotteryData;

    public int Reward => throw new System.NotImplementedException();
    //TODO: rotteryData, rotteryIconData 확률에 따라 결과계산 후 Reward반영

    public bool IsCleaned { get; } = false;
    //TODO: 다 긁어서 결과 나오면 true로 변경

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
}