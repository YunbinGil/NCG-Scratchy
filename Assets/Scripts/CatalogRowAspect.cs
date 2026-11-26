using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[DisallowMultipleComponent]
[RequireComponent(typeof(RectTransform))]
public sealed class CatalogRowAspect : UIBehaviour, ILayoutElement
{
    [SerializeField, Min(0.01f)] private float widthToHeight = 3f;

    // The parent allocates the width before asking for the row's height.
    public float preferredHeight => ((RectTransform)transform).rect.width / widthToHeight;
    public float minHeight => preferredHeight;
    public float flexibleHeight => 0f;
    public float minWidth => -1f;
    public float preferredWidth => -1f;
    public float flexibleWidth => -1f;
    public int layoutPriority => 2;

    public void CalculateLayoutInputHorizontal() { }
    public void CalculateLayoutInputVertical() { }
}
