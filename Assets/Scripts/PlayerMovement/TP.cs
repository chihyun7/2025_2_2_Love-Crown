using UnityEngine;
using Photon.Pun;

public class TP : MonoBehaviour
{
    public Transform targetPoint; // 텔레포트 목적지

    private void OnTriggerEnter(Collider other)
    {
        PhotonView pv = other.GetComponent<PhotonView>();

        // 플레이어가 아니면 무시
        if (pv == null) return;

        // 내 캐릭터가 아니면 무시
        if (!pv.IsMine) return;

        // 순간이동 (리짓바디 이동 버그 방지)
        Rigidbody rb = other.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        other.transform.position = targetPoint.position;
        other.transform.rotation = targetPoint.rotation;
    }
}
