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
            Debug.LogError("Item Database 로드 실패: Resources/Items 폴더에서 ItemData ScriptableObject를 찾을 수 없습니다.");
            return;
        }

        itemDatabase.Clear();
        foreach (ItemData item in allItems)
        {
            if (itemDatabase.ContainsKey(item.itemID))
            {
                Debug.LogWarning($"[DB Setup] 중복된 Item ID 발견: {item.itemID}. 이 아이템은 무시됩니다.");
                continue;
            }
            itemDatabase.Add(item.itemID, item);
        }

        Debug.Log($"Item Database 초기화 완료. 총 {itemDatabase.Count}개의 아이템 로드됨.");
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

    // 아이템 구매 여부 판별 (마스터 클라이언트에서 실행)
    [PunRPC]
    public void RpcRequestBuyItem(string itemID, PhotonMessageInfo info)
    {
        if (!PhotonNetwork.IsMasterClient) return;

        int requesterActorID = info.Sender.ActorNumber;

        if (!itemDatabase.TryGetValue(itemID, out ItemData itemData))
        {
            Debug.LogError($"[Server] 알 수 없는 아이템 ID: {itemID} 요청이 들어왔습니다.");
            return;
        }

        Inventory targetInventory = FindPlayerInventory(requesterActorID);

        if (targetInventory != null)
        {
            if (targetInventory.CanAfford(itemData.price))
            {
                Debug.Log($"[Server] {info.Sender.NickName}의 구매 승인.");

                targetInventory.pv.RPC("RpcExecuteBuy", RpcTarget.All, itemID, itemData.price);
            }
            else
            {
                Debug.Log($"[Server] 구매 거절: {info.Sender.NickName}의 골드가 부족합니다.");
            }
        }
        else
        {
            Debug.LogError($"[Server] Actor ID {requesterActorID}에 해당하는 Inventory 컴포넌트를 찾을 수 없습니다.");
        }
    }

    // 호감도 변경 요청 (선물/대화 선택 로직 통합)
    [PunRPC]
    public void RpcRequestChangeLikability(int requesterActorID, int npcViewID, int likabilityChange, string giftItemID = null)
    {
        if (!PhotonNetwork.IsMasterClient) return;

        PhotonView npcView = PhotonView.Find(npcViewID);

        if (npcView != null)
        {
            NPC targetNPC = npcView.GetComponent<NPC>();
            Inventory targetInventory = FindPlayerInventory(requesterActorID);

            // 선물하기 로직 (giftItemID가 있을 경우)
            if (!string.IsNullOrEmpty(giftItemID) && targetInventory != null)
            {
                bool hasItem = targetInventory.HasItem(giftItemID);

                if (hasItem)
                {
                    // 아이템 제거 (Inventory의 RPC를 호출해야 안전함)
                    targetInventory.pv.RPC("RemoveItem", RpcTarget.All, giftItemID);
                    Debug.Log($"[Server] {targetInventory.pv.Owner.NickName}의 NPC 선물 승인. 아이템 ({giftItemID}) 제거됨.");
                }
                else
                {
                    Debug.LogWarning($"[Server] 호감도 요청 거부: 플레이어({requesterActorID})가 아이템({giftItemID})을 가지고 있지 않습니다.");
                    return;
                }
            }
            // 선물 로직 종료

            if (targetNPC != null)
            {
                Debug.Log($"[Server] 플레이어 {requesterActorID}의 상호작용 승인. NPC 호감도 {likabilityChange} 변경 명령.");

                // NPC에게 호감도를 변경하라고 모든 클라이언트에게 RPC 호출
                npcView.RPC("RpcChangeLikability", RpcTarget.All, likabilityChange);
            }
            else
            {
                Debug.LogError($"[Server] ViewID {npcViewID}의 오브젝트에 NPC 컴포넌트가 없습니다.");
            }
        }
        else
        {
            Debug.LogError($"[Server] ViewID {npcViewID}에 해당하는 NPC 오브젝트를 찾을 수 없습니다.");
        }
    }
}