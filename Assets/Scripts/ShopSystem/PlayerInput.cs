using UnityEngine;

public class PlayerInput : MonoBehaviour
{
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.B))
        {
            if (!DialogueManager.IsDialogueActive && !UIManager.instance.shopPanel.activeInHierarchy)
            {
                UIManager.instance.ToggleInventoryPanel();
            }
        }

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (UIManager.instance.shopPanel.activeInHierarchy)
            {
                UIManager.instance.CloseShop();
            }
            else if (UIManager.instance.inventoryPanel.activeInHierarchy)
            {
                UIManager.instance.ToggleInventoryPanel();
            }
        }
    }
}