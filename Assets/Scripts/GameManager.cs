using UnityEngine;

public class GameManager : MonoBehaviour
{
    [SerializeField, Min(0)] private int startingMoney;

    private UiManager uiManager;
    public int Level {get; private set;}
    public int Exp {get; private set;}
    public int Money {get; private set;}
    
    private void Awake()
    {
        uiManager = GetComponent<UiManager>();
    }
    private void Start()
    {
        Level = 0;
        SetMoney(Mathf.Max(0, startingMoney));
    }

    public bool TrySpendMoney(int amount)
    {
        if (amount < 0 || amount > Money)
            return false;

        SetMoney(Money - amount);
        return true;
    }

    public void RefundMoney(int amount)
    {
        SetMoney(Money + amount);
    }

    private void SetMoney(int amount)
    {
        Money = amount;
        if (uiManager != null)
            uiManager.SetMoneyText(Money);
    }

    // Update is called once per frame
    private void Update()
    {
        
    }
}