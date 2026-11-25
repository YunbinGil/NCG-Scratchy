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

    [HideInInspector]
    public MachineShop equipmentShop;

    private GameManager playerProgress;
    private bool isLocked;
    private bool isPurchased = false;

    private void Start()
    {
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

    private void OnDestroy()
    {
        if (playerProgress != null)
            playerProgress.OnLevelChanged -= UpdateLock;

        if (purchaseButton != null)
            purchaseButton.onClick.RemoveListener(Purchase);
    }

    private void Purchase()
    {
        if (isLocked || isPurchased || equipmentShop == null)
            return;

        if (equipmentShop.PurchaseEquipment(equipmentPrice))
        {
            isPurchased = true;

            Localization.Bind(purchaseText, "shop.sold_out", "Sold Out");
            purchaseButton.interactable = false;
            equipmentImage.color = Color.gray;

            equipmentShop.SpawnEquipment(equipmentPrefab);

            Debug.Log(equipmentName + " 구매 완료!");
        }
    }
}
