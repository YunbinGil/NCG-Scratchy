using UnityEngine;

public class UpgradeUnlock : MonoBehaviour
{
    [SerializeField, Min(0)] private int requiredPlayerLevel = 1;
    [SerializeField] private UnlockLotteryCatalog unlockPrefab;
    [SerializeField] private Texture2D lightTexture;
    private GameManager gameManager;
    private ItemSpawner itemSpawner;

    private GameObject unlockItem;
    private bool isUnlocked;

    private void Awake()
    {
        SetChildrenActive(false);
    }

    private void Start()
    {
        if (gameManager == null)
            gameManager = FindFirstObjectByType<GameManager>();
        if (itemSpawner == null)
            itemSpawner = FindFirstObjectByType<ItemSpawner>();

        if (gameManager == null || itemSpawner == null || unlockPrefab == null)
        {
            Debug.LogWarning("UpgradeUnlock의 해금 프리팹과 씬 참조를 확인하세요.", this);
            return;
        }

        gameManager.OnLevelChanged += CheckUnlockCondition;
        CheckUnlockCondition(gameManager.Level);
    }

    private void CheckUnlockCondition(int playerLevel)
    {
        if (isUnlocked || unlockItem != null || playerLevel < requiredPlayerLevel)
            return;

        if (itemSpawner.TrySpawnItemAtTableCenter(unlockPrefab.gameObject, out unlockItem))
        {
            unlockItem.GetComponent<UnlockLotteryCatalog>()
                .Initialize(lightTexture, CompleteUnlock, "Upgrades Unlocked!");
        }
    }

    private void CompleteUnlock()
    {
        isUnlocked = true;
        SetChildrenActive(true);
    }

    private void SetChildrenActive(bool active)
    {
        foreach (Transform child in transform)
            child.gameObject.SetActive(active);
    }

    private void OnDestroy()
    {
        if (gameManager != null)
            gameManager.OnLevelChanged -= CheckUnlockCondition;

        if (unlockItem != null)
            Destroy(unlockItem);
    }
}
