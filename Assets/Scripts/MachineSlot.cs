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
    internal MachineSlot UnlockWithSlot { get; set; }
    internal UnlockLotteryCatalog UnlockRevealPrefab { get; set; }
    internal bool IsReadyForUnlockReveal { get; private set; }
    internal bool IsLocked => isLocked;

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
    private bool isPurchased;

    private void Start()
    {
        upgradeSlot = GetComponent<ScratchBotUpgradeSlot>();
        if (nameText != null && !LocalizedLabel.HasKey(nameText))
            nameText.text = equipmentName;
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

        if (purchaseButton != null && upgradeSlot == null)
        {
            purchaseButton.onClick.RemoveListener(Purchase);
            purchaseButton.onClick.AddListener(Purchase);
        }

        int currentLevel = playerProgress != null ? playerProgress.Level : 0;
        UpdateLock(currentLevel);
        ApplyLockVisuals();
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
        bool botUnlocked = IsBotActuallyUnlocked();
        if (UnlockWithSlot != null && equipmentShop != null)
        {
            bool sourceUnlocked = equipmentShop.HasRevealedEquipment(UnlockWithSlot.equipmentName) ||
                                  (UnlockWithSlot.isScratchBotSlot && botUnlocked);
            IsReadyForUnlockReveal = false;
            isLocked = !isPurchased && !sourceUnlocked;
        }
        else
        {
            bool levelLocked = currentLevel < UnlockLevel;
            bool botDependencyLocked = requiresScratchBotUnlocked && !botUnlocked;
            bool needsReveal = UnlockRevealPrefab != null && equipmentShop != null &&
                               !(isScratchBotSlot && botUnlocked) && !equipmentShop.HasRevealedEquipment(equipmentName);
            IsReadyForUnlockReveal = !isPurchased && !levelLocked && !botDependencyLocked && needsReveal;
            isLocked = !isPurchased && (levelLocked || botDependencyLocked || needsReveal);
        }

        if (lockOverlay != null)
            lockOverlay.SetActive(isLocked);

        if (upgradeSlot != null)
            upgradeSlot.RefreshLock();
        else if (purchaseButton != null)
            purchaseButton.interactable = !isLocked && !isPurchased;
    }

    public void RefreshScratchBotLock()
    {
        int currentLevel = playerProgress != null ? playerProgress.Level : 0;
        UpdateLock(currentLevel);
    }

    private void ApplyLockVisuals()
    {
        if (!isScratchBotSlot)
            return;

        if (!LocalizedLabel.HasKey(nameText))
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

        if (isPurchased && purchaseText != null)
            purchaseText.text = "Sold Out";

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
            purchaseButton.interactable = !isPurchased && !isLocked;
            purchaseButton.onClick.RemoveListener(Purchase);
        }
    }

    public void Purchase()
    {
        if (isPurchased || isLocked || equipmentShop == null || upgradeSlot != null)
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
        Localization.Bind(purchaseText, "shop.sold_out", "Sold Out");
        purchaseButton.interactable = false;
        equipmentImage.color = Color.gray;

        if (purchaseButton != null)
            purchaseButton.interactable = false;

        if (equipmentImage != null)
            equipmentImage.color = Color.gray;

        if (lockOverlay != null)
            lockOverlay.SetActive(false);

        // if (lockObject != null)
        //     lockObject.SetActive(false);

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