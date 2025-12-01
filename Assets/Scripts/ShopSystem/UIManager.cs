using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    public static UIManager instance;

    [Header("UI Elements")]
    public TextMeshProUGUI goldText;
    public GameObject shopPanel;
    public GameObject inventoryPanel;
    public GameObject questLogPanel;
    public GameObject confirmationPanel;
    public TextMeshProUGUI confirmText;
    public Button yesButton;
    public Button noButton;

    [Header("Layout & Prefabs")]
    public Transform shopContent;
    public Transform inventoryContent;
    public Transform questLogContent;
    public GameObject itemSlotPrefab;
    public GameObject questSlotPrefab;

    [Header("Skill UI")]
    public Slider playerskill01;
    public Slider playerAttack;
    public Slider playerskill02;
    public Slider playerskill03;

    public Inventory localPlayerInventory;
    public PlayerQuestLog localPlayerQuestLog;
    public TestCharacterPlayerMoveMent localPlayerMovement;
    public DisturbanceSystem disturbanceSystem;

    public bool isPlayerSkill01;
    public bool isPlayerAttact;
    public bool isPlayerSkill02;
    public bool isPlayerSkill03;

    void Awake()
    {
        if (instance == null) instance = this;
        else Destroy(gameObject);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.P))
        {
            Debug.Log("[UIManager] P키 눌림: 내 플레이어 강제 탐색 시도");
            FindLocalPlayer();
        }
    }

    public void FindLocalPlayer()
    {
        foreach (var player in FindObjectsOfType<PhotonView>())
        {
            if (player.IsMine && player.GetComponent<Inventory>() != null)
            {
                SetLocalPlayer(player.gameObject);
                Debug.Log($"[UIManager] P키로 내 플레이어 강제 탐색 성공: {player.Owner.NickName}");
                return;
            }
        }
        Debug.LogWarning("[UIManager] 내 플레이어를 찾지 못했습니다.");
    }

    public void SetLocalPlayer(GameObject playerObj)
    {
        localPlayerInventory = playerObj.GetComponent<Inventory>();
        localPlayerQuestLog = playerObj.GetComponent<PlayerQuestLog>();
        localPlayerMovement = playerObj.GetComponent<TestCharacterPlayerMoveMent>();
        disturbanceSystem = playerObj.GetComponent<DisturbanceSystem>();

        if (localPlayerInventory != null)
        {
            UpdateGoldText(localPlayerInventory.gold);
        }

        Debug.Log($"[UIManager] 플레이어 등록 완료: {playerObj.name}");
    }

    public void UpdateGoldText(int currentGold)
    {
        if (goldText != null)
            goldText.text = "Gold: " + currentGold;
    }

    public void ToggleInventoryPanel()
    {
        if (localPlayerInventory == null) return;

        inventoryPanel.SetActive(!inventoryPanel.activeInHierarchy);
        if (inventoryPanel.activeInHierarchy)
        {
            UpdateInventoryUI();
        }
    }

    public void UpdateInventoryUI()
    {
        if (localPlayerInventory == null) return;

        foreach (Transform child in inventoryContent) Destroy(child.gameObject);

        foreach (var itemEntry in localPlayerInventory.GetItems())
        {
            ItemData data = GameManager.Instance.GetItemData(itemEntry.itemID);
            if (data != null)
            {
                GameObject slotGO = Instantiate(itemSlotPrefab, inventoryContent);
                ItemSlot slot = slotGO.GetComponent<ItemSlot>();

                if (slot != null)
                {
                    slot.SetItem(data, itemEntry.quantity);
                }
            }
        }
    }

    public void ToggleQuestLogPanel()
    {
        if (localPlayerQuestLog == null) return;

        questLogPanel.SetActive(!questLogPanel.activeInHierarchy);
        if (questLogPanel.activeInHierarchy)
        {
            UpdateQuestLogUI(localPlayerQuestLog);
        }
    }

    public void UpdateQuestLogUI(PlayerQuestLog questLog)
    {
        if (questLog == null || !questLog.GetComponent<PhotonView>().IsMine) return;
        if (!questLogPanel.activeInHierarchy) return;

        foreach (Transform child in questLogContent) Destroy(child.gameObject);

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

            slotText.text = $"[진행중] {quest.questName} {progress}\n<size=70%>{quest.description}</size>";
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

    public void OpenShop(List<ItemData> itemsToSell, Shop shopInstance)
    {
        if (localPlayerInventory == null) return;

        shopPanel.SetActive(true);

        if (localPlayerMovement != null) localPlayerMovement.canMove = false;

        foreach (Transform child in shopContent) Destroy(child.gameObject);

        foreach (ItemData item in itemsToSell)
        {
            GameObject slotGO = Instantiate(itemSlotPrefab, shopContent);
            ItemSlot slot = slotGO.GetComponent<ItemSlot>();
            if (slot != null)
            {
                slot.Initialize(item, () => ShowConfirmationPopup(item, shopInstance));
            }
        }
    }

    public void CloseShop()
    {
        shopPanel.SetActive(false);
        if (localPlayerMovement != null) localPlayerMovement.canMove = true;
    }

    private void ShowConfirmationPopup(ItemData item, Shop shopInstance)
    {
        confirmationPanel.SetActive(true);
        confirmText.text = item.itemName + "을(를) 구매하시겠습니까?";

        yesButton.onClick.RemoveAllListeners();
        yesButton.onClick.AddListener(() =>
        {
            if (localPlayerInventory != null && localPlayerInventory.CanAfford(item.price))
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

    public void StartSkill01Cooldown() { if (!isPlayerSkill01) { isPlayerSkill01 = true; StartCoroutine(Skill01CooldownCoroutine()); } }
    public void StartSkill02Cooldown() { if (!isPlayerSkill02) { isPlayerSkill02 = true; StartCoroutine(Skill02CooldownCoroutine()); } }
    public void StartSkill03Cooldown() { if (!isPlayerSkill03) { isPlayerSkill03 = true; StartCoroutine(Skill03CooldownCoroutine()); } }
    public void StartAttackCooldown(float duration) { if (!isPlayerAttact) { isPlayerAttact = true; StartCoroutine(AttackCooldownCoroutine(duration)); } }

    private IEnumerator Skill01CooldownCoroutine() { return CooldownRoutine(playerskill01, () => isPlayerSkill01 = false); }
    private IEnumerator Skill02CooldownCoroutine() { return CooldownRoutine(playerskill02, () => isPlayerSkill02 = false); }
    private IEnumerator Skill03CooldownCoroutine() { return CooldownRoutine(playerskill03, () => isPlayerSkill03 = false); }

    private IEnumerator CooldownRoutine(Slider slider, System.Action onComplete)
    {
        if (disturbanceSystem == null) { onComplete?.Invoke(); yield break; }
        float duration = disturbanceSystem.COOLDOWN_DURATION;
        float current = duration;
        slider.maxValue = duration;
        while (current > 0) { current -= Time.deltaTime; slider.value = current; yield return null; }
        slider.value = 0; onComplete?.Invoke();
    }

    private IEnumerator AttackCooldownCoroutine(float duration)
    {
        float current = duration;
        playerAttack.maxValue = duration;
        while (current > 0) { current -= Time.deltaTime; playerAttack.value = current; yield return null; }
        playerAttack.value = 0; isPlayerAttact = false;
    }
}