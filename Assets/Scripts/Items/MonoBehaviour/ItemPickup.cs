using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class ItemPickup : MonoBehaviour
{
    public ItemDataSO ItemData;
    public int count = 1;

    private bool isPlayerNearby = false;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerNearby = true;
            // TODO: 显示UI提示（如 “按 F 拾取 xx”）
            Debug.Log($"靠近物品：{ItemData.itemName}");
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerNearby = false;
            // TODO: 隐藏提示
            Debug.Log("离开物品范围");
        }
    }

    private void Update()
    {
        if (isPlayerNearby && Input.GetKeyDown(KeyCode.F))
        {
            PickupItem();
        }
    }

    void PickupItem()
    {
        bool isAdd = InventoryManager.Instance.AddItem(ItemData, count, InventoryType.Backpack);
        Debug.Log($"拾取：{ItemData.itemName} x{count}");
        if (isAdd == true)
            Destroy(gameObject); // 拾取后销毁模型
    }
}
