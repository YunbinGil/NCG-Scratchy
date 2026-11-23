using UnityEngine;
using System.Collections.Generic;

public class ItemSpawner : MonoBehaviour
{
    [SerializeField] private List<GameObject> items;
    [SerializeField] private RectTransform spawnTable;

    private List<RectTransform> spawnItems = new List<RectTransform>();
    private List<Vector2> targetpoints = new List<Vector2>();

    private float objectSpeed = 600f;

    private void CreateItem(GameObject item)
    {
        GameObject spawnedObject  = Instantiate(item, transform.position, Quaternion.identity, spawnTable);
        RectTransform spawnItem = spawnedObject .GetComponent<RectTransform>();
        spawnItem.SetAsLastSibling();

        float minX = -spawnTable.rect.width / 3f;
        float maxX = spawnTable.rect.width / 3f;
        float minY = 0f;
        float maxY = spawnTable.rect.height / 2f;

        float targetpointX = Random.Range(minX, maxX);
        float targetpointY = Random.Range(minY, maxY);

        Vector2 targetpoint = new Vector2(targetpointX, targetpointY);

        // 생성한 아이템과 목표 좌표 저장
        spawnItems.Add(spawnItem);
        targetpoints.Add(targetpoint);
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            if (items.Count > 0)
            {
                int randomIndex = Random.Range(0, items.Count);
                CreateItem(items[randomIndex]);
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

            spawnItems[i].anchoredPosition = Vector2.MoveTowards
                (
                spawnItems[i].anchoredPosition, 
                targetpoints[i], 
                objectSpeed * Time.deltaTime
                );

            // 목표 지점에 도착하면 목록에서 제거
            if (Vector2.Distance(spawnItems[i].anchoredPosition, targetpoints[i]) < 0.01f)
            {
                spawnItems.RemoveAt(i);
                targetpoints.RemoveAt(i);
            }
        }
    }
}
