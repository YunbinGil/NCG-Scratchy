using UnityEngine;
using System.Collections.Generic;

public class ItemSpawner : MonoBehaviour
{
    [Header("일반 아이템")]
    [SerializeField] private List<GameObject> items;

    [Header("생성 및 드래그 영역")]
    [SerializeField] private RectTransform spawnTable;
    [SerializeField] public RectTransform dragArea;

    public RectTransform SpawnTable => spawnTable;

    [Header("도감 해금 아이템")]
    [SerializeField] private GameObject unlockPrefab;
    [SerializeField] private Texture2D lightTexture;

    private List<RectTransform> spawnItems = new List<RectTransform>();
    private List<Vector2> targetpoints = new List<Vector2>();

    private float objectSpeed = 600f;

    // 일반 아이템 랜덤 좌표 생성
    public bool TrySpawnItem(GameObject item)
    {
        if (spawnTable == null)
            return false;

        float minX = -spawnTable.rect.width / 3f;
        float maxX = spawnTable.rect.width / 3f;
        float minY = 0f;
        float maxY = spawnTable.rect.height / 2f;

        Vector2 targetpoint = new Vector2(
            Random.Range(minX, maxX),
            Random.Range(minY, maxY)
        );

        return TrySpawnItem(item, targetpoint, out _);
    }

    // 공통 아이템 생성 메서드
    public bool TrySpawnItem(
        GameObject item,
        Vector2 targetpoint,
        out GameObject spawnedObject)
    {
        return TrySpawnItem(item, targetpoint, false, out spawnedObject);
    }

    public bool TrySpawnItemAtTableCenter(GameObject item, out GameObject spawnedObject)
    {
        return TrySpawnItem(item, Vector2.zero, true, out spawnedObject);
    }

    private bool TrySpawnItem(
        GameObject item,
        Vector2 targetpoint,
        bool centerOnTable,
        out GameObject spawnedObject)
    {
        spawnedObject = null;

        if (spawnTable == null || item == null ||
            !item.TryGetComponent<RectTransform>(out _))
            return false;

        spawnedObject = Instantiate(
            item,
            transform.position,
            Quaternion.identity,
            spawnTable
        );

        RectTransform spawnItem =
            spawnedObject.GetComponent<RectTransform>();

        spawnItem.SetAsLastSibling();

        if (centerOnTable)
        {
            Vector2 itemCenter = spawnTable.InverseTransformPoint(
                spawnItem.TransformPoint(spawnItem.rect.center));
            targetpoint = spawnItem.anchoredPosition + spawnTable.rect.center - itemCenter;
        }

        Draggable draggable =
            spawnedObject.GetComponent<Draggable>();

        if (draggable != null)
        {
            draggable.SetTableRect(dragArea);
        }

        // 생성된 아이템과 목표 좌표 저장
        spawnItems.Add(spawnItem);
        targetpoints.Add(targetpoint);

        return true;
    }

    private void Update()
    {
        // 테스트용 일반 아이템 생성
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            if (items != null && items.Count > 0)
            {
                int randomIndex = Random.Range(0, items.Count);
                TrySpawnItem(items[randomIndex]);
            }
        }

        // 모든 아이템 이동 처리
        for (int i = spawnItems.Count - 1; i >= 0; i--)
        {
            if (spawnItems[i] == null)
            {
                spawnItems.RemoveAt(i);
                targetpoints.RemoveAt(i);
                continue;
            }

            spawnItems[i].anchoredPosition = Vector2.MoveTowards(
                spawnItems[i].anchoredPosition,
                targetpoints[i],
                objectSpeed * Time.deltaTime
            );

            // 목표 지점에 도착하면 목록에서 제거
            if (Vector2.Distance(
                spawnItems[i].anchoredPosition,
                targetpoints[i]) < 0.01f)
            {
                spawnItems.RemoveAt(i);
                targetpoints.RemoveAt(i);
            }
        }
    }
}