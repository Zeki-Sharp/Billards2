using UnityEngine;
#if UNITY_EDITOR
using Sirenix.OdinInspector;
using System.Collections.Generic;
#endif

/// <summary>
/// 技能显示信息
/// 
/// 【包含内容】：
/// - 技能名称
/// - 技能图标
/// - 技能描述
/// - 技能标识颜色
/// 
/// 【用途】：
/// - UI 显示
/// - 技能选择界面
/// - 技能提示信息
/// </summary>
[System.Serializable]
public class SkillInfo : TInfo
{
    [Header("技能特有信息")]
    [Tooltip("技能类型/分类")]
    public string skillType = "";
    
    [Tooltip("技能稀有度")]
#if UNITY_EDITOR
    [ValueDropdown("GetAvailableRarities")]
#endif
    public string rarity = "普通";
    
    [Tooltip("技能标签（如 default, common, 角色专属等）")]
#if UNITY_EDITOR
    [ValueDropdown("GetAvailableTags")]
#endif
    public string tag = "default";
    
#if UNITY_EDITOR
    /// <summary>
    /// 获取可用的稀有度选项
    /// </summary>
    private IEnumerable<string> GetAvailableRarities()
    {
        return new List<string> 
        { 
            "普通", 
            "稀有", 
            "史诗", 
            "传说" 
        };
    }
    
    /// <summary>
    /// 获取可用的标签选项
    /// </summary>
    private IEnumerable<string> GetAvailableTags()
    {
        var tags = new List<string>();
        
        // 添加固定标签
        tags.Add("default");
        tags.Add("common");
        
        // 查找所有角色数据，添加角色专属标签
        string[] characterDataGuids = UnityEditor.AssetDatabase.FindAssets("t:CharacterSelectionData");
        if (characterDataGuids.Length > 0)
        {
            string path = UnityEditor.AssetDatabase.GUIDToAssetPath(characterDataGuids[0]);
            var characterData = UnityEditor.AssetDatabase.LoadAssetAtPath<CharacterSelectionData>(path);
            
            if (characterData != null && characterData.availableCharacters != null)
            {
                foreach (var character in characterData.availableCharacters)
                {
                    if (character != null && !string.IsNullOrEmpty(character.playerName))
                    {
                        string characterTag = character.playerName;
                        if (!tags.Contains(characterTag))
                        {
                            tags.Add(characterTag);
                        }
                    }
                }
            }
        }
        
        return tags;
    }
#endif
    
    /// <summary>
    /// 获取调试信息
    /// </summary>
    public override string GetDebugInfo()
    {
        return $"[SkillInfo] {GetDisplayName()} ({skillType}, {rarity})";
    }
}

