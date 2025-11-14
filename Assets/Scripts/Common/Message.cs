using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#region 场景加载相关消息

public class NewGameMessage
{
    public string SceneName { get; }
    public NewGameMessage(string sceneName)
    {
        SceneName = sceneName;
    }
}
#endregion


public class DamageMessage
{
    public float Damage { get; }
    public Character Target;
    public DamageMessage(float damage, Character am)
    {
        Damage = damage;
        Target = am;
    }
}
public class CharacterDeathMessage
{
    public Character DeadCharacter { get; }

    public CharacterDeathMessage(Character deadCharacter)
    {
        DeadCharacter = deadCharacter;
    }
}
public class AttributeChangeMessage
{
    public AttributeType AT { get; }
    public AttributesModule Target;
    public AttributeChangeMessage(AttributeType at, AttributesModule am)
    {
        AT = at;
        Target = am;
    }
}

public class CurrencyAmountChangeMessage
{
    public int GoldCoinAmount { get; }
    public int GemstoneAmount { get; }

    public CurrencyAmountChangeMessage(int goldCoinAmount, int gemstoneAmount)
    {
        GoldCoinAmount = goldCoinAmount;
        GemstoneAmount = gemstoneAmount;
    }
}