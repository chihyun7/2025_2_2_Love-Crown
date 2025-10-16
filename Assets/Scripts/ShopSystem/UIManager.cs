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

        // localInventory를 찾았으면 골드 UI를 업데이트
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
    /// 로컬 플레이어의 Inventory 컴포넌트를 찾아 할당합니다.
    /// </summary>
    public void FindLocalPlayerInventory()
    {
        Inventory[] inventories = FindObjectsOfType<Inventory>();

        foreach (Inventory inv in inventories)
        {
            if (inv.pv != null && inv.pv.IsMine)
            {
                localInventory = inv;
                Debug.Log("UIManager: 로컬 플레이어 인벤토리 찾음");

                // 찾은 후 초기 골드 UI 업데이트
                UpdateGoldText(localInventory.gold);
                return;
            }
        }

        if (localInventory == null)
        {
            Debug.LogWarning("UIManager: 로컬 플레이어 인벤토리를 아직 찾지 못했습니다. 스폰 후 다시 시도됩니다.");
        }
    }


    /// <summary>
    /// Inventory.cs의 RpcExecuteBuy에서 호출되는 골드 UI 업데이트 함수입니다.
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

        // 널 조건부 접근(?. )을 명시적 널 체크로 변경하여 C# 호환성 문제 방지
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

        // 널 조건부 접근(?. )을 명시적 널 체크로 변경하여 C# 호환성 문제 방지
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
    /// Inventory.cs의 Dictionary<string, int> 구조를 반영하여 UI를 그립니다.
    /// </summary>
    public void UpdateInventoryUI()
    {
        if (localInventory == null)
        {
            Debug.LogError("로컬 인벤토리가 준비되지 않았습니다.");
            return;
        }

        // 기존 아이템 슬롯 제거
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

                // ItemSlot 프리팹 내부에 아이템 이름과 수량을 표시할 TextMeshProUGUI가 있다고 가정하고 직접 업데이트
                TextMeshProUGUI itemText = slotGO.GetComponentInChildren<TextMeshProUGUI>();
                if (itemText != null)
                {
                    itemText.text = $"{itemID} ({quantity})";
                }
                else
                {
                    Debug.LogWarning($"ItemSlot 프리팹에 TextMeshProUGUI 컴포넌트가 없습니다. ID: {itemID}");
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
                // TODO: UI에 골드 부족 메시지 표시
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
