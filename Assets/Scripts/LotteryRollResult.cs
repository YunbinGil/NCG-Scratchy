using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public struct LotterySlotOption
{
    public string label;
    public Sprite icon;
    [Min(0f)] public float weight; // 확률 가중치
    public int rewardFor2Match;
    public int rewardFor3Match;
}

public class LotteryRollResult
{
    public readonly LotterySlotOption[] Slots = new LotterySlotOption[3];
    public int TotalReward { get; private set; }
    public string SummaryText { get; private set; }

    public static LotteryRollResult Roll(IList<LotterySlotOption> pool)
    {
        var result = new LotteryRollResult();
        if (pool == null || pool.Count == 0) return result;

        float totalWeight = 0f;
        for (int i = 0; i < pool.Count; i++)
            totalWeight += Mathf.Max(0f, pool[i].weight);

        Dictionary<string, int> counts = new Dictionary<string, int>();

        for (int i = 0; i < 3; i++)
        {
            LotterySlotOption picked = PickByWeight(pool, totalWeight);
            result.Slots[i] = picked;

            if (!counts.ContainsKey(picked.label)) counts[picked.label] = 0;
            counts[picked.label]++;
        }

        int reward = 0;
        List<string> winLines = new List<string>();
        foreach (var pair in counts)
        {
            LotterySlotOption opt = FindOption(pool, pair.Key);
            if (pair.Value >= 3)
            {
                reward += opt.rewardFor3Match;
                winLines.Add($"{opt.label} 3매칭 (+{opt.rewardFor3Match})");
            }
            else if (pair.Value == 2)
            {
                reward += opt.rewardFor2Match;
                winLines.Add($"{opt.label} 2매칭 (+{opt.rewardFor2Match})");
            }
        }

        result.TotalReward = reward;
        result.SummaryText = winLines.Count > 0 ? string.Join(" / ", winLines) : "꽝";
        return result;
    }

    private static LotterySlotOption PickByWeight(IList<LotterySlotOption> pool, float totalWeight)
    {
        if (totalWeight <= 0f)
            return pool[UnityEngine.Random.Range(0, pool.Count)];

        float r = UnityEngine.Random.Range(0f, totalWeight);
        float acc = 0f;
        for (int i = 0; i < pool.Count; i++)
        {
            acc += Mathf.Max(0f, pool[i].weight);
            if (r <= acc) return pool[i];
        }
        return pool[pool.Count - 1];
    }

    private static LotterySlotOption FindOption(IList<LotterySlotOption> pool, string label)
    {
        for (int i = 0; i < pool.Count; i++)
            if (pool[i].label == label) return pool[i];
        return pool[0];
    }
}