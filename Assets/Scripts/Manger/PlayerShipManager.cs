using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 玩家船只管理器：管理不同类型的船只模型预制体和实例化
/// </summary>
[DefaultExecutionOrder(-99)]
public class PlayerShipManager : MonoBehaviour
{
    public static PlayerShipManager Instance { get; private set; }

    [System.Serializable]
    public struct ShipData
    {
        public ShipType shipType;
        [Header("船只模型")]
        public GameObject shipModelPrefab; // 船只模型预制体
    }

    [Header("船只配置列表")]
    [SerializeField] private List<ShipData> shipDataList = new List<ShipData>();
    
    public Transform PlayerShip;
    public Transform PlayerShipModelParent;
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this.gameObject);
            return;
        }

        Instance = this;

        PlayerShip = PlayerShipModelParent.parent;
    }

    /// <summary>
    /// 在 PlayerShip 对象下实例化船只模型
    /// </summary>
    /// <param name="shipType">船只类型</param>
    /// <param name="playerShip">玩家船只对象</param>
    /// <returns>实例化后的船只模型对象，如果失败返回null</returns>
    [ContextMenu("InstantiateShipModel")]
    public GameObject InstantiateShipModel()
    {
        ShipType shipType = PlayerDataManager.Instance.CurrentShipType;
        // 获取船只配置数据
        ShipData shipData = shipDataList.Find(x => x.shipType == shipType);
        if (shipData.shipModelPrefab == null || shipType == ShipType.None)
        {
            return null;
        }

        // 清理旧的船只模型（只清理挂点下的模型，不影响其他子对象）
        ClearOldShipModels();

        // 实例化新的船只模型
        GameObject shipModel = Instantiate(shipData.shipModelPrefab, PlayerShipModelParent);
        if (shipModel != null)
        {
            PlayerShip.gameObject.SetActive(true);
            Debug.Log($"已实例化船只模型: {shipType}");
        }
        return shipModel;
    }

    /// <summary>
    /// 清理旧的船只模型
    /// </summary>
    private void ClearOldShipModels()
    {
        // 清理挂点下的所有子对象（这些应该是之前实例化的船只模型）
        for (int i = PlayerShipModelParent.childCount - 1; i >= 0; i--)
        {
            Transform child = PlayerShipModelParent.GetChild(i);
            Destroy(child.gameObject);
        }
    }
}

