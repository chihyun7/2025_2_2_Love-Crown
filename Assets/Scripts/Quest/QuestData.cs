using UnityEngine;

[CreateAssetMenu(fileName = "New Quest", menuName = "Inventory/Quest")]
public class QuestData : ScriptableObject
{
    public string questID;
    public string questName;
    [TextArea(3, 10)]
    public string description;

    [Header("퀘스트 목표")]
    public QuestObjective objective;

    [Header("퀘스트 보상")]
    public int rewardGold = 0;
    public int rewardLikability = 0;
    public ItemData rewardItem;
    public int rewardItemQuantity = 0;

    [Header("퀘스트 수락/완료 대화")]
    public Dialogue startDialogue;
    public Dialogue completionDialogue;
}

[System.Serializable]
public class QuestObjective
{
    public enum ObjectiveType { Collect, Talk, ReachLikability }

    public ObjectiveType type;
    public string targetItemID;
    public int targetItemQuantity;

    public string targetNPCName;
    public int targetLikability;
}