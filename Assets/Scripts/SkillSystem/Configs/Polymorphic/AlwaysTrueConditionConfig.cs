using UnityEngine;
using Sirenix.OdinInspector;

/// <summary>
/// 始终为真条件配置 - 总是满足条件
/// </summary>
[System.Serializable]
public class AlwaysTrueConditionConfig : ConditionBase
{
    // 无需参数

    public override ICondition CreateCondition()
    {
        return new AlwaysTrueCondition();
    }

    public override string GetDebugInfo()
    {
        return "无条件（始终满足）";
    }
}

