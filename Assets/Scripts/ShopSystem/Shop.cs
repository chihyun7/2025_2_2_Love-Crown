using System.Collections.Generic;
using UnityEngine;

public class Shop : MonoBehaviour
{
    public List<ItemData> itemsForSale;

    public void BuyItem(ItemData item)
    {
        if (Inventory.instance.gold >= item.price)
        {
            Inventory.instance.gold -= item.price;
            Inventory.instance.AddItem(Instantiate(item));
            UIManager.instance.UpdateGoldUI();
            Debug.Log(item.itemName + "을(를) 구매했습니다.");
        }
        else
        {
            Debug.Log("골드가 부족합니다.");
        }
    }
}