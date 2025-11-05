using UnityEngine;
using Sirenix.OdinInspector;
using System.Collections.Generic;

/// <summary>
/// 伤害触发器配置 - 监听伤害事件，根据伤害类型触发技能
/// 
/// 【使用场景】：
/// - 只在特定类型的伤害时触发（如范围攻击触发点燃）
/// - 只在碰撞伤害时触发
/// - 过滤目标标签
/// 
/// 【配置示例】：
/// - triggerTypes = [Stopped] → 只在停止攻击（范围攻击）时触发
/// - triggerTypes = [Collision] → 只在碰撞伤害时触发
/// </summary>
[System.Serializable]
public class DamageTriggerConfig : TriggerBase
{
    /// <summary>
    /// 获取可用的伤害类型列表
    /// </summary>
    private static IEnumerable<ValueDropdownItem<DamageTriggerType>> GetAvailableDamageTypes()
    {
        return new ValueDropdownList<DamageTriggerType>
        {
            { "碰撞伤害 (Collision)", DamageTriggerType.Collision },
            { "停止攻击 (Stopped)", DamageTriggerType.Stopped },
            { "间隔伤害 (Interval)", DamageTriggerType.Interval },
            { "技能伤害 (Skill)", DamageTriggerType.Skill }
        };
    }
    
    /// <summary>
    /// 获取可用的 Tag 列表
    /// </summary>
    private static IEnumerable<ValueDropdownItem<string>> GetAvailableTags()
    {
        return new ValueDropdownList<string>
        {
            { "玩家 (Player)", "Player" },
            { "敌人 (Enemy)", "Enemy" },
            { "墙壁 (Wall)", "Wall" },
            { "洞 (Hole)", "Hole" },
            { "范围 (Range)", "Range" },
            { "陷阱 (Trap)", "Trap" },
            { "物品 (Item)", "Item" }
        };
    }
    
    [LabelText("触发的伤害类型")]
    [Tooltip("选择哪些伤害类型会触发技能（可多选）")]
    [ValueDropdown("GetAvailableDamageTypes")]
    [ListDrawerSettings(ShowIndexLabels = false, DraggableItems = false)]
    public DamageTriggerType[] triggerTypes = new DamageTriggerType[] { DamageTriggerType.Stopped };
    
    [LabelText("目标标签")]
    [Tooltip("目标必须具有的标签（留空表示不限制）")]
    [ValueDropdown("GetAvailableTags")]
    public string targetTag = "Enemy";
    
    [LabelText("显示调试日志")]
    [Tooltip("是否在Console中显示触发日志")]
    public bool showDebugLog = false;
    
    /// <summary>
    /// 创建触发器实例
    /// </summary>
    public override ITrigger CreateTrigger()
    {
        var trigger = new DamageTrigger
        {
            triggerTypes = this.triggerTypes,
            targetTag = this.targetTag,
            showDebugLog = this.showDebugLog
        };
        return trigger;
    }
    
    /// <summary>
    /// 获取调试信息
    /// </summary>
    public override string GetDebugInfo()
    {
        string types = string.Join(", ", triggerTypes);
        return $"DamageTrigger [类型: {types}, 目标: {targetTag}]";
    }
}

