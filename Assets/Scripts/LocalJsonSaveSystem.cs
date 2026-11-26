using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

[Serializable]
public class UpgradeSaveEntry
{
    public UpgradeType upgradeType;
    public int level;
    public float cost;
}

[Serializable]
public class GameSaveData
{
    public int level;
    public int exp;
    public int money;
    public bool canUnlockScratchBot;
    public bool isScratchBotUnlockedByLevel;

    public int currentCatalogIndex;
    public bool isUpgradePanelUnlocked;

    public PhoneEventSaveData phoneEventData = new PhoneEventSaveData();

    public bool isBotUnlocked;
    public int speedLevel = 1;
    public int capacityLevel = 1;
    public int powerLevel = 1;

    public List<SpawnedTableItemSaveData> spawnedTableItems = new List<SpawnedTableItemSaveData>();
    public List<UpgradeSaveEntry> upgrades = new List<UpgradeSaveEntry>();
    public List<ProductProgress.SaveEntry> productProgresses = new List<ProductProgress.SaveEntry>();
    public List<string> purchasedEquipmentNames = new List<string>();
    public List<string> revealedEquipmentNames = new List<string>();
}

public class LocalJsonSaveSystem : MonoBehaviour
{
    public static LocalJsonSaveSystem Instance { get; private set; }

    private const string SaveFileName = "gamesave.json";
    private const string LoadFlagKey = "ShouldLoadSavedData";
    private const string UpgradeUnlockedPrefKey = "UpgradePanel_IsUnlocked";

    [Header("연결 매니저 (비워두면 자동 탐색)")]
    [SerializeField] private GameManager gameManager;
    [SerializeField] private ScratchBotUpgradeManager botUpgradeManager;
    [SerializeField] private CatalogShop catalogShop;
    [SerializeField] private MachineShop machineShop;
    [SerializeField] private ItemSpawner itemSpawner;
    [SerializeField] private UpgradeUnlock upgradeUnlock;
    [SerializeField] private ProductProgress productProgress;

