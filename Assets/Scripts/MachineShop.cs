
using System.Collections.Generic;
using UnityEngine;

public class MachineShop : MonoBehaviour
{
    [Header("장비 리스트")]
    public List<GameObject> equipmentPrefabs;

    [Header("스크롤뷰 Content")]
    public Transform equipmentContent;
    public RectTransform canvasRect;

    [Header("임시 골드")]
    public int currentGold = 1000;

    private Vector3 spawnPoint;

    private void Start()
    {
        CreateEquipmentUI();
    }

    private void CreateEquipmentUI()
    {
        foreach (GameObject equipment in equipmentPrefabs)
        {
            GameObject item = Instantiate(equipment, equipmentContent);

            MachineSlot equipmentItem = item.GetComponent<MachineSlot>();
            equipmentItem.equipmentShop = this;
        }
    }

    public bool PurchaseEquipment(int price)
    {
        if (currentGold < price)
        {
            Debug.Log("골드가 부족합니다.");
            return false;
        }

        currentGold -= price;

        Debug.Log("남은 골드: " + currentGold);
        return true;
    }

    public void SpawnEquipment(GameObject equipmentPrefab)
    {
        if (equipmentPrefab == null || canvasRect == null)
            return;

        // Canvas를 부모로 지정해서 생성
        GameObject equipment = Instantiate(equipmentPrefab, canvasRect);

        RectTransform rect = equipment.GetComponent<RectTransform>();

        if (rect == null)
        {
            Debug.LogWarning("장비 프리팹에 RectTransform이 없습니다.");
            Destroy(equipment);
            return;
        }

        // 오른쪽 하단 기준
        rect.anchorMin = new Vector2(1f, 0f);
        rect.anchorMax = new Vector2(1f, 0f);
        rect.pivot = new Vector2(1f, 0f);

        // 오른쪽 하단에서 안쪽으로 50만큼 이동
        rect.anchoredPosition = new Vector2(-50f, 50f);

        if (equipment.TryGetComponent<TrashCan>(out _))
        {
            ItemSpawner itemSpawner = FindFirstObjectByType<ItemSpawner>();
            RectTransform itemTable = itemSpawner != null ? itemSpawner.SpawnTable : null;
            if (itemTable != null && itemTable.IsChildOf(canvasRect))
            {
                // Keep the drop target above the table graphic and below its items.
                rect.SetParent(itemTable, true);
                rect.SetAsFirstSibling();
                return;
            }
        }

        rect.SetAsLastSibling();
    }
}
