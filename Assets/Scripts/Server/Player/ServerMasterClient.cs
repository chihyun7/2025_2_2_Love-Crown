using System.Collections.Generic;
using Photon.Pun;
using UnityEngine;
using Photon.Realtime;

public class ServerMasterClient : MonoBehaviourPunCallbacks
{
    public static ServerMasterClient Instance;

    public PhotonView pv;
    private Dictionary<string, ItemData> itemDatabase = new Dictionary<string, ItemData>();

    // 🔹 추가: 누가 몇 명의 NPC를 꼬셨는지 집계
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
            if (itemDatabase.ContainsKey(item.itemID))
            {
                Debug.LogWarning($"[DB Setup] 중복된 Item ID 발견: {item.itemID}. 무시됨.");
                continue;
            }
            itemDatabase.Add(item.itemID, item);
        }

        Debug.Log($"Item Database 초기화 완료 ({itemDatabase.Count}개 아이템 로드).");
    }

    // 플레이어 인벤토리 찾기
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

    // 점수 조회 (MiniGameManager / AnnounceWinner 에서 사용)
    public int GetCharmedCount(int actorNumber)
    {
        return charmedCountPerPlayer.TryGetValue(actorNumber, out var v) ? v : 0;
    }

    // NPC 호감도 변경 RPC (이미 존재하는 버전 그대로 유지)
    [PunRPC]
    public void RpcRequestChangeLikability(int requesterActorID, int npcViewID, int likabilityChange, string giftItemID = null)
    {
        if (!PhotonNetwork.IsMasterClient) return;

        PhotonView npcView = PhotonView.Find(npcViewID);
        if (npcView == null)
        {
            Debug.LogError($"[Server] NPC ViewID {npcViewID} 를 찾을 수 없습니다.");
            return;
        }

        NPC targetNPC = npcView.GetComponent<NPC>();
        Inventory targetInventory = FindPlayerInventory(requesterActorID);

        if (targetNPC == null)
        {
            Debug.LogError($"[Server] ViewID {npcViewID} 객체에 NPC 컴포넌트가 없습니다.");
            return;
        }

        // 🔒 이미 다른 플레이어가 선점했으면 차단
        if (targetNPC.charmedByActorNumber != 0 && targetNPC.charmedByActorNumber != requesterActorID)
        {
            Debug.Log($"[Server] Player {requesterActorID} 가 접근했지만 NPC({npcViewID})는 이미 Player {targetNPC.charmedByActorNumber}가 꼬심.");
            return;
        }

        // 선물 처리
        if (!string.IsNullOrEmpty(giftItemID) && targetInventory != null)
        {
            if (targetInventory.HasItem(giftItemID))
            {
                targetInventory.pv.RPC("RemoveItem", RpcTarget.All, giftItemID);
            }
            else
            {
                Debug.LogWarning($"[Server] Player {requesterActorID} 선물 실패: {giftItemID} 없음");
                return;
            }
        }

        // 호감도 변경
        int preLikability = targetNPC.likability;
        int postLikability = preLikability + likabilityChange;
        targetNPC.likability = postLikability;

        // 70 이상 처음 돌파 시 선점 및 점수 증가
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

    // 점수 갱신 RPC (HUD용)
    [PunRPC]
    public void RpcUpdateCharmedCount(int actorNumber, int newCount)
    {
        var timer = FindObjectOfType<MiniGameTimer>();
        if (timer != null)
            Debug.Log($"[ServerMasterClient] Player {actorNumber} 현재 점수: {newCount}");
    }

    // === 🔸 승자 판정 및 알림 ===
    public void AnnounceWinner()
    {
        if (!PhotonNetwork.IsMasterClient) return;

        int winnerActor = 0;
        int topScore = int.MinValue;
        int tieCount = 0;

        // 플레이어별 점수 비교
        foreach (var p in PhotonNetwork.CurrentRoom.Players)
        {
            int actor = p.Value.ActorNumber;
            int score = GetCharmedCount(actor);
            if (score > topScore)
            {
                topScore = score;
                winnerActor = actor;
                tieCount = 1;
            }
            else if (score == topScore)
            {
                tieCount++;
            }
        }

        if (tieCount >= 2)
            winnerActor = 0; // 무승부 처리

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
            message = $"무승부!\nP1: {p1Score}  |  P2: {p2Score}";
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

            message = $"승자: {winnerName}\n\nP1: {p1Score}  |  P2: {p2Score}";
        }

        MiniGameTimer timer = FindObjectOfType<MiniGameTimer>();
        if (timer != null)
        {
            timer.photonView.RPC("RpcShowResult", RpcTarget.All, message);
        }

        Debug.Log($"[ServerMasterClient] 게임 종료! {message}");
    }
}
