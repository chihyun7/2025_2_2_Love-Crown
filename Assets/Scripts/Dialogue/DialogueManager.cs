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
            // 멀티플레이 환경에서는 내 플레이어의 퀘스트 로그를 찾아야 합니다.
            foreach (var log in FindObjectsOfType<PlayerQuestLog>())
            {
                if (log.GetComponent<PhotonView>().IsMine)
                {
                    localPlayerQuestLog = log;
                    break;
                }
            }
        }

        // 플레이어 이동 정지 (내 플레이어만)
        foreach (var playerMove in FindObjectsOfType<PlayerMove>())
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

    // --- 👇 [수정] OnChoiceSelected가 Choice 객체를 통째로 받도록 변경 ---
    private void OnChoiceSelected(Choice choice)
    {
        if (currentNpc == null) return;
        if (currentNpc.charmedByActorNumber != 0) return;

        // 1. [항상 적용] 호감도 변화 적용
        // (0이 아니면 호감도 변경 RPC 호출)
        if (choice.likabilityChange != 0)
        {
            currentNpc.IncreaseLikability(choice.likabilityChange);
            UpdateLikabilityUI();
        }

        // 2. [특별 기능] 퀘스트 액션 처리
        switch (choice.action)
        {
            case Choice.ChoiceAction.AcceptQuest:
                if (currentQuestOffer != null && localPlayerQuestLog != null)
                {
                    localPlayerQuestLog.AcceptQuest(currentQuestOffer);
                }
                break;

            case Choice.ChoiceAction.RejectQuest:
                // 거절. 아무것도 하지 않고 대화만 넘김
                break;

            case Choice.ChoiceAction.Normal:
            default:
                // 일반 대화. 아무것도 하지 않음
                break;
        }

        // 3. 다음 대사로 진행
        DisplayNextLine();
    }

    void EndDialogue()
    {
        IsDialogueActive = false;
        dialoguePanel.SetActive(false);
        likabilityText.gameObject.SetActive(false);

        // 플레이어 이동 재개 (내 플레이어만)
        foreach (var playerMove in FindObjectsOfType<PlayerMove>())
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
        sentenceText.text = currentLine.sentence;
        if (currentLine.choices.Length > 0) { ShowChoices(currentLine.choices); }
        else { StartCoroutine(WaitForSpaceBar()); }
    }

    // --- 👇 [수정] ShowChoices가 OnChoiceSelected에 int가 아닌 Choice를 넘기도록 변경 ---
    private void ShowChoices(Choice[] choices)
    {
        choiceLayout.SetActive(true);
        foreach (Choice choice in choices)
        {
            GameObject buttonGO = Instantiate(choiceButtonPrefab, choiceLayout.transform);
            buttonGO.GetComponentInChildren<TextMeshProUGUI>().text = choice.choiceText;

            // [중요] choice 변수를 람다 표현식에서 올바르게 캡처
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