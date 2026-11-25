using UnityEngine;

public class IconArea : MonoBehaviour
{
    [SerializeField] private int slotIndex;
    [SerializeField] private SpriteRenderer iconSpriteRenderer;
    [SerializeField] private Scratchable scratchable;

    public int SlotIndex
    {
        get => slotIndex;
        set
        {
            slotIndex = value;
            if (scratchable != null)
            {
                scratchable.SlotIndex = value;
            }
        }
    }

    public SpriteRenderer IconSpriteRenderer => iconSpriteRenderer;
    public Scratchable Scratchable => scratchable;

    public void SetIconSprite(Sprite sprite)
    {
        if (iconSpriteRenderer == null) return;
        iconSpriteRenderer.sprite = sprite;
        iconSpriteRenderer.enabled = sprite != null;
    }
}