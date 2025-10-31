using UnityEngine;

/// <summary>
/// Info 抽象基类 - 显示信息的统一接口
/// 
/// 【设计理念】：
/// - 分离显示信息和核心数据
/// - 支持动态显示内容
/// - 为多语言系统打基础
/// - UI 系统使用统一接口
/// 
/// 【参考 GC2】：
/// - 类似 GC2 的 TInfo 系统
/// - 提供 Name/Description/Icon 等通用属性
/// 
/// 【应用场景】：
/// - 技能显示信息（SkillInfo）
/// - 敌人显示信息（EnemyInfo）
/// - 玩家显示信息（PlayerInfo）
/// </summary>
[System.Serializable]
public abstract class TInfo
{
    [Header("基础显示信息")]
    [Tooltip("名称")]
    public string name = "";
    
    [Tooltip("缩写（用于紧凑显示）")]
    public string acronym = "";
    
    [Tooltip("描述")]
    [TextArea(2, 5)]
    public string description = "";
    
    [Header("视觉信息")]
    [Tooltip("图标")]
    public Sprite icon;
    
    [Tooltip("标识颜色")]
    public Color color = Color.white;
    
    /// <summary>
    /// 获取显示名称（优先使用 name，如果为空则使用 acronym）
    /// </summary>
    public string GetDisplayName()
    {
        return !string.IsNullOrEmpty(name) ? name : acronym;
    }
    
    /// <summary>
    /// 获取短名称（优先使用 acronym，如果为空则使用 name）
    /// </summary>
    public string GetShortName()
    {
        return !string.IsNullOrEmpty(acronym) ? acronym : name;
    }
    
    /// <summary>
    /// 是否有图标
    /// </summary>
    public bool HasIcon => icon != null;
    
    /// <summary>
    /// 获取调试信息
    /// </summary>
    public virtual string GetDebugInfo()
    {
        return $"[{GetType().Name}] {GetDisplayName()}";
    }
}

