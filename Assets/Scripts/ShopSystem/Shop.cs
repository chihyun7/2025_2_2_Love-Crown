using Photon.Pun;
using System.Collections.Generic;
using UnityEngine;

public class Shop : MonoBehaviour
{
    private Inventory localPlayerInventory = null;
    private bool playerIsClose = false;

    [Header("판매 아이템 목록")]
    public string[] itemIDsToSell;

    public List<ItemData> itemsForSale = new List<ItemData>();

    void Update()
    {
        if (playerIsClose && localPlayerInventory != null && Input.GetKeyDown(KeyCode.E))
        {
            if (itemIDsToSell.Length > 0)
            {
                // 현재는 첫 번째 아이템 구매 요청만 한다고 가정
                RequestPurchase(itemIDsToSell[0]);
            }
        }
    }

    public void RequestPurchase(string itemID)
    {
        if (localPlayerInventory == null || ServerMasterClient.Instance == null)
        {
            Debug.LogError("구매 요청 실패: 인벤토리 또는 마스터 클라이언트 참조가 없습니다.");
            return;
        }

        // ServerMasterClient의 RpcRequestBuyItem 함수를 마스터 클라이언트에게만 호출
        ServerMasterClient.Instance.pv.RPC("RpcRequestBuyItem", RpcTarget.MasterClient, itemID);

        Debug.Log($"구매 요청 전송: {itemID} (To Master Client)");
    }

    private void OnTriggerEnter(Collider other)
    {
        PhotonView otherPv = other.GetComponent<PhotonView>();
        if (other.CompareTag("Player") && otherPv != null && otherPv.IsMine)
        {
            playerIsClose = true;
            localPlayerInventory = other.GetComponent<Inventory>();
        }
    }

    private void OnTriggerExit(Collider other)
    {
        PhotonView otherPv = other.GetComponent<PhotonView>();
        if (other.CompareTag("Player") && otherPv != null && otherPv.IsMine)
        {
            playerIsClose = false;
            localPlayerInventory = null;
        }
    }
}