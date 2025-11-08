using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using System.Linq;

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
        if (pv == null)
        {
            pv = GetComponent<PhotonView>();
        }

        if (pv == null)
        {
            Debug.LogError("Inventory 컴포넌트에 PhotonView가 누락되었습니다.");
        }

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
        RpcChangeGold(-price);

        Debug.Log($"[Inventory] 아이템 [{itemID}] {1}개가 구매되어 인벤토리에 들어왔습니다."); // 아이템 획득 주석
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
        // 1. 이미 존재하는 아이템인지 찾기
        // FindIndex를 사용하여 ItemEntry의 itemID가 일치하는지 확인합니다.
        int index = items.FindIndex(entry => entry.itemID == itemID);

        if (index != -1)
        {
            // 2. 이미 존재: 수량 증가 후 업데이트
            ItemEntry existingEntry = items[index];
            existingEntry.quantity += quantity;
            items[index] = existingEntry;

            Debug.Log($"[AddItemToList] 기존 아이템 수량 증가: {itemID}, 새 수량: {existingEntry.quantity}");
        }
        else
        {
            // 3. 새 아이템: 리스트에 추가
            items.Add(new ItemEntry(itemID, quantity));

            Debug.Log($"[AddItemToList] 새 아이템 추가: {itemID}, 수량: {quantity}");
        }

        if (pv.IsMine && questLog != null)
        {
            questLog.UpdateQuestProgress(itemID, GetItemCount(itemID));
        }
    }

    [PunRPC]
    public void RemoveItem(string itemID)
    {
        int quantityToRemove = 1;

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

        if (pv.IsMine && questLog != null)
        {
            questLog.UpdateQuestProgress(itemID, GetItemCount(itemID));
        }
    }

    public bool HasItem(string itemID)
    {
        ItemEntry entry = items.Find(e => e.itemID == itemID);
        return entry.quantity > 0;
    }

    private void UpdateLocalUI()
    {
        if (items.Count == 0)
        {
            Debug.Log("Inventory is empty.");
        }
        else
        {
            foreach (var entry in items)
            {
                Debug.Log($"Item ID: {entry.itemID}, Quantity: {entry.quantity}");
            }
        }

        if (UIManager.instance != null)
        {
            UIManager.instance.UpdateGoldText(gold);

            // 인벤토리 패널이 열려 있는 경우에만 아이템 목록을 갱신
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

    public int GetItemCount(string itemID)
    {
        int index = items.FindIndex(entry => entry.itemID == itemID);
        if (index != -1)
        {
            return items[index].quantity;
        }
        return 0;
    }

    [PunRPC]
    public void RpcChangeGold(int amount)
    {
        gold += amount;
        if (pv.IsMine) UIManager.instance.UpdateGoldText(gold);
    }


    [PunRPC]
    public void RpcAddItem(string itemID, int quantity)
    {
        AddItemToList(itemID, quantity);
        if (pv.IsMine) UpdateLocalUI();
    }



}