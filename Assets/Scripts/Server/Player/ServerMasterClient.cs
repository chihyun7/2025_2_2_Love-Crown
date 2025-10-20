using System.Collections.Generic;
using Photon.Pun;
using UnityEngine;
using Photon.Realtime;
using System.Linq;
using UnityEngine.SceneManagement; // 씬 전환을 위해 추가
using ExitGames.Client.Photon;

// ItemData, Inventory, NPC 클래스 정의가 필요합니다. (아래 PlaceholderClasses.cs 참조)

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
        // NOTE: Resources.LoadAll을 사용하려면 ItemData가 실제 ScriptableObject로 존재해야 합니다.
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
        // 이 로직은 해당 ActorNumber의 Owner를 가진 PhotonView에서 Inventory 컴포넌트를 찾습니다.
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

    [PunRPC]
    public void RpcRequestBuyItem(string itemID, Player requesterPlayer)
    {
        if (!PhotonNetwork.IsMasterClient) return;

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
            // 인벤토리에 구매 RPC를 요청합니다.
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

        // 이미 매혹되었고, 매혹한 플레이어와 요청자가 다르면 실패
        if (targetNPC.charmedByActorNumber != 0 && targetNPC.charmedByActorNumber != requesterActorID) return;

        // 선물 처리: 아이템이 있으면 제거하고 계속 진행
        if (!string.IsNullOrEmpty(giftItemID) && targetInventory != null)
        {
            if (targetInventory.HasItem(giftItemID))
            {
                // RemoveItem RPC 호출 (아이템 사용)
                targetInventory.pv.RPC("RemoveItem", RpcTarget.All, giftItemID);
            }
            else
            {
                return; // 아이템이 없으면 실패
            }
        }

        // 호감도 변경
        int preLikability = targetNPC.likability;
        int postLikability = preLikability + likabilityChange;
        targetNPC.likability = postLikability;

        // 매혹 조건 확인 및 점수 업데이트
        if (targetNPC.charmedByActorNumber == 0 && postLikability >= targetNPC.charmThreshold)
        {
            targetNPC.charmedByActorNumber = requesterActorID;

            if (!charmedCountPerPlayer.ContainsKey(requesterActorID))
                charmedCountPerPlayer[requesterActorID] = 0;
            charmedCountPerPlayer[requesterActorID]++;

            // 모든 클라이언트에 점수 업데이트를 알립니다.
            pv.RPC("RpcUpdateCharmedCount", RpcTarget.All, requesterActorID, charmedCountPerPlayer[requesterActorID]);
        }

        // NPC 자신에게 호감도 변경을 알려 UI 업데이트 등을 수행하게 합니다.
        npcView.RPC("RpcChangeLikability", RpcTarget.All, likabilityChange);
    }

    [PunRPC]
    public void RpcUpdateCharmedCount(int actorNumber, int newCount)
    {
        // 점수 업데이트 시 디버그 로그를 출력합니다.
        var timer = FindObjectOfType<MiniGameTimer>();
        if (timer != null)
            Debug.Log($"[ServerMasterClient] Player {actorNumber} 현재 점수: {newCount}");
    }

    /// <summary>
    /// 게임 종료 시 호출되어 승자를 판정하고 결과를 전파합니다. (마스터 전용)
    /// </summary>
    public void AnnounceWinner()
    {
        if (!PhotonNetwork.IsMasterClient) return;

        int winnerActor = 0;
        int topScore = int.MinValue;
        int tieCount = 0;

        // 1. 최고 점수 계산
        foreach (var p in PhotonNetwork.CurrentRoom.Players)
        {
            int actor = p.Value.ActorNumber;
            int score = GetCharmedCount(actor);
            if (score > topScore) { topScore = score; winnerActor = actor; tieCount = 1; }
            else if (score == topScore) { tieCount++; }
        }

        // 2. 동점 처리 (동점 시 승자 없음 = 0)
        if (tieCount >= 2) winnerActor = 0;

        // 3. P1/P2 점수 계산 (최대 2인 플레이어로 가정)
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

        // 4. 모든 클라이언트에 RPC로 결과 전파
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

        // MiniGameTimer의 RPC를 호출하여 결과 UI 표시 요청
        MiniGameTimer timer = FindObjectOfType<MiniGameTimer>();
        if (timer != null)
        {
            timer.GetComponent<PhotonView>()?.RPC("RpcShowResult", RpcTarget.All, message);
        }

        Debug.Log($"[ServerMasterClient] 게임 종료! {message}");
    }

    /// <summary>
    /// 방을 나갈 때 (게임 종료 후 로비 이동 시) 싱글톤 객체를 정리하고 씬을 전환합니다.
    /// </summary>
    public override void OnLeftRoom()
    {
        if (Instance == this)
        {
            Instance = null;
        }
        // DontDestroyOnLoad 객체 파괴
        Destroy(gameObject);

        Debug.Log("[ServerMasterClient] OnLeftRoom: 싱글톤 정리 완료. 로비 씬으로 이동합니다.");
    }
}