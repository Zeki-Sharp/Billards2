using UnityEngine;
using System.Collections.Generic;
using System.Linq;
#if UNITY_EDITOR
using Sirenix.OdinInspector;
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
    
    [Tooltip("技能标签列表（使用 characterID，而非角色显示名）")]
#if UNITY_EDITOR
    [ListDrawerSettings(ShowFoldout = true, DraggableItems = false, HideAddButton = false, HideRemoveButton = false)]
    [ValueDropdown("GetAvailableTags", IsUniqueList = true)]
#endif
    public List<string> allowedTags = new List<string>();

    [SerializeField, HideInInspector]
    private string legacyTag = "default";
    
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
    /// 获取可用的技能标签（用于 Odin Dropdown）
    /// 
    /// 注意：由于 Odin Inspector 限制，必须在本类实现，无法引用 EditorHelper
    /// 逻辑与 EditorHelper.GetAllSkillTags() 保持一致
    /// </summary>
    private IEnumerable<ValueDropdownItem<string>> GetAvailableTags()
    {
        var tags = new ValueDropdownList<string>();
        
        // 动态读取角色 characterID
        string[] guids = UnityEditor.AssetDatabase.FindAssets("t:PlayerData");
        foreach (string guid in guids)
        {
            string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
            PlayerData playerData = UnityEditor.AssetDatabase.LoadAssetAtPath<PlayerData>(path);
            
            if (playerData != null && playerData.info != null && !string.IsNullOrEmpty(playerData.info.characterID))
            {
                string displayName = $"{playerData.info.name} ({playerData.info.characterID})";
                tags.Add(displayName, playerData.info.characterID);
            }
        }
        
        return tags;
    }

    [Button("全选角色标签"), GUIColor(0.2f, 0.7f, 1f)]
    private void SelectAllTags()
    {
        allowedTags = GetAvailableTags().Select(item => item.Value).Distinct().ToList();
    }

    [Button("清空标签"), GUIColor(1f, 0.4f, 0.4f)]
    private void ClearAllTags()
    {
        allowedTags.Clear();
    }
#endif
    
    /// <summary>
    /// 获取调试信息
    /// </summary>
    public override string GetDebugInfo()
    {
        return $"[SkillInfo] {GetDisplayName()} ({skillType}, {rarity})";
    }

    private void OnValidate()
    {
#if UNITY_EDITOR
        if (!string.IsNullOrEmpty(legacyTag) && (allowedTags == null || allowedTags.Count == 0))
        {
            allowedTags = new List<string> { legacyTag };
        }
        legacyTag = string.Empty;

        if (allowedTags != null)
        {
            allowedTags = allowedTags.Where(t => !string.IsNullOrEmpty(t)).Distinct().ToList();
        }
#endif
    }

    public IReadOnlyList<string> GetAllowedTags()
    {
        return allowedTags ?? (allowedTags = new List<string>());
    }
}

