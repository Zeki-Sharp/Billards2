using UnityEngine;
using Sirenix.OdinInspector;

/// <summary>
/// 关卡完成效果移除条件配置 - 关卡完成时移除效果
/// </summary>
[System.Serializable]
public class OnLevelCompletedEffectRemovalConditionConfig : EffectRemovalConditionBase
{
    // 无需参数

    public override IEffectRemovalCondition CreateEffectRemovalCondition()
    {
        return new OnLevelCompletedEffectRemovalCondition();
    }

    public override string GetDebugInfo()
    {
        return "关卡完成时移除";
    }
}

