using UnityEngine;
using System;

public class GameManager : MonoBehaviour
{
    private readonly int[] levelExpInfo =
    {
        3, 10, 50, 100, 1000, 2000, 10000, 50000
    };
    [SerializeField, Min(0)] private int startingMoney;

    [Header("스크래치 봇 해금 상태")]
    [SerializeField] private int scratchBotUnlockLevel = 6;
    public bool CanUnlockScratchBot { get; private set; }
    public event Action OnScratchBotUnlockAvailable;

    public bool IsScratchBotUnlocked { get; private set; }

    private ItemSpawner itemSpawner;
    private UiManager uiManager;
    public int Level { get; private set; }
    public int Exp { get; private set; }
    public int Money { get; private set; }

    // 레벨 상승 이벤트
    public event Action<int> OnLevelChanged;

    private void Awake()
    {
        uiManager = GetComponent<UiManager>();
        if (itemSpawner == null)
            itemSpawner = FindFirstObjectByType<ItemSpawner>();
    }
    private void Start()
    {
        SetMoney(startingMoney);
        Exp = 0;
        Level = 0;
        AddExp(0);
    }

    public void SetMoney(int amount)
    {
        Money = amount;
        uiManager.SetMoneyText(amount);
    }
    public void AddMoney(int add)
    {
        SetMoney(Money + add);
    }
    public bool TrySpendMoney(int amount)
    {
        if (amount < 0 || amount > Money) return false;
        SetMoney(Money - amount);
        return true;
    }
    public void AddExp(int add)
    {
        if (add < 0) return;

        Exp += add;

        if (Level < levelExpInfo.Length && Exp >= levelExpInfo[Level])
        {
            Level++;
            PhoneEventController.Instance?.CheckLevelEvents(Level);
            CheckScratchBotUnlock(Level);
            Exp = 0;

            // 레벨 상승 시 이벤트 발생
            OnLevelChanged?.Invoke(Level);
        }

        int requiredExp = Level < levelExpInfo.Length
            ? levelExpInfo[Level]
            : levelExpInfo[levelExpInfo.Length - 1];

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
            int req = Level < levelExpInfo.Length
                ? levelExpInfo[Level]
                : levelExpInfo[levelExpInfo.Length - 1];
            AddExp(req - Exp);
        }

        // F2: 업그레이드 테스트용 돈 추가
        if (Input.GetKeyDown(KeyCode.F2))
        {
            AddMoney(100000);
        }
#endif
    }
}