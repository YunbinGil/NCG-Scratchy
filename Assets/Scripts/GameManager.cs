using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    private readonly int[] levelExpInfo =
    {
        3, 10, 50, 100, 1000, 2000, 10000, 50000
    };

    [SerializeField, Min(10)] private int startingMoney;

    [Header("엔딩")]
    [SerializeField, Min(1)] private int endingMoneyGoal = 1000000;
    private bool isEndingLocked;
    private float timeScaleBeforeEnding;

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
    private float coinSizeUpgradeMultiplier = 1f;
    public float CoinSizeMultiplier => coinSizeUpgradeMultiplier *
        ((Upgrades?.GetLevel(UpgradeType.Kind) ?? 0) <= 1 ? 0.5f : 1f);
    public float CoinAlphaMultiplier { get; private set; } = 1f;
    public float LuckMultiplier { get; private set; } = 1f;


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
        if (isEndingLocked)
            return;

        // Let payout and saved-state restoration finish before locking gameplay.
        if (endingMoneyGoal > 0 && Money >= endingMoneyGoal &&
            PhoneEventController.Instance != null &&
            PhoneEventController.Instance.TryRingEndingCall(out PhoneCallTrigger phone))
        {
            LockForEndingCall(phone);
            return;
        }

        UnlockEffects.Tick();
    }

    private void LockForEndingCall(PhoneCallTrigger phone)
    {
        isEndingLocked = true;
        FindFirstObjectByType<PausePanelController>()?.ContinueGame();
        FindFirstObjectByType<InspectViewManager>()?.CloseInspect();
        var dialogue = DialogueManager.Instance;
        dialogue?.DismissForEndingCall();

        if (EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(null);

        foreach (Graphic graphic in FindObjectsByType<Graphic>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (!graphic.transform.IsChildOf(phone.transform) &&
                (dialogue == null || !dialogue.OwnsDialogueGraphic(graphic)))
                graphic.raycastTarget = false;
        }

        foreach (Selectable control in FindObjectsByType<Selectable>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (control.gameObject != phone.gameObject)
                control.interactable = false;
        }

        DisableGameplayUpdates<PhysicsRaycaster>();
        DisableGameplayUpdates<PausePanelController>();
        DisableGameplayUpdates<Scratchable>();
        DisableGameplayUpdates<ScratchBot>();
        DisableGameplayUpdates<ItemSpawner>();

        phone.SetPaused(false);
        timeScaleBeforeEnding = Time.timeScale;
        Time.timeScale = 0f;
    }

    private static void DisableGameplayUpdates<T>() where T : Behaviour
    {
        foreach (T behaviour in FindObjectsByType<T>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            behaviour.enabled = false;
    }

    private void OnDestroy()
    {
        if (isEndingLocked)
            Time.timeScale = timeScaleBeforeEnding;
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
        if (isEndingLocked)
            return;

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
        coinSizeUpgradeMultiplier = 1f + 0.2f * level;
    }

    public void UpgradeLeadCoinAlpha(int level)
    {
        CoinAlphaMultiplier = 1f + 0.25f * level;
    }

    public void UpgradeLuck(int level)
    {
        LuckMultiplier = 1f + 0.1f * level;
    }
}