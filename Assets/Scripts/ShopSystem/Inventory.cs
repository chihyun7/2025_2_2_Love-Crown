using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class Inventory : MonoBehaviourPunCallbacks
{
    public PhotonView pv;

    public List<ItemData> items = new List<ItemData>();
    public int gold = 100;

    void Awake()
    {
        pv = GetComponent<PhotonView>();

        if (pv == null)
        {
            Debug.LogError("Inventory 컴포넌트에 PhotonView가 누락되었습니다.");
        }
    }
    
    public bool CanAfford(int requiredPrice)
    {
        return gold >= requiredPrice;
    }

    [PunRPC]
    public void RpcExecuteBuy(string itemID, int price)
    {
        gold -= price;
        Debug.Log($"[{pv.Owner.NickName}]의 구매 실행: {itemID}. 남은 골드: {gold}");

        ItemData purchasedItem = Resources.Load<ItemData>("Items/" + itemID);

        if (purchasedItem != null)
        {
            AddItem(purchasedItem);
        }
        else
        {
            Debug.LogError($"ItemData 로드 실패: {itemID}.");
        }

        if (pv.IsMine)
        {
            UpdateLocalUI();
        }
    }

    public void AddItem(ItemData item)
    {
        items.Add(item);
        UpdateLocalUI();
    }

    public void RemoveItem(string itemID)
    {
        ItemData itemToRemove = items.Find(item => item.itemID == itemID);
        if (itemToRemove != null)
        {
            items.Remove(itemToRemove);
            UpdateLocalUI();
        }
    }

    public bool HasItem(string itemID)
    {
        return items.Exists(item => item.itemID == itemID);
    }

    private void UpdateLocalUI()
    {
        if (UIManager.instance != null)
        {
            // UIManager.instance.UpdateGoldText(gold); 

            if (UIManager.instance.inventoryPanel != null && UIManager.instance.inventoryPanel.activeInHierarchy)
            {
                UIManager.instance.UpdateInventoryUI();
            }
        }
    }
}