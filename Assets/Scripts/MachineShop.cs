using System.Collections.Generic;
using UnityEngine;

public class MachineShop : MonoBehaviour
{
    [System.Serializable]
    private class EquipmentEntry
    {
        public GameObject slotPrefab;
    }

    [Header("장비 목록")]
    [SerializeField] private List<EquipmentEntry> equipmentList = new List<EquipmentEntry>();

    [Header("스크롤뷰 Content")]
    public Transform equipmentContent;
    public RectTransform canvasRect;

    [Header("초기 골드 (GameManager 없을 때 폴백)")]
    [SerializeField] private int fallbackGold = 1000;

    [SerializeField] private GameManager gameManager;

    private readonly List<MachineSlot> equipmentSlots = new List<MachineSlot>();
    private readonly HashSet<string> revealedEquipmentNames = new HashSet<string>();
    private ScratchBotUpgradeManager botUpgradeManager;

    public IReadOnlyCollection<string> RevealedEquipmentNames => revealedEquipmentNames;

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

        botUpgradeManager = ScratchBotUpgradeManager.Instance;
        CreateEquipmentUI();

        if (gameManager != null)
            gameManager.OnLevelChanged += RefreshEquipmentLocks;
        if (botUpgradeManager != null)
            botUpgradeManager.OnUpgraded += RefreshEquipmentLocks;

        RefreshEquipmentLocks();
    }

    private void CreateEquipmentUI()
    {
        foreach (EquipmentEntry entry in equipmentList)
        {
            GameObject equipmentPrefab = entry?.slotPrefab;
            if (equipmentPrefab == null)
                continue;

            GameObject slotObj = Instantiate(equipmentPrefab, equipmentContent);
            GameAudio.RegisterButtons(slotObj);

            MachineSlot slot = slotObj.GetComponent<MachineSlot>();
            if (slot != null)
            {
                equipmentSlots.Add(slot);
                slot.Initialize(this);
            }
        }
    }

    internal bool HasRevealedEquipment(string equipmentName)
    {
        return revealedEquipmentNames.Contains(equipmentName) || purchasedEquipmentNames.Contains(equipmentName);
    }

    public void RestoreRevealedEquipments(List<string> equipmentNames)
    {
        revealedEquipmentNames.Clear();
        if (equipmentNames != null)
            revealedEquipmentNames.UnionWith(equipmentNames);

        RefreshEquipmentLocks();
    }

    internal void RecordUnlock(string equipmentName)
    {
        revealedEquipmentNames.Add(equipmentName);
        RefreshEquipmentLocks();
    }

    private void RefreshEquipmentLocks()
    {
        RefreshEquipmentLocks(gameManager != null ? gameManager.Level : 0);
    }

    private void RefreshEquipmentLocks(int playerLevel)
    {
        foreach (MachineSlot slot in equipmentSlots)
        {
            if (slot != null)
                slot.UpdateLock(playerLevel);
        }
    }

    private void OnDestroy()
    {
        if (gameManager != null)
            gameManager.OnLevelChanged -= RefreshEquipmentLocks;
        if (botUpgradeManager != null)
            botUpgradeManager.OnUpgraded -= RefreshEquipmentLocks;
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
        GameAudio.RegisterButtons(equipment);

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

        foreach (EquipmentEntry entry in equipmentList)
        {
            GameObject equipmentPrefab = entry?.slotPrefab;
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