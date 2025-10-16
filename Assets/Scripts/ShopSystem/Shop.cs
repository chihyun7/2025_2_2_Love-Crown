using Photon.Pun;
using System.Collections.Generic;
using UnityEngine;

public class Shop : MonoBehaviour
{
    private Inventory localPlayerInventory = null;
    private bool playerIsClose = false;

    [Header("�Ǹ� ������ ���")]
    public string[] itemIDsToSell;

    public List<ItemData> itemsForSale = new List<ItemData>();

    void Update()
    {
        if (playerIsClose && localPlayerInventory != null && Input.GetKeyDown(KeyCode.E))
        {
            if (itemIDsToSell.Length > 0)
            {
                // ����� ù ��° ������ ���� ��û�� �Ѵٰ� ����
                RequestPurchase(itemIDsToSell[0]);
            }
        }
    }

    public void RequestPurchase(string itemID)
    {
        if (localPlayerInventory == null || ServerMasterClient.Instance == null)
        {
            Debug.LogError("���� ��û ����: �κ��丮 �Ǵ� ������ Ŭ���̾�Ʈ ������ �����ϴ�.");
            return;
        }

        // ServerMasterClient�� RpcRequestBuyItem �Լ��� ������ Ŭ���̾�Ʈ���Ը� ȣ��
        ServerMasterClient.Instance.pv.RPC("RpcRequestBuyItem", RpcTarget.MasterClient, itemID);

        Debug.Log($"���� ��û ����: {itemID} (To Master Client)");
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