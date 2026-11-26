using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ScratchBotUpgradeSlot : MonoBehaviour
{
    [Header("Upgrade Info")]
    [SerializeField] private BotUpgradeType upgradeType = BotUpgradeType.Speed;
    [SerializeField] private string upgradeName;
    [SerializeField] private int maxLevel = 5;
    // [SerializeField] private GameObject lockObject;

    [Header("UI")]
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text levelText;
    [SerializeField] private TMP_Text costText;
    [SerializeField] private Button upgradeButton;

    private MachineSlot machineSlot;

    private void Awake()
    {
        machineSlot = GetComponent<MachineSlot>();
        if (upgradeButton == null)
        {
            upgradeButton = GetComponent<Button>();
        }
    }

    private void Start()
    {
        if (upgradeButton != null)
        {
            upgradeButton.onClick.AddListener(UpgradeLevelUp);
        }

        if (ScratchBotUpgradeManager.Instance != null)
        {
            ScratchBotUpgradeManager.Instance.OnUpgraded += UpdateUI;
        }

        UpdateUI();
    }

    private void OnDestroy()
    {
        if (ScratchBotUpgradeManager.Instance != null)
        {
            ScratchBotUpgradeManager.Instance.OnUpgraded -= UpdateUI;
        }
    }

    private void UpgradeLevelUp()
    {
        if (machineSlot != null && machineSlot.IsLocked)
            return;

        ScratchBotUpgradeManager manager = ScratchBotUpgradeManager.Instance;
        if (manager == null)
            return;

        if (!manager.TryPurchaseUpgrade(upgradeType))
            return;

        ApplyUpgrade();
        UpdateUI();
    }

    private void ApplyUpgrade()
    {
        ScratchBotUpgradeManager manager = ScratchBotUpgradeManager.Instance;
        if (manager == null)
            return;

        switch (upgradeType)
        {
            case BotUpgradeType.Speed:
                Debug.Log($"스크래치 봇 속도 업그레이드! 간격: {manager.CurrentScratchInterval:F2}s");
                break;

            case BotUpgradeType.Capacity:
                Debug.Log($"스크래치 봇 용량 업그레이드! 최대 수용: {manager.CurrentMaxCapacity}");
                break;

            case BotUpgradeType.Power:
                Debug.Log($"스크래치 봇 파워 업그레이드! 긁기 강도: {manager.CurrentScratchPower:F2}");
                break;
        }
    }

    internal void RefreshLock()
    {
        UpdateUI();
    }

    private void UpdateUI()
    {
        ScratchBotUpgradeManager manager = ScratchBotUpgradeManager.Instance;

        if (nameText != null && !LocalizedLabel.HasKey(nameText))
        {
            nameText.text = string.IsNullOrEmpty(upgradeName) ? upgradeType.ToString() : upgradeName;
        }

        if (manager == null)
        {
            if (levelText != null) levelText.text = "Lv. 1";
            if (costText != null) costText.text = "-";
            // if (lockObject != null) lockObject.SetActive(true);
            if (upgradeButton != null) upgradeButton.interactable = false;
            return;
        }

        bool isBotUnlocked = manager.isBotUnlocked;
        int currentLevel = manager.GetLevel(upgradeType);
        int effectiveMaxLevel = Mathf.Min(maxLevel, manager.MaxUpgradeLevel);
        bool isMax = currentLevel >= effectiveMaxLevel;

        if (levelText != null)
        {
            levelText.text = isMax ? $"Lv. {currentLevel} (MAX)" : $"Lv. {currentLevel}";
        }

        if (costText != null)
        {
            costText.text = isMax ? "MAX" : $"{manager.GetCost(upgradeType):N0}";
        }

        // if (lockObject != null)
        // {
        //     lockObject.SetActive(!isBotUnlocked);
        // }

        if (upgradeButton != null)
        {
            upgradeButton.interactable = isBotUnlocked && !isMax && (machineSlot == null || !machineSlot.IsLocked);
        }
    }
}