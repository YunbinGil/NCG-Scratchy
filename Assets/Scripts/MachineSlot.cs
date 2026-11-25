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

    [Header("스크래치 봇 해금 설정 (추가)")]
    [SerializeField] private bool isScratchBotSlot = false;
    [SerializeField] private bool requiresScratchBotUnlocked = false;
    [SerializeField] private int unlockLevel = 6;
    [SerializeField] private GameObject lockObject;
    [Header("잠금 해제")]
    [SerializeField, Min(0)] private int requiredPlayerLevel;
    [SerializeField] private GameObject lockOverlay;

    [HideInInspector]
    public MachineShop equipmentShop;

    private GameManager playerProgress;
    private bool isLocked;
    private bool isPurchased = false;
    private GameManager gameManager;

    private void Start()
    {
        if (nameText != null) nameText.text = equipmentName;
        if (priceText != null) priceText.text = equipmentPrice.ToString();
        if (purchaseText != null) purchaseText.text = string.Empty;

        gameManager = FindFirstObjectByType<GameManager>();
        if (gameManager != null)
        {
            gameManager.OnLevelChanged += HandleLevelChanged;
            gameManager.OnScratchBotUnlockAvailable += RefreshScratchBotLock;
        }

        if (requiresScratchBotUnlocked && ScratchBotUpgradeManager.Instance != null)
        {
            ScratchBotUpgradeManager.Instance.OnUpgraded += RefreshScratchBotLock;
        }

        RefreshScratchBotLock();

        if (purchaseButton != null)
            purchaseButton.onClick.AddListener(Purchase);
    }

    private void OnDestroy()
    {
        if (gameManager != null)
        {
            gameManager.OnLevelChanged -= HandleLevelChanged;
            gameManager.OnScratchBotUnlockAvailable -= RefreshScratchBotLock;
        }

        if (ScratchBotUpgradeManager.Instance != null)
        {
            ScratchBotUpgradeManager.Instance.OnUpgraded -= RefreshScratchBotLock;
        }
    }

    private void HandleLevelChanged(int _)
    {
        RefreshScratchBotLock();
    }

    private bool IsBotActuallyUnlocked()
    {
        return ScratchBotUpgradeManager.Instance != null &&
               ScratchBotUpgradeManager.Instance.isBotUnlocked;
    }

    public void RefreshScratchBotLock()
    {
        if (isPurchased)
            return;

        if (requiresScratchBotUnlocked)
        {
            bool botUnlocked = IsBotActuallyUnlocked();

            if (lockObject != null)
                lockObject.SetActive(!botUnlocked);

            if (purchaseButton != null)
                purchaseButton.interactable = botUnlocked;
            return;
        }

        if (!isScratchBotSlot)
            return;

        int currentLv = gameManager != null ? gameManager.Level : 0;
        bool canUnlock = currentLv >= unlockLevel ||
            (gameManager != null && gameManager.CanUnlockScratchBot);

        if (lockObject != null)
            lockObject.SetActive(!canUnlock);

        if (purchaseButton != null)
            purchaseButton.interactable = canUnlock;
        Localization.Bind(nameText, $"machine.{(equipmentName ?? string.Empty).ToLowerInvariant()}", equipmentName);
        priceText.text = equipmentPrice.ToString();
        purchaseText.text = string.Empty;
        Localization.BindAuthoredLabels(gameObject);

        playerProgress = equipmentShop != null ? equipmentShop.PlayerProgress : null;
        if (playerProgress != null)
            playerProgress.OnLevelChanged += UpdateLock;

        UpdateLock(playerProgress != null ? playerProgress.Level : 0);
        purchaseButton.onClick.AddListener(Purchase);
    }

    private void UpdateLock(int playerLevel)
    {
        isLocked = playerLevel < requiredPlayerLevel;

        if (lockOverlay != null)
            lockOverlay.SetActive(isLocked);

        purchaseButton.interactable = !isLocked && !isPurchased;
    }

    // private void OnDestroy()
    // {
    //     if (playerProgress != null)
    //         playerProgress.OnLevelChanged -= UpdateLock;

    //     if (purchaseButton != null)
    //         purchaseButton.onClick.RemoveListener(Purchase);
    // }

    private void Purchase()
    {
        if (isPurchased || equipmentShop == null)
        if (isLocked || isPurchased || equipmentShop == null)
            return;

        if (requiresScratchBotUnlocked && !IsBotActuallyUnlocked())
            return;

        if (isScratchBotSlot)
        {
            int currentLv = gameManager != null ? gameManager.Level : 0;
            bool canUnlock = currentLv >= unlockLevel ||
                (gameManager != null && gameManager.CanUnlockScratchBot);
            if (!canUnlock)
                return;
        }

        if (equipmentShop.PurchaseEquipment(equipmentPrice))
        {
            isPurchased = true;

            if (purchaseText != null)
                purchaseText.text = "Sold Out";
            Localization.Bind(purchaseText, "shop.sold_out", "Sold Out");
            purchaseButton.interactable = false;
            equipmentImage.color = Color.gray;

            if (purchaseButton != null)
                purchaseButton.interactable = false;

            if (equipmentImage != null)
                equipmentImage.color = Color.gray;

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
            else if (!requiresScratchBotUnlocked)
            {
                equipmentShop.SpawnEquipment(equipmentPrefab);
            }

            Debug.Log(equipmentName + " 구매 완료!");
        }
    }
}