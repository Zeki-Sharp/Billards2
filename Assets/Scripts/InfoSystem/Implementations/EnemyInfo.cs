using UnityEngine;
#if UNITY_EDITOR
using Sirenix.OdinInspector;
using System.Collections.Generic;
#endif

/// <summary>
/// 敌人显示信息
/// 
/// 【包含内容】：
/// - 敌人名称
/// - 敌人图标
/// - 敌人描述
/// - 敌人标识颜色
/// 
/// 【用途】：
/// - UI 显示
/// - 敌人图鉴
/// - 战斗信息展示
/// </summary>
[System.Serializable]
public class EnemyInfo : TInfo
{
    [Header("敌人特有信息")]
    [Tooltip("敌人类型/种族")]
#if UNITY_EDITOR
    [ValueDropdown("GetAvailableEnemyTypes")]
#endif
    public string enemyType = "";
    
    [Tooltip("威胁等级")]
#if UNITY_EDITOR
    [ValueDropdown("GetAvailableThreatLevels")]
#endif
    public string threatLevel = "低";
    
#if UNITY_EDITOR
    /// <summary>
    /// 获取可用的敌人类型选项
    /// </summary>
    private IEnumerable<string> GetAvailableEnemyTypes()
    {
        return new List<string> 
        { 
            "普通怪物",
            "精英怪物",
            "Boss",
            "小怪",
            "哥布林",
            "骷髅",
            "陷阱"
        };
    }
    
    /// <summary>
    /// 获取可用的威胁等级选项
    /// </summary>
    private IEnumerable<string> GetAvailableThreatLevels()
    {
        return new List<string> 
        { 
            "低", 
            "中", 
            "高", 
            "极高" 
        };
    }
#endif
    
    /// <summary>
    /// 是否为 Boss（通过 enemyType 判断）
    /// </summary>
    public bool IsBoss => enemyType == "Boss";
    
    /// <summary>
    /// 获取调试信息
    /// </summary>
    public override string GetDebugInfo()
    {
        string bossTag = IsBoss ? " [BOSS]" : "";
        return $"[EnemyInfo] {GetDisplayName()} ({enemyType}){bossTag}";
    }
}