    public static string SaveFilePath => Path.Combine(Application.persistentDataPath, SaveFileName);

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        CacheReferences();
    }

    private void Start()
    {
        CacheReferences();

        if (PlayerPrefs.GetInt(LoadFlagKey, 0) == 1 && HasSaveFile())
        {
            LoadFromFile();
            PlayerPrefs.SetInt(LoadFlagKey, 0);
            PlayerPrefs.Save();
        }
    }

    private void CacheReferences()
    {
        if (gameManager == null || !gameManager)
            gameManager = FindFirstObjectByType<GameManager>();

        if (botUpgradeManager == null || !botUpgradeManager)
            botUpgradeManager = ScratchBotUpgradeManager.Instance ?? FindFirstObjectByType<ScratchBotUpgradeManager>();

        if (catalogShop == null || !catalogShop)
            catalogShop = FindFirstObjectByType<CatalogShop>();

        if (machineShop == null || !machineShop)
            machineShop = FindFirstObjectByType<MachineShop>();

        if (itemSpawner == null || !itemSpawner)
            itemSpawner = FindFirstObjectByType<ItemSpawner>();

        if (upgradeUnlock == null || !upgradeUnlock)
            upgradeUnlock = UpgradeUnlock.Instance ?? FindFirstObjectByType<UpgradeUnlock>();

        if (productProgress == null || !productProgress)
            productProgress = FindFirstObjectByType<ProductProgress>();
    }

    public static bool HasSaveFile()
    {
        return File.Exists(SaveFilePath);
    }

    public static void DeleteSaveFile()
    {
        if (File.Exists(SaveFilePath))
        {
            File.Delete(SaveFilePath);
        }

        PlayerPrefs.SetInt(LoadFlagKey, 0);
        PlayerPrefs.SetInt(UpgradeUnlockedPrefKey, 0);
        PlayerPrefs.Save();
        Debug.Log($"로컬 세이브 파일 삭제 완료: {SaveFilePath}");
    }

    public void SaveToFile()
    {
        CacheReferences();

        GameSaveData data = new GameSaveData();

        if (gameManager != null && gameManager)
        {
            data.level = gameManager.Level;
            data.exp = gameManager.Exp;
            data.money = gameManager.Money;
            data.canUnlockScratchBot = gameManager.CanUnlockScratchBot;
            data.isScratchBotUnlockedByLevel = gameManager.IsScratchBotUnlocked;
        }

        if (catalogShop != null && catalogShop)
        {
            data.currentCatalogIndex = catalogShop.CurrentCatalogIndexForSave;
        }

        bool unlocked = (upgradeUnlock != null && upgradeUnlock && upgradeUnlock.IsUnlocked) ||
                        PlayerPrefs.GetInt(UpgradeUnlockedPrefKey, 0) == 1;
        data.isUpgradePanelUnlocked = unlocked;
        PlayerPrefs.SetInt(UpgradeUnlockedPrefKey, unlocked ? 1 : 0);
        PlayerPrefs.Save();

        if (botUpgradeManager != null && botUpgradeManager)
        {
            data.isBotUnlocked = botUpgradeManager.isBotUnlocked;
            data.speedLevel = botUpgradeManager.speedLevel;
            data.capacityLevel = botUpgradeManager.capacityLevel;
            data.powerLevel = botUpgradeManager.powerLevel;
        }

        UpgradeSlot[] slots = FindObjectsByType<UpgradeSlot>(FindObjectsSortMode.None);
        foreach (UpgradeSlot slot in slots)
        {
            if (slot == null || !slot) continue;
            data.upgrades.Add(new UpgradeSaveEntry
            {
                upgradeType = slot.UpgradeTypeValue,
                level = slot.UpgradeLevelValue,
                cost = slot.UpgradeCostValue
            });
        }

        if (productProgress != null && productProgress)
        {
            data.productProgresses.AddRange(productProgress.GetSaveEntries());
        }

        if (machineShop != null && machineShop)
        {
            data.purchasedEquipmentNames.AddRange(machineShop.PurchasedEquipmentNames);
            data.revealedEquipmentNames.AddRange(machineShop.RevealedEquipmentNames);
        }

        if (itemSpawner != null && itemSpawner)
        {
            data.spawnedTableItems.AddRange(itemSpawner.ExportSpawnedTableItems());
        }

        PhoneEventController phoneEventController = PhoneEventController.Instance ?? FindFirstObjectByType<PhoneEventController>();
        if (phoneEventController != null)
        {
            data.phoneEventData = phoneEventController.ExportSaveData();
        }

        if (itemSpawner != null && itemSpawner)
        {
            data.spawnedTableItems.AddRange(itemSpawner.ExportSpawnedTableItems());
        }

        PhoneEventController phoneEventController = PhoneEventController.Instance ?? FindFirstObjectByType<PhoneEventController>();
        if (phoneEventController != null)
        {
            data.phoneEventData = phoneEventController.ExportSaveData();
        }

        string json = JsonUtility.ToJson(data, true);
        File.WriteAllText(SaveFilePath, json);
        Debug.Log($"로컬 JSON 저장 완료: {SaveFilePath}");
    }

    public void LoadFromFile()
    {
        if (!HasSaveFile())
        {
            Debug.Log("불러올 로컬 JSON 저장 파일이 없습니다.");
            return;
        }

        CacheReferences();

        string json = File.ReadAllText(SaveFilePath);
        GameSaveData data = JsonUtility.FromJson<GameSaveData>(json);
        if (data == null)
            return;

        // 1. 플레이어 레벨/경험치/돈 복원
        if (gameManager != null && gameManager)
        {
            gameManager.ApplySaveData(
                data.level,
                data.exp,
                data.money,
                data.canUnlockScratchBot,
                data.isScratchBotUnlockedByLevel
            );
        }

        // 2. 도감 인덱스 및 상점 슬롯 복원 (기존 해금 모션 오브젝트 정리 포함)
        if (catalogShop != null && catalogShop)
        {
            catalogShop.ApplyCatalogIndex(data.currentCatalogIndex);
        }

        // 3. 업그레이드 패널 해금 상태 복원 (이미 해금됐으면 모션 없이 바로 오픈)
        if (upgradeUnlock != null && upgradeUnlock)
        {
            upgradeUnlock.ApplySavedUnlockState(data.isUpgradePanelUnlocked);
        }

        // 4. 복권 및 트로피 레벨/경험치 복원
        if (productProgress != null && productProgress && data.productProgresses != null)
        {
            productProgress.RestoreSaveEntries(data.productProgresses);
        }

        // 5. 스크래치 봇 복원
        if (botUpgradeManager != null && botUpgradeManager)
        {
            botUpgradeManager.isBotUnlocked = data.isBotUnlocked;
            botUpgradeManager.SetUpgradeLevel(BotUpgradeType.Speed, data.speedLevel);
            botUpgradeManager.SetUpgradeLevel(BotUpgradeType.Capacity, data.capacityLevel);
            botUpgradeManager.SetUpgradeLevel(BotUpgradeType.Power, data.powerLevel);
            botUpgradeManager.NotifyUpgraded();

            if (data.isBotUnlocked && itemSpawner != null && itemSpawner)
            {
                itemSpawner.TrySpawnScratchBot();
            }
        }

        // 6. 테이블 위 복권/트로피 아이템 복원
        if (itemSpawner != null && itemSpawner && data.spawnedTableItems != null)
        {
            itemSpawner.RestoreSpawnedTableItems(data.spawnedTableItems);
        }

        // 7. 업그레이드 슬롯 레벨/비용 복원
        UpgradeSlot[] slots = FindObjectsByType<UpgradeSlot>(FindObjectsSortMode.None);
        foreach (UpgradeSlot slot in slots)
        {
            if (slot == null || !slot) continue;
            foreach (UpgradeSaveEntry entry in data.upgrades)
            {
                if (entry.upgradeType == slot.UpgradeTypeValue)
                {
                    slot.ApplySaveData(entry.level, entry.cost);
                    break;
                }
            }
        }

        // 8. 전화 알림 복원
        PhoneEventController phoneEventController = PhoneEventController.Instance ?? FindFirstObjectByType<PhoneEventController>();
        if (phoneEventController != null && data.phoneEventData != null)
        {
            phoneEventController.RestoreSaveData(data.phoneEventData);
        }

        // 9. 구매한 장비 복원
        if (machineShop != null && machineShop && data.purchasedEquipmentNames != null)
        {
            machineShop.RestorePurchasedEquipments(data.purchasedEquipmentNames);
            machineShop.RestoreRevealedEquipments(data.revealedEquipmentNames);
        }

        Debug.Log("로컬 JSON 저장 데이터를 불러왔습니다.");
    }
}