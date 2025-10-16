using System.Collections.Generic;
using Photon.Pun;
using UnityEngine;
using Photon.Realtime;

public class ServerMasterClient : MonoBehaviourPunCallbacks
{
    public static ServerMasterClient Instance;

    public PhotonView pv;
    private Dictionary<string, ItemData> itemDatabase = new Dictionary<string, ItemData>();

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            pv = GetComponent<PhotonView>();

            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        SetupItemDatabase();
    }

    private void SetupItemDatabase()
    {
        ItemData[] allItems = Resources.LoadAll<ItemData>("Items");

        if (allItems.Length == 0)
        {
            Debug.LogError("Item Database �ε� ����: Resources/Items �������� ItemData ScriptableObject�� ã�� �� �����ϴ�.");
            return;
        }

        itemDatabase.Clear();
        foreach (ItemData item in allItems)
        {
            if (itemDatabase.ContainsKey(item.itemID))
            {
                Debug.LogWarning($"[DB Setup] �ߺ��� Item ID �߰�: {item.itemID}. �� �������� ���õ˴ϴ�.");
                continue;
            }
            itemDatabase.Add(item.itemID, item);
        }

        Debug.Log($"Item Database �ʱ�ȭ �Ϸ�. �� {itemDatabase.Count}���� ������ �ε��.");
    }

    private Inventory FindPlayerInventory(int actorNumber)
    {
        foreach (PhotonView view in FindObjectsOfType<PhotonView>())
        {
            if (view.Owner != null && view.Owner.ActorNumber == actorNumber)
            {
                return view.GetComponent<Inventory>();
            }
        }
        return null;
    }

    // ������ ���� ���� �Ǻ� (������ Ŭ���̾�Ʈ���� ����)
    [PunRPC]
    public void RpcRequestBuyItem(string itemID, Photon.Realtime.Player requesterPlayer, PhotonMessageInfo info)
    {
        // 1. ���� üũ(������ Ŭ���̾�Ʈ������ ����)
        if (!PhotonNetwork.IsMasterClient) return;

        // PhotonMessageInfo ��� ���޹��� Player ��ü���� ������ �����ɴϴ�.
        int requesterActorID = requesterPlayer.ActorNumber;
        string requesterNickName = requesterPlayer.NickName;

        Debug.Log($"[Server] ������ ���� ��û: {requesterNickName} (ID: {requesterActorID}) -> ������: {itemID}");

        // 1. ������ �����ͺ��̽����� ������ ����(����)�� ��ȸ
        if (!itemDatabase.TryGetValue(itemID, out ItemData itemData))
        {
            Debug.LogError($"[Server] �� �� ���� ������ ID: {itemID} ��û�� ���Խ��ϴ�.");
            return;
        }

        Inventory targetInventory = FindPlayerInventory(requesterActorID);

        if (targetInventory != null)
        {
            if (targetInventory.CanAfford(itemData.price))
            {
                Debug.Log($"[Server] {requesterNickName}�� ���� ����.");

                // �Ϲ������� Inventory�� ���� ������ All �Ǵ� Others�� ���۵˴ϴ�.
                targetInventory.pv.RPC("RpcExecuteBuy", RpcTarget.All, itemID, itemData.price);
            }
            else
            {
                Debug.Log($"[Server] ���� ����: {requesterNickName}�� ��尡 �����մϴ�.");
            }
        }
        else
        {
            Debug.LogError($"[Server] Actor ID {requesterActorID}�� �ش��ϴ� Inventory ������Ʈ�� ã�� �� �����ϴ�.");
        }
    }

    // ȣ���� ���� ��û (����/��ȭ ���� ���� ����)
    [PunRPC]
    public void RpcRequestChangeLikability(int requesterActorID, int npcViewID, int likabilityChange, string giftItemID = null)
    {
        if (!PhotonNetwork.IsMasterClient) return;

        PhotonView npcView = PhotonView.Find(npcViewID);

        if (npcView != null)
        {
            NPC targetNPC = npcView.GetComponent<NPC>();
            Inventory targetInventory = FindPlayerInventory(requesterActorID);

            // �����ϱ� ���� (giftItemID�� ���� ���)
            if (!string.IsNullOrEmpty(giftItemID) && targetInventory != null)
            {
                bool hasItem = targetInventory.HasItem(giftItemID);

                if (hasItem)
                {
                    // ������ ���� (Inventory�� RPC�� ȣ���ؾ� ������)
                    targetInventory.pv.RPC("RemoveItem", RpcTarget.All, giftItemID);
                    Debug.Log($"[Server] {targetInventory.pv.Owner.NickName}�� NPC ���� ����. ������ ({giftItemID}) ���ŵ�.");
                }
                else
                {
                    Debug.LogWarning($"[Server] ȣ���� ��û �ź�: �÷��̾�({requesterActorID})�� ������({giftItemID})�� ������ ���� �ʽ��ϴ�.");
                    return;
                }
            }
            // ���� ���� ����

            if (targetNPC != null)
            {
                Debug.Log($"[Server] �÷��̾� {requesterActorID}�� ��ȣ�ۿ� ����. NPC ȣ���� {likabilityChange} ���� ���.");

                // NPC���� ȣ������ �����϶�� ��� Ŭ���̾�Ʈ���� RPC ȣ��
                npcView.RPC("RpcChangeLikability", RpcTarget.All, likabilityChange);
            }
            else
            {
                Debug.LogError($"[Server] ViewID {npcViewID}�� ������Ʈ�� NPC ������Ʈ�� �����ϴ�.");
            }
        }
        else
        {
            Debug.LogError($"[Server] ViewID {npcViewID}�� �ش��ϴ� NPC ������Ʈ�� ã�� �� �����ϴ�.");
        }
    }
}