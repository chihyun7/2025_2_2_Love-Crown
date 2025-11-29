using UnityEngine;
using Photon.Pun;

public class Teleporter : MonoBehaviour
{
    public Transform destination;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            PhotonView pv = other.GetComponent<PhotonView>();

            if (pv != null && pv.IsMine)
            {
                other.transform.position = destination.position;

                other.transform.rotation = destination.rotation;

                Physics.SyncTransforms();
            }
        }
    }
}