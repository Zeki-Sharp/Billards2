using UnityEngine;

/// <summary>
/// 移除条件接口 - 定义何时移除技能效果
/// 独立的移除逻辑系统，不与具体技能效果绑定
/// </summary>
public interface IRemovalCondition
{
    /// <summary>
    /// 移除条件名称
    /// </summary>
    string ConditionName { get; }
    
    /// <summary>
    /// 初始化移除条件
    /// </summary>
    void Initialize();
    
    /// <summary>
    /// 检查是否应该移除
    /// </summary>
    /// <param name="eventData">事件数据</param>
    /// <returns>是否应该移除</returns>
    bool ShouldRemove(object eventData);
    
    /// <summary>
    /// 重置移除条件状态
    /// </summary>
    void Reset();
}
