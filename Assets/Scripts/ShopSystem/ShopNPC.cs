using UnityEngine;

public class ShopNPC : MonoBehaviour
{
    private bool playerIsClose = false;
    private Shop shop;

    void Start()
    {
        shop = GetComponent<Shop>();
    }

    void Update()
    {
        if (playerIsClose && Input.GetKeyDown(KeyCode.E) && !DialogueManager.IsDialogueActive)
        {
            if (!UIManager.instance.shopPanel.activeInHierarchy)
            {
                UIManager.instance.OpenShop(shop.itemsForSale, shop);
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player")) playerIsClose = true;
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player")) playerIsClose = false;
    }
}