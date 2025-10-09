using System.Collections.Generic;
using UnityEngine;

public class NPC : MonoBehaviour
{
    [Header("기본 대화")]
    public Dialogue regularDialogue;

    [Header("선물 관련 설정")]
    public List<string> preferredItemIDs;
    public Dialogue thankYouDialogue;
    public int likabilityBonus = 25;

    public List<string> rejectedItemIDs;
    public Dialogue rejectionDialogue;

    [Header("NPC 상태")]
    public int likability = 0;
    private bool playerIsClose = false;

    void Update()
    {
        if (playerIsClose && Input.GetKeyDown(KeyCode.E) && !DialogueManager.IsDialogueActive)
        {
            bool giftInteraction = CheckAndGiveGift();
            if (!giftInteraction)
            {
                TriggerRegularDialogue();
            }
        }
    }

    private bool CheckAndGiveGift()
    {
        Inventory playerInventory = Inventory.instance;

        // 1. 선호하는 아이템 목록을 확인
        foreach (string itemID in preferredItemIDs)
        {
            if (playerInventory.HasItem(itemID))
            {
                playerInventory.RemoveItem(itemID);
                this.likability += likabilityBonus;
                FindObjectOfType<DialogueManager>().StartDialogue(thankYouDialogue, this);
                return true;
            }
        }

        // 2. 거절하는 아이템 목록을 확인
        foreach (string itemID in rejectedItemIDs)
        {
            if (playerInventory.HasItem(itemID))
            {
                FindObjectOfType<DialogueManager>().StartDialogue(rejectionDialogue, this);
                return true;
            }
        }

        return false;
    }

    public void TriggerRegularDialogue()
    {
        FindObjectOfType<DialogueManager>().StartDialogue(regularDialogue, this);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player")) playerIsClose = true;
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player")) playerIsClose = false;
    }
}