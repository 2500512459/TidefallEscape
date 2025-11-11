using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class AttributesModule : MonoBehaviour
{
    public Dictionary<AttributeType, Attribute> attributes = new Dictionary<AttributeType, Attribute>();
    public void AddAttribute(AttributeType tp, float value, float minValue, float maxValue)
    {
        if (attributes.ContainsKey(tp))
        {
            Debug.LogWarning("Attribute already exists: " + tp.ToString());
            return;
        }
        var attribute = new Attribute(tp, value, minValue, maxValue);
        attribute.Module = this;
        attributes.Add(tp, attribute);
        Debug.Log("Attribute added: " + tp.ToString());
    }
    public float GetAttributeValue(AttributeType tp)
    {
        if (attributes.TryGetValue(tp, out Attribute attribute))
        {
            return attribute.Value;
        }
        Debug.LogError("Attribute not found: " + tp.ToString());
        return 0f;
    }
    public void SetAttributeValue(AttributeType tp, float value)
    {
        if (attributes.TryGetValue(tp, out Attribute attribute))
        {
            attribute.UpdateValue(value);
        }
        else
        {
            Debug.LogError("Attribute not found: " + tp.ToString());
        }
    }
}

