using UnityEngine;

public class LotteryPreviewItem : MonoBehaviour
{
    [SerializeField] private GameObject[] iconAreas;
    [SerializeField] private SpriteRenderer[] iconSprites;
    [SerializeField] private GameObject[] scratchables;
    [SerializeField] private GameObject stainArea;
    [SerializeField] private GameObject iconAreasPrefab;
    [SerializeField] private Vector3 offset;
    public GameObject[] GetIconAreas() => iconAreas;
    public SpriteRenderer[] GetIconSprites() => iconSprites;
    public GameObject[] GetScratchables() => scratchables;
    public GameObject GetIconAreasPrefab() => iconAreasPrefab;
    public Vector3 GetOffset() => offset;
}
