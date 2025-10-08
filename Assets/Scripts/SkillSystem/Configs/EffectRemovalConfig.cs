using UnityEngine;
using Sirenix.OdinInspector;

/// <summary>
/// 效果移除配置 - 用于配置持续效果何时移除的条件
/// 只有PropertyEffect（持续效果）需要配置此选项
/// </summary>
[System.Serializable]
public class EffectRemovalConfig
{
    [LabelText("效果移除类型")]
    [Tooltip("持续效果何时移除的条件")]
    public EffectRemovalType removalType = EffectRemovalType.Duration;
    
    [ShowIf("removalType", EffectRemovalType.Duration)]
    [LabelText("持续时间")]
    [Tooltip("持续时间（秒）")]
    [MinValue(0.1f)]
    public float duration = 30f;
    
    [ShowIf("removalType", EffectRemovalType.OnPlayerPhaseEnded)]
    [LabelText("回合结束时移除")]
    [Tooltip("回合结束时移除效果")]
    public bool removeOnPhaseEnd = true;
    
    [ShowIf("removalType", EffectRemovalType.OnConditionMet)]
    [LabelText("移除条件")]
    [Tooltip("满足条件时移除效果")]
    public ICondition removalCondition;
    
    [ShowIf("removalType", EffectRemovalType.InverseConditionCheck)]
    [LabelText("反向条件")]
    [Tooltip("反向条件检查 - 当条件不满足时移除效果")]
    public ICondition inverseCondition;
    
    /// <summary>
    /// 创建效果移除条件实例
    /// </summary>
    public IEffectRemovalCondition CreateEffectRemovalCondition()
    {
        switch (removalType)
        {
            case EffectRemovalType.Duration:
                return new DurationEffectRemovalCondition(duration);
            case EffectRemovalType.OnPlayerPhaseEnded:
                return new OnPhaseEndedEffectRemovalCondition();
            case EffectRemovalType.OnConditionMet:
                return new OnConditionMetEffectRemovalCondition(removalCondition);
            case EffectRemovalType.InverseConditionCheck:
                return new InverseConditionCheckEffectRemovalCondition(inverseCondition);
            case EffectRemovalType.Never:
                return new NeverEffectRemovalCondition();
            default:
                Debug.LogError($"不支持的效果移除类型: {removalType}");
                return null;
        }
    }
    
    /// <summary>
    /// 获取调试信息
    /// </summary>
    public string GetDebugInfo()
    {
        switch (removalType)
        {
            case EffectRemovalType.Duration:
                return $"效果移除: {removalType}({duration}s)";
            default:
                return $"效果移除: {removalType}";
        }
    }
}

/// <summary>
/// 效果移除类型枚举
/// </summary>
public enum EffectRemovalType
{
    Duration,            // 持续时间
    OnPlayerPhaseEnded,  // 回合结束移除
    OnConditionMet,      // 满足条件时移除
    InverseConditionCheck, // 反向条件检查移除
    Never                // 永不移除
}
