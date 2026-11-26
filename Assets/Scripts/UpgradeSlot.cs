using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UpgradeSlot : MonoBehaviour
{
    [Header("Upgrade Info")]
    [SerializeField] private UpgradeType upgradeType;
    [SerializeField] private string upgradeName;

    [Header("UI")]
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text levelText;
    [SerializeField] private TMP_Text costText;
    [SerializeField] private Button upgradeButton;

    private PlayerUpgradeManager upgrades;

    private void Awake()
    {
        if (upgradeButton == null)
            upgradeButton = GetComponent<Button>();
    }

    private void Start()
    {
        upgrades = FindFirstObjectByType<GameManager>()?.Upgrades;
        if (upgrades != null)
            upgrades.Changed += UpdateUI;
        if (upgradeButton != null)
            upgradeButton.onClick.AddListener(Purchase);

        if (!LocalizedLabel.HasKey(nameText))
        {
            string key = upgradeType switch
            {
                UpgradeType.Siez => "upgrade.size",
                UpgradeType.Kind => "upgrade.coin_type",
                _ => "upgrade.lucky"
            };
            Localization.Bind(nameText, key, upgradeName);
        }

        UpdateUI();
    }

    private void OnDestroy()
    {
        if (upgrades != null)
            upgrades.Changed -= UpdateUI;
        if (upgradeButton != null)
            upgradeButton.onClick.RemoveListener(Purchase);
    }

    private void Purchase()
    {
        upgrades?.TryUpgrade(upgradeType);
    }

    private void UpdateUI()
    {
        int level = upgrades?.GetLevel(upgradeType) ?? 0;
        Localization.Bind(levelText, "status.level", "Lv. {0}", level);

        bool canUpgrade = upgrades != null && upgrades.CanUpgrade(upgradeType);
        if (upgradeButton != null)
            upgradeButton.interactable = canUpgrade;

        if (upgradeType == UpgradeType.Luk || upgrades == null)
            Localization.Clear(costText, "—");
        else if (canUpgrade)
            Localization.Clear(costText, upgrades.GetCost(upgradeType).ToString("N0"));
        else
            Localization.Bind(costText, "status.max_level", "Max Level");
    }
}
