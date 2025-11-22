using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player : Character
{
    [Header("UI 组件")]
    public HealthBar healthBar;
    public VitalityBar vitalityBar;
    public ManaPointsBar manaPointsBar;

    [Header("角色属性（基础值）")]
    [SerializeField] private float baseMaxHealth = 100f;
    [SerializeField] private float baseMaxVitality = 100f;
    [SerializeField] private float baseMaxManaPoints = 100f;
    [SerializeField] private float baseAttack = 10f;
    [SerializeField] private float baseDefense = 5f;
    [Header("实时属性（调试）")]
    [SerializeField] private float currentMaxHealth;
    [SerializeField] private float currentMaxVitality;
    [SerializeField] private float currentMaxManaPoints;
    [SerializeField] private float currentAttack;
    [SerializeField] private float currentDefense;
    public float vitalityRecoveryRate = 8f;  // 每秒恢复
    private float lastVitalityValue;          // 上一帧体力值
    private InventoryManager inventoryManager;
    private InventoryDataSO equipmentInventory;
    private InventoryDataSO backpackInventory;
    private int baseBackpackCapacity;
    private bool hasCachedBackpackCapacity = false;

    public bool CanUseVitality => GetVitality() > 0f;

    protected override void Start()
    {
        base.Start();

        // 初始化生命
        attributesModule.AddAttribute(AttributeType.Hp, baseMaxHealth, 0f, baseMaxHealth);
        healthBar.SetMaxHealth(baseMaxHealth);
        healthBar.SetHealth(baseMaxHealth);
        healthBar.gameObject.SetActive(true);

        // 初始化法力
        attributesModule.AddAttribute(AttributeType.MP, baseMaxManaPoints, 0f, baseMaxManaPoints);
        manaPointsBar.SetMaxManaPoints(baseMaxManaPoints);
        manaPointsBar.SetManaPoints(baseMaxManaPoints);
        manaPointsBar.gameObject.SetActive(true);

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
    }

    protected override void OnEnable()
    {
        base.OnEnable();
        SubscribeInventoryEvents();
    }

    protected override void OnDisable()
    {
        base.OnDisable();
        UnsubscribeInventoryEvents();
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
    public override void TakeManaPoints(float manaPoints)
    {
        base.TakeManaPoints(manaPoints);
        manaPointsBar.SetManaPoints(attributesModule.GetAttributeValue(AttributeType.MP));
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
        float manaBonus = 0f;

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
                    manaBonus += item.extraMaxManaPoints * itemCount;
                    break;
            }
        }

        attributesModule.SetAttributeValue(AttributeType.Atk, baseAttack + attackBonus);
        attributesModule.SetAttributeValue(AttributeType.Def, baseDefense + defenseBonus);
        UpdateBackpackCapacity(capacityBonus);
        UpdateManaCapacity(manaBonus);
        UpdateDebugStats();
    }

    private void ResetEquipmentBonuses()
    {
        attributesModule.SetAttributeValue(AttributeType.Atk, baseAttack);
        attributesModule.SetAttributeValue(AttributeType.Def, baseDefense);
        UpdateBackpackCapacity(0);
        UpdateManaCapacity(0f);
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

    private void UpdateManaCapacity(float bonus)
    {
        float validBonus = Mathf.Max(0f, bonus);
        float targetMax = baseMaxManaPoints + validBonus;

        attributesModule.SetAttributeRange(AttributeType.MP, 0f, targetMax);

        float currentValue = Mathf.Min(attributesModule.GetAttributeValue(AttributeType.MP), targetMax);
        attributesModule.SetAttributeValue(AttributeType.MP, currentValue);

        if (manaPointsBar != null)
        {
            manaPointsBar.SetMaxManaPoints(targetMax);
            manaPointsBar.SetManaPoints(currentValue);
        }

        UpdateDebugStats();
    }

    private void UpdateDebugStats()
    {
        if (attributesModule == null || attributesModule.attributes == null)
            return;

        currentMaxHealth = GetAttributeMaxValue(AttributeType.Hp);
        currentMaxVitality = GetAttributeMaxValue(AttributeType.VIT);
        currentMaxManaPoints = GetAttributeMaxValue(AttributeType.MP);
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
