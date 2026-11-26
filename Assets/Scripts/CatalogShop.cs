using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CatalogShop : MonoBehaviour
{
    [Header("도감 목록")]
    [SerializeField] private List<CatalogData> catalogList = new List<CatalogData>();

    [Header("참조")]
    [SerializeField] private RectTransform ticketContent;
    [SerializeField] private GameManager gameManager;
    [SerializeField] private ItemSpawner itemSpawner;

    internal GameManager PlayerProgress => gameManager;
    public int CurrentCatalogIndexForSave => currentCatalogIndex;

    private int currentCatalogIndex;
    private UnlockEffect.Registration nextCatalog;

    private void Start()
    {
        if (ticketContent == null ||
            gameManager == null ||
            itemSpawner == null)
        {
            Debug.LogWarning("CatalogShop 참조를 확인하세요.", this);
            return;
        }

        while (currentCatalogIndex < catalogList.Count)
        {
            CatalogData catalog = catalogList[currentCatalogIndex];

            UnlockEffect unlock = GetUnlockEffect(catalog.catalogPrefab);
            if (unlock != null && (!unlock.MeetsLevel(0) || unlock.RequiresConfirmation))
                break;

            AddCatalog(catalog.catalogPrefab);
            currentCatalogIndex++;
        }

        WatchNextCatalog();
    }

    public void ApplyCatalogIndex(int savedIndex)
    {
        currentCatalogIndex = Mathf.Clamp(savedIndex, 0, catalogList.Count);

        for (int i = 0; i < currentCatalogIndex; i++)
        {
            if (ticketContent != null &&
                ticketContent.childCount <= i &&
                catalogList[i].catalogPrefab != null)
            {
                AddCatalog(catalogList[i].catalogPrefab);
            }
        }

        WatchNextCatalog();
    }

    private void WatchNextCatalog()
    {
        nextCatalog?.Cancel();
        if (gameManager == null || ticketContent == null || currentCatalogIndex >= catalogList.Count)
            return;

        GameObject prefab = catalogList[currentCatalogIndex].catalogPrefab;
        nextCatalog = gameManager.UnlockEffects.Watch(this, GetUnlockEffect(prefab), () =>
        {
            AddCatalog(prefab);
            currentCatalogIndex++;
            WatchNextCatalog();
        });
    }

    private static UnlockEffect GetUnlockEffect(GameObject prefab)
    {
        return prefab != null ? prefab.GetComponent<UnlockEffect>() : null;
    }

    private void AddCatalog(GameObject catalogPrefab)
    {
        if (catalogPrefab == null)
            return;

        GameObject catalog = Instantiate(catalogPrefab, ticketContent);
        GameAudio.RegisterButtons(catalog);

        foreach (CatalogSlot item in catalog.GetComponentsInChildren<CatalogSlot>(true))
        {
            item.Initialize(this);

            // Use the authored row height instead of the sprite's native height.
            if (item.transform is RectTransform row && row.sizeDelta.y > 0f)
            {
                if (!item.TryGetComponent<LayoutElement>(out var rowLayout))
                    rowLayout = item.gameObject.AddComponent<LayoutElement>();

                rowLayout.minHeight = row.sizeDelta.y;
                rowLayout.preferredHeight = row.sizeDelta.y;
                rowLayout.flexibleHeight = 0f;
            }

            Transform icon = item.transform.Find("UpgradeListImage");
            if (icon != null && icon.TryGetComponent<Image>(out var iconImage))
                iconImage.preserveAspect = true;
        }

        Localization.BindAuthoredLabels(catalog);

        if (ticketContent.TryGetComponent<VerticalLayoutGroup>(out var contentLayout))
            contentLayout.childForceExpandHeight = false;

        LayoutRebuilder.ForceRebuildLayoutImmediate(ticketContent);
        ticketContent.SetSizeWithCurrentAnchors(
            RectTransform.Axis.Vertical, LayoutUtility.GetPreferredHeight(ticketContent));
    }

    public bool TryPurchase(GameObject ticketPrefab, int price)
    {
        if (price < 0 || gameManager.Money < price)
            return false;

        if (!itemSpawner.TrySpawnItem(ticketPrefab))
            return false;

        if (!gameManager.TrySpendMoney(price))
            return false;

        GameAudio.Play(SoundEffect.Cash);
        return true;
    }

    private void OnDestroy()
    {
        nextCatalog?.Cancel();
    }
}