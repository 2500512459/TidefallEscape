using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteAlways]
public class DissolutionCenter : MonoBehaviour
{
    // 用于设置溶解中心
    public Transform target;

    public Material material;

    void Update()
    {
        if (target && material)
        {
            material.SetVector("_Center", target.position);
        }
    }

}
