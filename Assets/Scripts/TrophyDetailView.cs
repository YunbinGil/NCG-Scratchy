using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[Serializable]
public class TrophySymbolRow
{
    public GameObject rowRoot;
    public Image iconImage;
    public TMP_Text probText;
}

public class TrophyDetailView : MonoBehaviour
{
    [Header("제목 및 설명")]
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text descriptionText;

    [Header("심볼 확률 영역")]
    [SerializeField] private TMP_Text symbolHeaderText;
    [SerializeField] private List<TrophySymbolRow> symbolRows = new List<TrophySymbolRow>();

    public void Bind(TrophyData data, int sellPrice, float breakChance)
    {
        if (data == null) return;


        if (string.Equals(data.dishName, "DAY JOB", StringComparison.OrdinalIgnoreCase))
        {
            Localization.Bind(titleText, "item.trophy.day_job.name", data.dishName);
            Localization.Bind(descriptionText, "item.trophy.day_job.description", data.description);
        }
        else
        {
            Localization.Clear(titleText, data.dishName);
            Localization.Clear(descriptionText, data.description);
        }
        Localization.Bind(symbolHeaderText, "detail.symbol_probabilities", "Symbol Probabilities");

        float breakPercent = Mathf.Clamp(breakChance * 100f, 0f, 100f);
        float successPercent = 100f - breakPercent;
        int penalty = -Mathf.Abs(data.breakPenalty);

        Sprite cleanSprite = data.cleanSprite != null ? data.cleanSprite : data.inspectSprite;
        Sprite brokenSprite = data.brokenSprite != null ? data.brokenSprite : data.stainSprite;

        BindRow(
            index: 0,
            sprite: cleanSprite,
            lineText: $"{successPercent:0} %        $ {sellPrice}"
        );

        BindRow(
            index: 1,
            sprite: brokenSprite,
            lineText: $"{breakPercent:0} %        $ {penalty}"
        );

        for (int i = 2; i < symbolRows.Count; i++)
        {
            ClearRow(symbolRows[i]);
        }
    }

    public void Clear()
    {
        if (titleText != null) titleText.text = string.Empty;
        if (descriptionText != null) descriptionText.text = string.Empty;
        if (symbolHeaderText != null) symbolHeaderText.text = string.Empty;

        for (int i = 0; i < symbolRows.Count; i++)
        {
            ClearRow(symbolRows[i]);
        }
    }

    private void BindRow(int index, Sprite sprite, string lineText)
    {
        if (index < 0 || index >= symbolRows.Count) return;

        TrophySymbolRow row = symbolRows[index];
        if (row == null) return;

        if (row.rowRoot != null)
            row.rowRoot.SetActive(true);

        if (row.iconImage != null)
        {
            row.iconImage.sprite = sprite;
            row.iconImage.enabled = sprite != null;
        }

        if (row.probText != null)
            row.probText.text = lineText;
    }

    private void ClearRow(TrophySymbolRow row)
    {
        if (row == null) return;

        if (row.probText != null) row.probText.text = string.Empty;

        if (row.iconImage != null)
        {
            row.iconImage.sprite = null;
            row.iconImage.enabled = false;
        }

        if (row.rowRoot != null)
            row.rowRoot.SetActive(false);
    }
}