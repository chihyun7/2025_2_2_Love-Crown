using System.Collections.Generic;
using UnityEngine;

public class NPC : MonoBehaviour
{
    [Header("�⺻ ��ȭ")]
    public Dialogue regularDialogue;

    [Header("���� ���� ����")]
    public List<string> preferredItemIDs;
    public Dialogue thankYouDialogue;
    public int likabilityBonus = 25;

    public List<string> rejectedItemIDs;
    public Dialogue rejectionDialogue;

    [Header("NPC ����")]
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

        // 1. ��ȣ�ϴ� ������ ����� Ȯ��
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

        // 2. �����ϴ� ������ ����� Ȯ��
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