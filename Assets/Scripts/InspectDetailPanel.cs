using UnityEngine;

public class InspectDetailPanel : MonoBehaviour
{
    [Header("전용 패널")]
    [SerializeField] private GameObject dishDetailPanel;
    [SerializeField] private GameObject lotteryDetailPanel;

    [Header("상세 뷰 스크립트")]
    [SerializeField] private TrophyDetailView dishDetailView;
    [SerializeField] private LotteryDetailView lotteryDetailView;

    public void Show()
    {
        gameObject.SetActive(true);
    }

    public void Hide()
    {
        ClearAll();
        gameObject.SetActive(false);
    }

    public void ClearAll()
    {
        if (dishDetailView != null) dishDetailView.Clear();
        if (lotteryDetailView != null) lotteryDetailView.Clear();

        if (dishDetailPanel != null) dishDetailPanel.SetActive(false);
        if (lotteryDetailPanel != null) lotteryDetailPanel.SetActive(false);
    }

    public void ShowDish(TrophyData dishData)
    {
        ClearAll();
        if (dishDetailPanel != null) dishDetailPanel.SetActive(true);
        if (dishDetailView != null) dishDetailView.Bind(dishData);
    }

    public void ShowLottery(LotteryData lotteryData)
    {
        ClearAll();
        if (lotteryDetailPanel != null) lotteryDetailPanel.SetActive(true);
        if (lotteryDetailView != null) lotteryDetailView.Bind(lotteryData);
    }
}