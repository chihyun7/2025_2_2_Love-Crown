using System.Collections.Generic;
using Photon.Pun;
using UnityEngine;
using Photon.Realtime;
using UnityEngine.SceneManagement;
using ExitGames.Client.Photon;
using System.Linq;

public class GameManager : MonoBehaviourPunCallbacks
{
    public static GameManager Instance;

    public PhotonView pv;
    private List<ItemData> itemDatabase = new List<ItemData>();

    private Dictionary<int, int> charmedCountPerPlayer = new Dictionary<int, int>();

    private List<QuestData> questDatabase = new List<QuestData>();

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
        SetupQuestDatabase();
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
            if (itemDatabase.Find(data => data.itemID == item.itemID) != null)
            {
                Debug.LogWarning($"[DB Setup] 중복된 Item ID 발견: {item.itemID}. 무시됨.");
                continue;
            }
            itemDatabase.Add(item);
        }

        Debug.Log($"Item Database 초기화 완료 ({itemDatabase.Count}개 아이템 로드).");
    }

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

    // ==========================
    // --- 상자 (GiftChest 보상) ---
    // ==========================
    [PunRPC]
    public void RpcOpenChest(int chestViewID, int playerActorNumber)
    {
        // 마스터만 처리
        if (!PhotonNetwork.IsMasterClient)
        {
            Debug.LogWarning("[GameManager] RpcOpenChest는 마스터에서만 처리됩니다.");
            return;
        }

        // 1) ViewID로 상자 PhotonView 찾기
        PhotonView chestPv = PhotonView.Find(chestViewID);
        if (chestPv == null)
        {
            Debug.LogWarning($"[GameManager] ViewID {chestViewID}에 해당하는 상자를 찾을 수 없습니다.");
            return;
        }

        GiftChest chest = chestPv.GetComponent<GiftChest>();
        if (chest == null)
        {
            Debug.LogWarning("[GameManager] GiftChest 컴포넌트를 찾을 수 없습니다.");
            return;
        }

        // 이미 열린 상자면 무시
        if (chest.IsOpened)
        {
            Debug.Log("[GameManager] 이미 열린 상자입니다. 요청 무시.");
            return;
        }

        // 2) 상자 열림 상태 동기화 (모든 클라)
        chest.photonView.RPC("RpcSetChestState", RpcTarget.All, true);
        Debug.Log("[GameManager] 상자 열림 상태를 모든 클라이언트에 동기화했습니다.");

        // 3) 보상 아이템 결정
        string rewardItemID = GetRandomRewardFromChest(chest);
        if (string.IsNullOrEmpty(rewardItemID))
        {
            Debug.LogWarning("[GameManager] rewardItemIDs가 비어 있어 보상을 지급하지 않았습니다.");
        }
        else
        {
            // 4) 보상 받을 플레이어의 Inventory 찾기
            Inventory playerInventory = FindPlayerInventory(playerActorNumber);

            if (playerInventory != null && playerInventory.pv != null)
            {
                // 퀘스트 보상과 동일한 패턴: RpcTarget.All 로 해당 인벤토리 인스턴스에만 적용
                playerInventory.pv.RPC("RpcAddItem", RpcTarget.All, rewardItemID, 1);

                Debug.Log($"[GameManager] Player {playerActorNumber}에게 상자 보상 지급: {rewardItemID}");
            }
            else
            {
                Debug.LogWarning("[GameManager] 대상 플레이어의 Inventory를 찾지 못했습니다. 보상 지급 실패.");
            }
        }

        // 5) 상자 리스폰 루틴 시작 (마스터에서만 동작)
        chest.StartRespawnOnMaster();
    }

    // 상자의 rewardItemIDs 리스트에서 확률 기반으로 1개 선택
    private string GetRandomRewardFromChest(GiftChest chest)
    {
        if (chest.rewardItemIDs == null || chest.rewardItemIDs.Count == 0)
            return null;

        // ★ 전제: rewardItemIDs
        //  - 앞 10개: 일반 아이템
        //  - 그 다음 4개: 방해 아이템
        int normalCount = Mathf.Min(10, chest.rewardItemIDs.Count);
        int trapCount = Mathf.Max(0, chest.rewardItemIDs.Count - normalCount); // 최대 4개 예상

        // 방해 아이템이 아직 안 들어가 있고, 일반 아이템만 있을 때는
        // 예전처럼 80%만 아이템, 20%는 없음 처리
        if (trapCount == 0)
        {
            float roll = Random.Range(0f, 1f);

            if (roll > 0.80f)
            {
                Debug.Log("[Chest] 이번에는 아이템 없음 (확률 20%)");
                return null;
            }

            int index = Random.Range(0, normalCount);
            string chosen = chest.rewardItemIDs[index];
            Debug.Log($"[Chest] 일반 아이템 지급 (방해아이템 미설정): {chosen}");
            return chosen;
        }

        // ★ 방해 아이템까지 다 들어간 경우
        //  - 일반 아이템: 10개 × 8% = 80%
        //  - 방해 아이템: 최대 4개 × 5% = 20%
        float r = Random.Range(0f, 100f);   // 0 ~ 100

        if (r < normalCount * 8f)
        {
            // 0 ~ 80 구간 → 일반 아이템
            int idx = Random.Range(0, normalCount);  // 0 ~ 9
            string chosen = chest.rewardItemIDs[idx];
            Debug.Log($"[Chest] 일반 아이템 지급: {chosen} (roll={r})");
            return chosen;
        }
        else
        {
            // 80 ~ 100 구간 → 방해 아이템 (각 5%로 총 20%)
            int trapIndexBase = normalCount;         // 리스트에서 방해 아이템 시작 위치
            int idx = Random.Range(0, trapCount);    // 0 ~ 3
            string chosen = chest.rewardItemIDs[trapIndexBase + idx];
            Debug.Log($"[Chest] 방해 아이템 지급: {chosen} (roll={r})");
            return chosen;
        }
    }


    // --- 호감도/귀속 ---
    [PunRPC]
    public void RpcRequestChangeLikability(int requesterActorID, int npcViewID, int likabilityChange, string giftItemID = null)
    {
        if (!PhotonNetwork.IsMasterClient) return;

        PhotonView npcView = PhotonView.Find(npcViewID);
        if (npcView == null) return;

        NPC targetNPC = npcView.GetComponent<NPC>();
        Inventory targetInventory = FindPlayerInventory(requesterActorID);
        if (targetNPC == null) return;

        // 이미 다른 플레이어에게 귀속된 경우 차단
        if (targetNPC.charmedByActorNumber != 0 && targetNPC.charmedByActorNumber != requesterActorID)
        {
            Debug.Log($"[Server] Player {requesterActorID}가 접근했지만 NPC는 이미 {targetNPC.charmedByActorNumber}에게 귀속됨.");
            return;
        }

        // 선물 소모 처리
        if (!string.IsNullOrEmpty(giftItemID) && targetInventory != null)
        {
            if (targetInventory.HasItem(giftItemID))
            {
                targetInventory.pv.RPC("RemoveItem", RpcTarget.All, giftItemID);
            }
            else
            {
                Debug.Log($"[Server] Player {requesterActorID} 선물 실패 - 아이템 {giftItemID} 없음");
                return;
            }
        }

        // 호감도 갱신 (서버 기준)
        int preLikability = targetNPC.likability;
        int postLikability = preLikability + likabilityChange;
        targetNPC.likability = postLikability;

        // 처음으로 임계치 도달 → 귀속 처리
        if (targetNPC.charmedByActorNumber == 0 && postLikability >= targetNPC.charmThreshold)
        {
            targetNPC.charmedByActorNumber = requesterActorID;

            if (!charmedCountPerPlayer.ContainsKey(requesterActorID))
                charmedCountPerPlayer[requesterActorID] = 0;
            charmedCountPerPlayer[requesterActorID]++;

            // 점수 갱신
            pv.RPC("RpcUpdateCharmedCount", RpcTarget.All, requesterActorID, charmedCountPerPlayer[requesterActorID]);

            // --- 귀속 정보 전파 ---
            string ownerName = PhotonNetwork.CurrentRoom.GetPlayer(requesterActorID)?.NickName ?? $"Player{requesterActorID}";

            if (PhotonNetwork.CurrentRoom.PlayerCount == 1)
            {
                NPC localNPC = npcView.GetComponent<NPC>();
                if (localNPC != null)
                {
                    Debug.Log($"[ServerMasterClient] 싱글 방이므로 '{ownerName}'에게 즉시 귀속 처리");
                    localNPC.RpcSetCharmOwner(requesterActorID, ownerName);
                }
            }
            else
            {
                npcView.RPC("RpcSetCharmOwner", RpcTarget.All, requesterActorID, ownerName);
            }
        }

        // 각 클라의 NPC UI 업데이트
        npcView.RPC("RpcChangeLikability", RpcTarget.All, likabilityChange);
    }

    [PunRPC]
    public void RpcUpdateCharmedCount(int actorNumber, int newCount)
    {
        var timer = FindObjectOfType<MiniGameTimer>();
        if (timer != null)
            Debug.Log($"[ServerMasterClient] Player {actorNumber} 현재 점수: {newCount}");
    }

    // --- 게임 종료/승자 발표 ---
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

    public override void OnLeftRoom()
    {
        if (Instance == this) Instance = null;
        Destroy(gameObject);
        Debug.Log("[ServerMasterClient] OnLeftRoom: 싱글톤 정리 완료. 로비 씬으로 이동합니다.");
    }

    private void SetupQuestDatabase()
    {
        QuestData[] allQuests = Resources.LoadAll<QuestData>("Quests");
        questDatabase = allQuests.ToList();
        Debug.Log($"Quest Database 초기화 완료 ({questDatabase.Count}개 퀘스트 로드).");
    }

    public QuestData GetQuestData(string questID)
    {
        return questDatabase.Find(quest => quest.questID == questID);
    }

    [PunRPC]
    public void RpcRequestQuestComplete(int requesterActorID, string questID, int npcViewID)
    {
        if (!PhotonNetwork.IsMasterClient) return;

        QuestData quest = GetQuestData(questID);
        Inventory playerInventory = FindPlayerInventory(requesterActorID);

        if (quest == null || playerInventory == null)
        {
            Debug.LogError("퀘스트 완료 요청 실패: 데이터 없음");
            return;
        }

        if (quest.rewardGold > 0)
        {
            playerInventory.pv.RPC("RpcChangeGold", RpcTarget.All, quest.rewardGold);
        }

        if (quest.rewardItem != null && quest.rewardItemQuantity > 0)
        {
            playerInventory.pv.RPC("RpcAddItem", RpcTarget.All, quest.rewardItem.itemID, quest.rewardItemQuantity);
        }

        if (quest.rewardLikability > 0)
        {
            PhotonView npcView = PhotonView.Find(npcViewID);
            if (npcView != null)
            {
                NPC targetNPC = npcView.GetComponent<NPC>();
                if (targetNPC != null)
                {
                    targetNPC.likability += quest.rewardLikability;

                    if (targetNPC.charmedByActorNumber == 0 && targetNPC.likability >= targetNPC.charmThreshold)
                    {
                        targetNPC.charmedByActorNumber = requesterActorID;

                        if (!charmedCountPerPlayer.ContainsKey(requesterActorID))
                            charmedCountPerPlayer[requesterActorID] = 0;
                        charmedCountPerPlayer[requesterActorID]++;

                        pv.RPC("RpcUpdateCharmedCount", RpcTarget.All, requesterActorID, charmedCountPerPlayer[requesterActorID]);

                        string ownerName = PhotonNetwork.CurrentRoom.GetPlayer(requesterActorID)?.NickName ?? $"Player{requesterActorID}";
                        npcView.RPC("RpcSetCharmOwner", RpcTarget.All, requesterActorID, ownerName);
                    }

                    npcView.RPC("RpcChangeLikability", RpcTarget.All, quest.rewardLikability);
                }
            }
        }

        playerInventory.pv.RPC("RpcCompleteQuest", RpcTarget.All, questID);

        Debug.Log($"[Server] Player {requesterActorID} 퀘스트 완료: {questID} (호감도 보상: {quest.rewardLikability})");
    }

}
