using UnityEngine;

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
            PhoneEventController.Instance?.CheckLevelEvents(Level);
            Exp = 0;
        }
        uiManager.SetLevelText(Exp, levelExpInfo[Level]);
    }
    private void Update()
    {

    }
}