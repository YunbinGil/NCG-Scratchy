using System.Collections.Generic;
using UnityEngine;

public class TicketShop : MonoBehaviour
{
    [SerializeField] private List<GameObject> catalogPrefabs = new List<GameObject>();
    [SerializeField] private RectTransform ticketContent;
    [SerializeField] private GameManager gameManager;
    [SerializeField] private ItemSpawner itemSpawner;

    private void Start()
    {
        if (ticketContent == null)
        {
            Debug.LogWarning("TicketShop에 스크롤뷰 Content를 연결하세요.", this);
            return;
        }

        foreach (GameObject catalogPrefab in catalogPrefabs)
        {
            if (catalogPrefab == null)
                continue;

            GameObject catalog = Instantiate(catalogPrefab, ticketContent);
            foreach (TicketItem item in catalog.GetComponentsInChildren<TicketItem>(true))
                item.Initialize(this);
        }
    }

    public bool TryPurchase(GameObject ticketPrefab, int price)
    {
        if (itemSpawner.TrySpawnItem(ticketPrefab))
            return true;

        gameManager.RefundMoney(price);
        return false;
    }
}
