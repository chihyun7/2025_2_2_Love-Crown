using Photon.Pun;
using System.Collections.Generic;
using UnityEngine;
using Photon.Realtime;
using TMPro;
using System.Linq;

public class NPC : MonoBehaviourPun
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

    [Header("이름 UI")]
    public string npcName = "NPC 이름";
    public GameObject nameCanvasPrefab;
    private GameObject nameCanvasInstance;
    private TextMeshProUGUI nameText;

    [Header("퀘스트")]
    public QuestData questToOffer;

    [HideInInspector]
    public int charmedByActorNumber = 0;

    private PlayerQuestLog localPlayerQuestLog;

    private string charmedPlayerName = "";

    private Inventory localPlayerInventory = null;
    private bool playerIsClose = false;

    void Start()
    {
        if (nameCanvasPrefab != null)
        {
            nameCanvasInstance = Instantiate(nameCanvasPrefab, transform);

            float npcHeight = 2f;
            Collider col = GetComponent<Collider>();
            if (col != null)
                npcHeight = col.bounds.size.y;

            nameCanvasInstance.transform.localPosition = new Vector3(0, 1.7f, 0);
            nameCanvasInstance.transform.localRotation = Quaternion.identity;
            nameCanvasInstance.transform.localScale = Vector3.one * 0.004f;

            nameText = nameCanvasInstance.transform.Find("Canvas/NameText")?.GetComponent<TextMeshProUGUI>();

            if (nameText != null)
                nameText.text = npcName;
            else
                Debug.LogWarning($"[NPC] {npcName} 이름 텍스트를 찾을 수 없습니다!");
        }
        else
        {
            Debug.LogWarning($"[NPC] {npcName}의 NameCanvasPrefab이 비어있습니다!");
        }
    }



    void Update()
    {
        if (charmedByActorNumber != 0) return;

        if (playerIsClose && localPlayerInventory != null && Input.GetKeyDown(KeyCode.E) && !DialogueManager.IsDialogueActive)
        {
            if (UIManager.instance != null)
            {
                UIManager.instance.HideInteractionText();
            }

            if (CheckForQuestCompletion()) return;

            if (RequestGiftInteraction()) return;

            if (CheckForQuestOffer()) return;

            TriggerRegularDialogue();
        }
    }

    private bool CheckForQuestCompletion()
    {
        if (questToOffer == null || localPlayerQuestLog == null) return false;

        QuestStatus status = localPlayerQuestLog.GetQuestStatus(questToOffer.questID);

        if (status != null && !status.isCompleted)
        {
            bool conditionMet = false;

            if (questToOffer.objective.type == QuestObjective.ObjectiveType.Talk)
            {
                conditionMet = true;
            }
            else if (questToOffer.objective.type == QuestObjective.ObjectiveType.Collect)
            {
                int currentCount = localPlayerInventory.GetItemCount(questToOffer.objective.targetItemID);

                if (currentCount >= questToOffer.objective.targetItemQuantity)
                {
                    conditionMet = true;
                }
            }

            if (conditionMet)
            {
                if (questToOffer.objective.type == QuestObjective.ObjectiveType.Talk)
                {
                    FindObjectOfType<DialogueManager>().StartDialogue(questToOffer.completionDialogue, this, questToOffer);
                    return true;
                }

                GameManager.Instance.pv.RPC("RpcRequestQuestComplete",
                    RpcTarget.MasterClient,
                    PhotonNetwork.LocalPlayer.ActorNumber,
                    questToOffer.questID,
                    photonView.ViewID);

                FindObjectOfType<DialogueManager>().StartDialogue(questToOffer.completionDialogue, this);
                return true;
            }
        }
        return false;
    }


    private void HandleLocalLikability(int actorID, int likabilityChange)
    {
        likability += likabilityChange;

        if (charmedByActorNumber == 0 && likability >= charmThreshold)
        {
            charmedByActorNumber = actorID;
            charmedPlayerName = PhotonNetwork.LocalPlayer.NickName;

            if (nameText == null)
                nameText = GetComponentInChildren<TextMeshProUGUI>();

            if (nameText != null)
                nameText.enableWordWrapping = false;
            nameText.overflowMode = TextOverflowModes.Overflow; 
            nameText.text = $"{npcName} — {charmedPlayerName}에게 귀속됨.";


            Debug.Log($"[NPC] '{npcName}'이(가) Player '{charmedPlayerName}'에게 귀속됨 (로컬 처리)");
        }
    }

    public void IncreaseLikability(int amount)
    {
        if (charmedByActorNumber != 0) return;

        var npcPV = photonView != null ? photonView : GetComponent<PhotonView>();
        if (npcPV == null)
        {
            Debug.LogError("[NPC] PhotonView가 없습니다.");
            return;
        }

        if (GameManager.Instance != null)
        {
            GameManager.Instance.pv.RPC(
                "RpcRequestChangeLikability",
                RpcTarget.MasterClient,
                PhotonNetwork.LocalPlayer.ActorNumber, 
                npcPV.ViewID,                         
                amount,                               
                null                                   
            );
        }
        else
        {
            RpcChangeLikability(amount);
        }
    }


    private bool RequestGiftInteraction()
    {
        if (charmedByActorNumber != 0)
        {
            Debug.Log($"[NPC] 이미 Player {charmedByActorNumber}에게 귀속된 NPC입니다. 더 이상 상호작용 불가.");
            if (nameText != null)
                nameText.text = $"{npcName} ❤️ {charmedPlayerName}";
            return true;
        }

        string giftItemID = null;
        PhotonView npcPV = GetComponent<PhotonView>();

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
                GameManager.Instance.pv.RPC("RpcRequestChangeLikability", RpcTarget.MasterClient,
                    localPlayerInventory.pv.Owner.ActorNumber,
                    photonView.ViewID,
                    likabilityBonus,
                    giftItemID);

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

    private bool CheckForQuestOffer()
    {
        if (questToOffer == null || localPlayerQuestLog == null) return false;

        if (localPlayerQuestLog.GetQuestStatus(questToOffer.questID) == null &&
            !localPlayerQuestLog.HasCompletedQuest(questToOffer.questID))
        {
            FindObjectOfType<DialogueManager>().StartDialogue(questToOffer.startDialogue, this, questToOffer);
            return true;
        }
        return false;
    }

    [PunRPC]
    public void RpcChangeLikability(int likabilityChange)
    {
        likability += likabilityChange;
        Debug.Log($"NPC 호감도 변경: +{likabilityChange} → 현재 {likability}");

        if (charmedByActorNumber == 0 && likability >= charmThreshold)
        {
            charmedByActorNumber = PhotonNetwork.LocalPlayer.ActorNumber;
            charmedPlayerName = PhotonNetwork.LocalPlayer.NickName;

            if (nameText == null)
                nameText = GetComponentInChildren<TextMeshProUGUI>();
            if (nameText != null)
                nameText.text = $"{npcName} ❤️ {charmedPlayerName}";

            Debug.Log($"[NPC] '{npcName}'이(가) Player '{charmedPlayerName}'에게 귀속됨!");

            playerIsClose = false;
        }
    }



    public void TriggerRegularDialogue()
    {
        FindObjectOfType<DialogueManager>().StartDialogue(regularDialogue, this);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            PhotonView otherPv = other.GetComponent<PhotonView>();

            if (otherPv != null && otherPv.IsMine)
            {
                playerIsClose = true;
                localPlayerInventory = other.GetComponent<Inventory>();
                localPlayerQuestLog = other.GetComponent<PlayerQuestLog>();

                if (UIManager.instance != null)
                    UIManager.instance.ShowInteractionText($"대화하기 [E]");
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            PhotonView otherPv = other.GetComponent<PhotonView>();

            if (otherPv != null && otherPv.IsMine)
            {
                playerIsClose = false;
                localPlayerInventory = null;
                localPlayerQuestLog = null;

                if (UIManager.instance != null)
                    UIManager.instance.HideInteractionText();
            }
        }
    }

    void LateUpdate()
{
    if (nameCanvasInstance != null)
    {
        Camera cam = Camera.main;
        if (cam != null)
        {
            nameCanvasInstance.transform.LookAt(
                nameCanvasInstance.transform.position + cam.transform.rotation * Vector3.forward,
                cam.transform.rotation * Vector3.up
            );
        }
    }
}




[PunRPC]
    public void RpcSetCharmOwner(int ownerActorNumber, string ownerName)
    {
        charmedByActorNumber = ownerActorNumber;
        charmedPlayerName = ownerName;

        if (nameText == null)
            nameText = GetComponentInChildren<TextMeshProUGUI>();

        TextMeshProUGUI ownerText = null;
        if (nameCanvasInstance != null)
            ownerText = nameCanvasInstance.transform.Find("Canvas/OwnerText")?.GetComponent<TextMeshProUGUI>();

        if (nameText != null)
            nameText.text = npcName;

        if (ownerText != null)
            ownerText.text = $"{charmedPlayerName}";

        Debug.Log($"[NPC Sync] '{npcName}'이(가) {charmedPlayerName}에게 귀속됨!");
    }





}
