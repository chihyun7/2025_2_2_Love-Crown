using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;

// IPunObservable을 상속받아 Gold 변수의 상태를 동기화합니다.
public class Inventory : MonoBehaviourPunCallbacks, IPunObservable
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
    /// UIManager가 인벤토리 데이터를 읽어갈 수 있도록 하는 Getter 메서드입니다.
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

        // 3. UI 업데이트: 로컬 클라이언트에서만 실행 
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
                UIManager.instance.UpdateInventoryUI();
            }
        }
    }

    /// <summary>
    /// Gold 변수의 상태 동기화 로직입니다.
    /// 이 메서드가 작동하려면 PhotonView 컴포넌트의 Observed Components에 Inventory 스크립트가 할당되어 있어야 합니다.
    /// </summary>
    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting)
        {
            // 소유 클라이언트: 현재 골드 값을 모든 원격 클라이언트에게 보냅니다.
            stream.SendNext(gold);
        }
        else
        {
            // 원격 클라이언트: 소유자가 보낸 골드 값을 받습니다.
            int receivedGold = (int)stream.ReceiveNext();

            if (gold != receivedGold)
            {
                gold = receivedGold;

                // [수정] 동기화된 값이 변경되었을 때, 이 인벤토리가 로컬 플레이어의 것이라면 UI를 갱신합니다.
                if (pv.IsMine)
                {
                    UpdateLocalUI();
                }
            }
        }
    }
}