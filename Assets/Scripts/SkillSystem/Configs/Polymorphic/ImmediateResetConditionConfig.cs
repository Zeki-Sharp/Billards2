using UnityEngine;
using Sirenix.OdinInspector;

/// <summary>
/// 立即重置条件配置 - 技能触发后立即可以再次使用
/// </summary>
[System.Serializable]
public class ImmediateResetConditionConfig : ResetConditionBase
{
    // 无需参数

    public override IResetCondition CreateResetCondition()
    {
        return new ImmediateResetCondition();
    }

    public override string GetDebugInfo()
    {
        return "立即重置";
    }
}

