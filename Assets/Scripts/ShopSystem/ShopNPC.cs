using UnityEngine;
using Photon.Pun;

public class ShopNPC : MonoBehaviour
{
    private bool playerIsClose = false;
    private Shop shop;

    void Start()
    {
        shop = GetComponent<Shop>();

        if (shop == null)
        {
            Debug.LogError("ShopNPC requires a Shop component on the same GameObject.");
        }
    }

    void Update()
    {
        if (playerIsClose && Input.GetKeyDown(KeyCode.E) && !DialogueManager.IsDialogueActive)
        {
            if (shop != null && UIManager.instance != null)
            {
                if (!UIManager.instance.shopPanel.activeInHierarchy)
                {
                    UIManager.instance.OpenShop(shop.itemsForSale, shop);
                }
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        PhotonView otherPv = other.GetComponent<PhotonView>();
        if (other.CompareTag("Player") && otherPv != null && otherPv.IsMine)
        {
            playerIsClose = true;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        PhotonView otherPv = other.GetComponent<PhotonView>();
        if (other.CompareTag("Player") && otherPv != null && otherPv.IsMine)
        {
            playerIsClose = false;
        }
    }
}