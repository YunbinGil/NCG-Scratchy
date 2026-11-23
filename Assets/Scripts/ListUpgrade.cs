using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ListUpgrade : MonoBehaviour
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
        levelText.text = $"Lv. {upgradeLevel}";
        costText.text = $"{upgradeCost:N0}";
    }
}