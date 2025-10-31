using UnityEngine;
using Sirenix.OdinInspector;

/// <summary>
/// 回合结束重置条件配置 - 玩家回合结束时重置技能
/// </summary>
[System.Serializable]
public class OnPhaseEndedResetConditionConfig : ResetConditionBase
{
    // 无需参数

    public override IResetCondition CreateResetCondition()
    {
        return new OnPhaseEndedResetCondition();
    }

    public override string GetDebugInfo()
    {
        return "回合结束重置";
    }
}

