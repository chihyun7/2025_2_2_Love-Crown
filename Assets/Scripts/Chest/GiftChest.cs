using System.Collections;
using System.Collections.Generic;
using Photon.Pun;
using UnityEngine;

public class GiftChest : MonoBehaviourPun
{
    [Header("보상 아이템 ID 목록 (ItemData의 itemID와 동일)")]
    public List<string> rewardItemIDs = new List<string>();

    [Header("상자 리스폰 딜레이 (초)")]
    public float respawnDelay = 30f;

    [Header("비주얼 오브젝트")]
    public GameObject closedVisual;   // 닫힌 상자 모델
    public GameObject openedVisual;   // 열린 상자 모델 (없으면 null로 둬도 됨)

    private bool playerIsClose = false;
    private Inventory localPlayerInventory = null;

    // 서버/클라 공통 상태
    [SerializeField]
    private bool isOpened = false;

    public bool IsOpened => isOpened;

    private void Start()
    {
        UpdateVisual();
    }

    private void Update()
    {
        // 이미 열린 상자면 상호작용 불가
        if (isOpened) return;

        // 내 플레이어가 근처에 있고, 인벤토리 있고, 대화 안 열려 있고, E 키 눌렀을 때
        if (playerIsClose &&
            localPlayerInventory != null &&
            Input.GetKeyDown(KeyCode.E) &&
            !DialogueManager.IsDialogueActive)
        {
            // 마스터에게 "이 상자 열어줘" 요청
            if (GameManager.Instance != null && GameManager.Instance.pv != null)
            {
                GameManager.Instance.pv.RPC(
                    "RpcOpenChest",
                    RpcTarget.MasterClient,
                    photonView.ViewID,
                    localPlayerInventory.pv.Owner.ActorNumber
                );
            }
        }
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

    void UpdateVisual()
    {
        if (closedVisual != null) closedVisual.SetActive(!isOpened);
        if (openedVisual != null) openedVisual.SetActive(isOpened);
    }

    // === 서버가 호출하는 RPC: 열림/닫힘 상태 동기화 ===
    [PunRPC]
    public void RpcSetChestState(bool opened)
    {
        isOpened = opened;
        UpdateVisual();
    }

    // 마스터에서만 리스폰 코루틴 돌림
    public void StartRespawnOnMaster()
    {
        if (!PhotonNetwork.IsMasterClient) return;
        if (respawnDelay <= 0f) return;

        StartCoroutine(RespawnRoutine());
    }

    private IEnumerator RespawnRoutine()
    {
        yield return new WaitForSeconds(respawnDelay);

        if (PhotonNetwork.IsMasterClient)
        {
            photonView.RPC("RpcSetChestState", RpcTarget.All, false);
        }
    }
}

