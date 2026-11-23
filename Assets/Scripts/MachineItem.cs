using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class MachineItem : MonoBehaviour
{
    [Header("장비 정보")]
    public string equipmentName;
    public int equipmentPrice;
    public GameObject equipmentPrefab;

    [Header("UI")]
    public TMP_Text nameText;
    public TMP_Text priceText;
    public Button purchaseButton;
    public TMP_Text purchaseText;
    public Image equipmentImage;

    [HideInInspector]
    public MachineShop equipmentShop;

    private bool isPurchased = false;

    private void Start()
    {
        nameText.text = equipmentName;
        priceText.text = equipmentPrice.ToString();
        purchaseText.text = string.Empty;

        purchaseButton.onClick.AddListener(Purchase);
    }

    private void Purchase()
    {
        if (isPurchased)
            return;

        if (equipmentShop.PurchaseEquipment(equipmentPrice))
        {
            isPurchased = true;

            purchaseText.text = "Sold Out";
            purchaseButton.interactable = false;
            equipmentImage.color = Color.gray;

            equipmentShop.SpawnEquipment(equipmentPrefab);

            Debug.Log(equipmentName + " 구매 완료!");
        }
    }
}
