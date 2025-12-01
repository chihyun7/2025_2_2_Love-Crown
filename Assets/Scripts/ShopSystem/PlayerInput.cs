using Photon.Pun;
using UnityEngine;

public class PlayerInput : MonoBehaviour
{
    private PhotonView pv;

    void Start()
    {
        pv = GetComponent<PhotonView>();
    }

    void Update()
    {
        if (pv == null || !pv.IsMine) return;

        if (DialogueManager.IsDialogueActive) return;

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
            else if (UIManager.instance.questLogPanel.activeInHierarchy) // 추가
            {
                UIManager.instance.ToggleQuestLogPanel(); // 추가
            }
        }

        if (Input.GetKeyDown(KeyCode.J))
        {
            if (!DialogueManager.IsDialogueActive && !UIManager.instance.shopPanel.activeInHierarchy)
            {
                UIManager.instance.ToggleQuestLogPanel();
            }
        }
    }
}