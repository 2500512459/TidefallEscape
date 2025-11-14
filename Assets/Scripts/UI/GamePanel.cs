using System.Collections;
using System.Collections.Generic;
using CleverCrow.Fluid.BTs.Tasks;
using UnityEngine;
using UnityEngine.UIElements;

public class GamePanel : MonoSingleton<GamePanel>
{
    [SerializeField] private CurrencyDataSO currencyData;

    private int goldCoinAmount = 0;
    private int gemstoneAmount = 0;

    private VisualElement rootElement;
    private Label goldCoinLabel;
    private Label gemstoneLabel;

    protected override void Awake()
    {
        base.Awake();
        rootElement = GetComponent<UIDocument>().rootVisualElement;
        goldCoinLabel = rootElement.Q<Label>("GoldCoinAmount");
        gemstoneLabel = rootElement.Q<Label>("GemstoneAmount");
        InitializeCurrencyData();
        SyncCurrencyFromData();
    }

    private void OnEnable()
    {
        EventManager.Listen<CurrencyAmountChangeMessage>(this, OnCurrencyAmountChanged);
        SyncCurrencyFromData();
    }

    private void OnDisable()
    {
        EventManager.Unlisten<CurrencyAmountChangeMessage>(this);
    }
    public int GoldCoinAmount
    {
        get => goldCoinAmount;
        set
        {
            goldCoinAmount = value;
            UpdateCurrencyLabels();
        }
    }
    public int GemstoneAmount
    {
        get => gemstoneAmount;
        set
        {
            gemstoneAmount = value;
            UpdateCurrencyLabels();
        }
    }

    private void OnCurrencyAmountChanged(CurrencyAmountChangeMessage message)
    {
        GoldCoinAmount = message.GoldCoinAmount;
        GemstoneAmount = message.GemstoneAmount;
    }

    private void SyncCurrencyFromData()
    {
        if (currencyData != null)
        {
            goldCoinAmount = currencyData.GoldCoinAmount;
            gemstoneAmount = currencyData.GemstoneAmount;
        }
        else
        {
            goldCoinAmount = 0;
            gemstoneAmount = 0;
        }
        UpdateCurrencyLabels();
    }

    private void InitializeCurrencyData()
    {
        if (currencyData != null)
        {
            currencyData.EnsureLoaded();
            currencyData.Broadcast();
        }
        else
        {
            Debug.LogWarning("[GamePanel] 未分配 CurrencyDataSO，将使用默认货币配置。", this);
        }
    }

    private void UpdateCurrencyLabels()
    {
        if (goldCoinLabel != null)
        {
            goldCoinLabel.text = goldCoinAmount.ToString();
        }
        if (gemstoneLabel != null)
        {
            gemstoneLabel.text = gemstoneAmount.ToString();
        }
    }
}
