using Photon.Pun;
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static UnityEngine.EventSystems.EventTrigger;

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
    public GameObject itemSlotPrefab; // ItemSlot 스크립트가 붙어 있다고 가정

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
    }

    public void FindLocalPlayerInventory()
    {
        Inventory[] inventories = FindObjectsOfType<Inventory>();

        foreach (Inventory inv in inventories)
        {
            if (inv.pv != null && inv.pv.IsMine)
            {
                localInventory = inv;
                Debug.Log("UIManager: 로컬 플레이어 인벤토리 찾음");
                UpdateGoldText(localInventory.gold);
                CancelInvoke("RetryFindLocalPlayerInventory");
                return;
            }
        }

        if (localInventory == null && !IsInvoking("RetryFindLocalPlayerInventory"))
        {
            Debug.LogWarning("UIManager: 로컬 플레이어 인벤토리를 아직 찾지 못했습니다. 1초 후 재시도 예약.");
            Invoke("RetryFindLocalPlayerInventory", 1.0f);
        }
    }

    private void RetryFindLocalPlayerInventory()
    {
        FindLocalPlayerInventory();
    }


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
            Debug.LogError("로컬 인벤토리가 할당되지 않아 인벤토리 패널을 열 수 없습니다.");
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

            if (slot != null)
            {
                slot.Initialize(item, () => ShowConfirmationPopup(item, shopInstance));
            }
            else
            {
                Debug.LogError("ItemSlot 프리팹에 ItemSlot 컴포넌트가 누락되었습니다!");
            }
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

    public void UpdateInventoryUI()
    {
        if (localInventory == null || ServerMasterClient.Instance == null)
        {
            Debug.LogError("로컬 인벤토리 또는 ServerMasterClient가 준비되지 않았습니다. UI 갱신 실패.");
            return;
        }

        foreach (Transform child in inventoryContent)
        {
            Destroy(child.gameObject);
        }

        List<Inventory.ItemEntry> currentItems = localInventory.GetItems();


        Debug.Log("--- UIManager: 인벤토리 UI 갱신 중 ---");

        if (currentItems == null || currentItems.Count == 0)
        {
            Debug.Log("인벤토리가 현재 비어있습니다 (0개).");
        }
        else
        {
            Debug.Log($"인벤토리 아이템 발견: {currentItems.Count} 종류.");
            foreach (var entry in currentItems)
            {
                Debug.Log($"   -> 아이템 ID: {entry.itemID}, 수량: {entry.quantity}개");
            }
        }

        Debug.Log("------------------------------------------");


        if (currentItems == null) return;

        foreach (Inventory.ItemEntry itemEntry in currentItems)
        {
            string itemID = itemEntry.itemID;
            int quantity = itemEntry.quantity;

            if (quantity > 0)
            {

                ItemData data = ServerMasterClient.Instance.GetItemData(itemID);

                if (data == null)
                {
                    Debug.LogError($"[UIManager ERROR] ItemData를 찾을 수 없습니다. ItemID: '{itemID}'.");
                    continue;
                }

                GameObject slotGO = Instantiate(itemSlotPrefab, inventoryContent);
                ItemSlot slotComponent = slotGO.GetComponent<ItemSlot>();

                if (slotComponent != null)
                {
                    slotComponent.SetItem(data, quantity); 
                }
                else
                {
                    TextMeshProUGUI itemText = slotGO.GetComponentInChildren<TextMeshProUGUI>();
                    if (itemText != null)
                    {
                        itemText.text = $"{data.itemName} ({quantity})";
                    }
                    else
                    {
                        Debug.LogWarning($"ItemSlot 프리팹에 ItemSlot 및 TextMeshProUGUI 컴포넌트가 없습니다. ID: {itemID}");
                    }
                }
            }
        }
    }

    private void ShowConfirmationPopup(ItemData item, Shop shopInstance)
    {
        confirmationPanel.SetActive(true);
        confirmText.text = item.itemName + "을(를) 구매하시겠습니까?";

        yesButton.onClick.RemoveAllListeners();
        yesButton.onClick.AddListener(() =>
        {
            if (localInventory != null && localInventory.CanAfford(item.price))
            {
                shopInstance.RequestPurchase(item.itemID);
            }
            else
            {
                Debug.Log("골드가 부족합니다.");
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