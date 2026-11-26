using System;
using System.Collections.Generic;
using UnityEngine;

public class ProductProgress : MonoBehaviour
{
    [Serializable]
    public struct SaveEntry
    {
        public string productId;
        public int level;
        public int experience;
    }

    private class ProductState
    {
        public int Level;
        public int Exp;
    }

    // GameManager는 플레이어 한 명의 레벨을 관리하고,
    // 여기서는 상품 ID마다 레벨과 경험치를 따로 보관한다.
    private readonly Dictionary<string, ProductState> products =
        new Dictionary<string, ProductState>();

    public event Action<string, int> LevelChanged;
    public event Action<string, int, int> ProgressChanged;

    public int GetLevel(string productId)
    {
        ProductState product = FindProduct(productId);
        return product == null ? 0 : product.Level;
    }

    public int GetExperience(string productId)
    {
        ProductState product = FindProduct(productId);
        return product == null ? 0 : product.Exp;
    }

    public void AddExperience(string productId, int amount, int experiencePerLevel, int maxLevel)
    {
        if (amount < 0)
            throw new ArgumentOutOfRangeException(nameof(amount));
        if (experiencePerLevel <= 0)
            throw new ArgumentOutOfRangeException(nameof(experiencePerLevel));
        if (maxLevel < 0)
            throw new ArgumentOutOfRangeException(nameof(maxLevel));

        ProductState product = FindProduct(productId);
        if (amount == 0 || maxLevel == 0)
            return;

        if (product == null)
        {
            product = new ProductState();
            products.Add(productId, product);
        }

        if (product.Level >= maxLevel)
            return;

        int previousLevel = product.Level;
        long exp = (long)product.Exp + amount;

        // 기준에 도달하면 레벨을 한 번 올리고 초과 경험치는 버린다.
        if (exp >= experiencePerLevel)
        {
            product.Level++;
            exp = 0;
        }

        product.Exp = (int)exp;

        if (product.Level != previousLevel)
            LevelChanged?.Invoke(productId, product.Level);

        ProgressChanged?.Invoke(productId, product.Level, product.Exp);
    }

    public List<SaveEntry> GetSaveEntries()
    {
        List<SaveEntry> entries = new List<SaveEntry>(products.Count);
        foreach (KeyValuePair<string, ProductState> pair in products)
        {
            if (string.IsNullOrWhiteSpace(pair.Key) || pair.Value == null)
                continue;

            entries.Add(new SaveEntry
            {
                productId = pair.Key,
                level = Mathf.Max(0, pair.Value.Level),
                experience = Mathf.Max(0, pair.Value.Exp)
            });
        }
        return entries;
    }

    public void RestoreSaveEntries(IEnumerable<SaveEntry> entries)
    {
        if (entries == null)
            return;

        foreach (SaveEntry entry in entries)
        {
            if (string.IsNullOrWhiteSpace(entry.productId))
                continue;

            int safeLevel = Mathf.Max(0, entry.level);
            int safeExp = Mathf.Max(0, entry.experience);

            if (!products.TryGetValue(entry.productId, out ProductState state) || state == null)
            {
                state = new ProductState();
                products[entry.productId] = state;
            }

            state.Level = safeLevel;
            state.Exp = safeExp;

            ProgressChanged?.Invoke(entry.productId, state.Level, state.Exp);
        }
    }

    private ProductState FindProduct(string productId)
    {
        if (string.IsNullOrWhiteSpace(productId))
            throw new ArgumentException("A product ID is required.", nameof(productId));

        products.TryGetValue(productId, out ProductState product);
        return product;
    }
}