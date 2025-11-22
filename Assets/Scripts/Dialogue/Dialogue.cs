using UnityEngine;

[System.Serializable]
public class Choice
{
    public string choiceText;
    public int likabilityChange;

    public enum ChoiceAction { Normal, AcceptQuest, RejectQuest, CompleteQuest, ExitDialogue }
    public ChoiceAction action = ChoiceAction.Normal;
}

[System.Serializable]
public class DialogueLine
{
    public bool isPlayer;

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