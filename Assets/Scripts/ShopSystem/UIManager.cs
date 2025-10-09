using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

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
    public GameObject itemSlotPrefab;

    void Awake()
    {
        if (instance == null) instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        UpdateGoldUI();
    }

    public void UpdateGoldUI()
    {
        goldText.text = "Gold: " + Inventory.instance.gold;
    }

    public void ToggleInventoryPanel()
    {
        inventoryPanel.SetActive(!inventoryPanel.activeInHierarchy);
        if (inventoryPanel.activeInHierarchy)
        {
            UpdateInventoryUI();
        }
    }

    public void OpenShop(List<ItemData> itemsToSell, Shop shopInstance)
    {
        shopPanel.SetActive(true);
        FindObjectOfType<PlayerMove>().canMove = false;

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
        FindObjectOfType<PlayerMove>().canMove = true;
    }

    public void UpdateInventoryUI()
    {
        foreach (Transform child in inventoryContent)
        {
            Destroy(child.gameObject);
        }

        foreach (ItemData item in Inventory.instance.items)
        {
            GameObject slotGO = Instantiate(itemSlotPrefab, inventoryContent);
            ItemSlot slot = slotGO.GetComponent<ItemSlot>();
            slot.Initialize(item, null, false);
        }
    }

    private void ShowConfirmationPopup(ItemData item, Shop shopInstance)
    {
        confirmationPanel.SetActive(true);
        confirmText.text = item.itemName + "을(를) 구매하시겠습니까?";

        yesButton.onClick.RemoveAllListeners();
        yesButton.onClick.AddListener(() =>
        {
            shopInstance.BuyItem(item);
            confirmationPanel.SetActive(false);
        });

        noButton.onClick.RemoveAllListeners();
        noButton.onClick.AddListener(() =>
        {
            confirmationPanel.SetActive(false);
        });
    }
}