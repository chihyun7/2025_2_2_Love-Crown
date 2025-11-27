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
    public GameObject questLogPanel;

    [Header("Layout & Prefabs")]
    public Transform shopContent;
    public Transform inventoryContent;
    public GameObject itemSlotPrefab; // ItemSlot 스크립트가 붙어 있다고 가정
    public Transform questLogContent; // 퀘스트 로그의 Content 오브젝트 연결
    public GameObject questSlotPrefab; // 퀘스트 슬롯 프리팹

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

    public void ToggleQuestLogPanel()
    {
        questLogPanel.SetActive(!questLogPanel.activeInHierarchy);
        if (questLogPanel.activeInHierarchy)
        {
            UpdateQuestLogUI(FindObjectOfType<PlayerQuestLog>());
        }
    }

    public void UpdateQuestLogUI(PlayerQuestLog questLog)
    {
        if (questLog == null || !questLogPanel.activeInHierarchy) return;

        foreach (Transform child in questLogContent)
        {
            Destroy(child.gameObject);
        }

        foreach (var questStatus in questLog.activeQuests.Values)
        {
            QuestData quest = GameManager.Instance.GetQuestData(questStatus.questID);
            if (quest == null) continue;

            GameObject slotGO = Instantiate(questSlotPrefab, questLogContent);
            TextMeshProUGUI slotText = slotGO.GetComponentInChildren<TextMeshProUGUI>();

            string progress = "";
            if (quest.objective.type == QuestObjective.ObjectiveType.Collect)
            {
                progress = $"({questStatus.currentProgress} / {quest.objective.targetItemQuantity})";
            }

            slotText.text = $"[진행중] {quest.questName} {progress}\n<size=50%>{quest.description}</size>";
        }

        foreach (string questID in questLog.completedQuestIDs)
        {
            QuestData quest = GameManager.Instance.GetQuestData(questID);
            if (quest == null) continue;

            GameObject slotGO = Instantiate(questSlotPrefab, questLogContent);
            TextMeshProUGUI slotText = slotGO.GetComponentInChildren<TextMeshProUGUI>();
            slotText.text = $"[완료] {quest.questName}";
            slotText.color = Color.gray;
        }
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
        if (localInventory == null || !localInventory.pv.IsMine)
        {
            Debug.LogWarning("[UIManager] localInventory가 null이거나 IsMine이 아님. 다시 찾습니다.");
            FindLocalPlayerInventory();
        }

        if (localInventory == null)
        {
            Debug.LogError("[UIManager] 여전히 로컬 인벤토리를 찾지 못했습니다.");
            return;
        }

        Debug.Log($"[UIManager] localInventory Owner={localInventory.pv.Owner.ActorNumber}");


        if (localInventory == null || GameManager.Instance == null)
        {
            Debug.LogError("로컬 인벤토리 또는 GameManager가 준비되지 않았습니다. UI 갱신 실패.");
            return;
        }

        // 1) Content 밑에 있는 ItemSlot 전부 가져오기 (비활성 포함)
        ItemSlot[] slots = inventoryContent.GetComponentsInChildren<ItemSlot>(true);
        Debug.Log($"[UIManager] 인벤토리 슬롯 개수: {slots.Length}");

        if (slots == null || slots.Length == 0)
        {
            Debug.LogError("인벤토리 Content 밑에 ItemSlot 컴포넌트가 하나도 없습니다.");
            return;
        }

        // 2) 모든 슬롯 비우기
        foreach (var slot in slots)
        {
            slot.Clear();
        }

        // 3) 인벤토리 아이템 리스트 가져오기
        List<Inventory.ItemEntry> currentItems = localInventory.GetItems();

        Debug.Log("--- UIManager: 인벤토리 UI 갱신 중 ---");
        if (currentItems == null || currentItems.Count == 0)
        {
            Debug.Log("인벤토리가 현재 비어있습니다 (0개).");
            return;
        }

        // 아이템 목록 찍어보기
        foreach (var e in currentItems)
        {
            Debug.Log($"[UIManager] 인벤토리 아이템: {e.itemID}, x{e.quantity}");
        }

        // 4) 아이템을 슬롯에 채우기
        int slotIndex = 0;

        foreach (var entry in currentItems)
        {
            if (entry.quantity <= 0) continue;
            if (slotIndex >= slots.Length) break;

            string itemID = entry.itemID;
            int quantity = entry.quantity;

            ItemData data = GameManager.Instance.GetItemData(itemID);
            if (data == null)
            {
                Debug.LogError($"[UIManager ERROR] ItemData를 찾을 수 없습니다. ItemID: '{itemID}'.");
                continue;
            }

            Debug.Log($"[UIManager] 슬롯 {slotIndex} 에 SetItem 호출: {itemID}, x{quantity}");
            slots[slotIndex].SetItem(data, quantity);
            slotIndex++;
        }

        Debug.Log("------------------------------------------");
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