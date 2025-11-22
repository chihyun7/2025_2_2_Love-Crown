using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using Photon.Pun;

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
    private QuestData currentQuestOffer;
    private PlayerQuestLog localPlayerQuestLog;

    void Start()
    {
        dialogueLines = new Queue<DialogueLine>();
    }

    public void StartDialogue(Dialogue dialogue, NPC npc, QuestData questToOffer = null)
    {
        IsDialogueActive = true;
        currentNpc = npc;
        currentQuestOffer = questToOffer;

        localPlayerQuestLog = FindObjectOfType<PlayerQuestLog>();
        if (localPlayerQuestLog == null)
        {
            foreach (var log in FindObjectsOfType<PlayerQuestLog>())
            {
                if (log.GetComponent<PhotonView>().IsMine)
                {
                    localPlayerQuestLog = log;
                    break;
                }
            }
        }

        foreach (var playerMove in FindObjectsOfType<TestCharacterPlayerMoveMent>())
        {
            if (playerMove.GetComponent<PhotonView>().IsMine)
            {
                playerMove.canMove = false;
                break;
            }
        }

        dialoguePanel.SetActive(true);
        likabilityText.gameObject.SetActive(true);
        UpdateLikabilityUI();

        nameText.text = currentNpc.npcName;
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
            likabilityText.text = "호감도: " + currentNpc.likability;
        }
    }

    private void OnChoiceSelected(Choice choice)
    {
        if (currentNpc == null) return;
        if (currentNpc.charmedByActorNumber != 0) return;

        if (choice.likabilityChange != 0)
        {
            currentNpc.IncreaseLikability(choice.likabilityChange);
            UpdateLikabilityUI();
        }

        switch (choice.action)
        {
            case Choice.ChoiceAction.AcceptQuest:
                if (currentQuestOffer != null && localPlayerQuestLog != null)
                {
                    localPlayerQuestLog.AcceptQuest(currentQuestOffer);
                }
                break;

            case Choice.ChoiceAction.CompleteQuest:
                if (currentQuestOffer != null)
                {
                    GameManager.Instance.pv.RPC("RpcRequestQuestComplete",
                        RpcTarget.MasterClient,
                        PhotonNetwork.LocalPlayer.ActorNumber,
                        currentQuestOffer.questID,
                        currentNpc.photonView.ViewID);
                }
                break;

            case Choice.ChoiceAction.ExitDialogue:
                EndDialogue();
                return;

            case Choice.ChoiceAction.RejectQuest:
                break;

            case Choice.ChoiceAction.Normal:
            default:
                break;
        }

        DisplayNextLine();
    }

    void EndDialogue()
    {
        IsDialogueActive = false;
        dialoguePanel.SetActive(false);
        likabilityText.gameObject.SetActive(false);

        foreach (var playerMove in FindObjectsOfType<TestCharacterPlayerMoveMent>())
        {
            if (playerMove.GetComponent<PhotonView>().IsMine)
            {
                playerMove.canMove = true;
                break;
            }
        }

        currentNpc = null;
        currentQuestOffer = null;
        localPlayerQuestLog = null;
    }

    public void DisplayNextLine()
    {
        ClearChoiceButtons();
        if (dialogueLines.Count == 0) { EndDialogue(); return; }

        DialogueLine currentLine = dialogueLines.Dequeue();

        if (currentLine.isPlayer)
        {
            if (PhotonNetwork.IsConnected && !string.IsNullOrEmpty(PhotonNetwork.NickName))
                nameText.text = PhotonNetwork.NickName;
            else
                nameText.text = "나";

            nameText.color = Color.yellow;
        }
        else
        {
            nameText.text = currentNpc.npcName;
            nameText.color = Color.white;
        }

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

            Choice currentChoice = choice;
            buttonGO.GetComponent<Button>().onClick.AddListener(() => OnChoiceSelected(currentChoice));
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