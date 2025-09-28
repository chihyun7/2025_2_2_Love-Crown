using UnityEngine;

public class NPC : MonoBehaviour
{
    public Dialogue dialogue;
    public int likability = 0;
    private bool playerIsClose = false;

    public void TriggerDialogue()
    {
        FindObjectOfType<DialogueManager>().StartDialogue(this);
    }

    void Update()
    {
        if (playerIsClose && Input.GetKeyDown(KeyCode.E) && !DialogueManager.IsDialogueActive)
        {
            TriggerDialogue();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerIsClose = true;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerIsClose = false;
        }
    }
}