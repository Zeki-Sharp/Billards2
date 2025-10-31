using UnityEngine;
using Sirenix.OdinInspector;

/// <summary>
/// 永不重置条件配置 - 技能触发一次后永远不会重置
/// </summary>
[System.Serializable]
public class NeverResetConditionConfig : ResetConditionBase
{
    // 无需参数

    public override IResetCondition CreateResetCondition()
    {
        return new NeverResetCondition();
    }

    public override string GetDebugInfo()
    {
        return "永不重置";
    }
}

