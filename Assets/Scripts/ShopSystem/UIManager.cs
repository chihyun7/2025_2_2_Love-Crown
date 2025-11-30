using Photon.Pun;
using System;
using System.Collections;
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
    public GameObject itemSlotPrefab;
    public Transform questLogContent;
    public GameObject questSlotPrefab;

    [Header("Skill")]
    public Slider playerskill01;
    public Slider playerAttack;
    public Slider playerskill02;
    public Slider playerskill03;

    public DisturbanceSystem disturbanceSystem;
    public TestCharacterPlayerMoveMent characterPlayerMoveMent;
    public PlayerMove playerMove;

    public bool isPlayerSkill01;
    public bool isPlayerAttact;
    public bool isPlayerSkill02;
    public bool isPlayerSkill03;

    private Inventory localInventory;
    private GameObject localPlayerObject;
    public PhotonView pv;

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
        FindLocalPlayer();
    }

    private void Update()
    {
        if (pv == null || !pv.IsMine || localPlayerObject == null) return;

    }

    public void StartSkill01Cooldown()
    {
        if (isPlayerSkill01) return;

        isPlayerSkill01 = true;
        StartCoroutine(Skill01CooldownCoroutine());
    }

    private IEnumerator Skill01CooldownCoroutine()
    {
        if (disturbanceSystem == null)
        {
            isPlayerSkill01 = false;
            yield break;
        }

        float cooldownDuration = disturbanceSystem.COOLDOWN_DURATION;
        float currentTime = cooldownDuration;

        playerskill01.maxValue = cooldownDuration;

        while (currentTime > 0f)
        {
            currentTime -= Time.deltaTime;
            playerskill01.value = currentTime;
            yield return null;
        }

        playerskill01.value = 0f;
        isPlayerSkill01 = false;
    }

    public void StartSkill02Cooldown()
    {
        if (isPlayerSkill02) return;

        isPlayerSkill02 = true;
        StartCoroutine(Skill02CooldownCoroutine());
    }

    private IEnumerator Skill02CooldownCoroutine()
    {
        if (disturbanceSystem == null)
        {
            isPlayerSkill02 = false;
            yield break;
        }

        float cooldownDuration = disturbanceSystem.COOLDOWN_DURATION;
        float currentTime = cooldownDuration;

        playerskill02.maxValue = cooldownDuration;

        while (currentTime > 0f)
        {
            currentTime -= Time.deltaTime;
            playerskill02.value = currentTime;
            yield return null;
        }

        playerskill02.value = 0f;
        isPlayerSkill02 = false;
    }

    public void StartSkill03Cooldown()
    {
        if (isPlayerSkill03) return;

        isPlayerSkill03 = true;
        StartCoroutine(Skill03CooldownCoroutine());
    }

    private IEnumerator Skill03CooldownCoroutine()
    {
        if (disturbanceSystem == null)
        {
            isPlayerSkill03 = false;
            yield break;
        }

        float cooldownDuration = disturbanceSystem.COOLDOWN_DURATION;
        float currentTime = cooldownDuration;

        playerskill03.maxValue = cooldownDuration;

        while (currentTime > 0f)
        {
            currentTime -= Time.deltaTime;
            playerskill03.value = currentTime;
            yield return null;
        }

        playerskill03.value = 0f;
        isPlayerSkill03 = false;
    }

  
    public void StartAttackCooldown(float duration)
    {
        if (isPlayerAttact) return;

        isPlayerAttact = true;
        StartCoroutine(AttackCooldownCoroutine(duration));
    }

    private IEnumerator AttackCooldownCoroutine(float duration)
    {
        float currentTime = duration;
        playerAttack.maxValue = duration;

        while (currentTime > 0f)
        {
            currentTime -= Time.deltaTime;
            playerAttack.value = currentTime;
            yield return null;
        }

        playerAttack.value = 0f;
        isPlayerAttact = false;
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

    public void FindLocalPlayer()
    {
        Inventory[] inventories = FindObjectsOfType<Inventory>();

        foreach (Inventory inv in inventories)
        {
            if (inv.pv != null && inv.pv.IsMine)
            {
                localInventory = inv;
                localPlayerObject = inv.gameObject;

                characterPlayerMoveMent = localPlayerObject.GetComponent<TestCharacterPlayerMoveMent>();
                disturbanceSystem = localPlayerObject.GetComponent<DisturbanceSystem>();
                playerMove = localPlayerObject.GetComponent<PlayerMove>();

                UpdateGoldText(localInventory.gold);
                CancelInvoke("RetryFindLocalPlayer");
                return;
            }
        }

        if (localInventory == null && !IsInvoking("RetryFindLocalPlayer"))
        {
            Invoke("RetryFindLocalPlayer", 1.0f);
        }
    }

    private void RetryFindLocalPlayer()
    {
        FindLocalPlayer();
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
        if (localInventory == null) FindLocalPlayer();

        if (localInventory == null)
        {
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
        if (localInventory == null) FindLocalPlayer();

        shopPanel.SetActive(true);

        if (playerMove != null)
        {
            playerMove.canMove = false;
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
            }
        }
    }

    public void CloseShop()
    {
        shopPanel.SetActive(false);

        if (playerMove != null)
        {
            playerMove.canMove = true;
        }
    }

    public void UpdateInventoryUI()
    {
        if (localInventory == null || GameManager.Instance == null)
        {
            return;
        }

        ItemSlot[] slots = inventoryContent.GetComponentsInChildren<ItemSlot>(true);

        if (slots == null || slots.Length == 0)
        {
            return;
        }

        foreach (var slot in slots)
        {
            slot.Clear();
        }

        List<Inventory.ItemEntry> currentItems = localInventory.GetItems();

        if (currentItems == null || currentItems.Count == 0)
        {
            return;
        }

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
                continue;
            }

            slots[slotIndex].SetItem(data, quantity);
            slotIndex++;
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