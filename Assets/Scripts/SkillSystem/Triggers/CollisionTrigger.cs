using UnityEngine;

/// <summary>
/// 碰撞触发器 - 技能系统第一阶段最小验证
/// 监听 GameEventBus.OnAttack 事件，检测碰撞事件是否发生
/// 只负责检测碰撞事件，不处理触发逻辑
/// </summary>
public class CollisionTrigger : ITrigger
{
    public string TriggerName => "CollisionTrigger";
    
    private string targetTag = "Enemy"; // 默认目标标签
    private bool useAttackTypeFilter = true; // 是否使用攻击类型过滤
    private string attackType = "Hit"; // 攻击类型过滤
    
    /// <summary>
    /// 设置目标标签
    /// </summary>
    /// <param name="tag">目标标签</param>
    public void SetTargetTag(string tag)
    {
        targetTag = tag;
        Debug.Log($"[{TriggerName}] 设置目标标签: {targetTag}");
    }
    
    /// <summary>
    /// 设置攻击类型过滤
    /// </summary>
    /// <param name="useFilter">是否使用攻击类型过滤</param>
    /// <param name="attackType">攻击类型</param>
    public void SetAttackTypeFilter(bool useFilter, string attackType)
    {
        useAttackTypeFilter = useFilter;
        this.attackType = attackType;
        Debug.Log($"[{TriggerName}] 设置攻击类型过滤: {useFilter}, 类型: {attackType}");
    }
    
    /// <summary>
    /// 初始化触发器
    /// </summary>
    public void Initialize()
    {
        Debug.Log($"[{TriggerName}] 初始化完成，目标标签: {targetTag}");
    }
    
    /// <summary>
    /// 检查是否检测到碰撞事件
    /// </summary>
    /// <param name="eventData">事件数据，期望是 AttackData</param>
    /// <returns>是否检测到碰撞事件</returns>
    public bool CheckEvent(object eventData)
    {
        // 检查事件数据类型
        if (eventData is AttackData attackData)
        {
            // 检查攻击类型过滤（如果启用）
            bool attackTypeMatches = true;
            if (useAttackTypeFilter)
            {
                attackTypeMatches = attackData.AttackType == attackType;
            }
            
            if (attackTypeMatches)
            {
                // 检查目标标签是否匹配
                bool tagMatches = string.IsNullOrEmpty(targetTag) || attackData.TargetTag == targetTag;
                
                if (tagMatches)
                {
                    Debug.Log($"[{TriggerName}] 检测到碰撞事件: {attackData.AttackType} at {attackData.Position}, 目标标签: {attackData.TargetTag}");
                    return true;
                }
                else
                {
                    Debug.Log($"[{TriggerName}] 碰撞事件目标标签不匹配: 期望={targetTag}, 实际={attackData.TargetTag}");
                }
            }
            else
            {
                Debug.Log($"[{TriggerName}] 攻击类型不匹配: 期望={attackType}, 实际={attackData.AttackType}");
            }
            
            return false;
        }
        
        return false;
    }
    
    /// <summary>
    /// 重置触发器状态
    /// </summary>
    public void Reset()
    {
        // 第一阶段最小验证：不需要特殊重置逻辑
        Debug.Log($"[{TriggerName}] 状态重置");
    }
}
