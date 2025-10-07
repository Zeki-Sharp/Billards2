using UnityEngine;
using Sirenix.OdinInspector;

/// <summary>
/// 触发器配置 - 用于配置技能触发器的参数
/// 简化设计：选择类型后直接显示对应参数
/// </summary>
[System.Serializable]
public class TriggerConfig
{
    [Header("触发器类型")]
    public TriggerType triggerType = TriggerType.Collision;
    
    [Header("碰撞触发器参数")]
    [ShowIf("triggerType", TriggerType.Collision)]
    [Tooltip("目标标签")]
    public string targetTag = "Enemy";
    
    [ShowIf("triggerType", TriggerType.Collision)]
    [Tooltip("是否只检测特定类型的碰撞")]
    public bool useAttackTypeFilter = true;
    
    [ShowIf("triggerType", TriggerType.Collision)]
    [ShowIf("useAttackTypeFilter")]
    [Tooltip("攻击类型过滤（如果启用）")]
    public string attackType = "Hit";
    
    /// <summary>
    /// 创建触发器实例
    /// </summary>
    public ITrigger CreateTrigger()
    {
        switch (triggerType)
        {
            case TriggerType.Collision:
                var collisionTrigger = new CollisionTrigger();
                // 传递配置参数给触发器实例
                collisionTrigger.SetTargetTag(targetTag);
                return collisionTrigger;
            default:
                Debug.LogError($"不支持的触发器类型: {triggerType}");
                return null;
        }
    }
    
    /// <summary>
    /// 获取调试信息
    /// </summary>
    public string GetDebugInfo()
    {
        switch (triggerType)
        {
            case TriggerType.Collision:
                if (useAttackTypeFilter)
                {
                    return $"碰撞触发器: 标签={targetTag}, 类型={attackType}";
                }
                return $"碰撞触发器: 标签={targetTag}";
            default:
                return $"触发器: {triggerType}";
        }
    }
}

/// <summary>
/// 触发器类型枚举
/// </summary>
public enum TriggerType
{
    Collision,  // 碰撞触发器
    Kill,       // 击杀触发器（暂未实现）
    Charging,   // 蓄力触发器（暂未实现）
    Health,     // 血量变化触发器（暂未实现）
    Time        // 时间触发器（暂未实现）
}
