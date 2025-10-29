using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 角色类型过滤器委托
/// 用于在范围检测中按条件筛选角色（例如只检测敌人或玩家）
/// </summary>
public delegate bool CharacterTypeFilter(Character character);

/// <summary>
/// CharacterManager（角色管理器）
/// - 单例类，负责全局管理所有在场景中存在的 Character。
/// - 提供注册、反注册、范围查询、类型筛选等功能。
/// </summary>
public class CharacterManager : MonoSingleton<CharacterManager>
{
    // 当前场景中所有已注册的角色列表
    public List<Character> characters = new List<Character>();

    void Start()
    {
    }

    void Update()
    {
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
