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
    private PlayerUpgradeManager Upgrades => upgrades ??= FindFirstObjectByType<GameManager>()?.Upgrades;

    public UpgradeType UpgradeTypeValue => upgradeType;
    public int UpgradeLevelValue => Upgrades?.GetLevel(upgradeType) ?? 0;
    public float UpgradeCostValue => Upgrades?.GetCost(upgradeType) ?? 0;

    private void Awake()
    {
        if (upgradeButton == null)
            upgradeButton = GetComponent<Button>();
    }

    private void Start()
    {
        PlayerUpgradeManager manager = Upgrades;
        if (manager != null)
            manager.Changed += UpdateUI;
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

    public void ApplySaveData(int savedLevel, float savedCost)
    {
        PlayerUpgradeManager manager = Upgrades;
        if (manager == null)
            return;

        var entries = manager.Capture();
        entries.RemoveAll(entry => entry.upgradeType == upgradeType);
        entries.Add(new UpgradeSaveEntry
        {
            upgradeType = upgradeType,
            level = Mathf.Max(0, savedLevel),
            cost = Mathf.Max(0f, savedCost)
        });
        manager.Restore(entries);
    }

    public void ResetToDefault()
    {
        ApplySaveData(0, 100f);
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
