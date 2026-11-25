using UnityEngine;

public class LotteryPreviewItem : MonoBehaviour
{
    [SerializeField] private IconArea[] iconAreas;
    [SerializeField] private GameObject stainArea;
    [SerializeField] private GameObject iconAreasPrefab;
    [SerializeField] private Transform offsetTransform;

    public IconArea[] IconAreas => iconAreas;
    public int IconCount => iconAreas != null ? iconAreas.Length : 0;
    public GameObject StainArea => stainArea;
    public GameObject IconAreasPrefab => iconAreasPrefab;
    public Vector3 Offset => offsetTransform != null ? offsetTransform.localPosition : Vector3.zero;
}