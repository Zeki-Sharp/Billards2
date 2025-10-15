using UnityEngine;
using Sirenix.OdinInspector;
using System.Collections.Generic;

/// <summary>
/// 角色选择数据配置 - 存储所有可选角色的列表
/// </summary>
[CreateAssetMenu(fileName = "CharacterSelectionData", menuName = "Game/Character Selection/Character Selection Data")]
public class CharacterSelectionData : ScriptableObject
{
    [Header("角色列表")]
    [LabelText("可选角色")]
    [Tooltip("所有可选角色的 PlayerData 列表")]
    [InfoBox("每个 PlayerData 代表一个可选角色，列表顺序决定了在选人界面中的显示顺序")]
    public List<PlayerData> availableCharacters = new List<PlayerData>();
    
    [Header("界面设置")]
    [LabelText("按钮预制体")]
    [Tooltip("角色选择按钮的预制体")]
    [Required]
    public GameObject characterButtonPrefab;
    
    [LabelText("按钮容器")]
    [Tooltip("角色按钮的父容器")]
    [Required]
    public Transform buttonContainer;
    
    [Button("验证配置")]
    [GUIColor(0.4f, 0.8f, 1f)]
    void ValidateConfiguration()
    {
        bool isValid = true;
        
        // 检查角色列表
        if (availableCharacters == null || availableCharacters.Count == 0)
        {
            Debug.LogError("CharacterSelectionData: 角色列表为空！");
            isValid = false;
        }
        else
        {
            for (int i = 0; i < availableCharacters.Count; i++)
            {
                if (availableCharacters[i] == null)
                {
                    Debug.LogError($"CharacterSelectionData: 角色列表第 {i} 项为空！");
                    isValid = false;
                }
                else if (string.IsNullOrEmpty(availableCharacters[i].playerName))
                {
                    Debug.LogError($"CharacterSelectionData: 角色 {availableCharacters[i].name} 的名称为空！");
                    isValid = false;
                }
            }
        }
        
        // 检查按钮预制体
        if (characterButtonPrefab == null)
        {
            Debug.LogError("CharacterSelectionData: 按钮预制体未配置！");
            isValid = false;
        }
        
        // 检查按钮容器
        if (buttonContainer == null)
        {
            Debug.LogError("CharacterSelectionData: 按钮容器未配置！");
            isValid = false;
        }
        
        if (isValid)
        {
            Debug.Log($"CharacterSelectionData: 配置验证通过！共 {availableCharacters.Count} 个角色");
        }
    }
    
    /// <summary>
    /// 获取所有可用角色
    /// </summary>
    public List<PlayerData> GetAvailableCharacters()
    {
        return availableCharacters;
    }
    
    /// <summary>
    /// 获取角色数量
    /// </summary>
    public int GetCharacterCount()
    {
        return availableCharacters?.Count ?? 0;
    }
    
    /// <summary>
    /// 检查配置是否有效
    /// </summary>
    public bool IsValid()
    {
        return availableCharacters != null && 
               availableCharacters.Count > 0 && 
               characterButtonPrefab != null && 
               buttonContainer != null &&
               !availableCharacters.Exists(data => data == null || string.IsNullOrEmpty(data.playerName));
    }
    
    /// <summary>
    /// 获取调试信息
    /// </summary>
    public string GetDebugInfo()
    {
        if (!IsValid())
        {
            return "配置无效";
        }
        
        string info = $"角色数量: {availableCharacters.Count}\n";
        for (int i = 0; i < availableCharacters.Count; i++)
        {
            var character = availableCharacters[i];
            info += $"{i + 1}. {character.playerName} ({character.attackMode})\n";
        }
        
        return info;
    }
}
