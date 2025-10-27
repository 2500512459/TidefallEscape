using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


[ExecuteAlways]
public partial class Water : MonoBehaviour
{
    private static Water instance = null;
    public static Water Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindObjectOfType<Water>();

                if (instance == null)
                {
                    Debug.Log("Water is not exist!");
                }
            }
            return instance;
        }
    }

    bool valid = true;
    float waterTime = 0;

    // Start is called before the first frame update
    void Awake()
    {
        if (instance != null)
        {
            valid = false;
            return;
        }

        instance = (Water)this;

        waterTime = 0;

        InitLUT();//初始化 LUT 贴图并设置到全局 Shader 变量中
        InitDynamicWaves();//初始化动态波浪系统
        InitGeometry();//初始化LOD网格
    }

    private void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        if (!valid) return;

        waterTime += Time.deltaTime;

        UpdateWaves();//更新全局波浪数据
        UpdateDynamicWaves();//更新动态波浪数据
        UpdateGeometry();//移动网格
    }

    private void OnDestroy()
    {
        DestroyLUT();
    }
}
