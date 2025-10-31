using UnityEngine;
using Sirenix.OdinInspector;

/// <summary>
/// 计数条件配置 - 需要达到指定计数才触发
/// </summary>
[System.Serializable]
public class CountConditionConfig : ConditionBase
{
    [LabelText("需要达到的计数")]
    [Tooltip("需要达到的计数")]
    [MinValue(1)]
    public int requiredCount = 2;

    public override ICondition CreateCondition()
    {
        var countCondition = new CountCondition();
        countCondition.SetRequiredCount(requiredCount);
        return countCondition;
    }

    public override string GetDebugInfo()
    {
        return $"计数条件: 需要 {requiredCount} 次";
    }
}

