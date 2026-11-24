using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CatalogSlot : MonoBehaviour
{
    [SerializeField] private string ticketName;
    [SerializeField, Min(0)] private int price;
    [SerializeField] private GameObject ticketPrefab;

    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text priceText;
    [SerializeField] private Button purchaseButton;

    private CatalogShop shop;

    public void Initialize(CatalogShop ticketShop)
    {
        shop = ticketShop;

        if (nameText != null)
            nameText.text = ticketName;

        if (priceText != null)
            priceText.text = price.ToString();

        if (purchaseButton == null)
            purchaseButton = GetComponent<Button>();

        if (purchaseButton != null)
            purchaseButton.onClick.AddListener(Purchase);
    }

    private void Purchase()
    {
        if (shop != null)
            shop.TryPurchase(ticketPrefab, price);
    }

    private void OnDestroy()
    {
        if (purchaseButton != null)
            purchaseButton.onClick.RemoveListener(Purchase);
    }
}
