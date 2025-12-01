using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

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

[RequireComponent(typeof(PhotonView))]
public class PlayerQuestLog : MonoBehaviour
{
    private PhotonView pv;
    private Inventory inventory;

    public Dictionary<string, QuestStatus> activeQuests = new Dictionary<string, QuestStatus>();
    public List<string> completedQuestIDs = new List<string>();

    void Awake()
    {
        pv = GetComponent<PhotonView>();
        inventory = GetComponent<Inventory>();
    }

    public void AcceptQuest(QuestData quest)
    {
        if (pv.IsMine)
        {
            if (PhotonNetwork.IsConnected)
            {
                pv.RPC("RpcAddQuest", RpcTarget.All, quest.questID);
            }
            else
            {
                RpcAddQuest(quest.questID);
            }
        }
    }

    [PunRPC]
    private void RpcAddQuest(string questID)
    {
        if (!activeQuests.ContainsKey(questID) && !completedQuestIDs.Contains(questID))
        {
            activeQuests[questID] = new QuestStatus(questID);

            if (pv.IsMine && UIManager.instance != null)
            {
                UIManager.instance.UpdateQuestLogUI(this);
            }
        }
    }

    public void UpdateQuestProgress(string objectiveItemID, int quantity)
    {
        if (!pv.IsMine) return;

        foreach (var questStatus in activeQuests.Values)
        {
            QuestData quest = GameManager.Instance.GetQuestData(questStatus.questID);
            if (quest != null && !questStatus.isCompleted &&
                quest.objective.type == QuestObjective.ObjectiveType.Collect &&
                quest.objective.targetItemID == objectiveItemID)
            {
                int progress = inventory.GetItemCount(objectiveItemID);

                if (PhotonNetwork.IsConnected)
                    pv.RPC("RpcUpdateQuestProgress", RpcTarget.All, quest.questID, progress);
                else
                    RpcUpdateQuestProgress(quest.questID, progress);
            }
        }
    }

    [PunRPC]
    private void RpcUpdateQuestProgress(string questID, int newProgress)
    {
        if (activeQuests.ContainsKey(questID))
        {
            activeQuests[questID].currentProgress = newProgress;

            QuestData quest = GameManager.Instance.GetQuestData(questID);
            if (quest != null && newProgress >= quest.objective.targetItemQuantity)
            {
                
            }

            if (pv.IsMine && UIManager.instance != null)
            {
                UIManager.instance.UpdateQuestLogUI(this);
            }
        }
    }

    public void CompleteQuest(string questID)
    {
        if (pv.IsMine)
        {
            if (PhotonNetwork.IsConnected)
                pv.RPC("RpcCompleteQuest", RpcTarget.All, questID);
            else
                RpcCompleteQuest(questID);
        }
    }

    [PunRPC]
    private void RpcCompleteQuest(string questID)
    {
        if (activeQuests.ContainsKey(questID))
        {
            activeQuests.Remove(questID);
            completedQuestIDs.Add(questID);

            if (pv.IsMine && UIManager.instance != null)
            {
                UIManager.instance.UpdateQuestLogUI(this);
            }
        }
    }

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