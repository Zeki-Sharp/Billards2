using UnityEngine;

/// <summary>
/// 反向条件检查移除条件
/// 当原始条件不满足时移除效果
/// 通用的移除条件，可以配合任何条件使用
/// </summary>
public class InverseConditionCheck : IRemovalCondition
{
    public string ConditionName => "InverseConditionCheck";
    
    private ICondition originalCondition;
    
    /// <summary>
    /// 设置原始条件
    /// </summary>
    /// <param name="condition">原始条件</param>
    public void SetOriginalCondition(ICondition condition)
    {
        originalCondition = condition;
    }
    
    /// <summary>
    /// 初始化移除条件
    /// </summary>
    public void Initialize()
    {
    }
    
    /// <summary>
    /// 检查是否应该移除
    /// </summary>
    /// <param name="eventData">事件数据</param>
    /// <returns>是否应该移除</returns>
    public bool ShouldRemove(object eventData)
    {
        if (originalCondition == null)
        {
            Debug.LogWarning($"[{ConditionName}] 原始条件未设置");
            return false;
        }
        
        // 当原始条件不满足时移除
        return !originalCondition.CheckCondition(eventData);
    }
    
    /// <summary>
    /// 重置移除条件状态
    /// </summary>
    public void Reset()
    {
        // 反向条件检查不需要重置
    }
}
