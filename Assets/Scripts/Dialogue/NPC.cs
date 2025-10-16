using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

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

                // 마스터 클라이언트에게 호감도 증가 요청 (RpcRequestChangeLikability는 ServerMasterClient에 구현되어 있어야 함)
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
        // 마스터 클라이언트의 명령을 받아 호감도를 동기화합니다.
        this.likability += likabilityChange;
        Debug.Log($"NPC 호감도 변경: {likabilityChange}. 현재 호감도: {this.likability}");
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