using UnityEngine;

/// <summary>
/// 永不移除效果移除条件
/// 效果永远不会被自动移除
/// 适用于永久效果（如永久属性提升）
/// </summary>
public class NeverEffectRemovalCondition : IEffectRemovalCondition
{
    public string ConditionName => "NeverEffectRemovalCondition";

    /// <summary>
    /// 初始化效果移除条件
    /// </summary>
    public void Initialize()
    {
    }

    /// <summary>
    /// 检查是否应该移除效果 - 总是返回false
    /// </summary>
    /// <param name="eventData">事件数据</param>
    /// <returns>总是返回false，表示永不移除效果</returns>
    public bool ShouldRemoveEffect(SkillArgs args)
    {
        Debug.Log($"[{ConditionName}] 检查效果移除条件: 永不移除");
        return false;
    }

    /// <summary>
    /// 重置效果移除条件状态
    /// </summary>
    public void Reset()
    {
    }
}
