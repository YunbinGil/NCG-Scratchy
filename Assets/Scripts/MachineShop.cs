using System.Collections.Generic;
using UnityEngine;

public class MachineShop : MonoBehaviour
{
    [System.Serializable]
    private class EquipmentEntry
    {
        public GameObject slotPrefab;
        [Min(0)] public int unlockLevel;
        public UnlockLotteryCatalog unlockPrefab;
    }

    [Header("장비 목록")]
    [SerializeField] private List<EquipmentEntry> equipmentList = new List<EquipmentEntry>();

    [Header("스크롤뷰 Content")]
    public Transform equipmentContent;
    public RectTransform canvasRect;

    [Header("초기 골드 (GameManager 없을 때 폴백)")]
    [SerializeField] private int fallbackGold = 1000;

    [Header("해금 연출")]
    [SerializeField] private Texture2D unlockLightTexture;

    [SerializeField] private GameManager gameManager;

    private readonly List<MachineSlot> equipmentSlots = new List<MachineSlot>();
    private readonly HashSet<string> revealedEquipmentNames = new HashSet<string>();
    private readonly Dictionary<string, GameObject> spawnedEquipments = new Dictionary<string, GameObject>();
    private ItemSpawner itemSpawner;
    private ScratchBotUpgradeManager botUpgradeManager;
    private GameObject activeUnlockItem;
    private bool unlockCheckRequested;

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

    public bool IsEquipmentPurchased(string equipmentName)
    {
        return !string.IsNullOrWhiteSpace(equipmentName) && purchasedEquipmentNames.Contains(equipmentName);
    }

    private void Start()
    {
        if (gameManager == null)
            gameManager = FindFirstObjectByType<GameManager>();

        itemSpawner = FindFirstObjectByType<ItemSpawner>();
        botUpgradeManager = ScratchBotUpgradeManager.Instance;
        CreateEquipmentUI();

        if (gameManager != null)
            gameManager.OnLevelChanged += RefreshEquipmentLocks;
        if (botUpgradeManager != null)
            botUpgradeManager.OnUpgraded += RefreshEquipmentLocks;

        RefreshEquipmentLocks();
        SyncPurchasedSlotsAndEquipments();
    }

