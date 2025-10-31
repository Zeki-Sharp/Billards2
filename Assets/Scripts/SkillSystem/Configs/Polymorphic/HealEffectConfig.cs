using UnityEngine;
using Sirenix.OdinInspector;

/// <summary>
/// 治疗效果配置 - 恢复玩家生命值
/// </summary>
[System.Serializable]
public class HealEffectConfig : EffectBase
{
    [LabelText("治疗量")]
    [Tooltip("恢复的生命值（支持动态值，如固定值、随机值、基于属性等）")]
    [SerializeReference]
    public PropertyGetFloat healAmount = new ConstantFloat(20f);

    public override IEffect CreateEffect(IEffectRemovalCondition effectRemovalCondition = null)
    {
        var healEffect = new HealEffect();
        
        // 兼容性：如果没有设置 Property，使用默认固定值 20
        var healProp = healAmount ?? new ConstantFloat(20f);
        healEffect.SetHealAmount(healProp);
        
        // 治疗是瞬时效果，不需要移除条件
        return healEffect;
    }

    public override string GetDebugInfo()
    {
        return $"治疗: +{healAmount} HP";
    }
}

