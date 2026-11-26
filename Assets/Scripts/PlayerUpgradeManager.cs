using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class PlayerUpgradeManager
{
    private const int MaxSizeLevel = 5;
    private const int MaxCoinLevel = 2;
    private const int MaxLuckLevel = 5;
    private const int BaseCost = 100;
    private const double CostFactor = 2.5;

    private readonly GameManager gameManager;
    private int sizeLevel;
    private int coinLevel;
    private int luckLevel;

    public event Action Changed;

    public PlayerUpgradeManager(GameManager gameManager)
    {
        this.gameManager = gameManager;
    }

    public int GetLevel(UpgradeType type)
    {
        return type switch
        {
            UpgradeType.Siez => sizeLevel,
            UpgradeType.Kind => coinLevel,
            UpgradeType.Luk => luckLevel,
            _ => 0
        };
    }

    public int GetCost(UpgradeType type)
    {
        return (int)Math.Min(int.MaxValue, Math.Round(BaseCost * Math.Pow(CostFactor, GetLevel(type))));
    }

    public bool CanUpgrade(UpgradeType type)
    {
        return type switch
        {
            UpgradeType.Siez => sizeLevel < MaxSizeLevel,
            UpgradeType.Kind => coinLevel < MaxCoinLevel,
            UpgradeType.Luk => luckLevel < MaxLuckLevel,
            _ => false
        };
    }

    public bool TryUpgrade(UpgradeType type)
    {
        if (!CanUpgrade(type) || !gameManager.TrySpendMoney(GetCost(type)))
            return false;

        switch (type)
        {
            case UpgradeType.Siez:
                sizeLevel++;
                break;
            case UpgradeType.Kind:
                coinLevel++;
                break;
            case UpgradeType.Luk:
                luckLevel++;
                break;
        }

        ApplyEffects();
        GameAudio.Play(SoundEffect.Cash);
        return true;
    }

    public List<UpgradeSaveEntry> Capture()
    {
        return new List<UpgradeSaveEntry>
        {
            new UpgradeSaveEntry { upgradeType = UpgradeType.Siez, level = sizeLevel, cost = GetCost(UpgradeType.Siez) },
            new UpgradeSaveEntry { upgradeType = UpgradeType.Kind, level = coinLevel, cost = GetCost(UpgradeType.Kind) },
            new UpgradeSaveEntry { upgradeType = UpgradeType.Luk, level = luckLevel, cost = GetCost(UpgradeType.Luk) }
        };
    }

    public void Restore(List<UpgradeSaveEntry> entries)
    {
        sizeLevel = 0;
        coinLevel = 0;
        luckLevel = 0;

        if (entries != null)
        {
            foreach (UpgradeSaveEntry entry in entries)
            {
                if (entry == null)
                    continue;

                switch (entry.upgradeType)
                {
                    case UpgradeType.Siez:
                        sizeLevel = Mathf.Clamp(entry.level, 0, MaxSizeLevel);
                        break;
                    case UpgradeType.Kind:
                        coinLevel = Mathf.Clamp(entry.level, 0, MaxCoinLevel);
                        break;
                    case UpgradeType.Luk:
                        luckLevel = Mathf.Clamp(entry.level, 0, MaxLuckLevel);
                        break;
                }
            }
        }

        ApplyEffects();
    }

    private void ApplyEffects()
    {
        gameManager.UpgradeCursorSize(sizeLevel);
        gameManager.UpgradeLeadCoinAlpha(coinLevel);
        gameManager.UpgradeLuck(luckLevel);
        Changed?.Invoke();
    }
}
