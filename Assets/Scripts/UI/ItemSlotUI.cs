using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// ������Ʒ�� UI �߼���
/// - ��ʾͼ�� / ���� / ����
/// - ֧����ק������������ܣ�
/// </summary>
public class ItemSlotUI : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IDropHandler
{
    [Header("UI����")]
    public Image backgroundImage;           // ��ɫ�߿򱳾�
    public Image itemIcon;                  // ͼ��
    public TextMeshProUGUI itemName;        // ����
    public TextMeshProUGUI itemCount;       // �����ı�

    [Header("״̬����")]
    public ItemStack currentItem;           // ��ǰ��Ʒ����

    [Header("���ݱ�ʶ")]
    public InventoryType inventoryType;  // �����ĸ� SO
    public int slotIndex;                // ���б��е�����

    private CanvasGroup canvasGroup;        // ��קʱ����͸����
    private Transform originalParent;       // ��קǰ�ĸ��ڵ�
    private static ItemSlotUI draggedSlot;  // ��ǰ���ڱ���ק�Ĳۣ���̬�����㽻����
    private static GameObject dragIcon;     // ��ק���������ͼ�꣨��̬�����㽻����

    private void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
    }

    /// <summary>
    /// ������Ʒ��Ϣ��������������������ˢ����ʾ
    /// </summary>
    public void SetItem(ItemStack itemStack, InventoryType type, int index)
    {
        inventoryType = type;
        slotIndex = index;
        currentItem = itemStack;

        if (itemStack == null || itemStack.item == null)
        {
            ClearSlot();
            return;
        }

        itemIcon.enabled = true;
        itemIcon.sprite = itemStack.item.icon;
        itemName.text = itemStack.item.itemName;
        itemCount.text = itemStack.count > 1 ? itemStack.count.ToString() : "";
    }

    /// <summary>
    /// ��ղ�λ��ʾ
    /// </summary>
    public void ClearSlot()
    {
        currentItem = null;
        itemIcon.enabled = false;
        itemIcon.sprite = null;
        itemName.text = "";
        itemCount.text = "";
    }

    // ======================
    // ��ק��������
    // ======================
    //��ʼ��ק
    public void OnBeginDrag(PointerEventData eventData)
    {
        if (currentItem == null || currentItem.item == null)
            return;

        draggedSlot = this;
        originalParent = transform.parent;

        // ����������קͼ��
        dragIcon = new GameObject("DragIcon");
        dragIcon.transform.SetParent(UIManger.Instance.Root); // ����UI���ڵ�
        var img = dragIcon.AddComponent<Image>();
        img.sprite = itemIcon.sprite;
        img.raycastTarget = false; // ���⵲ס����¼�
        dragIcon.GetComponent<RectTransform>().sizeDelta = itemIcon.rectTransform.sizeDelta;

        canvasGroup.alpha = 0.6f; // �ñ���ק�Ĳ۱䵭
    }
    //��קʱ
    public void OnDrag(PointerEventData eventData)
    {
        if (dragIcon != null)
        {
            dragIcon.transform.position = eventData.position;
        }
    }
    //��ק����
    public void OnEndDrag(PointerEventData eventData)
    {
        // ��ק����������ͼ��
        if (dragIcon != null)
        {
            Destroy(dragIcon);
            dragIcon = null;
        }

        canvasGroup.alpha = 1f;
        draggedSlot = null;
    }

    /// <summary>
    /// ��������Ʒ�ϵ��˲��Ϸ��ɿ�ʱ����
    /// </summary>
    public void OnDrop(PointerEventData eventData)
    {
        // ��Ч������û����קԴ �� ��קĿ�����Լ�
        if (draggedSlot == null || draggedSlot == this)
            return;

        // ============= ��ȡ˫����Դ��Ŀ����Ϣ =============
        var fromSlot = draggedSlot;
        var toSlot = this;

        var fromItem = fromSlot.currentItem;
        var toItem = toSlot.currentItem;

        var fromType = fromSlot.inventoryType;
        var toType = toSlot.inventoryType;

        var fromIndex = fromSlot.slotIndex;
        var toIndex = toSlot.slotIndex;

        var fromInventory = InventoryManager.Instance.GetInventory(fromType);
        var toInventory = InventoryManager.Instance.GetInventory(toType);

        // ============= ����Ƿ�����ͬ��Ʒ�������߼��� =============
        if (toItem != null && fromItem != null &&
            toItem.item == fromItem.item &&
            toItem.count < toItem.item.maxStack) // maxStack: ÿ����Ʒ�Ķѵ�����
        {
            int canAdd = toItem.item.maxStack - toItem.count;
            int toAdd = Mathf.Min(canAdd, fromItem.count);

            // �޸� SO �е���ʵ����
            var toData = toInventory.items[toIndex];
            var fromData = fromInventory.items[fromIndex];

            toItem.count += toAdd;
            fromItem.count -= toAdd;

            // �����קԴ��Ʒ�ƶ���û��ʣ�࣬�����
            if (fromItem.count <= 0)
            {
                fromInventory.items[fromIndex] = null;
                fromSlot.ClearSlot();
            }

            // ����Ŀ����
            toSlot.SetItem(toItem, toType, toIndex);

            InventoryManager.Instance.OnInventoryChanged(fromType);
            InventoryManager.Instance.OnInventoryChanged(toType);
            return;
        }

        // ============= ���������ͨ���� =============
        var temp = toInventory.items[toIndex];
        toInventory.items[toIndex] = fromInventory.items[fromIndex];
        fromInventory.items[fromIndex] = temp;

        toSlot.SetItem(toInventory.items[toIndex], toType, toIndex);
        fromSlot.SetItem(fromInventory.items[fromIndex], fromType, fromIndex);

        InventoryManager.Instance.OnInventoryChanged(fromType);
        InventoryManager.Instance.OnInventoryChanged(toType);
    }

    // ======================
    // ���ߺ���
    // ======================

    /// <summary>
    /// �жϸò��Ƿ�Ϊ��
    /// </summary>
    public bool IsEmpty()
    {
        return currentItem == null || currentItem.item == null;
    }

}
