using UnityEngine;

public class LotteryPreviewItem : MonoBehaviour
{
    [SerializeField] private IconArea[] iconAreas;
    [SerializeField] private GameObject iconAreasPrefab;
    [SerializeField] private Vector3 offset;

    public IconArea[] IconAreas => iconAreas;
    public int IconCount => iconAreas != null ? iconAreas.Length : 0;
    public GameObject IconAreasPrefab => iconAreasPrefab;
    public Vector3 Offset => offset;
}