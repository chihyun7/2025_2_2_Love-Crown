using UnityEngine;

[CreateAssetMenu(fileName = "New Quest", menuName = "Inventory/Quest")]
public class QuestData : ScriptableObject
{
    public string questID; // 퀘스트 고유 ID (예: "NPC1_Rose_Quest")
    public string questName;
    [TextArea(3, 10)]
    public string description;

    [Header("퀘스트 목표")]
    public QuestObjective objective;

    [Header("퀘스트 보상")]
    public int rewardGold = 0;
    public ItemData rewardItem;
    public int rewardItemQuantity = 0;

    [Header("퀘스트 수락/완료 대화")]
    public Dialogue startDialogue; // 퀘스트를 제안하는 대화
    public Dialogue completionDialogue; // 퀘스트를 완료했을 때의 대화
}

// 퀘스트 목표를 정의하는 별도의 클래스
[System.Serializable]
public class QuestObjective
{
    public enum ObjectiveType { Collect, Talk, ReachLikability }

    public ObjectiveType type;
    public string targetItemID; // (Collect) 필요한 아이템 ID
    public int targetItemQuantity; // (Collect) 필요한 수량

    public string targetNPCName; // (Talk, Likability) 대상 NPC 이름
    public int targetLikability; // (Likability) 목표 호감도
}