using UnityEngine;

public class ShopUI : MonoSingleton<ShopUI>
{
    public ShopType shopType = ShopType.WeaponShop;
    [Header("Panel Root")]
    [SerializeField] GameObject panelRoot;
    [SerializeField] ShopScrollViewPanel shopScrollViewPanel;
    public bool IsVisible => panelRoot != null ? panelRoot.activeSelf : gameObject.activeSelf;

    protected override void Awake()
    {
        base.Awake();
        if (Instance == this)
        {
            if (panelRoot == null)
            {
                panelRoot = gameObject;
            }
            shopScrollViewPanel.shopType = shopType;
            HidePanel();

        }
    }

    public void ShowPanel(ShopType shopType = ShopType.WeaponShop)
    {
        if (panelRoot != null)
        {
            panelRoot.SetActive(true);
            shopScrollViewPanel.shopType = shopType;
            shopScrollViewPanel.OnShopButtonClick();
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

