using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

// ItemData 클래스는 ScriptableObject로 별도로 정의되어 있다고 가정합니다.
// public class ItemData : ScriptableObject { public string itemID; public string itemName; public Sprite icon; public int price; }

public class ItemSlot : MonoBehaviour
{
    // 유니티 인스펙터에 연결해야 하는 UI 요소들
    public Image itemIcon;
    public TextMeshProUGUI itemNameText;
    public TextMeshProUGUI itemQuantityText;
    public TextMeshProUGUI itemPriceText;
    public Button button;

    private string currentItemID;

    // 상점에서 호출: 구매 버튼 역할
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

    /// <summary>
    /// 인벤토리에서 호출: 사용/버리기 버튼 역할
    /// </summary>
    public void SetItem(ItemData item, int quantity)
    {
        SetUIData(item, quantity, 0);

        // 인벤토리에서는 가격 숨김
        if (itemPriceText != null) itemPriceText.gameObject.SetActive(false);

        if (button != null)
        {
            button.onClick.RemoveAllListeners();

            // ✅ 수정: 인벤토리에서도 버튼 상호작용 가능하도록 설정
            button.interactable = true;

            // ✅ 수정: 인벤토리 아이템 사용 리스너 연결
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

    // 아이템 사용 로직 (인벤토리 전용)
    private void OnItemUsed(string itemID)
    {
        Debug.Log($"[ItemSlot] 인벤토리에서 아이템 {itemID} 사용 시도.");
        // TODO: 여기에 아이템 사용 처리를 위한 Inventory.pv.RPC(...) 호출 로직을 구현해야 합니다.
    }
}