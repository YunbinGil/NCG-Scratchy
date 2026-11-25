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

    public bool isBotUnlocked;
    public int speedLevel = 1;
    public int capacityLevel = 1;
    public int powerLevel = 1;

    public List<UpgradeSaveEntry> upgrades = new List<UpgradeSaveEntry>();
    public List<string> purchasedEquipmentNames = new List<string>();
}

public class LocalJsonSaveSystem : MonoBehaviour
{
    public static LocalJsonSaveSystem Instance { get; private set; }

    private const string SaveFileName = "gamesave.json";
    private const string LoadFlagKey = "ShouldLoadSavedData";

    [Header("연결 매니저 (비워두면 자동 탐색)")]
    [SerializeField] private GameManager gameManager;
    [SerializeField] private ScratchBotUpgradeManager botUpgradeManager;
    [SerializeField] private CatalogShop catalogShop;
    [SerializeField] private MachineShop machineShop;
    [SerializeField] private ItemSpawner itemSpawner;

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
        if (PlayerPrefs.GetInt(LoadFlagKey, 0) == 1 && HasSaveFile())
        {
            LoadFromFile();
            PlayerPrefs.SetInt(LoadFlagKey, 0);
            PlayerPrefs.Save();
        }
    }

    private void CacheReferences()
    {
        if (gameManager == null) gameManager = FindFirstObjectByType<GameManager>();
        if (botUpgradeManager == null) botUpgradeManager = ScratchBotUpgradeManager.Instance ?? FindFirstObjectByType<ScratchBotUpgradeManager>();
        if (catalogShop == null) catalogShop = FindFirstObjectByType<CatalogShop>();
        if (machineShop == null) machineShop = FindFirstObjectByType<MachineShop>();
        if (itemSpawner == null) itemSpawner = FindFirstObjectByType<ItemSpawner>();
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
        PlayerPrefs.Save();
        Debug.Log($"로컬 세이브 파일 삭제 완료: {SaveFilePath}");
    }

    public void SaveToFile()
    {
        CacheReferences();

        GameSaveData data = new GameSaveData();

        if (gameManager != null)
        {
            data.level = gameManager.Level;
            data.exp = gameManager.Exp;
            data.money = gameManager.Money;
            data.canUnlockScratchBot = gameManager.CanUnlockScratchBot;
            data.isScratchBotUnlockedByLevel = gameManager.IsScratchBotUnlocked;
        }

        if (catalogShop != null)
        {
            data.currentCatalogIndex = catalogShop.CurrentCatalogIndexForSave;
        }

        if (botUpgradeManager != null)
        {
            data.isBotUnlocked = botUpgradeManager.isBotUnlocked;
            data.speedLevel = botUpgradeManager.speedLevel;
            data.capacityLevel = botUpgradeManager.capacityLevel;
            data.powerLevel = botUpgradeManager.powerLevel;
        }

        UpgradeSlot[] slots = FindObjectsByType<UpgradeSlot>(FindObjectsSortMode.None);
        foreach (UpgradeSlot slot in slots)
        {
            data.upgrades.Add(new UpgradeSaveEntry
            {
                upgradeType = slot.UpgradeTypeValue,
                level = slot.UpgradeLevelValue,
                cost = slot.UpgradeCostValue
            });
        }

        if (machineShop != null)
        {
            data.purchasedEquipmentNames.AddRange(machineShop.PurchasedEquipmentNames);
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

        if (gameManager != null)
        {
            gameManager.ApplySaveData(
                data.level,
                data.exp,
                data.money,
                data.canUnlockScratchBot,
                data.isScratchBotUnlockedByLevel
            );
        }

        if (catalogShop != null)
        {
            catalogShop.ApplyCatalogIndex(data.currentCatalogIndex);
        }

        if (botUpgradeManager != null)
        {
            botUpgradeManager.isBotUnlocked = data.isBotUnlocked;
            botUpgradeManager.SetUpgradeLevel(BotUpgradeType.Speed, data.speedLevel);
            botUpgradeManager.SetUpgradeLevel(BotUpgradeType.Capacity, data.capacityLevel);
            botUpgradeManager.SetUpgradeLevel(BotUpgradeType.Power, data.powerLevel);
            botUpgradeManager.NotifyUpgraded();

            if (data.isBotUnlocked && itemSpawner != null)
            {
                itemSpawner.TrySpawnScratchBot();
            }
        }

        UpgradeSlot[] slots = FindObjectsByType<UpgradeSlot>(FindObjectsSortMode.None);
        foreach (UpgradeSlot slot in slots)
        {
            foreach (UpgradeSaveEntry entry in data.upgrades)
            {
                if (entry.upgradeType == slot.UpgradeTypeValue)
                {
                    slot.ApplySaveData(entry.level, entry.cost);
                    break;
                }
            }
        }

        if (machineShop != null && data.purchasedEquipmentNames != null)
        {
            machineShop.RestorePurchasedEquipments(data.purchasedEquipmentNames);
        }

        Debug.Log("로컬 JSON 저장 데이터를 불러왔습니다.");
    }
}