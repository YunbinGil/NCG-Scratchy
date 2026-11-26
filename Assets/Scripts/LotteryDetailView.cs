using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LotteryDetailView : MonoBehaviour
{
    [Header("기본 정보 텍스트")]
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text descriptionText;
    [SerializeField] private TMP_Text matchRuleText;
    [SerializeField] private TMP_Text hardnessText;

    [Header("심볼 행 풀링")]
    [SerializeField] private Transform rowContainer;
    [SerializeField] private GameObject rowPrefab;
    [SerializeField] private List<LotterySymbolRow> symbolRows = new List<LotterySymbolRow>();

    public void Bind(LotteryData data, float luckMultiplier = 1f)
    {
        if (data == null) return;

        int requiredMatchCount = Mathf.Max(1, data.requiredMatchCount);

        BindText(titleText, data.nameKey, data.lotteryName);
        BindText(descriptionText, data.descriptionKey, data.description);
        Localization.Bind(matchRuleText, "detail.match_rule", "Match {0} to win", requiredMatchCount);
        Localization.Bind(hardnessText, "detail.hardness", "Hard: {0}", data.hardness);

        BindSymbolRows(data, requiredMatchCount, luckMultiplier);
    }

    private static void BindText(TMP_Text label, string key, string fallback)
    {
        if (label == null) return;

        if (string.IsNullOrWhiteSpace(key))
            Localization.Clear(label, fallback);
        else
            Localization.Bind(label, key, fallback);
    }

    public void Clear()
    {
        Localization.Clear(titleText, string.Empty);
        Localization.Clear(descriptionText, string.Empty);
        if (matchRuleText != null) matchRuleText.text = string.Empty;
        if (hardnessText != null) hardnessText.text = string.Empty;

        for (int i = 0; i < symbolRows.Count; i++)
        {
            LotterySymbolRow row = symbolRows[i];
            if (row == null) continue;

            if (row.probText != null) row.probText.text = string.Empty;
            if (row.iconImage != null)
            {
                row.iconImage.sprite = null;
                row.iconImage.enabled = false;
            }
            if (row.rowRoot != null) row.rowRoot.SetActive(false);
        }
    }

    private void EnsureRowCapacity(int requiredCount)
    {
        if (requiredCount <= symbolRows.Count) return;
        if (rowPrefab == null || rowContainer == null)
        {
            Debug.LogWarning($"{name}: rowPrefab 또는 rowContainer가 비어 있습니다.", this);
            return;
        }

        while (symbolRows.Count < requiredCount)
        {
            GameObject instance = Instantiate(rowPrefab, rowContainer, false);
            LotterySymbolRow row = instance.GetComponent<LotterySymbolRow>();
            if (row == null)
            {
                Debug.LogWarning($"{instance.name}에 LotterySymbolRow 컴포넌트가 없습니다.", instance);
                instance.SetActive(false);
                break;
            }

            if (row.rowRoot == null)
                row.rowRoot = instance;

            symbolRows.Add(row);
        }
    }

    private void BindSymbolRows(LotteryData data, int requiredMatchCount, float luckMultiplier)
    {
        var icons = data.availableIcons;
        int iconCount = icons != null ? icons.Count : 0;

        EnsureRowCapacity(iconCount);

        for (int i = 0; i < symbolRows.Count; i++)
        {
            LotterySymbolRow row = symbolRows[i];
            if (row == null) continue;

            bool hasData = i < iconCount && icons[i] != null;
            if (row.rowRoot != null)
                row.rowRoot.SetActive(hasData);

            if (!hasData) continue;

            LotteryIconData iconData = icons[i];
            float probPercent = data.GetSymbolProbabilityPercent(iconData, luckMultiplier);
            int payout = iconData.GetPayoutForMatchCount(requiredMatchCount);

            if (row.iconImage != null)
            {
                row.iconImage.sprite = iconData.iconSprite;
                row.iconImage.enabled = iconData.iconSprite != null;
            }

            if (row.probText != null)
            {
                row.probText.text = $"{probPercent:0.##} %        $ {payout}";
            }
        }

        if (rowContainer is RectTransform containerRect)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(containerRect);
        }
    }
}