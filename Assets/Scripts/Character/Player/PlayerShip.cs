using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 玩家船（继承 Character）
/// </summary>
public class PlayerShip : Character
{
    public HealthBar healthBar;
    public float maxHealth = 100;

    protected override void Start()
    {
        base.Start();

        attributesModule.AddAttribute(AttributeType.Hp, maxHealth, 0, maxHealth);
        healthBar.SetMaxHealth(maxHealth);
        healthBar.gameObject.SetActive(true);
    }

    protected override void Update()
    {
        base.Update();

        if(isDead)
        {
            healthBar.gameObject.SetActive(false);
        }
    }

    public override void TakeDamage(float damage)
    {
        base.TakeDamage(damage);

        healthBar.SetHealth(attributesModule.GetAttributeValue(AttributeType.Hp));
    }
}
