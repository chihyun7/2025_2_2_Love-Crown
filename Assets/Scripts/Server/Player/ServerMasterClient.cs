using System.Collections.Generic;
using Photon.Pun;
using UnityEngine;
using Photon.Realtime;
using System.Linq;

// ItemData 클래스가 프로젝트에 정의되어 있어야 합니다. (ScriptableObject로 추정)

public class ServerMasterClient : MonoBehaviourPunCallbacks
{
    public static ServerMasterClient Instance;

    public PhotonView pv;
    // 🚨 List<ItemData>를 사용하도록 유지
    private List<ItemData> itemDatabase = new List<ItemData>();

    private Dictionary<int, int> charmedCountPerPlayer = new Dictionary<int, int>();

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
            Debug.LogError("Item Database 로드 실패: Resources/Items 폴더에서 ItemData를 찾을 수 없습니다.");
            return;
        }

        itemDatabase.Clear();
        foreach (ItemData item in allItems)
        {
            if (itemDatabase.Find(data => data.itemID == item.itemID) != null) // List 중복 체크
            {
                Debug.LogWarning($"[DB Setup] 중복된 Item ID 발견: {item.itemID}. 무시됨.");
                continue;
            }
            itemDatabase.Add(item);
        }

        Debug.Log($"Item Database 초기화 완료 ({itemDatabase.Count}개 아이템 로드).");
    }

    // List<ItemData>에서 ItemData 검색
    public ItemData GetItemData(string itemID)
    {
        return itemDatabase.Find(item => item.itemID == itemID);
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

    public int GetCharmedCount(int actorNumber)
    {
        return charmedCountPerPlayer.TryGetValue(actorNumber, out var v) ? v : 0;
    }

    // 🚨 RPC 수정 1: 서명을 (string, Player)로 일치시켜 오류 해결
    [PunRPC]
    public void RpcRequestBuyItem(string itemID, Player requesterPlayer)
    {
        if (!PhotonNetwork.IsMasterClient) return;

        // 🚨 List<ItemData>에서 Find 메서드를 사용하여 아이템을 찾음 (TryGetValue 오류 해결)
        ItemData itemData = GetItemData(itemID);

        if (itemData == null)
        {
            Debug.LogWarning($"[Server] 구매 요청된 ItemID: {itemID}를 데이터베이스에서 찾을 수 없습니다.");
            return;
        }

        int requesterActorID = requesterPlayer.ActorNumber;
        Inventory playerInventory = FindPlayerInventory(requesterActorID);

        if (playerInventory != null && playerInventory.CanAfford(itemData.price))
        {
            playerInventory.pv.RPC("RpcExecuteBuy", RpcTarget.All, itemID, itemData.price);
        }
    }


    // NPC 호감도 변경 RPC
    [PunRPC]
    public void RpcRequestChangeLikability(int requesterActorID, int npcViewID, int likabilityChange, string giftItemID = null)
    {
        if (!PhotonNetwork.IsMasterClient) return;

        PhotonView npcView = PhotonView.Find(npcViewID);
        if (npcView == null) return;

        NPC targetNPC = npcView.GetComponent<NPC>();
        Inventory targetInventory = FindPlayerInventory(requesterActorID);

        if (targetNPC == null) return;

        if (targetNPC.charmedByActorNumber != 0 && targetNPC.charmedByActorNumber != requesterActorID) return;

        // 선물 처리
        if (!string.IsNullOrEmpty(giftItemID) && targetInventory != null)
        {
            if (targetInventory.HasItem(giftItemID))
            {
                // RemoveItem RPC 호출
                targetInventory.pv.RPC("RemoveItem", RpcTarget.All, giftItemID);
            }
            else
            {
                return;
            }
        }

        // 호감도 변경
        int preLikability = targetNPC.likability;
        int postLikability = preLikability + likabilityChange;
        targetNPC.likability = postLikability;

        if (targetNPC.charmedByActorNumber == 0 && postLikability >= targetNPC.charmThreshold)
        {
            targetNPC.charmedByActorNumber = requesterActorID;

            if (!charmedCountPerPlayer.ContainsKey(requesterActorID))
                charmedCountPerPlayer[requesterActorID] = 0;
            charmedCountPerPlayer[requesterActorID]++;

            pv.RPC("RpcUpdateCharmedCount", RpcTarget.All, requesterActorID, charmedCountPerPlayer[requesterActorID]);
        }

        npcView.RPC("RpcChangeLikability", RpcTarget.All, likabilityChange);
    }

    [PunRPC]
    public void RpcUpdateCharmedCount(int actorNumber, int newCount)
    {
        var timer = FindObjectOfType<MiniGameTimer>();
        if (timer != null)
            Debug.Log($"[ServerMasterClient] Player {actorNumber} 현재 점수: {newCount}");
    }

    public void AnnounceWinner()
    {
        if (!PhotonNetwork.IsMasterClient) return;

        int winnerActor = 0;
        int topScore = int.MinValue;
        int tieCount = 0;

        foreach (var p in PhotonNetwork.CurrentRoom.Players)
        {
            int actor = p.Value.ActorNumber;
            int score = GetCharmedCount(actor);
            if (score > topScore) { topScore = score; winnerActor = actor; tieCount = 1; }
            else if (score == topScore) { tieCount++; }
        }

        if (tieCount >= 2) winnerActor = 0;

        int p1Actor = 0, p1Score = 0, p2Actor = 0, p2Score = 0;
        if (PhotonNetwork.CurrentRoom.PlayerCount >= 1)
        {
            var a = PhotonNetwork.PlayerList[0].ActorNumber;
            p1Actor = a; p1Score = GetCharmedCount(a);
        }
        if (PhotonNetwork.CurrentRoom.PlayerCount >= 2)
        {
            var b = PhotonNetwork.PlayerList[1].ActorNumber;
            p2Actor = b; p2Score = GetCharmedCount(b);
        }

        pv.RPC("RpcAnnounceWinner", RpcTarget.All, winnerActor, p1Actor, p1Score, p2Actor, p2Score);
    }

    [PunRPC]
    public void RpcAnnounceWinner(int winnerActorNumber, int p1Actor, int p1Score, int p2Actor, int p2Score)
    {
        string message;

        if (winnerActorNumber == 0)
        {
            message = $"무승부!\nP1: {p1Score} | P2: {p2Score}";
        }
        else
        {
            string winnerName = "(알 수 없음)";
            foreach (var p in PhotonNetwork.PlayerList)
            {
                if (p.ActorNumber == winnerActorNumber)
                {
                    winnerName = p.NickName;
                    break;
                }
            }

            message = $"승자: {winnerName}\n\nP1: {p1Score} | P2: {p2Score}";
        }

        MiniGameTimer timer = FindObjectOfType<MiniGameTimer>();
        if (timer != null)
        {
            timer.GetComponent<PhotonView>()?.RPC("RpcShowResult", RpcTarget.All, message);
        }

        Debug.Log($"[ServerMasterClient] 게임 종료! {message}");
    }
}