using UnityEngine;
using Sirenix.OdinInspector;

/// <summary>
/// 移除条件配置 - 用于配置技能效果的移除条件
/// 独立的移除条件系统，不与具体技能效果绑定
/// </summary>
[System.Serializable]
public class RemovalConditionConfig
{
    [Header("移除条件类型")]
    [Tooltip("移除条件类型")]
    public RemovalConditionType conditionType = RemovalConditionType.OnPlayerPhaseEnded;
    
    /// <summary>
    /// 创建移除条件实例
    /// </summary>
    public IRemovalCondition CreateRemovalCondition()
    {
        switch (conditionType)
        {
            case RemovalConditionType.OnPlayerPhaseEnded:
                return new OnPlayerPhaseEndedCondition();
            case RemovalConditionType.Duration:
                return new DurationCondition();
            case RemovalConditionType.Immediate:
                return new ImmediateRemoveCondition();
            case RemovalConditionType.Never:
                return new NeverRemoveCondition();
            default:
                Debug.LogError($"不支持的移除条件类型: {conditionType}");
                return null;
        }
    }
    
    /// <summary>
    /// 获取调试信息
    /// </summary>
    public string GetDebugInfo()
    {
        return $"移除条件: {conditionType}";
    }
}

/// <summary>
/// 移除条件类型枚举
/// </summary>
public enum RemovalConditionType
{
    OnPlayerPhaseEnded,   // 玩家回合结束时移除
    Duration,             // 持续时间移除（暂未实现）
    Immediate,            // 立即移除（用于ActionEffect，执行后重置触发条件）
    Never                 // 永不移除
}
