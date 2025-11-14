using UnityEngine;

[CreateAssetMenu(fileName = "CurrencyData", menuName = "Currency/Currency Data")]
public class CurrencyDataSO : ScriptableObject
{
    [System.Serializable]
    private class CurrencyState
    {
        public int goldCoinAmount;
        public int gemstoneAmount;
    }

    [Header("默认货币数量")]
    public int defaultGoldCoinAmount;
    public int defaultGemstoneAmount;

    [Tooltip("用于保存到 PlayerPrefs 的唯一键名")]
    public string saveKey = "CurrencyData";

    private CurrencyState runtimeState = new CurrencyState();
    private bool isLoaded;

    public int GoldCoinAmount
    {
        get
        {
            EnsureLoaded();
            return runtimeState.goldCoinAmount;
        }
    }

    public int GemstoneAmount
    {
        get
        {
            EnsureLoaded();
            return runtimeState.gemstoneAmount;
        }
    }

    public void EnsureLoaded()
    {
        if (isLoaded) return;

        runtimeState.goldCoinAmount = defaultGoldCoinAmount;
        runtimeState.gemstoneAmount = defaultGemstoneAmount;

        Load();
        isLoaded = true;
    }

    public void SetGoldCoins(int amount, bool broadcast = true)
    {
        EnsureLoaded();
        amount = Mathf.Max(0, amount);
        if (runtimeState.goldCoinAmount == amount) return;
        runtimeState.goldCoinAmount = amount;
        Save();
        if (broadcast)
        {
            Broadcast();
        }
    }

    public void SetGemstones(int amount, bool broadcast = true)
    {
        EnsureLoaded();
        amount = Mathf.Max(0, amount);
        if (runtimeState.gemstoneAmount == amount) return;
        runtimeState.gemstoneAmount = amount;
        Save();
        if (broadcast)
        {
            Broadcast();
        }
    }

    public void AddGoldCoins(int amount, bool broadcast = true)
    {
        if (amount == 0) return;
        EnsureLoaded();
        runtimeState.goldCoinAmount = Mathf.Max(0, runtimeState.goldCoinAmount + amount);
        Save();
        if (broadcast)
        {
            Broadcast();
        }
    }

    public void AddGemstones(int amount, bool broadcast = true)
    {
        if (amount == 0) return;
        EnsureLoaded();
        runtimeState.gemstoneAmount = Mathf.Max(0, runtimeState.gemstoneAmount + amount);
        Save();
        if (broadcast)
        {
            Broadcast();
        }
    }

    public void Broadcast()
    {
        EnsureLoaded();
        EventManager.Raise(new CurrencyAmountChangeMessage(runtimeState.goldCoinAmount, runtimeState.gemstoneAmount));
    }

    private void Load()
    {
        string key = GetSaveKey();
        var saveManager = SaveManager.Instance;
        if (saveManager != null)
        {
            saveManager.Load(runtimeState, key);
        }
        else if (PlayerPrefs.HasKey(key))
        {
            string jsonData = PlayerPrefs.GetString(key);
            JsonUtility.FromJsonOverwrite(jsonData, runtimeState);
        }
    }

    private void Save()
    {
        if (!isLoaded) return;

        string key = GetSaveKey();
        var saveManager = SaveManager.Instance;
        if (saveManager != null)
        {
            saveManager.Save(runtimeState, key);
        }
        else
        {
            string jsonData = JsonUtility.ToJson(runtimeState);
            PlayerPrefs.SetString(key, jsonData);
            PlayerPrefs.Save();
        }
    }

    private string GetSaveKey()
    {
        if (!string.IsNullOrEmpty(saveKey))
        {
            return saveKey;
        }
        return "CurrencyData";
    }
}



