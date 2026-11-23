
using System.Collections.Generic;
using UnityEngine;

public class MachineShop : MonoBehaviour
{
    [Header("장비 리스트")]
    public List<GameObject> equipmentPrefabs;

    [Header("스크롤뷰 Content")]
    public Transform equipmentContent;

    [Header("임시 골드")]
    public int currentGold = 1000;

    private void Start()
    {
        CreateEquipmentUI();
    }

    private void CreateEquipmentUI()
    {
        foreach (GameObject equipment in equipmentPrefabs)
        {
            GameObject item = Instantiate(equipment, equipmentContent);

            MachineItem equipmentItem = item.GetComponent<MachineItem>();
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
}
