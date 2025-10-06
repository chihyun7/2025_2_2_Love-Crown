using System.Collections.Generic;
using UnityEngine;

public class Inventory : MonoBehaviour
{
    public static Inventory instance;
    public List<ItemData> items = new List<ItemData>();
    public int gold = 100;

    void Awake()
    {
        if (instance == null) { instance = this; DontDestroyOnLoad(gameObject); }
        else { Destroy(gameObject); }
    }

    public void AddItem(ItemData item)
    {
        items.Add(item);
        if (UIManager.instance.inventoryPanel.activeInHierarchy)
        {
            UIManager.instance.UpdateInventoryUI();
        }
    }

    public void RemoveItem(string itemID)
    {
        ItemData itemToRemove = items.Find(item => item.itemID == itemID);
        if (itemToRemove != null)
        {
            items.Remove(itemToRemove);
            if (UIManager.instance.inventoryPanel.activeInHierarchy)
            {
                UIManager.instance.UpdateInventoryUI();
            }
        }
    }

    public bool HasItem(string itemID)
    {
        return items.Exists(item => item.itemID == itemID);
    }
}