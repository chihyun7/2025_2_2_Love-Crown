using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using Photon.Pun;

public class UIManager : MonoBehaviour
{
    public static UIManager instance;

    [Header("UI Elements")]
    public TextMeshProUGUI goldText;
    public GameObject shopPanel;
    public GameObject inventoryPanel;
    public GameObject confirmationPanel;
    public TextMeshProUGUI confirmText;
    public Button yesButton;
    public Button noButton;

    [Header("Layout & Prefabs")]
    public Transform shopContent;
    public Transform inventoryContent;
    public GameObject itemSlotPrefab; // ItemSlot ��ũ��Ʈ�� �پ� �ִٰ� ����

    private Inventory localInventory;

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        // Start ������ �κ��丮�� ã��, �����ϸ� ���� �ð� �� �ٽ� �õ��ϴ� ������ �����մϴ�.
        FindLocalPlayerInventory();
    }

    /// <summary>
    /// ���� �÷��̾��� Inventory ������Ʈ�� ã�� �Ҵ��մϴ�.
    /// </summary>
    public void FindLocalPlayerInventory()
    {
        Inventory[] inventories = FindObjectsOfType<Inventory>();

        foreach (Inventory inv in inventories)
        {
            // PhotonView�� �����ڰ� ���� �÷��̾����� Ȯ��
            if (inv.pv != null && inv.pv.IsMine)
            {
                localInventory = inv;
                Debug.Log("UIManager: ���� �÷��̾� �κ��丮 ã��");

                // ã�� �� ��� �ʱ� ��� UI ������Ʈ
                UpdateGoldText(localInventory.gold);

                // ���������Ƿ� ������ ��õ� Invoke�� ����մϴ�.
                CancelInvoke("RetryFindLocalPlayerInventory");
                return;
            }
        }

        // ���� �κ��丮�� ���� ã�� ���ߴٸ� 1�� �ڿ� ��õ��� �����մϴ�.
        if (localInventory == null && !IsInvoking("RetryFindLocalPlayerInventory"))
        {
            Debug.LogWarning("UIManager: ���� �÷��̾� �κ��丮�� ���� ã�� ���߽��ϴ�. 1�� �� ��õ� ����.");
            Invoke("RetryFindLocalPlayerInventory", 1.0f);
        }
    }

    private void RetryFindLocalPlayerInventory()
    {
        // FindLocalPlayerInventory�� �ٽ� �����Ͽ� ��õ��մϴ�.
        FindLocalPlayerInventory();
    }


    /// <summary>
    /// Inventory.cs���� ȣ��Ǵ� ��� UI ������Ʈ �Լ��Դϴ�.
    /// </summary>
    public void UpdateGoldText(int currentGold)
    {
        if (goldText != null)
        {
            goldText.text = "Gold: " + currentGold.ToString();
        }
    }

    public void ToggleInventoryPanel()
    {
        // ���� �κ��丮�� �Ҵ���� �ʾҴٸ� �ٽ� ã���ϴ�.
        if (localInventory == null) FindLocalPlayerInventory();

        if (localInventory == null)
        {
            Debug.LogError("���� �κ��丮�� �Ҵ���� �ʾ� �κ��丮 �г��� �� �� �����ϴ�.");
            return;
        }

        inventoryPanel.SetActive(!inventoryPanel.activeInHierarchy);
        if (inventoryPanel.activeInHierarchy)
        {
            UpdateInventoryUI();
        }
    }

    public void OpenShop(List<ItemData> itemsToSell, Shop shopInstance)
    {
        if (localInventory == null) FindLocalPlayerInventory();

        shopPanel.SetActive(true);

        if (localInventory != null)
        {
            PlayerMove playerMove = localInventory.GetComponent<PlayerMove>();
            if (playerMove != null)
            {
                playerMove.canMove = false;
            }
        }


        foreach (Transform child in shopContent)
        {
            Destroy(child.gameObject);
        }

        foreach (ItemData item in itemsToSell)
        {
            GameObject slotGO = Instantiate(itemSlotPrefab, shopContent);
            ItemSlot slot = slotGO.GetComponent<ItemSlot>();

            slot.Initialize(item, () => ShowConfirmationPopup(item, shopInstance));
        }
    }

    public void CloseShop()
    {
        shopPanel.SetActive(false);

        if (localInventory != null)
        {
            PlayerMove playerMove = localInventory.GetComponent<PlayerMove>();
            if (playerMove != null)
            {
                playerMove.canMove = true;
            }
        }
    }


    /// <summary>
    /// Inventory.cs�� Dictionary<string, int> ������ �ݿ��Ͽ� UI�� �׸��ϴ�.
    /// </summary>
    public void UpdateInventoryUI()
    {
        if (localInventory == null)
        {
            Debug.LogError("���� �κ��丮�� �غ���� �ʾҽ��ϴ�.");
            return;
        }

        foreach (Transform child in inventoryContent)
        {
            Destroy(child.gameObject);
        }

        Dictionary<string, int> currentItems = localInventory.GetItems();

        if (currentItems == null) return;

        foreach (KeyValuePair<string, int> itemEntry in currentItems)
        {
            string itemID = itemEntry.Key;
            int quantity = itemEntry.Value;

            if (quantity > 0)
            {
                GameObject slotGO = Instantiate(itemSlotPrefab, inventoryContent);

                TextMeshProUGUI itemText = slotGO.GetComponentInChildren<TextMeshProUGUI>();
                if (itemText != null)
                {
                    itemText.text = $"{itemID} ({quantity})";
                }
                else
                {
                    Debug.LogWarning($"ItemSlot �����տ� TextMeshProUGUI ������Ʈ�� �����ϴ�. ID: {itemID}");
                }
            }
        }
    }

    private void ShowConfirmationPopup(ItemData item, Shop shopInstance)
    {
        confirmationPanel.SetActive(true);
        confirmText.text = item.itemName + "��(��) �����Ͻðڽ��ϱ�?";

        yesButton.onClick.RemoveAllListeners();
        yesButton.onClick.AddListener(() =>
        {
            if (localInventory != null && localInventory.CanAfford(item.price))
            {
                shopInstance.RequestPurchase(item.itemID);
            }
            else
            {
                Debug.Log("��尡 �����մϴ�.");
            }
            confirmationPanel.SetActive(false);
        });

        noButton.onClick.RemoveAllListeners();
        noButton.onClick.AddListener(() =>
        {
            confirmationPanel.SetActive(false);
        });
    }
}