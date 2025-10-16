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
        //�� üũ �߰�: ServerMasterClient�� �ʱ�ȭ�Ǿ����� Ȯ���մϴ�.
        if (ServerMasterClient.Instance == null)
        {
            Debug.LogError("���� ��û ����: ServerMasterClient �ν��Ͻ��� �������� �ʾ� RPC�� ���� �� �����ϴ�.");
            return;
        }

        // PV(PhotonView)�� ������ �� �� �� Ȯ���ϸ� �� �����մϴ�.
        if (ServerMasterClient.Instance.pv == null)
        {
            Debug.LogError("���� ��û ����: ServerMasterClient�� PhotonView(pv)�� �Ҵ���� �ʾҽ��ϴ�.");
            return;
        }

        // ��� �˻縦 ����ϸ� RPC ȣ�� ����
        ServerMasterClient.Instance.pv.RPC(
            "RpcRequestBuyItem",
            RpcTarget.MasterClient,
            itemID,
            PhotonNetwork.LocalPlayer
        );

        Debug.Log($"���� ��û ���۵�: ItemID={itemID}");
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