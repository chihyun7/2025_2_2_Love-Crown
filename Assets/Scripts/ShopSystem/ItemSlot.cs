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

    // ★ 시작할 때 기본으로 다 숨기기 (플레이스홀더 안 보이게)
    private void Start()
    {
        Clear();
    }

    // ======================
    // 상점용
    // ======================
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

    // ======================
    // 인벤토리용
    // ======================
    public void SetItem(ItemData item, int quantity)
    {
        Debug.Log($"[ItemSlot] SetItem 호출: {item.itemID}, x{quantity}");

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

        if (itemIcon != null)
        {
            itemIcon.sprite = item.icon;
            itemIcon.gameObject.SetActive(true);   // ← 아이템 들어오면 켜기
        }

        if (itemNameText != null)
        {
            itemNameText.text = item.itemName;
            itemNameText.gameObject.SetActive(true);
        }

        if (itemQuantityText != null)
        {
            itemQuantityText.text = $"x{quantity}";
            itemQuantityText.gameObject.SetActive(true);  // ← 수량도 켜기
        }

        if (itemPriceText != null)
        {
            itemPriceText.text = $"{price} G";
            // 상점 전용 표시 여부는 Initialize() 쪽에서 제어
        }
    }

    private void OnItemUsed(string itemID)
    {
        Debug.Log($"[ItemSlot] 인벤토리에서 아이템 {itemID} 사용 시도.");
    }

    // ======================
    // 슬롯 비우기 (인벤토리 기본 상태)
    // ======================
    public void Clear()
    {
        currentItemID = null;

        // 아이콘/텍스트 비우고 끄기
        if (itemIcon != null)
        {
            itemIcon.sprite = null;
            itemIcon.gameObject.SetActive(false);
        }

        if (itemNameText != null)
        {
            itemNameText.text = "";
            itemNameText.gameObject.SetActive(false);
        }

        if (itemQuantityText != null)
        {
            itemQuantityText.text = "";
            itemQuantityText.gameObject.SetActive(false);
        }

        if (itemPriceText != null)
        {
            itemPriceText.text = "";
            itemPriceText.gameObject.SetActive(false);
        }

        if (button != null)
        {
            button.onClick.RemoveAllListeners();
            button.interactable = false;
        }
    }
}
