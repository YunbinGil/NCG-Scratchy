using UnityEngine;

public class GameManager : MonoBehaviour
{
    private UiManager uiManager;
    private void Awake()
    {
        uiManager = GameObject.FindWithTag("GameController").GetComponent<UiManager>();
    }
}