using UnityEngine;

/// <summary>
/// 持续时间效果移除条件
/// 在指定时间后移除效果
/// 主要用于PropertyEffect（持续效果）
/// </summary>
public class DurationEffectRemovalCondition : IEffectRemovalCondition
{
    public string ConditionName => "DurationEffectRemovalCondition";
    
    private float duration;
    private float startTime;
    private bool isInitialized = false;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="durationSeconds">持续时间（秒）</param>
    public DurationEffectRemovalCondition(float durationSeconds)
    {
        duration = durationSeconds;
    }

    /// <summary>
    /// 初始化效果移除条件
    /// </summary>
    public void Initialize()
    {
        startTime = Time.time;
        isInitialized = true;
    }

    /// <summary>
    /// 检查是否应该移除效果
    /// </summary>
    /// <param name="eventData">事件数据</param>
    /// <returns>是否应该移除效果</returns>
    public bool ShouldRemoveEffect(object eventData)
    {
        if (!isInitialized)
        {
            Debug.LogWarning($"[{ConditionName}] 未初始化，无法检查移除条件");
            return false;
        }
        
        float elapsedTime = Time.time - startTime;
        bool shouldRemove = elapsedTime >= duration;
        
        if (shouldRemove)
        {
            Debug.Log($"[{ConditionName}] 持续时间到达 ({elapsedTime:F2}s >= {duration}s)，应该移除效果");
        }
        
        return shouldRemove;
    }

    /// <summary>
    /// 重置效果移除条件状态
    /// </summary>
    public void Reset()
    {
        startTime = Time.time;
        isInitialized = true;
    }
}
