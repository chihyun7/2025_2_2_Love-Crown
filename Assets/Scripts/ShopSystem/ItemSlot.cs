using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

public class ItemSlot : MonoBehaviour
{
    public Image itemIcon;
    public TextMeshProUGUI itemPriceText;
    public Button button;

    public void Initialize(ItemData item, Action onClickAction, bool isShopSlot = true)
    {
        itemIcon.sprite = item.icon;

        if (isShopSlot)
        {
            itemPriceText.text = item.price + " G";
        }
        else
        {
            itemPriceText.gameObject.SetActive(false);
        }

        if (onClickAction != null)
        {
            button.onClick.AddListener(() => onClickAction());
        }
    }
}