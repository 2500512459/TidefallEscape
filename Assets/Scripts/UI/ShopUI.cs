using UnityEngine;

public class ShopUI : MonoSingleton<ShopUI>
{
    public ShopType shopType = ShopType.WeaponShop;
    [Header("默认交易倍率")]
    public float defaultBuyPriceMultiplier = 1f;
    public float defaultSellPriceMultiplier = 1f;

    [Header("Panel Root")]
    [SerializeField] GameObject panelRoot;
    [SerializeField] ShopScrollViewPanel shopScrollViewPanel;
    public bool IsVisible => panelRoot != null ? panelRoot.activeSelf : gameObject.activeSelf;

    private float currentBuyPriceMultiplier = 1f;
    private float currentSellPriceMultiplier = 1f;

    protected override void Awake()
    {
        base.Awake();
        if (Instance == this)
        {
            if (panelRoot == null)
            {
                panelRoot = gameObject;
            }

            currentBuyPriceMultiplier = defaultBuyPriceMultiplier;
            currentSellPriceMultiplier = defaultSellPriceMultiplier;
            shopScrollViewPanel.SetShopType(shopType, currentBuyPriceMultiplier, currentSellPriceMultiplier);

            HidePanel();
        }
    }

    public void ShowPanel()
    {
        ShowPanel(shopType, currentBuyPriceMultiplier, currentSellPriceMultiplier);
    }

    public void ShowPanel(ShopType newShopType, float buyMultiplier, float sellMultiplier)
    {
        // 兼容旧接口：从ShopManager获取库存数据
        var shopMgr = ShopManager.Instance;
        if (shopMgr == null)
        {
            Debug.LogError("[ShopUI] ShopManager 实例不存在");
            return;
        }

        var inventoryData = shopMgr.GetInventory(newShopType);
        if (inventoryData == null)
        {
            Debug.LogError($"[ShopUI] 未配置 {newShopType} 的商店库存数据");
            return;
        }

        ShowPanel(inventoryData, newShopType, buyMultiplier, sellMultiplier);
    }

    public void ShowPanel(InventoryDataSO shopInventoryData, ShopType newShopType, float buyMultiplier, float sellMultiplier)
    {
        shopType = newShopType;
        currentBuyPriceMultiplier = Mathf.Max(0f, buyMultiplier);
        currentSellPriceMultiplier = Mathf.Max(0f, sellMultiplier);

        if (panelRoot != null)
        {
            panelRoot.SetActive(true);
            shopScrollViewPanel.SetShopInventory(shopInventoryData, shopType, currentBuyPriceMultiplier, currentSellPriceMultiplier);
        }
    }

    public void HidePanel()
    {
        if (panelRoot != null)
        {
            panelRoot.SetActive(false);
        }
    }

    public void TogglePanel()
    {
        if (IsVisible)
        {
            HidePanel();
        }
        else
        {
            ShowPanel();
        }
    }
}


