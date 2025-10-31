using UnityEngine;
using Sirenix.OdinInspector;

/// <summary>
/// 持续时间效果移除条件配置 - 经过指定时间后移除效果
/// </summary>
[System.Serializable]
public class DurationEffectRemovalConditionConfig : EffectRemovalConditionBase
{
    [LabelText("持续时间")]
    [Tooltip("效果持续时间（秒）")]
    [MinValue(0.1f)]
    public float duration = 30f;

    public override IEffectRemovalCondition CreateEffectRemovalCondition()
    {
        return new DurationEffectRemovalCondition(duration);
    }

    public override string GetDebugInfo()
    {
        return $"持续时间: {duration}秒后移除";
    }
}

