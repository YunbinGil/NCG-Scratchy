using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CatalogShop : MonoBehaviour
{
    [Header("도감 목록")]
    [SerializeField]
    private List<CatalogData> catalogList = new List<CatalogData>();

    [Header("해금 연출")]
    [SerializeField] private GameObject unlockPrefab;
    [SerializeField] private Texture2D lightTexture;
    [SerializeField] private Vector2 unlockPoint;

    [Header("참조")]
    [SerializeField] private RectTransform ticketContent;
    [SerializeField] private GameManager gameManager;
    [SerializeField] private ItemSpawner itemSpawner;

    internal GameManager PlayerProgress => gameManager;
    public int CurrentCatalogIndexForSave => currentCatalogIndex;

    private int currentCatalogIndex = 0;
    private bool isUnlocking = false;
    private GameObject activeUnlockObject;

    private void Start()
    {
        if (ticketContent == null ||
            gameManager == null ||
            itemSpawner == null)
        {
            Debug.LogWarning("LotteryShop 참조를 확인하세요.", this);
            return;
        }

        // 0레벨 도감은 시작과 동시에 상점에 등록
        while (currentCatalogIndex < catalogList.Count)
        {
            Debug.Log($"{currentCatalogIndex}레벨 도감 등록");
            CatalogData catalog = catalogList[currentCatalogIndex];

            if (catalog.unlockLevel != 0)
                break;

            AddCatalog(catalog.catalogPrefab);
            currentCatalogIndex++;
        }

        // 레벨 상승 이벤트 구독
        gameManager.OnLevelChanged -= CheckUnlockCondition;
        gameManager.OnLevelChanged += CheckUnlockCondition;

        // 다음 도감의 해금 조건 확인
        CheckUnlockCondition(gameManager.Level);
    }

    public void ApplyCatalogIndex(int savedIndex)
    {
        ClearActiveUnlockObject();
        isUnlocking = false;

        if (catalogList == null || catalogList.Count == 0)
        {
            currentCatalogIndex = 0;
            return;
        }

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

        if (currentCatalogIndex < catalogList.Count && gameManager != null)
        {
            CheckUnlockCondition(gameManager.Level);
        }
    }

    // 해금 조건 확인
    private void CheckUnlockCondition(int level)
    {
        if (isUnlocking || activeUnlockObject != null)
            return;

        if (catalogList == null || currentCatalogIndex < 0 || currentCatalogIndex >= catalogList.Count)
            return;

        CatalogData catalog = catalogList[currentCatalogIndex];

        if (level < catalog.unlockLevel)
            return;

        SpawnUnlockItem();
    }

    // 해금용 아이템 생성
    private void SpawnUnlockItem()
    {
        if (catalogList == null || currentCatalogIndex < 0 || currentCatalogIndex >= catalogList.Count)
        {
            isUnlocking = false;
            return;
        }

        if (unlockPrefab == null ||
            !unlockPrefab.TryGetComponent<UnlockLotteryCatalog>(out _))
        {
            Debug.LogWarning("해금 프리팹을 확인하세요.", this);
            return;
        }

        ClearActiveUnlockObject();
        isUnlocking = true;

        // 테이블 중앙으로 이동하도록 좌표 고정
        if (!itemSpawner.TrySpawnItemAtTableCenter(
            unlockPrefab,
            out GameObject spawnedObject))
        {
            isUnlocking = false;
            return;
        }

        activeUnlockObject = spawnedObject;

        UnlockLotteryCatalog unlock =
            spawnedObject.GetComponent<UnlockLotteryCatalog>();

        unlock.Initialize(lightTexture, CompleteUnlock);
    }

    // 해금 연출 완료
    private void CompleteUnlock()
    {
        activeUnlockObject = null;

        if (catalogList == null || currentCatalogIndex < 0 || currentCatalogIndex >= catalogList.Count)
        {
            isUnlocking = false;
            return;
        }

        CatalogData catalog = catalogList[currentCatalogIndex];

        AddCatalog(catalog.catalogPrefab);

        currentCatalogIndex++;
        isUnlocking = false;

        // 다음 도감의 해금 조건 확인
        if (gameManager != null)
        {
            CheckUnlockCondition(gameManager.Level);
        }
    }

    private void ClearActiveUnlockObject()
    {
        if (activeUnlockObject != null)
        {
            Destroy(activeUnlockObject);
            activeUnlockObject = null;
        }
    }

    // 상점에 도감 추가
    private void AddCatalog(GameObject catalogPrefab)
    {
        if (catalogPrefab == null || ticketContent == null)
            return;

        GameObject catalog = Instantiate(
            catalogPrefab,
            ticketContent
        );

        foreach (CatalogSlot item in
            catalog.GetComponentsInChildren<CatalogSlot>(true))
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

    // 티켓 구매
    public bool TryPurchase(GameObject ticketPrefab, int price)
    {
        if (price < 0 || gameManager == null || gameManager.Money < price)
            return false;

        if (!itemSpawner.TrySpawnItem(ticketPrefab))
            return false;

        return gameManager.TrySpendMoney(price);
    }

    private void OnDestroy()
    {
        if (gameManager != null)
        {
            gameManager.OnLevelChanged -= CheckUnlockCondition;
        }

        ClearActiveUnlockObject();
    }
}