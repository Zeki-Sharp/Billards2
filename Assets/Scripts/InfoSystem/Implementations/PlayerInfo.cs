using UnityEngine;
#if UNITY_EDITOR
using Sirenix.OdinInspector;
using System.Collections.Generic;
#endif

/// <summary>
/// 玩家显示信息
/// 
/// 【包含内容】：
/// - 角色名称
/// - 角色图标
/// - 角色描述
/// - 角色标识颜色
/// 
/// 【用途】：
/// - UI 显示
/// - 角色选择界面
/// - 游戏内信息展示
/// </summary>
[System.Serializable]
public class PlayerInfo : TInfo
{
    [Header("玩家特有信息")]
    [Tooltip("角色职业/类型")]
#if UNITY_EDITOR
    [ValueDropdown("GetAvailableClasses")]
#endif
    public string characterClass = "";
    
    [Tooltip("角色稀有度")]
#if UNITY_EDITOR
    [ValueDropdown("GetAvailableRarities")]
#endif
    public string rarity = "普通";
    
#if UNITY_EDITOR
    /// <summary>
    /// 获取可用的职业选项
    /// </summary>
    private IEnumerable<string> GetAvailableClasses()
    {
        return new List<string> 
        { 
            "战士", 
            "法师", 
            "射手", 
            "刺客",
            "坦克",
            "辅助"
        };
    }
    
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
#endif
    
    /// <summary>
    /// 获取调试信息
    /// </summary>
    public override string GetDebugInfo()
    {
        return $"[PlayerInfo] {GetDisplayName()} ({characterClass})";
    }
}

