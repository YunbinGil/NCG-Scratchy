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

    private void Awake()
    {
        if (lotteryButton == null)
            lotteryButton = GetComponent<Button>();

        if (lotteryButton != null)
        {
            lotteryButton.onClick.RemoveListener(HandleClick);
            lotteryButton.onClick.AddListener(HandleClick);
        }
    }

    private void OnDestroy()
    {
        if (lotteryButton != null)
            lotteryButton.onClick.RemoveListener(HandleClick);
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
}