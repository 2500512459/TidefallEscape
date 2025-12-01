using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Player : Character
{
    [Header("UI 组件")]
    public HealthBar healthBar;
    public VitalityBar vitalityBar;
    public MoistureBar moistureBar;

    [Header("角色属性（基础值）")]
    [SerializeField] private float baseMaxHealth = 100f;
    [SerializeField] private float baseMaxVitality = 100f;
    [SerializeField] private float baseMaxMoisture = 100f;
    [SerializeField] private float baseAttack = 10f;
    [SerializeField] private float baseDefense = 5f;
    public float moistureDecayTimeInMinutes = 10f; // 水分清空时间（分钟）
    [Header("实时属性（调试）")]
    [SerializeField] private float currentMaxHealth;
    [SerializeField] private float currentMaxVitality;
    [SerializeField] private float currentMaxMoisture;
    [SerializeField] private float currentAttack;
    [SerializeField] private float currentDefense;
    public float vitalityRecoveryRate = 8f;  // 每秒恢复
    private float lastVitalityValue;          // 上一帧体力值
    private InventoryManager inventoryManager;
    private InventoryDataSO equipmentInventory;
    private InventoryDataSO backpackInventory;
    private int baseBackpackCapacity;
    private bool hasCachedBackpackCapacity = false;
    private Coroutine moistureDecayCoroutine;

    public bool CanUseVitality => GetVitality() > 0f;

    protected override void Start()
    {
        base.Start();

        // 初始化生命
        attributesModule.AddAttribute(AttributeType.Hp, baseMaxHealth, 0f, baseMaxHealth);
        healthBar.SetMaxHealth(baseMaxHealth);
        healthBar.SetHealth(baseMaxHealth);
        healthBar.gameObject.SetActive(true);

        // 初始化水分
        attributesModule.AddAttribute(AttributeType.Moisture, baseMaxMoisture, 0f, baseMaxMoisture);
        moistureBar.SetMaxMoisture(baseMaxMoisture);
        moistureBar.SetMoisture(baseMaxMoisture);
        moistureBar.gameObject.SetActive(true);

        // 初始化体力（VIT）
        attributesModule.AddAttribute(AttributeType.VIT, baseMaxVitality, 0f, baseMaxVitality);
        lastVitalityValue = baseMaxVitality;
        vitalityBar.UpdateRadialProgressCircle((int)baseMaxVitality, (int)baseMaxVitality);

        attributesModule.AddAttribute(AttributeType.Atk, baseAttack, 0f, float.MaxValue);
        attributesModule.AddAttribute(AttributeType.Def, baseDefense, 0f, float.MaxValue);

        InitializeInventoryReferences();
        ApplyEquipmentBonuses();
        UpdateDebugStats();

        var playerCtrl = GetComponent<PlayerCtrl>();
        if (playerCtrl != null && vitalityBar != null)
        {
            vitalityBar.InitializeCameraDependencies(playerCtrl.PlayerCamera, transform);
        }
        
        HandleSceneChange(SceneManager.GetActiveScene().name);
    }

    protected override void OnEnable()
    {
        base.OnEnable();
        SubscribeInventoryEvents();
        EventManager.Listen<SceneLoadedMessage>(this, OnSceneLoaded);
    }

    protected override void OnDisable()
    {
        base.OnDisable();
        UnsubscribeInventoryEvents();
        EventManager.Unlisten<SceneLoadedMessage>(this);
    }



    protected override void Update()
    {
        base.Update();

        // 死亡时隐藏UI
        if (isDead)
        {
            healthBar.gameObject.SetActive(false);
            if (vitalityBar != null)
                vitalityBar.gameObject.SetActive(false);
            if (moistureBar != null)
                moistureBar.gameObject.SetActive(false);
            return;
        }

        // 如果体力有变化则更新UI
        float currentVIT = GetVitality();
        if (Mathf.Abs(currentVIT - lastVitalityValue) > 0.01f)
        {
            UpdateVitalityBar(currentVIT);
            lastVitalityValue = currentVIT;
        }

        UpdateDebugStats();
    }

    public override void TakeDamage(float damage)
    {
        base.TakeDamage(damage);
        healthBar.SetHealth(attributesModule.GetAttributeValue(AttributeType.Hp));
    }
    public override void TakeMoisture(float moisture)
    {
        base.TakeMoisture(moisture);
        if (moistureBar != null)
            moistureBar.SetMoisture(attributesModule.GetAttributeValue(AttributeType.Moisture));
    }

    private void OnSceneLoaded(SceneLoadedMessage msg)
    {
        HandleSceneChange(msg.SceneName);
    }

    private void HandleSceneChange(string sceneName)
    {
        if (sceneName == "HomeScene")
        {
            if (moistureDecayCoroutine != null)
            {
                StopCoroutine(moistureDecayCoroutine);
                moistureDecayCoroutine = null;
            }
            RecoverMoistureFull();
        }
        else
        {
            if (moistureDecayCoroutine == null)
            {
                moistureDecayCoroutine = StartCoroutine(MoistureDecayRoutine());
            }
        }
    }

    private void RecoverMoistureFull()
    {
        float maxMoisture = GetAttributeMaxValue(AttributeType.Moisture);
        attributesModule.SetAttributeValue(AttributeType.Moisture, maxMoisture);
        if (moistureBar != null)
            moistureBar.SetMoisture(maxMoisture);
    }

    private IEnumerator MoistureDecayRoutine()
    {
        while (true)
        {
            if (!isDead)
            {
                float decayAmount = (Time.deltaTime / (moistureDecayTimeInMinutes * 60f)) * GetAttributeMaxValue(AttributeType.Moisture);
                TakeMoisture(decayAmount);
            }
            yield return null;
        }
    }

    // ======= 体力系统接口 =======
    // 获取当前体力
    public float GetVitality()
    {
        return attributesModule.GetAttributeValue(AttributeType.VIT);
    }
    // 消耗体力
    public void ConsumeVitality(float amount)
    {
        float current = GetVitality();
        attributesModule.SetAttributeValue(AttributeType.VIT, current - amount);
    }
    // 恢复体力
    public void RecoverVitality(float amount)
    {
        float current = GetVitality();
        float max = GetAttributeMaxValue(AttributeType.VIT);

        if (current < max)
            attributesModule.SetAttributeValue(AttributeType.VIT, current + amount);
    }
    // 更新UI
    private void UpdateVitalityBar(float currentVIT)
    {
        if (vitalityBar != null)
        {
            vitalityBar.UpdateRadialProgressCircle((int)currentVIT, (int)GetAttributeMaxValue(AttributeType.VIT));
        }
    }

    public void SetVitalityBarVisible(bool visible)
    {
        if (vitalityBar != null)
            vitalityBar.gameObject.SetActive(visible);
    }

    private void SubscribeInventoryEvents()
    {
        InitializeInventoryReferences();
        if (inventoryManager != null)
        {
            inventoryManager.OnInventoryChangedEvent -= HandleInventoryChanged;
            inventoryManager.OnInventoryChangedEvent += HandleInventoryChanged;
        }
    }

    private void UnsubscribeInventoryEvents()
    {
        if (inventoryManager != null)
        {
            inventoryManager.OnInventoryChangedEvent -= HandleInventoryChanged;
        }
    }

    private void HandleInventoryChanged(InventoryType changedType)
    {
        if (changedType == InventoryType.Equipment)
        {
            ApplyEquipmentBonuses();
        }
    }

    private void InitializeInventoryReferences()
    {
        if (inventoryManager == null)
        {
            inventoryManager = InventoryManager.Instance;
        }

        if (inventoryManager == null)
            return;

        if (equipmentInventory == null)
        {
            equipmentInventory = inventoryManager.EquipmentData;
        }

        if (backpackInventory == null)
        {
            backpackInventory = inventoryManager.BackpackData;
        }

        if (!hasCachedBackpackCapacity && backpackInventory != null)
        {
            baseBackpackCapacity = backpackInventory.maxCount;
            hasCachedBackpackCapacity = true;
        }
    }

    private void ApplyEquipmentBonuses()
    {
        InitializeInventoryReferences();

        if (equipmentInventory == null || equipmentInventory.items == null)
        {
            ResetEquipmentBonuses();
            return;
        }

        float attackBonus = 0f;
        float defenseBonus = 0f;
        int capacityBonus = 0;
        float moistureBonus = 0f;

        foreach (var stack in equipmentInventory.items)
        {
            if (stack == null || stack.item == null)
                continue;

            int itemCount = stack.count <= 0 ? 1 : stack.count;
            var item = stack.item;

            switch (item.type)
            {
                case ItemType.Weapon:
                    attackBonus += item.attackBonus * itemCount;
                    break;
                case ItemType.Helmets:
                case ItemType.Armor:
                    defenseBonus += item.defenseBonus * itemCount;
                    break;
                case ItemType.backpacks:
                    capacityBonus += item.extraBackpackSlots * itemCount;
                    break;
                case ItemType.necklaces:
                    moistureBonus += item.extraMaxMoisture * itemCount;
                    break;
            }
        }

        attributesModule.SetAttributeValue(AttributeType.Atk, baseAttack + attackBonus);
        attributesModule.SetAttributeValue(AttributeType.Def, baseDefense + defenseBonus);
        UpdateBackpackCapacity(capacityBonus);
        UpdateMoistureCapacity(moistureBonus);
        UpdateDebugStats();
    }

    private void ResetEquipmentBonuses()
    {
        attributesModule.SetAttributeValue(AttributeType.Atk, baseAttack);
        attributesModule.SetAttributeValue(AttributeType.Def, baseDefense);
        UpdateBackpackCapacity(0);
        UpdateMoistureCapacity(0f);
        UpdateDebugStats();
    }

    private void UpdateBackpackCapacity(int bonusSlots)
    {
        if (backpackInventory == null || !hasCachedBackpackCapacity)
            return;

        int targetCapacity = Mathf.Max(0, baseBackpackCapacity + bonusSlots);
        if (backpackInventory.maxCount != targetCapacity)
        {
            backpackInventory.maxCount = targetCapacity;
        }
    }

    private void UpdateMoistureCapacity(float bonus)
    {
        float validBonus = Mathf.Max(0f, bonus);
        float targetMax = baseMaxMoisture + validBonus;

        attributesModule.SetAttributeRange(AttributeType.Moisture, 0f, targetMax);

        float currentValue = Mathf.Min(attributesModule.GetAttributeValue(AttributeType.Moisture), targetMax);
        attributesModule.SetAttributeValue(AttributeType.Moisture, currentValue);

        if (moistureBar != null)
        {
            moistureBar.SetMaxMoisture(targetMax);
            moistureBar.SetMoisture(currentValue);
        }

        UpdateDebugStats();
    }

    private void UpdateDebugStats()
    {
        if (attributesModule == null || attributesModule.attributes == null)
            return;

        currentMaxHealth = GetAttributeMaxValue(AttributeType.Hp);
        currentMaxVitality = GetAttributeMaxValue(AttributeType.VIT);
        currentMaxMoisture = GetAttributeMaxValue(AttributeType.Moisture);
        currentAttack = attributesModule.GetAttributeValue(AttributeType.Atk);
        currentDefense = attributesModule.GetAttributeValue(AttributeType.Def);
    }

    private float GetAttributeMaxValue(AttributeType type)
    {
        if (attributesModule != null && attributesModule.attributes != null &&
            attributesModule.attributes.TryGetValue(type, out var attribute))
        {
            return attribute.MaxValue;
        }

        return 0f;
    }
}
