using UnityEngine;
using Sirenix.OdinInspector;

/// <summary>
/// 永不移除效果条件配置 - 效果永久存在
/// </summary>
[System.Serializable]
public class NeverEffectRemovalConditionConfig : EffectRemovalConditionBase
{
    // 无需参数

    public override IEffectRemovalCondition CreateEffectRemovalCondition()
    {
        return new NeverEffectRemovalCondition();
    }

    public override string GetDebugInfo()
    {
        return "永不移除";
    }
}

