using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CatalogSlot : MonoBehaviour
{
    [SerializeField] private string ticketName;
    [SerializeField, Min(0)] private int price;
    [SerializeField] private GameObject ticketPrefab;

    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text priceText;
    [SerializeField] private Button purchaseButton;

    [Header("레벨 관리")]
    [SerializeField] private RectMask2D experienceMask;
    [SerializeField] private Image experienceFill;
    [SerializeField] private TMP_Text levelText;
    [SerializeField] private string productId;
    [SerializeField, Min(0)] private int experiencePerPurchase = 1;
    [SerializeField, Min(0)] private int experiencePerLevel = 3;
    [SerializeField, Min(0)] private int experienceIncreasePerLevel = 2;
    [SerializeField, Min(0)] private int maxLevel = 10;

    [Header("잠금 해제")]
    [SerializeField, Min(0)] private int requiredPlayerLevel;
    [SerializeField] private GameObject lockOverlay;

    private CatalogShop shop;
    private GameManager playerProgress;
    private bool isLocked;
    private ProductProgress productProgress;
    private string activeProductId;
    private float experienceRatio;

    public void Initialize(CatalogShop ticketShop)
    {
        if (playerProgress != null)
            playerProgress.OnLevelChanged -= UpdateLock;

        shop = ticketShop;
        playerProgress = shop != null ? shop.PlayerProgress : null;

        if (nameText != null)
            Localization.Bind(nameText, $"shop.item.{(ticketName ?? string.Empty).ToLowerInvariant()}", ticketName);

        if (priceText != null)
            priceText.text = price.ToString();

        if (purchaseButton == null)
            purchaseButton = GetComponent<Button>();

        if (purchaseButton != null)
        {
            purchaseButton.onClick.RemoveListener(Purchase);
            purchaseButton.onClick.AddListener(Purchase);
        }

        if (playerProgress != null)
            playerProgress.OnLevelChanged += UpdateLock;

        UpdateLock(playerProgress != null ? playerProgress.Level : 0);
        ConnectProgress();
    }

    private void ConnectProgress()
    {
        if (productProgress != null)
            productProgress.ProgressChanged -= UpdateProgress;

        productProgress = null;
        if (experienceMask == null || shop == null || ticketPrefab == null)
            return;

        activeProductId = string.IsNullOrWhiteSpace(productId) ? $"lottery:{ticketPrefab.name}" : productId;
        productProgress = FindFirstObjectByType<ProductProgress>();
        if (productProgress == null)
            productProgress = shop.gameObject.AddComponent<ProductProgress>();

        productProgress.ProgressChanged += UpdateProgress;
        UpdateProgress(activeProductId, productProgress.GetLevel(activeProductId),
            productProgress.GetExperience(activeProductId));
    }

    private void UpdateLock(int playerLevel)
    {
        isLocked = playerLevel < requiredPlayerLevel;

        if (lockOverlay != null)
            lockOverlay.SetActive(isLocked);

        if (purchaseButton != null)
            purchaseButton.interactable = !isLocked;
    }

    private void Purchase()
    {
        if (isLocked || shop == null || !shop.TryPurchase(ticketPrefab, price))
            return;

        if (productProgress != null)
        {
            int level = productProgress.GetLevel(activeProductId);
            productProgress.AddExperience(activeProductId, experiencePerPurchase, GetRequiredExperience(level), maxLevel);
        }
    }

    private int GetRequiredExperience(int level)
    {
        long requiredExperience = experiencePerLevel + (long)level * experienceIncreasePerLevel;
        return (int)System.Math.Min(requiredExperience, int.MaxValue);
    }

    private void UpdateProgress(string changedProductId, int level, int experience)
    {
        if (changedProductId != activeProductId)
            return;

        experienceRatio = level >= maxLevel ? 1f : Mathf.Clamp01((float)experience / GetRequiredExperience(level));
        if (levelText != null)
            Localization.Bind(levelText, "status.level", "Lv. {0}", level);

        UpdateMask();
    }

    private void OnRectTransformDimensionsChange()
    {
        if (productProgress != null)
            UpdateMask();
    }

    private void UpdateMask()
    {
        if (experienceFill != null)
            experienceFill.enabled = experienceRatio > 0f;

        float width = experienceMask.rectTransform.rect.width;
        Vector4 padding = experienceMask.padding;
        padding.z = width * (1f - experienceRatio);
        experienceMask.padding = padding;
    }

    private void OnDestroy()
    {
        if (playerProgress != null)
            playerProgress.OnLevelChanged -= UpdateLock;

        if (purchaseButton != null)
            purchaseButton.onClick.RemoveListener(Purchase);

        if (productProgress != null)
            productProgress.ProgressChanged -= UpdateProgress;
    }
}
