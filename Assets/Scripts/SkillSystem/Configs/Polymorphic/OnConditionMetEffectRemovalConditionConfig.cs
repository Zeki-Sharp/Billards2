using UnityEngine;
using Sirenix.OdinInspector;

/// <summary>
/// 条件满足效果移除条件配置 - 当指定条件满足时移除效果
/// </summary>
[System.Serializable]
public class OnConditionMetEffectRemovalConditionConfig : EffectRemovalConditionBase
{
    [LabelText("移除条件")]
    [Tooltip("满足此条件时移除效果")]
    [SerializeReference]
    public ConditionBase removalCondition;

    public override IEffectRemovalCondition CreateEffectRemovalCondition()
    {
        if (removalCondition != null)
        {
            var condition = removalCondition.CreateCondition();
            return new OnConditionMetEffectRemovalCondition(condition);
        }
        
        Debug.LogWarning("OnConditionMetEffectRemovalConditionConfig: 未设置移除条件");
        return new NeverEffectRemovalCondition();
    }

    public override string GetDebugInfo()
    {
        return removalCondition != null ? $"条件满足时移除: {removalCondition.GetDebugInfo()}" : "条件满足时移除（未设置条件）";
    }
}

