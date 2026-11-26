using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UpgradeSlot : MonoBehaviour
{
    [Header("Upgrade Info")]
    [SerializeField] private UpgradeType upgradeType;
    [SerializeField] private string upgradeName;
    private int upgradeLevel = 0;
    private float upgradeCost = 100f;
    private float costMultiplier = 1.5f;

    [Header("UI")]
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text levelText;
    [SerializeField] private TMP_Text costText;
    [SerializeField] private Button upgradeButton;
    [SerializeField] private float defaultCost = 100f;
    public UpgradeType UpgradeTypeValue => upgradeType;
    public int UpgradeLevelValue => upgradeLevel;
    public float UpgradeCostValue => upgradeCost;

    private void Awake()
    {
        if (upgradeButton == null)
        {
            upgradeButton = GetComponent<Button>();
        }
    }
    public void ResetToDefault()
    {
        upgradeLevel = 0;
        upgradeCost = defaultCost;
        UpdateUI();
    }

    private void Start()
    {
        upgradeButton.onClick.AddListener(UpgradeLevelUp);
        UpdateUI();
    }

    private void UpgradeLevelUp()
    {
        // TODO: 재화 확인 및 차감

        upgradeLevel++;
        upgradeCost += upgradeCost * costMultiplier;

        ApplyUpgrade();
        UpdateUI();
    }

    public void ApplySaveData(int savedLevel, float savedCost)
    {
        upgradeLevel = Mathf.Max(0, savedLevel);
        upgradeCost = Mathf.Max(0f, savedCost);
        ApplyUpgrade();
        UpdateUI();
    }

    private void ApplyUpgrade()
    {
        switch (upgradeType) // TODO: 안에 업그레이드 내용들 구현
        {
            case UpgradeType.Luk:
                Debug.Log("행운 업그레이드");
                break;

            case UpgradeType.Siez:
                Debug.Log("사이즈 업그레이드");
                break;

            case UpgradeType.Kind:
                Debug.Log("종류 업그레이드");
                break;
        }
    }

    private void UpdateUI()
    {
        nameText.text = upgradeName;
        Localization.Bind(levelText, "status.level", "Lv. {0}", upgradeLevel);
        costText.text = $"{upgradeCost:N0}";
    }
}