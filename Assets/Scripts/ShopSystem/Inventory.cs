using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using System.Linq;

public class Inventory : MonoBehaviourPunCallbacks, IPunObservable
{
    public PhotonView pv;

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
        if (pv == null)
        {
            pv = GetComponent<PhotonView>();
        }

        if (pv == null)
        {
            Debug.LogError("Inventory 컴포넌트에 PhotonView가 누락되었습니다.");
        }
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

        Debug.Log($"[Inventory] {pv.Owner.NickName} 구매 실행 완료: {itemID}. 남은 골드: {gold}");
    }

    // 인벤토리에 아이템을 추가하는 일반 메서드 (RPC 아님)
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
    }

    /// <summary>
    /// 🚨 RPC 수정: ServerMasterClient에서 호출되는 RPC 전용 아이템 제거 메서드.
    /// RPC 서명을 'RemoveItem(String)'에 맞추기 위해 int quantity를 제거하고 1로 고정합니다.
    /// </summary>
    [PunRPC]
    public void RemoveItem(string itemID)
    {
        int quantityToRemove = 1; // RPC 호출 시에는 항상 1개를 제거하도록 고정

        int index = items.FindIndex(entry => entry.itemID == itemID);

        if (index != -1)
        {
            ItemEntry existingEntry = items[index];
            existingEntry.quantity -= quantityToRemove;

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
        }
    }

    public bool HasItem(string itemID)
    {
        ItemEntry entry = items.Find(e => e.itemID == itemID);
        return entry.quantity > 0;
    }

    private void UpdateLocalUI()
    {
        if (UIManager.instance != null)
        {
            UIManager.instance.UpdateGoldText(gold);

            if (UIManager.instance.inventoryPanel != null && UIManager.instance.inventoryPanel.activeInHierarchy)
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

                if (pv.IsMine)
                {
                    UpdateLocalUI();
                }
            }
        }
    }
}