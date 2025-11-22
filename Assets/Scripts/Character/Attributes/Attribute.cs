using System;
using UnityEngine;
using UnityEngine.Events;

public enum AttributeType
{
    Hp, //生命
    MP, //法力
    VIT,    //体力
    Atk,    //攻击力
    Def,    //防御力
}
public class Attribute
{
    public AttributeType Type { get; private set; }
    public float Value { get; private set; }
    public float MinValue { get; private set; }
    public float MaxValue { get; private set; }
    public AttributesModule Module;
    public Attribute(AttributeType tp, float value, float minValue, float maxValue)
    {
        Type = tp;
        Value = value;
        MinValue = minValue;
        MaxValue = maxValue;
        UpdateValue(value);
    }
    public void UpdateValue(float newValue)
    {
        if (newValue < MinValue)
        {
            SetValue(MinValue);
        }
        else if (newValue > MaxValue)
        {
            SetValue(MaxValue);
        }
        else
        {
            SetValue(newValue);
        }
    }
    void SetValue(float newValue)
    {
        if (Value != newValue)
        {
            Value = newValue;
            //raise event
            //Debug.Log("Attribute Change : " + Type.ToString() + Value.ToString());
        }
    }

    public void SetRange(float minValue, float maxValue)
    {
        MinValue = minValue;
        MaxValue = maxValue;
        UpdateValue(Value);
    }
}

