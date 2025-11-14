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
                RequestPurchase(itemIDsToSell[0]);
            }
        }
    }

    public void RequestPurchase(string itemID)
    {
        if (GameManager.Instance == null)
        {
            Debug.LogError("구매 요청 실패: ServerMasterClient 인스턴스가 존재하지 않아 RPC를 보낼 수 없습니다.");
            return;
        }

        if (GameManager.Instance.pv == null)
        {
            Debug.LogError("구매 요청 실패: ServerMasterClient의 PhotonView(pv)가 할당되지 않았습니다.");
            return;
        }

        GameManager.Instance.pv.RPC(
            "RpcRequestBuyItem",
            RpcTarget.MasterClient,
            itemID,
            PhotonNetwork.LocalPlayer
        );

        Debug.Log($"구매 요청 전송됨: ItemID={itemID}");
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