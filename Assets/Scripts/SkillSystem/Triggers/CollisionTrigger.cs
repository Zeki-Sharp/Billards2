using UnityEngine;

/// <summary>
/// 碰撞触发器 - 技能系统碰撞事件检测
/// 只负责检测碰撞事件，不处理触发逻辑
/// </summary>
public class CollisionTrigger : ITrigger
{
    public string TriggerName => "CollisionTrigger";
    
    private string targetTag = "Enemy"; // 默认目标标签
    
    /// <summary>
    /// 设置目标标签
    /// </summary>
    /// <param name="tag">目标标签</param>
    public void SetTargetTag(string tag)
    {
        targetTag = tag;
    }
    
    /// <summary>
    /// 初始化触发器
    /// </summary>
    public void Initialize()
    {
    }
    
    /// <summary>
    /// 检查是否检测到碰撞事件
    /// </summary>
    /// <param name="args">技能参数</param>
    /// <returns>是否检测到碰撞事件</returns>
    public bool CheckEvent(SkillArgs args)
    {
        // 检查 CollisionEvent
        if (args.TryGetEventData<CollisionEvent>(out var collisionEvent))
        {
            // 检查是否在玩家回合
            var gameFlowController = GameFlowController.Instance;
            if (gameFlowController == null || !gameFlowController.IsPlayerPhase)
            {
                return false;
            }
            
            // 检查碰撞发起者是否是玩家
            if (collisionEvent.Source == null || !collisionEvent.Source.CompareTag("Player"))
            {
                return false;
            }
            
            // 检查目标标签是否匹配
            bool tagMatches = string.IsNullOrEmpty(targetTag) || 
                             (collisionEvent.Target != null && collisionEvent.Target.CompareTag(targetTag));
            
            return tagMatches;
        }
        
        return false;
    }
    
    /// <summary>
    /// 重置触发器状态
    /// </summary>
    public void Reset()
    {
        // 不需要特殊重置逻辑
    }
}
