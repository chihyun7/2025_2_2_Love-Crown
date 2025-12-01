using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;

public class Inventory : MonoBehaviourPunCallbacks, IPunObservable
{
    public PhotonView pv;
    private PlayerQuestLog questLog;

    [System.Serializable]
    public struct ItemEntry
    {
        public string itemID;
        public int quantity;

        public ItemEntry(string id, int qty)
        {
            itemID = id;
            quantity = qty;
        }
    }

    private List<ItemEntry> items = new List<ItemEntry>();
    public int gold = 100;

    void Awake()
    {
        if (pv == null) pv = GetComponent<PhotonView>();
        questLog = GetComponent<PlayerQuestLog>();
    }

    public List<ItemEntry> GetItems()
    {
        return items;
    }

    public bool CanAfford(int requiredPrice)
    {
        return gold >= requiredPrice;
    }

    [PunRPC]
    public void RpcExecuteBuy(string itemID, int price)
    {
        gold -= price;
        AddItemToList(itemID, 1);

        if (pv.IsMine)
        {
            UpdateLocalUI();
        }
    }

    public void AddItem(string itemID, int quantity = 1)
    {
        AddItemToList(itemID, quantity);

        if (pv.IsMine)
        {
            UpdateLocalUI();
        }
    }

    private void AddItemToList(string itemID, int quantity)
    {
        int index = items.FindIndex(entry => entry.itemID == itemID);

        if (index != -1)
        {
            ItemEntry existingEntry = items[index];
            existingEntry.quantity += quantity;
            items[index] = existingEntry;
        }
        else
        {
            items.Add(new ItemEntry(itemID, quantity));
        }

        if (pv.IsMine && questLog != null)
        {
            questLog.UpdateQuestProgress(itemID, GetItemCount(itemID));
        }
    }

    [PunRPC]
    public void RemoveItem(string itemID)
    {
        int index = items.FindIndex(entry => entry.itemID == itemID);

        if (index != -1)
        {
            ItemEntry existingEntry = items[index];
            existingEntry.quantity -= 1;

            if (existingEntry.quantity <= 0)
            {
                items.RemoveAt(index);
            }
            else
            {
                items[index] = existingEntry;
            }
        }

        if (pv.IsMine)
        {
            UpdateLocalUI();
            if (questLog != null)
            {
                questLog.UpdateQuestProgress(itemID, GetItemCount(itemID));
            }
        }
    }

    public bool HasItem(string itemID)
    {
        return GetItemCount(itemID) > 0;
    }

    public int GetItemCount(string itemID)
    {
        int index = items.FindIndex(entry => entry.itemID == itemID);
        if (index != -1)
        {
            return items[index].quantity;
        }
        return 0;
    }

    private void UpdateLocalUI()
    {
        if (!pv.IsMine) return;

        if (UIManager.instance != null)
        {
            UIManager.instance.UpdateGoldText(gold);

            if (UIManager.instance.inventoryPanel.activeInHierarchy)
            {
                UIManager.instance.UpdateInventoryUI();
            }
        }
    }

    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting)
        {
            stream.SendNext(gold);
        }
        else
        {
            int receivedGold = (int)stream.ReceiveNext();
            if (gold != receivedGold)
            {
                gold = receivedGold;
                if (pv.IsMine) UpdateLocalUI();
            }
        }
    }

    [PunRPC]
    public void RpcChangeGold(int amount)
    {
        gold += amount;
        if (pv.IsMine)
        {
            if (UIManager.instance != null)
            {
                UIManager.instance.UpdateGoldText(gold);
            }
        }
    }

    [PunRPC]
    public void RpcAddItem(string itemID, int quantity)
    {
        AddItemToList(itemID, quantity);

        if (pv.IsMine)
        {
            UpdateLocalUI();
        }
    }
}