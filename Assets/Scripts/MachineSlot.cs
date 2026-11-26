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

    internal bool IsLocked => isLocked;

    [Header("스크래치 봇 해금 설정 (추가)")]
    [SerializeField] private bool isScratchBotSlot = false;
    [SerializeField] private bool requiresScratchBotUnlocked = false;
    [SerializeField] private GameObject lockObject;

    [HideInInspector]
    public MachineShop equipmentShop;

    private GameManager playerProgress;
    private UnlockEffect unlock;
    private MachineSlot linkedSlot;
    private UnlockEffect.Registration unlockRegistration;
    private ScratchBotUpgradeSlot upgradeSlot;
    private bool isLocked;
    private bool isPurchased;

    internal void Initialize(MachineShop shop)
    {
        equipmentShop = shop;
        playerProgress = shop.PlayerProgress;
        unlock = GetComponent<UnlockEffect>();
        linkedSlot = unlock != null && unlock.UnlockWith != null
            ? unlock.UnlockWith.GetComponent<MachineSlot>()
            : null;
        upgradeSlot = GetComponent<ScratchBotUpgradeSlot>();

        if (unlock != null && linkedSlot == null && playerProgress != null)
        {
            unlockRegistration = playerProgress.UnlockEffects.Watch(
                this, unlock, () => shop.RecordUnlock(equipmentName),
                () => !requiresScratchBotUnlocked || IsBotActuallyUnlocked(),
                () => isPurchased || shop.HasRevealedEquipment(equipmentName) ||
                      (isScratchBotSlot && IsBotActuallyUnlocked()));
        }
    }

    private void Start()
    {
        upgradeSlot = GetComponent<ScratchBotUpgradeSlot>();
        if (nameText != null && !LocalizedLabel.HasKey(nameText))
            nameText.text = equipmentName;
        if (priceText != null) priceText.text = equipmentPrice.ToString();
        if (purchaseText != null) purchaseText.text = string.Empty;

        if (equipmentShop == null)
        {
            playerProgress = FindFirstObjectByType<GameManager>();
            unlock = GetComponent<UnlockEffect>();
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

    private bool IsBotActuallyUnlocked()
    {
        return ScratchBotUpgradeManager.Instance != null &&
               ScratchBotUpgradeManager.Instance.isBotUnlocked;
    }

    public void UpdateLock(int currentLevel)
    {
        bool botUnlocked = IsBotActuallyUnlocked();
        if (linkedSlot != null && equipmentShop != null)
        {
            bool sourceUnlocked = equipmentShop.HasRevealedEquipment(linkedSlot.equipmentName) ||
                                  (linkedSlot.isScratchBotSlot && botUnlocked);
            isLocked = !isPurchased && !sourceUnlocked;
        }
        else
        {
            bool levelLocked = unlock != null && !unlock.MeetsLevel(currentLevel);
            bool botDependencyLocked = requiresScratchBotUnlocked && !botUnlocked;
            bool awaitingConfirmation = unlock != null && unlock.RequiresConfirmation && equipmentShop != null &&
                                        !(isScratchBotSlot && botUnlocked) && !equipmentShop.HasRevealedEquipment(equipmentName);
            isLocked = !isPurchased && (levelLocked || botDependencyLocked || awaitingConfirmation);
        }

        if (lockOverlay != null)
            lockOverlay.SetActive(isLocked);

        if (upgradeSlot != null)
            upgradeSlot.RefreshLock();
        else if (purchaseButton != null)
            purchaseButton.interactable = !isLocked && !isPurchased;
    }

    private void ApplyLockVisuals()
    {
        if (!isScratchBotSlot)
            return;

        if (!LocalizedLabel.HasKey(nameText))
            Localization.Bind(nameText, $"machine.{(equipmentName ?? string.Empty).ToLowerInvariant()}", equipmentName);
        Localization.BindAuthoredLabels(gameObject);

    }

    private void OnDestroy()
    {
        unlockRegistration?.Cancel();
        if (purchaseButton != null)
            purchaseButton.onClick.RemoveListener(Purchase);
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