    private void CreateEquipmentUI()
    {
        foreach (EquipmentEntry entry in equipmentList)
        {
            GameObject equipmentPrefab = entry?.slotPrefab;
            if (equipmentPrefab == null)
                continue;

            GameObject slotObj = Instantiate(equipmentPrefab, equipmentContent);

            MachineSlot slot = slotObj.GetComponent<MachineSlot>();
            if (slot != null)
            {
                slot.equipmentShop = this;
                slot.UnlockLevel = entry.unlockLevel;
                slot.UnlockRevealPrefab = entry.unlockPrefab;
                equipmentSlots.Add(slot);
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
        SyncPurchasedSlotsAndEquipments();
    }

    private void LateUpdate()
    {
        if (!unlockCheckRequested || activeUnlockItem != null || itemSpawner == null)
            return;

        unlockCheckRequested = false;
        foreach (MachineSlot slot in equipmentSlots)
        {
            if (slot == null || !slot.IsReadyForUnlockReveal || HasRevealedEquipment(slot.EffectiveEquipmentName))
                continue;

            if (!itemSpawner.TrySpawnItemAtTableCenter(slot.UnlockRevealPrefab.gameObject, out activeUnlockItem))
                continue;

            string equipmentName = slot.EffectiveEquipmentName;
            activeUnlockItem.GetComponent<UnlockLotteryCatalog>().Initialize(
                unlockLightTexture, () => CompleteUnlock(equipmentName), "Equipment Unlocked!", "unlock.equipment.complete");
            return;
        }
    }

    private void CompleteUnlock(string equipmentName)
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

        unlockCheckRequested = true;
    }

    private void OnDestroy()
    {
        if (gameManager != null)
            gameManager.OnLevelChanged -= RefreshEquipmentLocks;
        if (botUpgradeManager != null)
            botUpgradeManager.OnUpgraded -= RefreshEquipmentLocks;
        if (activeUnlockItem != null)
            Destroy(activeUnlockItem);
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

        string key = equipmentPrefab.name;
        if (spawnedEquipments.TryGetValue(key, out GameObject existing) && existing != null)
            return;

        GameObject equipment = Instantiate(equipmentPrefab, canvasRect);
        spawnedEquipments[key] = equipment;

        RectTransform rect = equipment.GetComponent<RectTransform>();
        if (rect == null)
        {
            Debug.LogWarning("장비 프리팹에 RectTransform이 없습니다.");
            Destroy(equipment);
            spawnedEquipments.Remove(key);
            return;
        }

        rect.anchorMin = new Vector2(1f, 0f);
        rect.anchorMax = new Vector2(1f, 0f);
        rect.pivot = new Vector2(1f, 0f);
        rect.anchoredPosition = new Vector2(-50f, 50f);
        rect.SetAsLastSibling();
    }

    public void RecordPurchasedEquipment(string equipmentName, GameObject equipmentPrefab, bool isScratchBot = false)
    {
        if (!string.IsNullOrWhiteSpace(equipmentName) && !purchasedEquipmentNames.Contains(equipmentName))
        {
            purchasedEquipmentNames.Add(equipmentName);
        }

        if (!isScratchBot && equipmentPrefab != null)
        {
            SpawnEquipment(equipmentPrefab);
        }
    }

    public void RestorePurchasedEquipments(List<string> savedEquipmentNames)
    {
        purchasedEquipmentNames.Clear();

        if (savedEquipmentNames != null)
        {
            foreach (string equipmentName in savedEquipmentNames)
            {
                if (!string.IsNullOrWhiteSpace(equipmentName) && !purchasedEquipmentNames.Contains(equipmentName))
                {
                    purchasedEquipmentNames.Add(equipmentName);
                }
            }
        }

        SyncPurchasedSlotsAndEquipments();
    }

    private void SyncPurchasedSlotsAndEquipments()
    {
        if (canvasRect == null)
        {
            Canvas mainCanvas = FindFirstObjectByType<Canvas>();
            if (mainCanvas != null)
                canvasRect = mainCanvas.GetComponent<RectTransform>();
        }

        bool botUnlocked = ScratchBotUpgradeManager.Instance != null &&
                           ScratchBotUpgradeManager.Instance.isBotUnlocked;

        foreach (MachineSlot slot in equipmentSlots)
        {
            if (slot == null)
                continue;

            bool purchased = IsEquipmentPurchased(slot.EffectiveEquipmentName) ||
                             (slot.IsScratchBotSlot && botUnlocked);

            if (purchased)
            {
                if (!string.IsNullOrWhiteSpace(slot.EffectiveEquipmentName) &&
                    !purchasedEquipmentNames.Contains(slot.EffectiveEquipmentName))
                {
                    purchasedEquipmentNames.Add(slot.EffectiveEquipmentName);
                }

                slot.ApplyPurchasedState(true);

                if (!slot.IsScratchBotSlot && slot.equipmentPrefab != null)
                {
                    SpawnEquipment(slot.equipmentPrefab);
                }
            }
        }

        foreach (EquipmentEntry entry in equipmentList)
        {
            GameObject slotPrefab = entry?.slotPrefab;
            if (slotPrefab == null)
                continue;

            MachineSlot prefabSlot = slotPrefab.GetComponent<MachineSlot>();
            string key = prefabSlot != null ? prefabSlot.EffectiveEquipmentName : slotPrefab.name;

            if (IsEquipmentPurchased(key) && prefabSlot != null && !prefabSlot.IsScratchBotSlot && prefabSlot.equipmentPrefab != null)
            {
                SpawnEquipment(prefabSlot.equipmentPrefab);
            }
        }
    }
}