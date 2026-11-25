using System.Collections.Generic;
using UnityEngine;

public class TicketShop : MonoBehaviour
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

    private int currentCatalogIndex = 0;
    private bool isUnlocking = false;

    private void Start()
    {
        if (ticketContent == null ||
            gameManager == null ||
            itemSpawner == null)
        {
            Debug.LogWarning("TicketShop 참조를 확인하세요.", this);
            return;
        }

        // 0레벨 도감은 시작과 동시에 상점에 등록
        while (currentCatalogIndex < catalogList.Count)
        {
            CatalogData catalog = catalogList[currentCatalogIndex];

            if (catalog.unlockLevel != 0)
                break;

            AddCatalog(catalog.catalogPrefab);
            currentCatalogIndex++;
        }

        // 레벨 상승 이벤트 구독
        gameManager.OnLevelChanged += CheckUnlockCondition;

        // 다음 도감의 해금 조건 확인
        CheckUnlockCondition(gameManager.Level);
    }

    // 해금 조건 확인
    private void CheckUnlockCondition(int level)
    {
        if (isUnlocking)
            return;

        if (currentCatalogIndex >= catalogList.Count)
            return;

        CatalogData catalog = catalogList[currentCatalogIndex];

        if (level < catalog.unlockLevel)
            return;

        SpawnUnlockItem();
    }

    // 해금용 아이템 생성
    private void SpawnUnlockItem()
    {
        isUnlocking = true;

        if (!itemSpawner.TrySpawnUnlockItem(CompleteUnlock))
        {
            isUnlocking = false;
        }
    }

    // 해금 연출 완료
    private void CompleteUnlock()
    {
        CatalogData catalog = catalogList[currentCatalogIndex];

        AddCatalog(catalog.catalogPrefab);

        currentCatalogIndex++;
        isUnlocking = false;

        // 다음 도감의 해금 조건 확인
        CheckUnlockCondition(gameManager.Level);
    }

    // 상점에 도감 추가
    private void AddCatalog(GameObject catalogPrefab)
    {
        if (catalogPrefab == null)
            return;

        GameObject catalog = Instantiate(
            catalogPrefab,
            ticketContent
        );

        // foreach (TicketItem item in
        //     catalog.GetComponentsInChildren<TicketItem>(true))
        // {
        //     item.Initialize(this);
        // }
    }

    // 티켓 구매
    public bool TryPurchase(GameObject ticketPrefab, int price)
    {
        if (price < 0 || gameManager.Money < price)
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
    }
}