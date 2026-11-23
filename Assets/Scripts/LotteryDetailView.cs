using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[Serializable]
public class LotterySymbolRow
{
    public GameObject rowRoot;
    public Image iconImage;
    public TMP_Text probText;
}

public class LotteryDetailView : MonoBehaviour
{
    [Header("기본 정보 텍스트")]
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text matchRuleText;
    [SerializeField] private TMP_Text hardnessText;

    [Header("심볼 확률 및 매칭 보상금 행 리스트")]
    [SerializeField] private List<LotterySymbolRow> symbolRows = new List<LotterySymbolRow>();

    public void Bind(LotteryData data)
    {
        if (data == null) return;

        int requiredMatchCount = Mathf.Max(1, data.requiredMatchCount);

        if (titleText != null) titleText.text = data.lotteryName;
        if (matchRuleText != null) matchRuleText.text = $"{requiredMatchCount}개 일치 시 당첨";
        if (hardnessText != null) hardnessText.text = $"단단함: {data.hardness}";

        BindSymbolRows(data, requiredMatchCount);
    }

    public void Clear()
    {
        if (titleText != null) titleText.text = string.Empty;
        if (matchRuleText != null) matchRuleText.text = string.Empty;
        if (hardnessText != null) hardnessText.text = string.Empty;

        for (int i = 0; i < symbolRows.Count; i++)
        {
            if (symbolRows[i] == null) continue;
            if (symbolRows[i].probText != null) symbolRows[i].probText.text = string.Empty;
            if (symbolRows[i].iconImage != null)
            {
                symbolRows[i].iconImage.sprite = null;
                symbolRows[i].iconImage.enabled = false;
            }
            if (symbolRows[i].rowRoot != null) symbolRows[i].rowRoot.SetActive(false);
        }
    }

    private void BindSymbolRows(LotteryData data, int requiredMatchCount)
    {
        var icons = data.availableIcons;
        int iconCount = icons != null ? icons.Count : 0;

        float totalWeight = 0f;
        for (int i = 0; i < iconCount; i++)
        {
            if (icons[i] != null && icons[i].spawnWeight > 0f)
                totalWeight += icons[i].spawnWeight;
        }

        for (int i = 0; i < symbolRows.Count; i++)
        {
            LotterySymbolRow row = symbolRows[i];
            if (row == null) continue;

            bool hasData = i < iconCount && icons[i] != null;
            if (row.rowRoot != null)
                row.rowRoot.SetActive(hasData);

            if (!hasData) continue;

            LotteryIconData iconData = icons[i];
            float probPercent = totalWeight > 0f ? (iconData.spawnWeight / totalWeight) * 100f : 0f;
            int payout = iconData.GetPayoutForMatchCount(requiredMatchCount);

            if (row.iconImage != null)
            {
                row.iconImage.sprite = iconData.iconSprite;
                row.iconImage.enabled = iconData.iconSprite != null;
            }

            if (row.probText != null)
            {
                row.probText.text = $"{probPercent:0} %        $ {payout}";
            }
        }
    }
}