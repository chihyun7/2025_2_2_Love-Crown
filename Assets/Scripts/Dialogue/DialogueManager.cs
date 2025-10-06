using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class DialogueManager : MonoBehaviour
{
    public GameObject dialoguePanel;
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI sentenceText;
    public TextMeshProUGUI likabilityText;
    public GameObject choiceLayout;
    public GameObject choiceButtonPrefab;

    public static bool IsDialogueActive = false;
    private Queue<DialogueLine> dialogueLines;
    private NPC currentNpc;

    public void StartDialogue(Dialogue dialogue, NPC npc)
    {
        IsDialogueActive = true;
        currentNpc = npc;
        FindObjectOfType<PlayerMove>().canMove = false;

        dialoguePanel.SetActive(true);
        likabilityText.gameObject.SetActive(true);
        UpdateLikabilityUI();

        nameText.text = currentNpc.name;
        dialogueLines.Clear();

        foreach (DialogueLine line in dialogue.lines)
        {
            dialogueLines.Enqueue(line);
        }

        DisplayNextLine();
    }

    void UpdateLikabilityUI()
    {
        if (currentNpc != null)
        {
            likabilityText.text = "È£°¨µµ: " + currentNpc.likability;
        }
    }

    private void OnChoiceSelected(int likabilityChange)
    {
        currentNpc.likability += likabilityChange;
        UpdateLikabilityUI();
        DisplayNextLine();
    }

    void Start() { dialogueLines = new Queue<DialogueLine>(); }
    void EndDialogue()
    {
        IsDialogueActive = false;
        dialoguePanel.SetActive(false);
        likabilityText.gameObject.SetActive(false);
        FindObjectOfType<PlayerMove>().canMove = true;
        currentNpc = null;
    }
    public void DisplayNextLine()
    {
        ClearChoiceButtons();
        if (dialogueLines.Count == 0) { EndDialogue(); return; }
        DialogueLine currentLine = dialogueLines.Dequeue();
        sentenceText.text = currentLine.sentence;
        if (currentLine.choices.Length > 0) { ShowChoices(currentLine.choices); }
        else { StartCoroutine(WaitForSpaceBar()); }
    }
    private void ShowChoices(Choice[] choices)
    {
        choiceLayout.SetActive(true);
        foreach (Choice choice in choices)
        {
            GameObject buttonGO = Instantiate(choiceButtonPrefab, choiceLayout.transform);
            buttonGO.GetComponentInChildren<TextMeshProUGUI>().text = choice.choiceText;
            buttonGO.GetComponent<Button>().onClick.AddListener(() => OnChoiceSelected(choice.likabilityChange));
        }
    }
    private void ClearChoiceButtons()
    {
        foreach (Transform child in choiceLayout.transform) { Destroy(child.gameObject); }
        choiceLayout.SetActive(false);
    }
    private IEnumerator WaitForSpaceBar()
    {
        yield return new WaitUntil(() => Input.GetKeyDown(KeyCode.Space));
        DisplayNextLine();
    }
}