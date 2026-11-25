using System;
using UnityEngine;

public enum BotUpgradeType
{
    UnlockBot,  // 스크래치 봇 본체 해금
    Speed,      // 긁는 속도
    Capacity,   // 동시 수용 용량
    Power       // 한 번에 긁는 강도
}

public class ScratchBotUpgradeManager : MonoBehaviour
{
    public static ScratchBotUpgradeManager Instance { get; private set; }

    public event Action OnUpgraded;

    [Header("해금 및 레벨 상태")]
    public bool isBotUnlocked = false;
    public int speedLevel = 1;
    public int capacityLevel = 1;
    public int powerLevel = 1;

    [Header("최대 레벨 설정 (Speed / Capacity / Power)")]
    [SerializeField] private int maxUpgradeLevel = 5;
    public int MaxUpgradeLevel => maxUpgradeLevel;

    [Header("기본 가격 설정")]
    [SerializeField] private double unlockBaseCost = 1000;
    [SerializeField] private double speedBaseCost = 5000;
    [SerializeField] private double capacityBaseCost = 2500;
    [SerializeField] private double powerBaseCost = 2000;

    [Header("레벨당 가격 증가 배율")]
    [SerializeField] private double costGrowthRate = 5;

    [Header("레벨당 스탯 계산식 설정")]
    [SerializeField] private float baseInterval = 0.5f;      // 기본 긁기 간격(초)
    [SerializeField] private float speedReductionPerLv = 0.03f;
    [SerializeField] private int baseCapacity = 1;           // 기본 수용 복권 수
    [SerializeField] private float baseScratchPower = 0.15f;  // 1회당 긁히는 진행도
    [SerializeField] private float powerGainPerLv = 0.08f;

    [Header("외부 매니저 참조")]
    [SerializeField] private GameManager gameManager;
    [SerializeField] private ItemSpawner itemSpawner;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        if (gameManager == null)
            gameManager = FindFirstObjectByType<GameManager>();

        if (itemSpawner == null)
            itemSpawner = FindFirstObjectByType<ItemSpawner>();
    }

    // 현재 적용되는 실제 스탯 프로퍼티 (여러 스크래치 봇이 공통 참조)
    public float CurrentScratchInterval => Mathf.Max(0.05f, baseInterval - (speedLevel - 1) * speedReductionPerLv);
    public int CurrentMaxCapacity => baseCapacity + (capacityLevel - 1);
    public float CurrentScratchPower => baseScratchPower + (powerLevel - 1) * powerGainPerLv;

    public bool IsMaxLevel(BotUpgradeType type)
    {
        return type != BotUpgradeType.UnlockBot && GetLevel(type) >= maxUpgradeLevel;
    }

    public int GetLevel(BotUpgradeType type)
    {
        return type switch
        {
            BotUpgradeType.UnlockBot => isBotUnlocked ? 1 : 0,
            BotUpgradeType.Speed => speedLevel,
            BotUpgradeType.Capacity => capacityLevel,
            BotUpgradeType.Power => powerLevel,
            _ => 0
        };
    }

    public double GetCost(BotUpgradeType type)
    {
        if (type == BotUpgradeType.UnlockBot)
            return unlockBaseCost;

        double baseCost = type switch
        {
            BotUpgradeType.Speed => speedBaseCost,
            BotUpgradeType.Capacity => capacityBaseCost,
            BotUpgradeType.Power => powerBaseCost,
            _ => 0
        };

        int level = GetLevel(type);
        return level <= 0 ? baseCost : baseCost * Math.Pow(costGrowthRate, level - 1);
    }

    public bool TryPurchaseUpgrade(BotUpgradeType type)
    {
        if (type == BotUpgradeType.UnlockBot)
        {
            if (isBotUnlocked)
                return false;

            if (gameManager != null && !gameManager.CanUnlockScratchBot && gameManager.Level < 6)
                return false;
        }
        else
        {
            if (!isBotUnlocked || IsMaxLevel(type))
                return false;

            if (itemSpawner != null && itemSpawner.SpawnedScratchBot == null)
                return false;
        }

        double cost = GetCost(type);
        if (!TrySpendPlayerMoney(cost))
            return false;

        switch (type)
        {
            case BotUpgradeType.UnlockBot:
                isBotUnlocked = true;
                itemSpawner?.TrySpawnScratchBot();
                break;
            case BotUpgradeType.Speed:
                speedLevel = Mathf.Min(maxUpgradeLevel, speedLevel + 1);
                break;
            case BotUpgradeType.Capacity:
                capacityLevel = Mathf.Min(maxUpgradeLevel, capacityLevel + 1);
                break;
            case BotUpgradeType.Power:
                powerLevel = Mathf.Min(maxUpgradeLevel, powerLevel + 1);
                break;
        }

        NotifyUpgraded();
        return true;
    }

    public void NotifyUpgraded()
    {
        OnUpgraded?.Invoke();
    }

    private bool TrySpendPlayerMoney(double amount)
    {
        if (gameManager == null)
            return true;

        int spendAmount = Mathf.CeilToInt((float)amount);
        return gameManager.TrySpendMoney(spendAmount);
    }

    public static string FormatNumber(double num)
    {
        string[] units = { "", "K", "M", "B", "T", "Qa", "Qi" };
        int unitIndex = 0;

        while (num >= 1000 && unitIndex < units.Length - 1)
        {
            num /= 1000;
            unitIndex++;
        }

        return unitIndex == 0 ? $"$ {num:F0}" : $"$ {num:F2} {units[unitIndex]}";
    }

    public void SetUpgradeLevel(BotUpgradeType type, int level)
    {
        int clamped = Mathf.Clamp(level, 1, maxUpgradeLevel);

        switch (type)
        {
            case BotUpgradeType.Speed:
                speedLevel = clamped;
                break;
            case BotUpgradeType.Capacity:
                capacityLevel = clamped;
                break;
            case BotUpgradeType.Power:
                powerLevel = clamped;
                break;
        }
    }
}