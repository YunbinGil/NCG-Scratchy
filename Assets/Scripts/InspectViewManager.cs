using UnityEngine;
using UnityEngine.UI;

public enum ViewState
{
    Table,   // 기본 테이블뷰
    Inspect  // 오브젝트 확대 + 우측 상세 패널 표시 상태
}

public interface IInspectableSource
{
    Sprite GetInspectSprite();
    float GetInspectScale();
    void OpenInDetailPanel(InspectDetailPanel detailPanel);
}

public class InspectViewManager : MonoBehaviour
{
    public static InspectViewManager Instance { get; private set; }

    [Header("Inspect 상태에서만 켜지는 루트")]
    [SerializeField] private GameObject inspectOverlayRoot;

    [Header("배경 및 중앙 확대 스프라이트")]
    [SerializeField] private Button dimmedBackgroundButton;
    [SerializeField] private SpriteRenderer centerPreviewRenderer;
    [SerializeField] private float basePreviewScale = 1f;

    [Header("우측 상세 패널")]
    [SerializeField] private InspectDetailPanel detailPanel;

    public ViewState CurrentState { get; private set; } = ViewState.Table;
    public bool IsInspecting => CurrentState == ViewState.Inspect;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        if (dimmedBackgroundButton != null)
        {
            dimmedBackgroundButton.onClick.RemoveListener(CloseInspect);
            dimmedBackgroundButton.onClick.AddListener(CloseInspect);
        }

        SetState(ViewState.Table);
    }

    public void OpenInspect(IInspectableSource target)
    {
        if (target == null) return;

        if (centerPreviewRenderer != null)
        {
            Sprite sprite = target.GetInspectSprite();
            centerPreviewRenderer.sprite = sprite;
            centerPreviewRenderer.enabled = sprite != null;

            float scale = Mathf.Max(0.1f, target.GetInspectScale());
            centerPreviewRenderer.transform.localScale = Vector3.one * (basePreviewScale * scale);
        }

        if (detailPanel != null)
        {
            detailPanel.Show();
            target.OpenInDetailPanel(detailPanel);
        }

        SetState(ViewState.Inspect);
    }

    public void CloseInspect()
    {
        if (detailPanel != null)
        {
            detailPanel.Hide();
        }

        if (centerPreviewRenderer != null)
        {
            centerPreviewRenderer.sprite = null;
            centerPreviewRenderer.enabled = false;
        }

        SetState(ViewState.Table);
    }

    public void SetState(ViewState nextState)
    {
        CurrentState = nextState;
        bool isInspect = nextState == ViewState.Inspect;

        if (inspectOverlayRoot != null)
        {
            inspectOverlayRoot.SetActive(isInspect);
        }
    }
}