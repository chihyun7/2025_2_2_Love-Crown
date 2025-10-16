using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

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

    private Inventory localPlayerInventory = null;
    private bool playerIsClose = false;

    void Update()
    {
        if (playerIsClose && localPlayerInventory != null && Input.GetKeyDown(KeyCode.E) && !DialogueManager.IsDialogueActive)
        {
            bool giftInteraction = RequestGiftInteraction();
            if (!giftInteraction)
            {
                TriggerRegularDialogue();
            }
        }
    }

    private bool RequestGiftInteraction()
    {
        string giftItemID = null;

        foreach (string itemID in preferredItemIDs)
        {
            if (localPlayerInventory.HasItem(itemID))
            {
                giftItemID = itemID;

                // ������ Ŭ���̾�Ʈ���� ȣ���� ���� ��û (RpcRequestChangeLikability�� ServerMasterClient�� �����Ǿ� �־�� ��)
                ServerMasterClient.Instance.pv.RPC("RpcRequestChangeLikability", RpcTarget.MasterClient,
                                                localPlayerInventory.pv.Owner.ActorNumber,
                                                giftItemID,
                                                likabilityBonus);

                FindObjectOfType<DialogueManager>().StartDialogue(thankYouDialogue, this);
                return true;
            }
        }

        foreach (string itemID in rejectedItemIDs)
        {
            if (localPlayerInventory.HasItem(itemID))
            {
                FindObjectOfType<DialogueManager>().StartDialogue(rejectionDialogue, this);
                return true;
            }
        }

        return false;
    }

    [PunRPC]
    public void RpcChangeLikability(int likabilityChange)
    {
        // ������ Ŭ���̾�Ʈ�� ����� �޾� ȣ������ ����ȭ�մϴ�.
        this.likability += likabilityChange;
        Debug.Log($"NPC ȣ���� ����: {likabilityChange}. ���� ȣ����: {this.likability}");
    }

    public void TriggerRegularDialogue()
    {
        FindObjectOfType<DialogueManager>().StartDialogue(regularDialogue, this);
    }

    private void OnTriggerEnter(Collider other)
    {
        PhotonView otherPv = other.GetComponent<PhotonView>();
        if (other.CompareTag("Player") && otherPv != null && otherPv.IsMine)
        {
            playerIsClose = true;
            localPlayerInventory = other.GetComponent<Inventory>();
        }
    }

    private void OnTriggerExit(Collider other)
    {
        PhotonView otherPv = other.GetComponent<PhotonView>();
        if (other.CompareTag("Player") && otherPv != null && otherPv.IsMine)
        {
            playerIsClose = false;
            localPlayerInventory = null;
        }
    }
}