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
            // TODO: ��ʾUI��ʾ���� ���� F ʰȡ xx����
            Debug.Log($"������Ʒ��{ItemData.itemName}");
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerNearby = false;
            // TODO: ������ʾ
            Debug.Log("�뿪��Ʒ��Χ");
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
        Debug.Log($"ʰȡ��{ItemData.itemName} x{count}");
        if (isAdd == true)
            Destroy(gameObject); // ʰȡ������ģ��
    }
}
