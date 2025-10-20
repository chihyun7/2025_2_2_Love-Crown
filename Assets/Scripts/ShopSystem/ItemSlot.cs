using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

public class ItemSlot : MonoBehaviour
{
    [Header("UI References")]
    public Image itemIcon;
    public TextMeshProUGUI itemNameText;
    public TextMeshProUGUI itemPriceText; // 상점용 (가격)
    public TextMeshProUGUI itemQuantityText; // 인벤토리용 (수량)
    public Button button;

    /// <summary>
    /// 상점 슬롯 초기화 (가격 표시, 버튼 액션 활성화)
    /// </summary>
    public void Initialize(ItemData item, Action onClickAction)
    {
        if (itemIcon != null) itemIcon.sprite = item.icon;
        if (itemNameText != null) itemNameText.text = item.itemName;

        if (itemPriceText != null)
        {
            itemPriceText.text = item.price.ToString() + " G";
            itemPriceText.gameObject.SetActive(true);
        }

        if (itemQuantityText != null) itemQuantityText.gameObject.SetActive(false);

        if (button != null)
        {
            button.onClick.RemoveAllListeners();
            if (onClickAction != null)
            {
                button.onClick.AddListener(() => onClickAction());
                button.interactable = true;
            }
            else
            {
                button.interactable = false;
            }
        }
    }

    /// <summary>
    /// 🚨 오류 해결: UIManager에서 호출하는 인벤토리 슬롯 초기화 메서드 (SetItem)
    /// </summary>
    public void SetItem(ItemData item, int quantity)
    {
        if (itemIcon != null) itemIcon.sprite = item.icon;
        if (itemNameText != null) itemNameText.text = item.itemName;

        if (itemPriceText != null) itemPriceText.gameObject.SetActive(false);

        if (itemQuantityText != null)
        {
            itemQuantityText.text = "x" + quantity.ToString();
            itemQuantityText.gameObject.SetActive(true);
        }

        if (button != null)
        {
            button.onClick.RemoveAllListeners();
            button.interactable = false;
        }
    }
}