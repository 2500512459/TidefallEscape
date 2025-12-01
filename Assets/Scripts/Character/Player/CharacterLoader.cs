using UnityEngine;
using UnityEngine.Animations.Rigging;

public class CharacterLoader : MonoBehaviour
{
    private bool hasLoaded = false; // 标记是否已经加载

    void OnEnable()
    {
        if(hasLoaded) return;
        LoadCharacter();
    }

    void LoadCharacter()
    {
        hasLoaded = true;
        // 1. 获取当前职业
        if (PlayerDataManager.Instance == null)
        {
            Debug.LogError("PlayerDataManager Instance not found!");
            return;
        }

        ProfessionType currentProfession = PlayerDataManager.Instance.GetSelectedProfession();
        Debug.Log($"正在加载职业角色: {currentProfession}");

        // 2. 获取对应数据
        PlayerDataManager.ProfessionData data = PlayerDataManager.Instance.GetProfessionData(currentProfession);

        // 检查是否获取到了有效数据（假设Prefab不为空即为有效）
        if (data.characterPrefab == null)
        {
            Debug.LogError($"未找到职业 {currentProfession} 的模型预制体配置，或配置为空！");
            return;
        }

        // 3. 实例化模型
        // 保持与Player Transform一致
        GameObject modelInstance = Instantiate(data.characterPrefab, transform);
        modelInstance.transform.localPosition = Vector3.zero;
        modelInstance.transform.localRotation = Quaternion.identity;
        modelInstance.name = $"{currentProfession}_Model";

        // 4. 挂载 WeaponHandler
        WeaponHandler weaponHandler = modelInstance.AddComponent<WeaponHandler>();

        // 5. 查找挂点 (递归查找)
        Transform hipSocket = FindDeepChild(modelInstance.transform, data.hipSocketName);
        Transform handSocket = FindDeepChild(modelInstance.transform, data.handSocketName);

        if (hipSocket == null) Debug.LogWarning($"无法在模型中找到腰部挂点: {data.hipSocketName}");
        if (handSocket == null) Debug.LogWarning($"无法在模型中找到手部挂点: {data.handSocketName}");

        // 6. 实例化特效 (如果是Prefab需要实例化)
        ParticleSystem p1 = InstantiateEffect(data.effect1, modelInstance.transform);
        ParticleSystem p2 = InstantiateEffect(data.effect2, modelInstance.transform);
        ParticleSystem p3 = InstantiateEffect(data.effect3, modelInstance.transform);


        // 7. 初始化 WeaponHandler
        var attackCheck = GetComponentInChildren<PlayerAttackCheck>();
        if (attackCheck == null)
        {
             Debug.LogError("CharacterLoader: PlayerAttackCheck component not found on Player!");
        }
        weaponHandler.Initialize(data.weaponPrefab, hipSocket, handSocket, p1, p2, p3, attackCheck);

        // 8. 设置Lookout职业的Rig (如果在Lookout职业且存在Rig配置)
        if (currentProfession == ProfessionType.Lookout)
        {
            SetupLookoutRig(modelInstance);
        }
        
        // 9. 初始化状态机 (确保在模型加载后执行)
        var stateMachine = GetComponent<PlayerStateMachine>();
        if (stateMachine != null)
        {
            stateMachine.Initialize();
        }
        else
        {
            Debug.LogError("CharacterLoader: PlayerStateMachine component not found on Player!");
        }

        Debug.Log($"角色加载完成: {currentProfession}");
    }

    private ParticleSystem InstantiateEffect(ParticleSystem prefab, Transform parent)
    {
        if (prefab == null) return null;
        ParticleSystem instance = Instantiate(prefab, parent);
        // 使用特效预制体自身的Transform设置
        instance.transform.localPosition = prefab.transform.localPosition;
        instance.transform.localRotation = prefab.transform.localRotation;
        instance.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear); // 初始停止
        return instance;
    }

    private void SetupLookoutRig(GameObject modelInstance)
    {
        var tpsController = GetComponent<ThirdPersonShooterController>();
        if (tpsController == null) return;

        // 查找名为 "Rig 1" 的子物体
        Transform rigTrans = FindDeepChild(modelInstance.transform, "Rig 1");
        Transform bodyAimTrans = FindDeepChild(rigTrans, "BodyAim");
        Transform handleAimTrans = FindDeepChild(rigTrans, "HandleAim");
        Transform handAimTrans = FindDeepChild(rigTrans, "HandAim");
        if (rigTrans != null && bodyAimTrans != null && handleAimTrans != null && handAimTrans != null)
        {
            Rig rig = rigTrans.GetComponent<Rig>();
            MultiAimConstraint bodyAimRig = bodyAimTrans.GetComponent<MultiAimConstraint>();
            MultiAimConstraint handleAimRig = handleAimTrans.GetComponent<MultiAimConstraint>();
            TwoBoneIKConstraint handAimRig = handAimTrans.GetComponent<TwoBoneIKConstraint>();
            if (rig != null)
            {
                // 1. 设置初始权重为0
                rig.weight = 0f;
                
                // 2. 传递Rig给Controller
                tpsController.SetAimRig(rig, bodyAimRig, handleAimRig, handAimRig);

                // 3. 设置BodyAim和Aim的目标
                Transform target = tpsController.AimTarget;
                SetupConstraintTarget(rigTrans, "BodyAim", target);
                SetupConstraintTarget(rigTrans, "HandleAim", target);
            }
        }

        // 4.重建 RigBuilder
        RigBuilder rigBuilder = modelInstance.GetComponent<RigBuilder>();
        if (rigBuilder != null)
        {
            rigBuilder.Build();
        }
    }

    private void SetupConstraintTarget(Transform parent, string name, Transform target)
    {
        Transform t = FindDeepChild(parent, name);
        if (t != null)
        {
            var constraint = t.GetComponent<MultiAimConstraint>();
            if (constraint != null)
            {
                var data = constraint.data;
                data.sourceObjects = new WeightedTransformArray { new WeightedTransform(target, 1f) };
                constraint.data = data;
            }
        }
    }

    // 递归查找子物体辅助方法
    private Transform FindDeepChild(Transform parent, string name)
    {
        if (parent.name == name) return parent;
        
        foreach (Transform child in parent)
        {
            Transform result = FindDeepChild(child, name);
            if (result != null) return result;
        }
        return null;
    }
}
