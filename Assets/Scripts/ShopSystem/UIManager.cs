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
        FindLocalPlayerInventory();

        // localInventory�� ã������ ��� UI�� ������Ʈ
        if (localInventory != null)
        {
            UpdateGoldText(localInventory.gold);
        }
        else
        {
            UpdateGoldText(0);
        }
    }

    /// <summary>
    /// ���� �÷��̾��� Inventory ������Ʈ�� ã�� �Ҵ��մϴ�.
    /// </summary>
    public void FindLocalPlayerInventory()
    {
        Inventory[] inventories = FindObjectsOfType<Inventory>();

        foreach (Inventory inv in inventories)
        {
            if (inv.pv != null && inv.pv.IsMine)
            {
                localInventory = inv;
                Debug.Log("UIManager: ���� �÷��̾� �κ��丮 ã��");

                // ã�� �� �ʱ� ��� UI ������Ʈ
                UpdateGoldText(localInventory.gold);
                return;
            }
        }

        if (localInventory == null)
        {
            Debug.LogWarning("UIManager: ���� �÷��̾� �κ��丮�� ���� ã�� ���߽��ϴ�. ���� �� �ٽ� �õ��˴ϴ�.");
        }
    }


    /// <summary>
    /// Inventory.cs�� RpcExecuteBuy���� ȣ��Ǵ� ��� UI ������Ʈ �Լ��Դϴ�.
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

        // �� ���Ǻ� ����(?. )�� ����� �� üũ�� �����Ͽ� C# ȣȯ�� ���� ����
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

        // �� ���Ǻ� ����(?. )�� ����� �� üũ�� �����Ͽ� C# ȣȯ�� ���� ����
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

        // ���� ������ ���� ����
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

                // ItemSlot ������ ���ο� ������ �̸��� ������ ǥ���� TextMeshProUGUI�� �ִٰ� �����ϰ� ���� ������Ʈ
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
                // TODO: UI�� ��� ���� �޽��� ǥ��
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
