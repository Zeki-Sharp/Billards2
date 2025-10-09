using UnityEngine;
using Sirenix.OdinInspector;

/// <summary>
/// 技能效果配置 - 用于配置技能效果的参数
/// 简化设计：选择类型后直接显示对应参数
/// </summary>
[System.Serializable]
public class SkillEffectConfig
{
    [LabelText("效果类型")]
    [Tooltip("选择技能产生的效果类型")]
    public SkillEffectType effectType = SkillEffectType.StatModifier;
    
    [ShowIf("effectType", SkillEffectType.StatModifier)]
    [LabelText("目标属性名称")]
    [Tooltip("目标属性名称")]
    public string targetStat = "Damage";
    
    [ShowIf("effectType", SkillEffectType.StatModifier)]
    [LabelText("修改倍数")]
    [Tooltip("修改倍数（2.0 = +100%）")]
    public float modifierValue = 2f;
    
    [ShowIf("effectType", SkillEffectType.StatModifier)]
    [LabelText("修改器类型")]
    [Tooltip("修改器类型")]
    public StatModifierType modifierType = StatModifierType.PercentMult;
    
    [ShowIf("effectType", SkillEffectType.Heal)]
    [LabelText("治疗量")]
    [Tooltip("治疗量（恢复的生命值）")]
    [MinValue(0)]
    public float healAmount = 20f;
    
    /// <summary>
    /// 创建效果实例
    /// </summary>
    public IEffect CreateEffect(IEffectRemovalCondition effectRemovalCondition = null)
    {
        switch (effectType)
        {
            case SkillEffectType.StatModifier:
                var statModifierEffect = new StatModifierEffect();
                statModifierEffect.SetModifier(targetStat, modifierValue);
                // 设置新的效果移除条件
                if (effectRemovalCondition != null)
                {
                    statModifierEffect.SetEffectRemovalCondition(effectRemovalCondition);
                }
                return statModifierEffect;
                
            case SkillEffectType.Heal:
                var healEffect = new HealEffect();
                healEffect.SetHealAmount(healAmount);
                // 治疗是瞬时效果，不需要移除条件
                return healEffect;
                
            case SkillEffectType.Spawn:
                var spawnEffect = new SpawnEffect();
                // Spawn效果是空占位符，不需要移除条件
                return spawnEffect;
                
            default:
                Debug.LogError($"不支持的效果类型: {effectType}");
                return null;
        }
    }
    
    /// <summary>
    /// 获取调试信息
    /// </summary>
    public string GetDebugInfo()
    {
        switch (effectType)
        {
            case SkillEffectType.StatModifier:
                return $"属性修改: {targetStat} x{modifierValue} ({modifierType})";
            case SkillEffectType.Heal:
                return $"治疗: +{healAmount} HP";
            case SkillEffectType.Spawn:
                return $"生成效果: 空占位符（功能由掉落系统处理）";
            default:
                return $"效果: {effectType}";
        }
    }
}

/// <summary>
/// 技能效果类型枚举
/// </summary>
public enum SkillEffectType
{
    StatModifier,   // 属性修改效果
    Heal,           // 治疗效果（恢复当前生命值）
    Status,         // 状态效果（暂未实现）
    Resource,       // 资源效果（暂未实现）
    Spawn,          // 生成效果（暂未实现）
    Chain           // 连锁效果（暂未实现）
}
