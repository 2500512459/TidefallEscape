using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player : Character
{
    [Header("UI 组件")]
    public HealthBar healthBar;
    public VitalityBar vitalityBar;
    public ManaPointsBar manaPointsBar;

    [Header("角色属性")]
    public float maxHealth = 100f;
    public float maxVitality = 100f;
    public float maxManaPoints = 100f;
    public float vitalityRecoveryRate = 8f;  // 每秒恢复
    private float lastVitalityValue;          // 上一帧体力值

    public bool CanUseVitality => GetVitality() > 0f;

    protected override void Start()
    {
        base.Start();

        // 初始化生命
        attributesModule.AddAttribute(AttributeType.Hp, maxHealth, 0f, maxHealth);
        healthBar.SetMaxHealth(maxHealth);
        healthBar.SetHealth(maxHealth);
        healthBar.gameObject.SetActive(true);

        // 初始化法力
        attributesModule.AddAttribute(AttributeType.MP, maxManaPoints, 0f, maxManaPoints);
        manaPointsBar.SetMaxManaPoints(maxManaPoints);
        manaPointsBar.SetManaPoints(maxManaPoints);
        manaPointsBar.gameObject.SetActive(true);

        // 初始化体力（VIT）
        attributesModule.AddAttribute(AttributeType.VIT, maxVitality, 0f, maxVitality);
        lastVitalityValue = maxVitality;
        vitalityBar.UpdateRadialProgressCircle((int)maxVitality, (int)maxVitality);

        var playerCtrl = GetComponent<PlayerCtrl>();
        if (playerCtrl != null && vitalityBar != null)
        {
            vitalityBar.InitializeCameraDependencies(playerCtrl.PlayerCamera, transform);
        }
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
        float max = maxVitality;

        if (current < max)
            attributesModule.SetAttributeValue(AttributeType.VIT, current + amount);
    }
    // 更新UI
    private void UpdateVitalityBar(float currentVIT)
    {
        if (vitalityBar != null)
        {
            vitalityBar.UpdateRadialProgressCircle((int)currentVIT, (int)maxVitality);
        }
    }
}
