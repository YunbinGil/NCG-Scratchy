using UnityEngine;

public class UpgradeUnlock : MonoBehaviour
{
    public static UpgradeUnlock Instance { get; private set; }

    private const string UnlockedPrefKey = "UpgradePanel_IsUnlocked";

    [SerializeField, Min(0)] private int requiredPlayerLevel = 1;
    [SerializeField] private UnlockLotteryCatalog unlockPrefab;
    [SerializeField] private Texture2D lightTexture;

    private GameManager gameManager;
    private ItemSpawner itemSpawner;

    private GameObject unlockItem;
    private bool isUnlocked;
    private bool suppressUnlockAnimation;

    public bool IsUnlocked => isUnlocked;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        isUnlocked = PlayerPrefs.GetInt(UnlockedPrefKey, 0) == 1;
        if (isUnlocked)
        {
            suppressUnlockAnimation = true;
            SetChildrenActive(true);
        }
        else
        {
            SetChildrenActive(false);
        }
    }

    private void Start()
    {
        if (gameManager == null)
            gameManager = FindFirstObjectByType<GameManager>();
        if (itemSpawner == null)
            itemSpawner = FindFirstObjectByType<ItemSpawner>();

        if (isUnlocked)
        {
            suppressUnlockAnimation = true;
            SetChildrenActive(true);
        }

        if (gameManager != null)
        {
            gameManager.OnLevelChanged -= CheckUnlockCondition;
            gameManager.OnLevelChanged += CheckUnlockCondition;
        }

        if (isUnlocked || suppressUnlockAnimation)
            return;

        if (gameManager == null || itemSpawner == null || unlockPrefab == null)
        {
            Debug.LogWarning("UpgradeUnlock의 해금 프리팹과 씬 참조를 확인하세요.", this);
            return;
        }

        CheckUnlockCondition(gameManager.Level);
    }

    public void ApplySavedUnlockState(bool unlocked)
    {
        isUnlocked = unlocked;
        suppressUnlockAnimation = unlocked;

        PlayerPrefs.SetInt(UnlockedPrefKey, unlocked ? 1 : 0);
        PlayerPrefs.Save();

        if (unlocked)
        {
            if (unlockItem != null)
            {
                Destroy(unlockItem);
                unlockItem = null;
            }

            SetChildrenActive(true);
        }
        else
        {
            SetChildrenActive(false);
        }
    }

    public void ResetUnlockStateForNewGame()
    {
        isUnlocked = false;
        suppressUnlockAnimation = false;
        PlayerPrefs.SetInt(UnlockedPrefKey, 0);
        PlayerPrefs.Save();

        if (unlockItem != null)
        {
            Destroy(unlockItem);
            unlockItem = null;
        }

        SetChildrenActive(false);
    }

    private void CheckUnlockCondition(int playerLevel)
    {
        if (isUnlocked || suppressUnlockAnimation || PlayerPrefs.GetInt(UnlockedPrefKey, 0) == 1 ||
            unlockItem != null || playerLevel < requiredPlayerLevel)
            return;

        if (itemSpawner == null || unlockPrefab == null)
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
        suppressUnlockAnimation = true;
        PlayerPrefs.SetInt(UnlockedPrefKey, 1);
        PlayerPrefs.Save();
        SetChildrenActive(true);
    }

    private void SetChildrenActive(bool active)
    {
        foreach (Transform child in transform)
        {
            if (child != null)
                child.gameObject.SetActive(active);
        }
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;

        if (gameManager != null)
            gameManager.OnLevelChanged -= CheckUnlockCondition;

        if (unlockItem != null)
            Destroy(unlockItem);
    }
}