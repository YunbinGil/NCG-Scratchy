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

    [HideInInspector]
    public MachineShop equipmentShop;

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
    }

    private void Purchase()
    {
        if (isPurchased || equipmentShop == null)
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