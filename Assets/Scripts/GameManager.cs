using UnityEngine;
using System;

public class GameManager : MonoBehaviour
{
    private readonly int[] levelExpInfo =
    {
        3, 10, 50, 100, 1000, 2000, 10000, 50000
    };
    [SerializeField, Min(0)] private int startingMoney;

    private UiManager uiManager;
    public int Level { get; private set; }
    public int Exp { get; private set; }
    public int Money { get; private set; }

    // 레벨 상승 이벤트
    public event Action<int> OnLevelChanged;

    private void Awake()
    {
        uiManager = GetComponent<UiManager>();
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
            Exp = 0;

            // 레벨 상승 시 이벤트 발생
            OnLevelChanged?.Invoke(Level);
        }

        int requiredExp = Level < levelExpInfo.Length
            ? levelExpInfo[Level]
            : levelExpInfo[levelExpInfo.Length - 1];

        uiManager.SetLevelText(Exp, requiredExp);
    }
    private void Update()
    {

    }
}