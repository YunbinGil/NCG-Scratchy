using System.Collections.Generic;
using UnityEngine;

public class MachineShop : MonoBehaviour
{
    [Header("장비 리스트")]
    public List<GameObject> equipmentPrefabs;

    [Header("스크롤뷰 Content")]
    public Transform equipmentContent;
    public RectTransform canvasRect;

    [Header("초기 골드 (GameManager 없을 때 폴백)")]
    [SerializeField] private int fallbackGold = 1000;

    [SerializeField] private GameManager gameManager;

    internal GameManager PlayerProgress => gameManager;

    public int Gold
    {
        get => gameManager != null ? gameManager.Money : fallbackGold;
        private set
        {
            if (gameManager != null)
                gameManager.SetMoney(value);
            else
                fallbackGold = value;
        }
    }

    private readonly List<string> purchasedEquipmentNames = new List<string>();
    public IReadOnlyList<string> PurchasedEquipmentNames => purchasedEquipmentNames;

    private void Start()
    {
        if (gameManager == null)
            gameManager = FindFirstObjectByType<GameManager>();

        CreateEquipmentUI();
    }

    private void CreateEquipmentUI()
    {
        foreach (GameObject equipmentPrefab in equipmentPrefabs)
        {
            if (equipmentPrefab == null)
                continue;

            GameObject slotObj = Instantiate(equipmentPrefab, equipmentContent);

            MachineSlot slot = slotObj.GetComponent<MachineSlot>();
            if (slot != null)
            {
                slot.equipmentShop = this;
            }
        }
    }

    public bool PurchaseEquipment(int price)
    {
        if (price < 0)
            return false;

        if (gameManager != null)
        {
            if (!gameManager.TrySpendMoney(price))
            {
                Debug.Log("골드가 부족합니다.");
                return false;
            }

            Debug.Log("남은 골드: " + gameManager.Money);
            return true;
        }

        if (fallbackGold < price)
        {
            Debug.Log("골드가 부족합니다.");
            return false;
        }

        fallbackGold -= price;
        Debug.Log("남은 골드: " + fallbackGold);
        return true;
    }

    public void SpawnEquipment(GameObject equipmentPrefab)
    {
        if (equipmentPrefab == null || canvasRect == null)
            return;

        GameObject equipment = Instantiate(equipmentPrefab, canvasRect);

        RectTransform rect = equipment.GetComponent<RectTransform>();
        if (rect == null)
        {
            Debug.LogWarning("장비 프리팹에 RectTransform이 없습니다.");
            Destroy(equipment);
            return;
        }

        rect.anchorMin = new Vector2(1f, 0f);
        rect.anchorMax = new Vector2(1f, 0f);
        rect.pivot = new Vector2(1f, 0f);
        rect.anchoredPosition = new Vector2(-50f, 50f);
        rect.SetAsLastSibling();
    }

    public void RecordPurchasedEquipment(string equipmentName, GameObject equipmentPrefab)
    {
        if (!string.IsNullOrEmpty(equipmentName) && !purchasedEquipmentNames.Contains(equipmentName))
        {
            purchasedEquipmentNames.Add(equipmentName);
        }

        SpawnEquipment(equipmentPrefab);
    }

    public void RestorePurchasedEquipments(List<string> savedEquipmentNames)
    {
        purchasedEquipmentNames.Clear();
        if (savedEquipmentNames == null)
            return;

        foreach (string equipmentName in savedEquipmentNames)
        {
            if (!string.IsNullOrEmpty(equipmentName) && !purchasedEquipmentNames.Contains(equipmentName))
            {
                purchasedEquipmentNames.Add(equipmentName);
            }
        }

        foreach (GameObject equipmentPrefab in equipmentPrefabs)
        {
            if (equipmentPrefab == null)
                continue;

            MachineSlot slot = equipmentPrefab.GetComponent<MachineSlot>();
            if (slot != null && purchasedEquipmentNames.Contains(slot.equipmentName))
            {
                SpawnEquipment(equipmentPrefab);
            }
        }
    }
}