using UnityEngine;
using Sirenix.OdinInspector;

/// <summary>
/// 回合结束效果移除条件配置 - 玩家回合结束时移除效果
/// </summary>
[System.Serializable]
public class OnPhaseEndedEffectRemovalConditionConfig : EffectRemovalConditionBase
{
    // 无需参数

    public override IEffectRemovalCondition CreateEffectRemovalCondition()
    {
        return new OnPhaseEndedEffectRemovalCondition();
    }

    public override string GetDebugInfo()
    {
        return "回合结束时移除";
    }
}

