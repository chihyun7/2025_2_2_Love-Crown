using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

public class ItemSlot : MonoBehaviour
{
    public Image itemIcon;
    public TextMeshProUGUI itemNameText;
    public TextMeshProUGUI itemQuantityText;
    public TextMeshProUGUI itemPriceText;
    public Button button;

    private string currentItemID;


    public void Initialize(ItemData item, Action onBuyClicked)
    {
        SetUIData(item, 1, item.price);

        if (itemPriceText != null) itemPriceText.gameObject.SetActive(true);

        if (button != null)
        {
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => onBuyClicked?.Invoke());
            button.interactable = true;
        }
    }

    public void SetItem(ItemData item, int quantity)
    {
        SetUIData(item, quantity, 0);

        // 인벤토리에서는 가격 숨김
        if (itemPriceText != null) itemPriceText.gameObject.SetActive(false);

        if (button != null)
        {
            button.onClick.RemoveAllListeners();

            button.interactable = true;

            button.onClick.AddListener(() => OnItemUsed(item.itemID));
        }
    }

    private void SetUIData(ItemData item, int quantity, int price)
    {
        currentItemID = item.itemID;

        if (itemIcon != null) itemIcon.sprite = item.icon;
        if (itemNameText != null) itemNameText.text = item.itemName;
        if (itemQuantityText != null) itemQuantityText.text = $"x{quantity}";
        if (itemPriceText != null) itemPriceText.text = $"{price} G";
    }


    private void OnItemUsed(string itemID)
    {
        Debug.Log($"[ItemSlot] 인벤토리에서 아이템 {itemID} 사용 시도.");
    }
}