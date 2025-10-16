using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;

public class Inventory : MonoBehaviourPunCallbacks
{
    public PhotonView pv;

    // 아이템 ID(string)와 수량(int)을 추적하는 Dictionary를 사용합니다.
    private Dictionary<string, int> items = new Dictionary<string, int>();

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

    /// <summary>
    /// UIManager가 인벤토리 데이터를 읽어갈 수 있도록 하는 Getter 메서드입니다. (누락된 부분)
    /// </summary>
    public Dictionary<string, int> GetItems()
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
        // 1. 골드 차감 
        gold -= price;

        // 2. 인벤토리에 아이템 추가
        if (items.ContainsKey(itemID))
        {
            items[itemID]++;
        }
        else
        {
            items.Add(itemID, 1);
        }

        // 3. UI 업데이트: 로컬 클라이언트에서만 실행 (멀티플레이 환경에서 필수)
        if (pv.IsMine)
        {
            UpdateLocalUI();
        }

        Debug.Log($"[Inventory] {pv.Owner.NickName} 구매 실행 완료: {itemID}. 남은 골드: {gold}");
    }

    public void AddItem(string itemID, int quantity = 1)
    {
        if (items.ContainsKey(itemID))
        {
            items[itemID] += quantity;
        }
        else
        {
            items.Add(itemID, quantity);
        }

        if (pv.IsMine)
        {
            UpdateLocalUI();
        }
    }

    public void RemoveItem(string itemID, int quantity = 1)
    {
        if (items.ContainsKey(itemID))
        {
            items[itemID] -= quantity;
            if (items[itemID] <= 0)
            {
                items.Remove(itemID);
            }
        }

        if (pv.IsMine)
        {
            UpdateLocalUI();
        }
    }

    public bool HasItem(string itemID)
    {
        return items.ContainsKey(itemID) && items[itemID] > 0;
    }

    // UI 업데이트 함수: 로컬 플레이어 전용
    private void UpdateLocalUI()
    {
        if (UIManager.instance != null)
        {
            // 골드 텍스트 업데이트
            UIManager.instance.UpdateGoldText(gold);

            // 인벤토리 패널이 열려있는 경우에만 목록 UI 업데이트
            if (UIManager.instance.inventoryPanel != null && UIManager.instance.inventoryPanel.activeInHierarchy)
            {
                // UIManager에서 GetItems()를 호출하여 목록을 그립니다.
                UIManager.instance.UpdateInventoryUI();
            }
        }
    }
}