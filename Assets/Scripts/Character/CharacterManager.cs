using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 角色激活等级（用于根据与玩家距离控制开销）
/// </summary>
public enum CharacterActiveLevel
{
    /// <summary>
    /// 完全激活：行为树、动画等全部正常运行
    /// </summary>
    Full = 0,

    /// <summary>
    /// 简化激活：后续可以在需要时做简化逻辑，目前与 Full 等价
    /// </summary>
    Simple = 1,

    /// <summary>
    /// 休眠：尽可能不再执行 AI 逻辑，仅保持必要的存在（用于远距离单位）
    /// </summary>
    Sleep = 2,
}

/// <summary>
/// 角色类型过滤器委托
/// 用于在范围检测中按条件筛选角色（例如只检测敌人或玩家）
/// </summary>
public delegate bool CharacterTypeFilter(Character character);

/// <summary>
/// CharacterManager（角色管理器）
/// - 单例类，负责全局管理所有在场景中存在的 Character。
/// - 提供注册、反注册、范围查询、类型筛选等功能。
/// - 额外：根据与玩家的距离，为角色分配激活等级（用于控制 AI/动画 等开销）。
/// </summary>
public class CharacterManager : MonoSingleton<CharacterManager>
{
    // 当前场景中所有已注册的角色列表
    public List<Character> characters = new List<Character>();

    [Header("距离激活配置")]
    [Tooltip("用于作为距离参考的玩家/主角")]
    public Transform playerTransform;

    [Tooltip("完全激活半径")]
    public float fullActiveRadius = 40f;

    [Tooltip("简化激活半径(中距离:Simple),超过此距离则进入 Sleep")]
    public float simpleActiveRadius = 80f;

    private void Update()
    {
        UpdateCharactersActiveLevelByDistance();
    }

    /// <summary>
    /// 按与玩家的距离，为所有已注册角色设置激活等级。
    /// 近 -> Full，中等 -> Simple，远 -> Sleep。
    /// </summary>
    private void UpdateCharactersActiveLevelByDistance()
    {
        if (characters == null || characters.Count == 0) return;

        // 确保有玩家参考点
        if (playerTransform == null)
        {
            TryAutoAssignPlayerTransform();
            if (playerTransform == null) return;
        }

        Vector3 center = playerTransform.position;
        float fullSqr = Mathf.Max(0.01f, fullActiveRadius * fullActiveRadius);
        float simpleSqr = Mathf.Max(fullSqr, simpleActiveRadius * simpleActiveRadius);

        // 遍历所有角色，根据距离设置激活等级
        for (int i = 0; i < characters.Count; i++)
        {
            Character character = characters[i];
            if (character == null) continue;

            // 玩家自身通常不需要被距离逻辑控制，这里可根据项目需要过滤
            if (character.transform == playerTransform) continue;

            Vector3 diff = character.transform.position - center;
            float sqrDist = diff.sqrMagnitude;

            CharacterActiveLevel level;
            if (sqrDist <= fullSqr)
            {
                level = CharacterActiveLevel.Full;
            }
            else if (sqrDist <= simpleSqr)
            {
                level = CharacterActiveLevel.Simple;
            }
            else
            {
                level = CharacterActiveLevel.Sleep;
            }

            character.SetActiveLevel(level);
        }
    }

    /// <summary>
    /// 若未手动指定玩家 Transform，则尝试自动从场景中查找。
    /// 优先 PlayerShipCtrl，再尝试 Player。
    /// </summary>
    private void TryAutoAssignPlayerTransform()
    {
        if (playerTransform != null) return;

        // 优先尝试基于船只的玩家控制脚本
        var shipPlayer = GameObject.FindObjectOfType<PlayerShipCtrl>();
        if (shipPlayer != null)
        {
            playerTransform = shipPlayer.transform;
            return;
        }

        // 其次尝试查找 Player（若有独立 Player 角色类）
        var player = GameObject.FindObjectOfType<Player>();
        if (player != null)
        {
            playerTransform = player.transform;
        }
    }

    /// <summary>
    /// 注册角色实例
    /// 通常在Character.OnEnable时调用
    /// </summary>
    public void Register(Character character)
    {
        if (!characters.Contains(character))
        {
            characters.Add(character);
        }
    }

    /// <summary>
    /// 注销角色实例
    /// 通常在Character.OnDisable时调用
    /// </summary>
    public void Unregister(Character character)
    {
        characters.Remove(character);
    }

    /// <summary>
    /// 获取指定范围内的所有角色
    /// （不带类型过滤）
    /// </summary>
    /// <param name="me">调用者自身（会被忽略）</param>
    /// <param name="position">检测中心点</param>
    /// <param name="range">检测半径</param>
    public List<Character> GetCharactersWithinRange(Character me, Vector3 position, float range)
    {
        List<Character> nearbyCharacters = new List<Character>();
        foreach (Character character in characters)
        {
            // 排除自身
            if (character == me) continue;

            // 判断距离是否在范围内
            if (Vector3.Distance(character.transform.position, position) <= range)
            {
                nearbyCharacters.Add(character);
            }
        }
        return nearbyCharacters;
    }

    /// <summary>
    /// 获取指定范围内的所有角色（支持过滤器）
    /// </summary>
    /// <param name="me">调用者自身</param>
    /// <param name="position">检测中心点</param>
    /// <param name="range">检测半径</param>
    /// <param name="filter">可选：类型过滤器</param>
    public List<Character> GetCharactersWithinRange(Character me, Vector3 position, float range, CharacterTypeFilter filter = null)
    {
        List<Character> nearbyCharacters = new List<Character>();
        foreach (Character character in characters)
        {
            if (character == me) continue;

            if (Vector3.Distance(character.transform.position, position) <= range)
            {
                // 若未传入过滤器或过滤器通过，则加入结果
                if (filter == null || filter(character))
                {
                    nearbyCharacters.Add(character);
                }
            }
        }
        return nearbyCharacters;
    }

    /// <summary>
    /// 获取指定类型的角色列表（泛型约束）
    /// 示例：GetCharactersByType<PlayerCharacter>()
    /// </summary>
    public List<T> GetCharactersByType<T>() where T : Character
    {
        List<T> typedCharacters = new List<T>();
        foreach (var character in characters)
        {
            if (character is T)
            {
                typedCharacters.Add((T)character);
            }
        }
        return typedCharacters;
    }
}
