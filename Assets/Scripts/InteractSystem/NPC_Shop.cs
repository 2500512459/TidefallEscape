using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NPC_Shop : BaseInteractable
{
    public ShopType shopType = ShopType.WeaponShop;
    public float buyPriceMultiplier = 1f;
    public float sellPriceMultiplier = 0.5f;

    [Header("商店库存")]
    [Tooltip("该NPC的库存数据副本（运行时自动从ShopManager复制）")]
    [SerializeField] private InventoryDataSO shopInventoryData;

    private void OnEnable()
    {
        RefreshShopInventory();
    }

    private void RefreshShopInventory()
    {
        var shopMgr = ShopManager.Instance;
        if (shopMgr == null)
        {
            Debug.LogError($"[NPC_Shop] ShopManager 实例不存在，无法刷新 {shopType} 的库存数据", this);
            return;
        }

        var sourceInventory = shopMgr.GetInventory(shopType);
        if (sourceInventory == null)
        {
            Debug.LogError($"[NPC_Shop] 未配置 {shopType} 的商店库存数据", this);
            return;
        }

        if (shopInventoryData == null)
        {
            shopInventoryData = ScriptableObject.CreateInstance<InventoryDataSO>();
        }

        shopInventoryData.type = sourceInventory.type;
        shopInventoryData.maxCount = sourceInventory.maxCount;

        if (shopInventoryData.items == null)
        {
            shopInventoryData.items = new List<ItemStack>(sourceInventory.items.Count);
        }
        shopInventoryData.items.Clear();

        foreach (var stack in sourceInventory.items)
        {
            if (stack != null && stack.item != null)
            {
                shopInventoryData.items.Add(new ItemStack(stack.item, stack.count));
            }
            else
            {
                shopInventoryData.items.Add(null);
            }
        }

        shopInventoryData.EnsureSlotCount(shopInventoryData.maxCount);
    }
    
    public override void OnFocus(Character player)
    {
        // 根据商店是否打开显示不同的提示
        if (ShopUI.Instance != null && ShopUI.Instance.IsVisible)
        {
            InteractHintUI.Instance.ShowHint("关闭商店", key);
        }
        else
        {
            // 显示默认提示（通常是"进入商店"）
            base.OnFocus(player);
        }
    }
    
    public override void Interact(Character player)
    {
        if (ShopUI.Instance.IsVisible)
        {
            ShopUI.Instance.HidePanel();
            PlayerInput.Instance.EnableAllInputs();
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            
            // 关闭商店后，重新显示交互提示
            // 使用协程延迟一帧，确保 PlayerCtrl 的交互系统能正确更新
            StartCoroutine(RefreshHintAfterClose(player));
        }
        else
        {
            if (shopInventoryData == null)
            {
                RefreshShopInventory();
            }

            var shopPanel = ShopUI.Instance;
            if (shopPanel == null)
            {
                Debug.LogError("[NPC_Shop] ShopUI 实例不存在，无法打开商店");
                return;
            }

            shopPanel.ShowPanel(shopInventoryData, shopType, buyPriceMultiplier, sellPriceMultiplier);
            InteractHintUI.Instance.ShowHint("关闭商店", key);
            PlayerInput.Instance.DisableAllInputsExcept(PlayerInput.Instance.InteractionEventInput);
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

    }
    
    private IEnumerator RefreshHintAfterClose(Character player)
    {
        // 等待一帧，确保 PlayerCtrl 的交互系统已经更新
        yield return null;
        
        // 重新显示交互提示
        OnFocus(player);
    }
}
