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
