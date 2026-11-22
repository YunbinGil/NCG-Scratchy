using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[Serializable]
public class DishSymbolRow
{
    public GameObject rowRoot;
    public Image iconImage;
    public TMP_Text probText;
}

public class DishDetailView : MonoBehaviour
{
    [Header("제목 및 설명")]
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text descriptionText;

    [Header("심볼 확률 영역")]
    [SerializeField] private TMP_Text symbolHeaderText;
    [SerializeField] private List<DishSymbolRow> symbolRows = new List<DishSymbolRow>();

    public void Bind(DishData data)
    {
        if (data == null) return;

        if (titleText != null) titleText.text = data.dishName;
        if (descriptionText != null) descriptionText.text = data.description;
        if (symbolHeaderText != null) symbolHeaderText.text = "심볼 확률";

        float breakPercent = Mathf.Clamp(data.breakChance * 100f, 0f, 100f);
        float successPercent = 100f - breakPercent;
        int reward = data.sellPriceAfterClean;
        int penalty = -Mathf.Abs(data.breakPenalty);

        Sprite cleanSprite = data.cleanSprite != null ? data.cleanSprite : data.inspectSprite;
        Sprite brokenSprite = data.brokenSprite != null ? data.brokenSprite : data.stainSprite;

        BindRow(
            index: 0,
            sprite: cleanSprite,
            lineText: $"{successPercent:0} %        $ {reward}"
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

        DishSymbolRow row = symbolRows[index];
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

    private void ClearRow(DishSymbolRow row)
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