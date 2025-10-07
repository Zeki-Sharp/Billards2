using UnityEngine;

/// <summary>
/// 碰撞触发器 - 技能系统第一阶段最小验证
/// 监听 GameEventBus.OnAttack 事件，检测碰撞事件是否发生
/// 只负责检测碰撞事件，不处理触发逻辑
/// </summary>
public class CollisionTrigger : ITrigger
{
    public string TriggerName => "CollisionTrigger";
    
    /// <summary>
    /// 初始化触发器
    /// </summary>
    public void Initialize()
    {
        // 第一阶段最小验证：不需要特殊初始化
        Debug.Log($"[{TriggerName}] 初始化完成");
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
            // 检测是否为碰撞类型的攻击（Hit类型表示碰撞）
            bool isCollisionEvent = attackData.AttackType == "Hit";
            
            if (isCollisionEvent)
            {
                Debug.Log($"[{TriggerName}] 检测到碰撞事件: {attackData.AttackType} at {attackData.Position}");
            }
            
            return isCollisionEvent;
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
