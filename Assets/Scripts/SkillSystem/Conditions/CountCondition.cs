using UnityEngine;

/// <summary>
/// 计数条件 - 技能系统第一阶段最小验证
/// 检查碰撞次数是否达到阈值（如3次），用于连击等计数场景
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
        Debug.Log($"[{ConditionName}] 初始化完成，需要计数: {requiredCount}");
    }
    
    /// <summary>
    /// 检查条件是否满足
    /// </summary>
    /// <param name="eventData">事件数据，期望是 AttackData</param>
    /// <returns>条件是否满足</returns>
    public bool CheckCondition(object eventData)
    {
        // 检查事件数据类型
        if (eventData is AttackData attackData)
        {
            // 检查是否为碰撞类型的攻击
            if (attackData.AttackType == "Hit")
            {
                currentCount++;
                Debug.Log($"[{ConditionName}] 碰撞计数: {currentCount}/{requiredCount}");
                
                bool conditionMet = currentCount >= requiredCount;
                
                if (conditionMet)
                {
                    Debug.Log($"[{ConditionName}] 条件满足！达到计数阈值 {requiredCount}");
                }
                
                return conditionMet;
            }
        }
        
        return false;
    }
    
    /// <summary>
    /// 重置条件状态
    /// </summary>
    public void Reset()
    {
        currentCount = 0;
        Debug.Log($"[{ConditionName}] 计数重置: {currentCount}/{requiredCount}");
    }
}
