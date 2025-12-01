using UnityEngine;

public class ExcavationInteractable : BaseInteractable
{
    [Header("宝箱配置")]
    [Tooltip("交互后要生成的宝箱预制体")]
    public GameObject chestPrefab;

    [Tooltip("宝箱生成点，不指定则使用当前物体位置")]
    public Transform spawnPoint;

    private GameObject spawnedChest;

    public override void Interact(Character player)
    {
        if (spawnedChest != null)
        {
            return;
        }

        if (chestPrefab == null)
        {
            Debug.LogWarning($"[ExcavationInteractable] {name} 缺少 chestPrefab，无法生成宝箱。");
            return;
        }

        Vector3 spawnPos = spawnPoint != null ? spawnPoint.position : transform.position;
        Quaternion spawnRot = spawnPoint != null ? spawnPoint.rotation : transform.rotation;

        spawnedChest = Instantiate(chestPrefab, spawnPos, spawnRot);

        Destroy(gameObject);
    }
}

