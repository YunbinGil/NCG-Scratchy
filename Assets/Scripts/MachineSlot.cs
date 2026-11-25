using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class MachineSlot : MonoBehaviour
{
    [Header("장비 정보")]
    public string equipmentName;
    public int equipmentPrice;
    public GameObject equipmentPrefab;

    [Header("UI")]
    public TMP_Text nameText;
    public TMP_Text priceText;
    public Button purchaseButton;
    public TMP_Text purchaseText;
    public Image equipmentImage;

    [Header("잠금 해제")]
    [SerializeField, Min(0)] private int requiredPlayerLevel;
    [SerializeField] private GameObject lockOverlay;

    [Header("스크래치 봇 해금 설정 (추가)")]
    [SerializeField] private bool isScratchBotSlot = false;
    [SerializeField] private bool requiresScratchBotUnlocked = false;
    [SerializeField] private int unlockLevel = 6;
    [SerializeField] private GameObject lockObject;

    [HideInInspector]
    public MachineShop equipmentShop;

    private GameManager playerProgress;
    private GameManager gameManager;
    private bool isLocked;
    private bool isPurchased;

    private int EffectiveRequiredLevel =>
        isScratchBotSlot ? Mathf.Max(requiredPlayerLevel, unlockLevel) : requiredPlayerLevel;

    private void Start()
    {
        if (nameText != null) nameText.text = equipmentName;
        if (priceText != null) priceText.text = equipmentPrice.ToString();
        if (purchaseText != null) purchaseText.text = string.Empty;

        gameManager = FindFirstObjectByType<GameManager>();
        playerProgress = equipmentShop != null && equipmentShop.PlayerProgress != null
            ? equipmentShop.PlayerProgress
            : gameManager;

        if (playerProgress != null)
            playerProgress.OnLevelChanged += UpdateLock;

        if (gameManager != null && gameManager != playerProgress)
        {
            gameManager.OnLevelChanged += UpdateLock;
            gameManager.OnScratchBotUnlockAvailable += HandleScratchBotUnlockAvailable;
        }
        else if (gameManager != null)
        {
            gameManager.OnScratchBotUnlockAvailable += HandleScratchBotUnlockAvailable;
        }

        if (requiresScratchBotUnlocked && ScratchBotUpgradeManager.Instance != null)
        {
            ScratchBotUpgradeManager.Instance.OnUpgraded += RefreshScratchBotLock;
        }

        if (purchaseButton != null)
        {
            purchaseButton.onClick.RemoveListener(Purchase);
            purchaseButton.onClick.AddListener(Purchase);
        }

        int currentLevel = playerProgress != null ? playerProgress.Level : 0;
        UpdateLock(currentLevel);
    }

    private void OnDestroy()
    {
        if (playerProgress != null)
            playerProgress.OnLevelChanged -= UpdateLock;

        if (gameManager != null)
        {
            gameManager.OnLevelChanged -= UpdateLock;
            gameManager.OnScratchBotUnlockAvailable -= HandleScratchBotUnlockAvailable;
        }

        if (ScratchBotUpgradeManager.Instance != null)
            ScratchBotUpgradeManager.Instance.OnUpgraded -= RefreshScratchBotLock;

        if (purchaseButton != null)
            purchaseButton.onClick.RemoveListener(Purchase);
    }

    private void HandleScratchBotUnlockAvailable()
    {
        RefreshScratchBotLock();
    }

    private bool IsBotActuallyUnlocked()
    {
        return ScratchBotUpgradeManager.Instance != null &&
               ScratchBotUpgradeManager.Instance.isBotUnlocked;
    }

    public void UpdateLock(int currentLevel)
    {
        if (isPurchased)
        {
            isLocked = false;
            ApplyLockVisuals();
            return;
        }

        bool levelLocked = currentLevel < EffectiveRequiredLevel;
        bool botDependencyLocked = requiresScratchBotUnlocked && !IsBotActuallyUnlocked();
        isLocked = levelLocked || botDependencyLocked;

        ApplyLockVisuals();
    }

    public void RefreshScratchBotLock()
    {
        int currentLevel = playerProgress != null ? playerProgress.Level : 0;
        UpdateLock(currentLevel);
    }

    private void ApplyLockVisuals()
    {
        if (lockOverlay != null)
            lockOverlay.SetActive(isLocked);

        if (lockObject != null)
            lockObject.SetActive(isLocked);

        if (purchaseButton != null)
            purchaseButton.interactable = !isPurchased && !isLocked;

        if (isPurchased && purchaseText != null)
            purchaseText.text = "Sold Out";
    }

    public void Purchase()
    {
        if (isPurchased || isLocked)
            return;

        if (equipmentShop == null)
        {
            Debug.LogWarning($"{name}: equipmentShop이 연결되지 않았습니다.");
            return;
        }

        if (!equipmentShop.PurchaseEquipment(equipmentPrice))
            return;

        isPurchased = true;

        if (purchaseText != null)
            purchaseText.text = "Sold Out";

        if (purchaseButton != null)
            purchaseButton.interactable = false;

        if (equipmentImage != null)
            equipmentImage.color = Color.gray;

        if (lockOverlay != null)
            lockOverlay.SetActive(false);

        if (lockObject != null)
            lockObject.SetActive(false);

        if (isScratchBotSlot)
        {
            if (ScratchBotUpgradeManager.Instance != null)
            {
                ScratchBotUpgradeManager.Instance.isBotUnlocked = true;
                ScratchBotUpgradeManager.Instance.NotifyUpgraded();
            }

            ItemSpawner spawner = FindFirstObjectByType<ItemSpawner>();
            spawner?.TrySpawnScratchBot();
        }
        else if (!requiresScratchBotUnlocked && equipmentPrefab != null)
        {
            equipmentShop.SpawnEquipment(equipmentPrefab);
        }

        Debug.Log(equipmentName + " 구매 완료!");
    }
}