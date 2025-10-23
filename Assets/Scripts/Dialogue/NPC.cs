using Photon.Pun;
using System.Collections.Generic;
using UnityEngine;
using Photon.Realtime;
using TMPro;

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
    public GameObject nameCanvasPrefab;  // 프리팹 연결용
    private GameObject nameCanvasInstance;
    private TextMeshProUGUI nameText;

    [HideInInspector]
    public int charmedByActorNumber = 0;

    // === [추가] 귀속된 플레이어 이름 저장용 ===
    private string charmedPlayerName = "";

    private Inventory localPlayerInventory = null;
    private bool playerIsClose = false;

    void Start()
    {
        // 이름 UI 생성
        if (nameCanvasPrefab != null)
        {
            // ✅ 캔버스를 NPC의 자식으로 명확히 붙이기
            nameCanvasInstance = Instantiate(nameCanvasPrefab, transform);

            // ✅ 위치 조정 (NPC 콜라이더 높이 기준)
            float npcHeight = 2f;
            Collider col = GetComponent<Collider>();
            if (col != null)
                npcHeight = col.bounds.size.y;

            nameCanvasInstance.transform.localPosition = new Vector3(0, npcHeight + 0.5f, 0);
            nameCanvasInstance.transform.localRotation = Quaternion.identity;
            nameCanvasInstance.transform.localScale = Vector3.one * 0.008f;

            // ✅ TextMeshPro 참조
            nameText = nameCanvasInstance.GetComponentInChildren<TextMeshProUGUI>();
            if (nameText != null)
            {
                nameText.text = npcName;
                nameText.alignment = TextAlignmentOptions.Center;
            }
            else
            {
                Debug.LogWarning($"[NPC] {npcName} 이름 텍스트를 찾을 수 없습니다!");
            }
        }
        else
        {
            Debug.LogWarning($"[NPC] {npcName}의 NameCanvasPrefab이 비어있습니다!");
        }
    }


    void Update()
    {
        // ✅ 귀속 상태면 아무 동작도 하지 않음
        if (charmedByActorNumber != 0)
            return;

        if (playerIsClose && localPlayerInventory != null && Input.GetKeyDown(KeyCode.E) && !DialogueManager.IsDialogueActive)
        {
            bool giftInteraction = RequestGiftInteraction();
            if (!giftInteraction)
                TriggerRegularDialogue();
        }
    }


    // ✅ 싱글 플레이나 마스터가 직접 귀속 처리할 때 사용
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
                nameText.enableWordWrapping = false;  // ✅ 자동 줄바꿈 비활성화
            nameText.overflowMode = TextOverflowModes.Overflow; // ✅ 한 줄 유지
            nameText.text = $"{npcName} — {charmedPlayerName}에게 귀속됨."; // ✅ 보기 좋게 변경


            Debug.Log($"[NPC] '{npcName}'이(가) Player '{charmedPlayerName}'에게 귀속됨 (로컬 처리)");
        }
    }

    // 대화/선택지로 호감도 올릴 때 반드시 이 함수를 호출하세요.
    // 서버(Master)에 likability 변경을 요청하고, 임계치 도달 시 귀속 로직까지 동일하게 처리됩니다.
    public void IncreaseLikability(int amount)
    {
        // 이미 귀속이면 더 못 올리게
        if (charmedByActorNumber != 0) return;

        var npcPV = photonView != null ? photonView : GetComponent<PhotonView>();
        if (npcPV == null)
        {
            Debug.LogError("[NPC] PhotonView가 없습니다.");
            return;
        }

        // 서버에게 호감도 변경 요청 (선물 아님 -> giftItemID = null)
        if (ServerMasterClient.Instance != null)
        {
            ServerMasterClient.Instance.pv.RPC(
                "RpcRequestChangeLikability",
                RpcTarget.MasterClient,
                PhotonNetwork.LocalPlayer.ActorNumber, // 요청자
                npcPV.ViewID,                          // 대상 NPC
                amount,                                // 증가량
                null                                   // 선물 아님
            );
        }
        else
        {
            // 예외/오프라인 대비: 로컬 반영
            RpcChangeLikability(amount);
        }
    }


    private bool RequestGiftInteraction()
    {
        // 이미 귀속된 상태면 대화/호감도 금지
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

        // 선물 아이템 확인
        foreach (string itemID in preferredItemIDs)
        {
            if (localPlayerInventory.HasItem(itemID))
            {
                giftItemID = itemID;

                //  마스터면 직접 처리, 아니면 RPC로 요청
                if (PhotonNetwork.IsMasterClient)
                {
                    Debug.Log("[NPC] 단일 테스트 모드 (마스터 직접 귀속 처리)");
                    HandleLocalLikability(localPlayerInventory.pv.Owner.ActorNumber, likabilityBonus);
                }
                else
                {
                    ServerMasterClient.Instance.pv.RPC("RpcRequestChangeLikability", RpcTarget.MasterClient,
                        localPlayerInventory.pv.Owner.ActorNumber,
                        npcPV.ViewID,
                        likabilityBonus,
                        giftItemID);
                }

                FindObjectOfType<DialogueManager>().StartDialogue(thankYouDialogue, this);
                return true;
            }
        }

        // 거절 아이템
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
        likability += likabilityChange;
        Debug.Log($"NPC 호감도 변경: +{likabilityChange} → 현재 {likability}");

        // 귀속 조건 확인
        if (charmedByActorNumber == 0 && likability >= charmThreshold)
        {
            charmedByActorNumber = PhotonNetwork.LocalPlayer.ActorNumber;
            charmedPlayerName = PhotonNetwork.LocalPlayer.NickName;

            // 머리 위 이름 갱신
            if (nameText == null)
                nameText = GetComponentInChildren<TextMeshProUGUI>();
            if (nameText != null)
                nameText.text = $"{npcName} ❤️ {charmedPlayerName}";

            Debug.Log($"[NPC] '{npcName}'이(가) Player '{charmedPlayerName}'에게 귀속됨!");

            // ✅ 즉시 차단되게 flag 설정
            playerIsClose = false;
        }
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

    void LateUpdate()
    {
        if (nameCanvasInstance != null)
        {
            var cam = Camera.main;
            if (cam != null)
                nameCanvasInstance.transform.LookAt(nameCanvasInstance.transform.position + cam.transform.forward);
        }
    }

    // === [추가] 귀속 상태를 클라이언트 전체에 동기화하는 RPC ===
    [PunRPC]
    public void RpcSetCharmOwner(int ownerActorNumber, string ownerName)
    {
        charmedByActorNumber = ownerActorNumber;
        charmedPlayerName = ownerName;

        if (nameText == null)
            nameText = GetComponentInChildren<TextMeshProUGUI>();

        // ✅ OwnerText 찾기
        TextMeshProUGUI ownerText = null;
        if (nameCanvasInstance != null)
            ownerText = nameCanvasInstance.transform.Find("Canvas/OwnerText")?.GetComponent<TextMeshProUGUI>();

        // NPC 이름 유지
        if (nameText != null)
            nameText.text = npcName;

        // 플레이어 이름 표시 (밑줄)
        if (ownerText != null)
            ownerText.text = $"{charmedPlayerName}";

        Debug.Log($"[NPC Sync] '{npcName}'이(가) {charmedPlayerName}에게 귀속됨!");
    }



}
