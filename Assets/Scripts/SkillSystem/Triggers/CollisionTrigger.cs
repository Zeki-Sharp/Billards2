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
    /// <param name="eventData">事件数据，期望是 AttackData</param>
    /// <returns>是否检测到碰撞事件</returns>
    public bool CheckEvent(object eventData)
    {
        // 检查事件数据类型
        if (eventData is AttackData attackData)
        {
            // 【关键修复1】检查是否在玩家回合
            var gameFlowController = GameFlowController.Instance;
            bool isPlayerPhase = gameFlowController != null && gameFlowController.IsPlayerPhase;
            
            // 【关键修复2】检查攻击者是否是玩家
            bool isPlayerAttacker = attackData.Attacker != null && attackData.Attacker.CompareTag("Player");
            
            // 检查目标标签是否匹配
            bool tagMatches = string.IsNullOrEmpty(targetTag) || attackData.TargetTag == targetTag;
            
            // 只有当在玩家回合、攻击者是玩家且目标标签匹配时才触发
            if (isPlayerPhase && isPlayerAttacker && tagMatches)
            {
                return true;
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
        // 不需要特殊重置逻辑
    }
}
