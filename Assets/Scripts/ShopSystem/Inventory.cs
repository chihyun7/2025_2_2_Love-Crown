using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;

public class Inventory : MonoBehaviourPunCallbacks
{
    public PhotonView pv;

    // ������ ID(string)�� ����(int)�� �����ϴ� Dictionary�� ����մϴ�.
    private Dictionary<string, int> items = new Dictionary<string, int>();

    public int gold = 100;

    void Awake()
    {
        if (pv == null)
        {
            pv = GetComponent<PhotonView>();
        }

        if (pv == null)
        {
            Debug.LogError("Inventory ������Ʈ�� PhotonView�� �����Ǿ����ϴ�.");
        }
    }

    /// <summary>
    /// UIManager�� �κ��丮 �����͸� �о �� �ֵ��� �ϴ� Getter �޼����Դϴ�. (������ �κ�)
    /// </summary>
    public Dictionary<string, int> GetItems()
    {
        return items;
    }

    public bool CanAfford(int requiredPrice)
    {
        return gold >= requiredPrice;
    }

    [PunRPC]
    public void RpcExecuteBuy(string itemID, int price)
    {
        // 1. ��� ���� 
        gold -= price;

        // 2. �κ��丮�� ������ �߰�
        if (items.ContainsKey(itemID))
        {
            items[itemID]++;
        }
        else
        {
            items.Add(itemID, 1);
        }

        // 3. UI ������Ʈ: ���� Ŭ���̾�Ʈ������ ���� (��Ƽ�÷��� ȯ�濡�� �ʼ�)
        if (pv.IsMine)
        {
            UpdateLocalUI();
        }

        Debug.Log($"[Inventory] {pv.Owner.NickName} ���� ���� �Ϸ�: {itemID}. ���� ���: {gold}");
    }

    public void AddItem(string itemID, int quantity = 1)
    {
        if (items.ContainsKey(itemID))
        {
            items[itemID] += quantity;
        }
        else
        {
            items.Add(itemID, quantity);
        }

        if (pv.IsMine)
        {
            UpdateLocalUI();
        }
    }

    public void RemoveItem(string itemID, int quantity = 1)
    {
        if (items.ContainsKey(itemID))
        {
            items[itemID] -= quantity;
            if (items[itemID] <= 0)
            {
                items.Remove(itemID);
            }
        }

        if (pv.IsMine)
        {
            UpdateLocalUI();
        }
    }

    public bool HasItem(string itemID)
    {
        return items.ContainsKey(itemID) && items[itemID] > 0;
    }

    // UI ������Ʈ �Լ�: ���� �÷��̾� ����
    private void UpdateLocalUI()
    {
        if (UIManager.instance != null)
        {
            // ��� �ؽ�Ʈ ������Ʈ
            UIManager.instance.UpdateGoldText(gold);

            // �κ��丮 �г��� �����ִ� ��쿡�� ��� UI ������Ʈ
            if (UIManager.instance.inventoryPanel != null && UIManager.instance.inventoryPanel.activeInHierarchy)
            {
                // UIManager���� GetItems()�� ȣ���Ͽ� ����� �׸��ϴ�.
                UIManager.instance.UpdateInventoryUI();
            }
        }
    }
}