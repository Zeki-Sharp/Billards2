using UnityEngine;

/// <summary>
/// 总是为真的条件 - 用于无条件触发
/// 当技能没有条件限制时使用，触发即执行
/// </summary>
public class AlwaysTrueCondition : ICondition
{
    public string ConditionName => "AlwaysTrueCondition";
    
    /// <summary>
    /// 初始化条件
    /// </summary>
    public void Initialize()
    {
    }
    
    /// <summary>
    /// 检查条件是否满足 - 总是返回true
    /// </summary>
    /// <param name="eventData">事件数据</param>
    /// <returns>总是返回true</returns>
    public bool CheckCondition(object eventData)
    {
        return true;
    }
    
    /// <summary>
    /// 重置条件状态
    /// </summary>
    public void Reset()
    {
        // 无条件状态，无需重置
    }
    
    /// <summary>
    /// 回合结束时重置条件状态
    /// </summary>
    public void ResetOnPhaseEnd()
    {
        // 无条件状态，无需重置
    }
}
