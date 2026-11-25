using UnityEngine;

public class LotteryPreviewItem : MonoBehaviour
{
    [SerializeField] private GameObject[] iconAreas;
    [SerializeField] private SpriteRenderer[] iconSprites;
    [SerializeField] private Scratchable[] scratchables;
    [SerializeField] private GameObject stainArea;
    
    public GameObject[] GetIconAreas() => iconAreas;
    public SpriteRenderer[] GetIconSprites() => iconSprites;
    public Scratchable[] GetScratchables() => scratchables;
    public int IconCount => iconAreas.Length;
    public GameObject GetStainArea() => stainArea;
}
