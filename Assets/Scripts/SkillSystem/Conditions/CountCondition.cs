using UnityEngine;

/// <summary>
/// 计数条件 - 技能系统第一阶段最小验证
/// 统计触发器触发次数，达到阈值时满足条件
/// 通用计数器，不关心具体事件类型，由触发器负责事件过滤
/// 支持条件重置
/// </summary>
public class CountCondition : ICondition
{
    public string ConditionName => "CountCondition";
    
    private int currentCount = 0;
    private int requiredCount = 3; // 默认需要3次碰撞
    
    /// <summary>
    /// 设置需要的计数阈值
    /// </summary>
    /// <param name="count">需要的计数</param>
    public void SetRequiredCount(int count)
    {
        requiredCount = count;
        Debug.Log($"[{ConditionName}] 设置计数阈值: {requiredCount}");
    }
    
    /// <summary>
    /// 初始化条件
    /// </summary>
    public void Initialize()
    {
        currentCount = 0;
    }
    
    /// <summary>
    /// 检查条件是否满足
    /// </summary>
    /// <param name="eventData">事件数据（由触发器过滤，条件只负责计数）</param>
    /// <returns>条件是否满足</returns>
    public bool CheckCondition(object eventData)
    {
        // 不管什么事件，只要被调用就说明触发器检测到了事件
        // 直接计数即可
        currentCount++;
        bool conditionMet = currentCount >= requiredCount;
        
        Debug.Log($"[{ConditionName}] 条件检查 - 当前计数: {currentCount}/{requiredCount}, 条件满足: {conditionMet}");
        
        if (conditionMet)
        {
            Debug.Log($"[{ConditionName}] ✅ 条件满足！达到计数阈值 {requiredCount}");
        }
        
        return conditionMet;
    }
    
    /// <summary>
    /// 重置条件状态
    /// </summary>
    public void Reset()
    {
        Debug.Log($"[{ConditionName}] 🔄 重置条件 - 计数从 {currentCount} 重置为 0");
        currentCount = 0;
    }
    
    /// <summary>
    /// 回合结束时重置条件状态
    /// </summary>
    public void ResetOnPhaseEnd()
    {
        // CountCondition 默认在回合结束时重置
        Debug.Log($"[{ConditionName}] 🔄 回合结束重置条件 - 计数从 {currentCount} 重置为 0");
        currentCount = 0;
    }
}
