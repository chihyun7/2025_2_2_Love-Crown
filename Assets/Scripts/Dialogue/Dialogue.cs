using UnityEngine;

[System.Serializable]
public class Choice
{
    public string choiceText;
    public int likabilityChange; // 호감도 변화량 (0이면 변화 없음)

    public enum ChoiceAction { Normal, AcceptQuest, RejectQuest }
    public ChoiceAction action = ChoiceAction.Normal;
}

[System.Serializable]
public class DialogueLine
{
    [TextArea(2, 5)]
    public string sentence;
    public Choice[] choices;
}

[System.Serializable]
public class Dialogue
{
    public string name;
    public DialogueLine[] lines;
}