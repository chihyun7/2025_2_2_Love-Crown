using UnityEngine;

[System.Serializable]
public class Choice
{
    public string choiceText;
    public int likabilityChange;
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