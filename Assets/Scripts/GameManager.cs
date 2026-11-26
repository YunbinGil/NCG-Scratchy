using System;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    private readonly int[] levelExpInfo =
    {
        3, 10, 50, 100, 1000, 2000, 10000, 50000
    };

    [SerializeField, Min(10)] private int startingMoney;

    [Header("스크래치 봇 해금 상태")]
    [SerializeField] private int scratchBotUnlockLevel = 6;
    public bool CanUnlockScratchBot { get; private set; }
    public bool IsScratchBotUnlocked { get; private set; }
    public event Action OnScratchBotUnlockAvailable;

    private ItemSpawner itemSpawner;
    internal ItemSpawner ItemSpawner => itemSpawner;
    internal RectTransform TableEffectParent => itemSpawner != null ? itemSpawner.TableEffectParent : null;
    private UiManager uiManager;
    private bool hasRestoredPlayerState;

    public int Level { get; private set; }
    public int Exp { get; private set; }
    public int Money { get; private set; }

    public event Action<int> OnLevelChanged;
    internal UnlockEffect.Scheduler UnlockEffects { get; private set; }
    public PlayerUpgradeManager Upgrades { get; private set; }
    public float CoinSizeMultiplier { get; private set; } = 1f;
    public float CoinAlphaMultiplier { get; private set; } = 1f;
    public float LuckMultiplier { get; private set; } = 1f; //Lottery.RollWeightedIcons또는 Lottery.RollWeightedIcons에서 참조


    private void Awake()
    {
        Upgrades = new PlayerUpgradeManager(this);
        UnlockEffects = new UnlockEffect.Scheduler(this);
        uiManager = GetComponent<UiManager>();
        if (itemSpawner == null)
            itemSpawner = FindFirstObjectByType<ItemSpawner>();

        FindFirstObjectByType<UpgradeUnlock>(FindObjectsInactive.Include)?.Initialize(this);
    }

    private void Start()
    {
        if (hasRestoredPlayerState)
            return;

        SetMoney(startingMoney);
        Exp = 0;
        Level = 0;
        AddExp(0);
    }

    public void SetMoney(int amount)
    {
        Money = Mathf.Max(0, amount);
        if (uiManager != null)
            uiManager.SetMoneyText(Money);
    }

    public void AddMoney(int addAmount)
    {
        SetMoney(Money + addAmount);
    }

    public bool TrySpendMoney(int amount)
    {
        if (amount < 0 || amount > Money)
            return false;

        SetMoney(Money - amount);
        return true;
    }

    public void AddExp(int addExpAmount)
    {
        if (addExpAmount < 0)
            return;

        Exp += addExpAmount;

        if (Level < levelExpInfo.Length && Exp >= levelExpInfo[Level])
        {
            Level++;
            PhoneEventController.Instance?.CheckLevelEvents(Level);
            CheckScratchBotUnlock(Level);
            Exp = 0;

            OnLevelChanged?.Invoke(Level);
        }

        RefreshLevelUI();
    }

    public void ApplySaveData(
        int savedLevel,
        int savedExp,
        int savedMoney,
        bool canUnlockBot,
        bool isBotUnlockedByLevel)
    {
        hasRestoredPlayerState = true;
        Level = Mathf.Max(0, savedLevel);
        Exp = Mathf.Max(0, savedExp);
        CanUnlockScratchBot = canUnlockBot || Level >= scratchBotUnlockLevel;
        IsScratchBotUnlocked = isBotUnlockedByLevel || Level >= scratchBotUnlockLevel;

        SetMoney(savedMoney);
        RefreshLevelUI();

        if (CanUnlockScratchBot)
            OnScratchBotUnlockAvailable?.Invoke();

        UnlockEffects.SuppressAutomaticOnRestore();
        OnLevelChanged?.Invoke(Level);
    }

    private void LateUpdate()
    {
        UnlockEffects.Tick();
    }

    private void RefreshLevelUI()
    {
        int requiredExp = Level < levelExpInfo.Length
            ? levelExpInfo[Level]
            : levelExpInfo[levelExpInfo.Length - 1];

        if (uiManager != null)
            uiManager.SetLevelText(Exp, requiredExp);
    }

    private void CheckScratchBotUnlock(int currentLevel)
    {
        if (CanUnlockScratchBot || currentLevel < scratchBotUnlockLevel)
            return;

        CanUnlockScratchBot = true;
        IsScratchBotUnlocked = true;
        OnScratchBotUnlockAvailable?.Invoke();
    }

    private void Update()
    {
#if UNITY_EDITOR
        // F1: 현재 레벨 필요 경험치만큼 한 번에 추가 (연타하면 6레벨까지 금방 도달)
        if (Input.GetKeyDown(KeyCode.F1))
        {
            int requiredExp = Level < levelExpInfo.Length
                ? levelExpInfo[Level]
                : levelExpInfo[levelExpInfo.Length - 1];
            AddExp(requiredExp - Exp);
        }

        // F2: 업그레이드 테스트용 돈 추가
        if (Input.GetKeyDown(KeyCode.F2))
        {
            AddMoney(100000);
        }
#endif
    }
    public void UpgradeCursorSize(int level)
    {
        CoinSizeMultiplier = 1f + 0.2f * level;
    }

    public void UpgradeLeadCoinAlpha(int level)
    {
        CoinAlphaMultiplier = 1f + 0.25f * level;
    }

    public void UpgradeLuck()
    {
        LuckMultiplier += 0.1f;
    }
}