using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "LotteryData_New", menuName = "Scritchy Scratchy/Lottery Data")]
public class LotteryData : ScriptableObject
{
    [Header("기본 정보")]
    public int id = 1;
    public string lotteryName = "Scratchy Scratchy";
    public int price = 10;
    [TextArea(2, 3)]
    public string description = "A scratch-off lottery ticket with a chance to win big! Scratch the surface to reveal symbols and see if you match enough to claim your prize.";

    [Header("Localization")]
    public string nameKey;
    public string descriptionKey;

    [Header("스프라이트 (2D)")]
    public Sprite tableSprite;
    public Sprite inspectSprite;
    public Sprite scratchCoverSprite;
    public GameObject lotteryViewPrefab;
    public GameObject lotteryTablePrefab;

    [Header("복권 규칙")]
    public int requiredPlayerLevel;
    [Range(1, 10)] public int scratchCount = 3;
    [Range(1, 10)] public int requiredMatchCount = 2;
    [Min(0)] public int hardness = 0;

    [Header("등장 아이콘 목록")]
    public List<LotteryIconData> availableIcons = new List<LotteryIconData>();

    internal float GetSpawnWeight(LotteryIconData icon, float luckMultiplier)
    {
        if (icon == null || icon.spawnWeight <= 0f)
            return 0f;

        bool isJackpotSymbol = availableIcons != null && availableIcons.Count > 0 &&
            icon == availableIcons[availableIcons.Count - 1];
        return isJackpotSymbol ? icon.spawnWeight * luckMultiplier : icon.spawnWeight;
    }

    internal float GetSymbolProbabilityPercent(LotteryIconData symbol, float luckMultiplier)
    {
        if (symbol == null || availableIcons == null)
            return 0f;

        float totalWeight = 0f;
        float symbolWeight = 0f;
        int validCount = 0;
        int symbolCount = 0;
        foreach (LotteryIconData icon in availableIcons)
        {
            if (icon == null)
                continue;

            float weight = GetSpawnWeight(icon, luckMultiplier);
            validCount++;
            totalWeight += weight;
            if (icon == symbol)
            {
                symbolCount++;
                symbolWeight += weight;
            }
        }

        if (totalWeight > 0f)
            return 100f * symbolWeight / totalWeight;

        return validCount > 0 ? 100f * symbolCount / validCount : 0f;
    }
}