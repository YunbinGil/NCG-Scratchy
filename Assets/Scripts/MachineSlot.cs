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
    [SerializeField] private GameObject lockOverlay;

    internal int UnlockLevel { get; set; }
    internal UnlockLotteryCatalog UnlockRevealPrefab { get; set; }
    internal bool IsReadyForUnlockReveal { get; private set; }
    internal bool IsScratchBotSlot => isScratchBotSlot;

    [Header("스크래치 봇 해금 설정 (추가)")]
    [SerializeField] private bool isScratchBotSlot = false;
    [SerializeField] private bool requiresScratchBotUnlocked = false;
    [SerializeField] private GameObject lockObject;

    [HideInInspector]
    public MachineShop equipmentShop;

    private GameManager playerProgress;
    private GameManager gameManager;
    private ScratchBotUpgradeSlot upgradeSlot;
    private bool isLocked;
    internal bool IsLocked => isLocked;
    private bool isPurchased;

    public string EffectiveEquipmentName =>
        !string.IsNullOrWhiteSpace(equipmentName)
            ? equipmentName
            : (equipmentPrefab != null ? equipmentPrefab.name : gameObject.name);

    private void Start()
    {
        upgradeSlot = GetComponent<ScratchBotUpgradeSlot>();
        if (nameText != null) nameText.text = equipmentName;
        if (priceText != null) priceText.text = equipmentPrice.ToString();
        if (purchaseText != null && !isPurchased) purchaseText.text = string.Empty;

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

        if (purchaseButton != null && upgradeSlot == null)
        {
            purchaseButton.onClick.RemoveListener(Purchase);
            purchaseButton.onClick.AddListener(Purchase);
        }

        if (equipmentShop != null && equipmentShop.IsEquipmentPurchased(EffectiveEquipmentName))
        {
            isPurchased = true;
        }

        int currentLevel = playerProgress != null ? playerProgress.Level : 0;
        UpdateLock(currentLevel);
        ApplyLockVisuals();

        if (isPurchased)
        {
            ApplyPurchasedState(true);
        }
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
            IsReadyForUnlockReveal = false;
            isLocked = false;

            if (lockOverlay != null)
                lockOverlay.SetActive(false);

            if (purchaseButton != null && upgradeSlot == null)
                purchaseButton.interactable = false;

            return;
        }

        bool levelLocked = currentLevel < UnlockLevel;
        bool botUnlocked = IsBotActuallyUnlocked();
        bool botDependencyLocked = requiresScratchBotUnlocked && !botUnlocked;
        bool needsReveal = UnlockRevealPrefab != null && equipmentShop != null &&
                           !(isScratchBotSlot && botUnlocked) && !equipmentShop.HasRevealedEquipment(EffectiveEquipmentName);
        IsReadyForUnlockReveal = !levelLocked && !botDependencyLocked && needsReveal;
        isLocked = levelLocked || botDependencyLocked || needsReveal;

        if (lockOverlay != null)
            lockOverlay.SetActive(isLocked);

        if (upgradeSlot != null)
            upgradeSlot.RefreshLock();
        else if (purchaseButton != null)
            purchaseButton.interactable = !isLocked;
    }

    public void RefreshScratchBotLock()
    {
        int currentLevel = playerProgress != null ? playerProgress.Level : 0;
        UpdateLock(currentLevel);
    }

    public void ApplyPurchasedState(bool purchased)
    {
        isPurchased = purchased;
        if (!purchased)
            return;

        isLocked = false;
        IsReadyForUnlockReveal = false;

        if (purchaseText != null)
        {
            purchaseText.text = "Sold Out";
            Localization.Bind(purchaseText, "shop.sold_out", "Sold Out");
        }

        if (purchaseButton != null)
            purchaseButton.interactable = false;

        if (equipmentImage != null)
            equipmentImage.color = Color.gray;

        if (lockOverlay != null)
            lockOverlay.SetActive(false);

        if (lockObject != null)
            lockObject.SetActive(false);
    }

    private void ApplyLockVisuals()
    {
        if (!isScratchBotSlot)
            return;

        Localization.Bind(nameText, $"machine.{(equipmentName ?? string.Empty).ToLowerInvariant()}", equipmentName);
        Localization.BindAuthoredLabels(gameObject);

        if (equipmentShop != null && equipmentShop.PlayerProgress != null && equipmentShop.PlayerProgress != playerProgress)
        {
            equipmentShop.PlayerProgress.OnLevelChanged -= UpdateLock;
            equipmentShop.PlayerProgress.OnLevelChanged += UpdateLock;
        }
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
        {
            purchaseButton.onClick.RemoveListener(Purchase);
        }
    }

    public void Purchase()
    {
        if (isPurchased || isLocked || equipmentShop == null || upgradeSlot != null)
            return;

        if (!equipmentShop.PurchaseEquipment(equipmentPrice))
            return;

        ApplyPurchasedState(true);

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

        equipmentShop.RecordPurchasedEquipment(EffectiveEquipmentName, equipmentPrefab, isScratchBotSlot);

        Debug.Log(EffectiveEquipmentName + " 구매 완료!");
    }
}