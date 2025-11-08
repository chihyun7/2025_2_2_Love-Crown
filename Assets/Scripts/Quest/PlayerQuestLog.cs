using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

// [System.Serializable]은 인스펙터에 노출시키기 위함이 아닌,
// 이 클래스 자체를 데이터 묶음으로 사용하기 위함입니다.
[System.Serializable]
public class QuestStatus
{
    public string questID;
    public int currentProgress;
    public bool isCompleted;

    public QuestStatus(string id)
    {
        questID = id;
        currentProgress = 0;
        isCompleted = false;
    }
}

// 이 스크립트는 Player 프리팹에 Inventory.cs와 함께 추가합니다.
[RequireComponent(typeof(PhotonView))]
public class PlayerQuestLog : MonoBehaviour
{
    private PhotonView pv;
    private Inventory inventory;

    // 퀘스트 ID(string)를 키로 사용하여 퀘스트 상태(QuestStatus)를 관리
    public Dictionary<string, QuestStatus> activeQuests = new Dictionary<string, QuestStatus>();
    public List<string> completedQuestIDs = new List<string>();

    void Awake()
    {
        pv = GetComponent<PhotonView>();
        inventory = GetComponent<Inventory>();
    }

    // 퀘스트 수락 (NPC가 호출)
    public void AcceptQuest(QuestData quest)
    {
        if (pv.IsMine)
        {
            pv.RPC("RpcAddQuest", RpcTarget.All, quest.questID);
        }
    }

    [PunRPC]
    private void RpcAddQuest(string questID)
    {
        if (!activeQuests.ContainsKey(questID) && !completedQuestIDs.Contains(questID))
        {
            activeQuests[questID] = new QuestStatus(questID);
            Debug.Log($"퀘스트 수락됨: {questID}");
            // 퀘스트 UI 갱신 호출
            if (pv.IsMine) UIManager.instance.UpdateQuestLogUI(this);
        }
    }

    // 퀘스트 진행도 업데이트 (인벤토리 등에서 호출)
    public void UpdateQuestProgress(string objectiveItemID, int quantity)
    {
        if (!pv.IsMine) return;

        foreach (var questStatus in activeQuests.Values)
        {
            QuestData quest = ServerMasterClient.Instance.GetQuestData(questStatus.questID);
            if (quest != null && !questStatus.isCompleted &&
                quest.objective.type == QuestObjective.ObjectiveType.Collect &&
                quest.objective.targetItemID == objectiveItemID)
            {
                // 이 퀘스트는 이 아이템을 필요로 함
                int progress = inventory.GetItemCount(objectiveItemID);
                pv.RPC("RpcUpdateQuestProgress", RpcTarget.All, quest.questID, progress);
            }
        }
    }

    [PunRPC]
    private void RpcUpdateQuestProgress(string questID, int newProgress)
    {
        if (activeQuests.ContainsKey(questID))
        {
            activeQuests[questID].currentProgress = newProgress;
            Debug.Log($"퀘스트 진행도 갱신: {questID} - {newProgress}");

            QuestData quest = ServerMasterClient.Instance.GetQuestData(questID);
            if (quest != null && newProgress >= quest.objective.targetItemQuantity)
            {
                activeQuests[questID].isCompleted = true; // 완료 상태로 변경
                Debug.Log($"퀘스트 완료 가능: {questID}");
            }
            if (pv.IsMine) UIManager.instance.UpdateQuestLogUI(this);
        }
    }

    // 퀘스트 완료 처리 (NPC가 호출)
    public void CompleteQuest(string questID)
    {
        if (pv.IsMine)
        {
            pv.RPC("RpcCompleteQuest", RpcTarget.All, questID);
        }
    }

    [PunRPC]
    private void RpcCompleteQuest(string questID)
    {
        if (activeQuests.ContainsKey(questID))
        {
            activeQuests.Remove(questID);
            completedQuestIDs.Add(questID);
            Debug.Log($"퀘스트 완료 처리됨: {questID}");
            if (pv.IsMine) UIManager.instance.UpdateQuestLogUI(this);
        }
    }

    // NPC가 퀘스트 상태를 확인할 수 있도록 헬퍼 함수 제공
    public QuestStatus GetQuestStatus(string questID)
    {
        if (activeQuests.TryGetValue(questID, out QuestStatus status))
        {
            return status;
        }
        return null;
    }

    public bool HasCompletedQuest(string questID)
    {
        return completedQuestIDs.Contains(questID);
    }
}