using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// ��ɫ���͹�����ί��
/// �����ڷ�Χ����а�����ɸѡ��ɫ������ֻ�����˻���ң�
/// </summary>
public delegate bool CharacterTypeFilter(Character character);

/// <summary>
/// CharacterManager����ɫ��������
/// - �����࣬����ȫ�ֹ��������ڳ����д��ڵ� Character��
/// - �ṩע�ᡢ��ע�ᡢ��Χ��ѯ������ɸѡ�ȹ��ܡ�
/// </summary>
public class CharacterManager : MonoSingleton<CharacterManager>
{
    // ��ǰ������������ע��Ľ�ɫ�б�
    public List<Character> characters = new List<Character>();

    void Start()
    {
    }

    void Update()
    {
    }

    /// <summary>
    /// ע���ɫʵ��
    /// ͨ����Character.OnEnableʱ����
    /// </summary>
    public void Register(Character character)
    {
        if (!characters.Contains(character))
        {
            characters.Add(character);
        }
    }

    /// <summary>
    /// ע����ɫʵ��
    /// ͨ����Character.OnDisableʱ����
    /// </summary>
    public void Unregister(Character character)
    {
        characters.Remove(character);
    }

    /// <summary>
    /// ��ȡָ����Χ�ڵ����н�ɫ
    /// ���������͹��ˣ�
    /// </summary>
    /// <param name="me">�����������ᱻ���ԣ�</param>
    /// <param name="position">������ĵ�</param>
    /// <param name="range">���뾶</param>
    public List<Character> GetCharactersWithinRange(Character me, Vector3 position, float range)
    {
        List<Character> nearbyCharacters = new List<Character>();
        foreach (Character character in characters)
        {
            // �ų�����
            if (character == me) continue;

            // �жϾ����Ƿ��ڷ�Χ��
            if (Vector3.Distance(character.transform.position, position) <= range)
            {
                nearbyCharacters.Add(character);
            }
        }
        return nearbyCharacters;
    }

    /// <summary>
    /// ��ȡָ����Χ�ڵ����н�ɫ��֧�ֹ�������
    /// </summary>
    /// <param name="me">����������</param>
    /// <param name="position">������ĵ�</param>
    /// <param name="range">���뾶</param>
    /// <param name="filter">��ѡ�����͹�����</param>
    public List<Character> GetCharactersWithinRange(Character me, Vector3 position, float range, CharacterTypeFilter filter = null)
    {
        List<Character> nearbyCharacters = new List<Character>();
        foreach (Character character in characters)
        {
            if (character == me) continue;

            if (Vector3.Distance(character.transform.position, position) <= range)
            {
                // ��δ����������������ͨ�����������
                if (filter == null || filter(character))
                {
                    nearbyCharacters.Add(character);
                }
            }
        }
        return nearbyCharacters;
    }

    /// <summary>
    /// ��ȡָ�����͵Ľ�ɫ�б�����Լ����
    /// ʾ����GetCharactersByType<PlayerCharacter>()
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
