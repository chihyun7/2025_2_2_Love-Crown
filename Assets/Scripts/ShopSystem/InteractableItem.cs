using UnityEngine;
using Photon.Pun;

public class InteractableItem : MonoBehaviourPun
{
    public string itemID;
    public float pickupDistance = 2f;

    private Inventory localInventory;
    private Transform localPlayer;

    void Update()
    {
        // 플레이어 찾기
        if (localPlayer == null)
            FindLocalPlayer();

        if (localPlayer == null) return;
        if (localInventory == null) return;

        // 거리 체크
        float dist = Vector3.Distance(localPlayer.position, transform.position);

        if (dist <= pickupDistance)
        {
            // 대화중이면 막기
            if (DialogueManager.IsDialogueActive) return;

            if (Input.GetKeyDown(KeyCode.E))
            {
                TryPickup();
            }
        }
    }

    void FindLocalPlayer()
    {
        foreach (var inv in FindObjectsOfType<Inventory>())
        {
            if (inv.pv != null && inv.pv.IsMine)
            {
                localInventory = inv;
                localPlayer = inv.transform;
                return;
            }
        }
    }

    void TryPickup()
    {
        if (string.IsNullOrEmpty(itemID))
        {
            Debug.LogWarning("[ItemPickup] itemID가 비어있습니다.");
            return;
        }

        // 네트워크 모든 클라이언트에 아이템 지급
        localInventory.pv.RPC("RpcAddItem", RpcTarget.All, itemID, 1);

        // 아이템 오브젝트 파괴(모든 클라)
        PhotonNetwork.Destroy(this.gameObject);
    }
}
