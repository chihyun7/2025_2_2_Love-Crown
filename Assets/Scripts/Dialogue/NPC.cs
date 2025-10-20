using Photon.Pun;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using Photon.Realtime; // Photon.Realtime.Player 등의 타입 사용을 위해 추가할 수 있음 (현재 코드에는 불필요)

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

    [Header("미니게임 선점 시스템")]
    [Tooltip("이 값 이상이 되면 NPC가 특정 플레이어에게 선점됩니다.")]
    public int charmThreshold = 70;

    [HideInInspector]
    public int charmedByActorNumber = 0;


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
        PhotonView npcPV = this.GetComponent<PhotonView>();

        if (npcPV == null)
        {
            Debug.LogError("NPC에 PhotonView가 없습니다. RPC 호출 불가.");
            return false;
        }

        foreach (string itemID in preferredItemIDs)
        {
            if (localPlayerInventory.HasItem(itemID))
            {
                giftItemID = itemID;

                // 🚨 수정: RPC 파라미터 순서를 ServerMasterClient.cs의 정의와 일치시킵니다.
                // RpcRequestChangeLikability(int requesterActorID, int npcViewID, int likabilityChange, string giftItemID)
                ServerMasterClient.Instance.pv.RPC("RpcRequestChangeLikability", RpcTarget.MasterClient,
                    localPlayerInventory.pv.Owner.ActorNumber,      // 1. requesterActorID (int)
                    npcPV.ViewID,                                   // 2. npcViewID (int)
                    likabilityBonus,                                // 3. likabilityChange (int)
                    giftItemID);                                    // 4. giftItemID (string)

                FindObjectOfType<DialogueManager>().StartDialogue(thankYouDialogue, this);
                return true;
            }
        }

        foreach (string itemID in rejectedItemIDs)
        {
            if (localPlayerInventory.HasItem(itemID))
            {
                // 거절당한 아이템은 호감도 변경 요청을 보내지 않고, 대화만 출력
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