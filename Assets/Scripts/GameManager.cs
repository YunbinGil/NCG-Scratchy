using UnityEngine;

public class GameManager : MonoBehaviour
{
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
        Money = 0;
    }

    // Update is called once per frame
    private void Update()
    {
        
    }
}