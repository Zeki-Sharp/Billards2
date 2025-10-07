using UnityEngine;
using Sirenix.OdinInspector;

/// <summary>
/// 技能效果配置 - 用于配置技能效果的参数
/// 简化设计：选择类型后直接显示对应参数
/// </summary>
[System.Serializable]
public class SkillEffectConfig
{
    [Header("效果类型")]
    public SkillEffectType effectType = SkillEffectType.StatModifier;
    
    [Header("属性修改效果参数")]
    [ShowIf("effectType", SkillEffectType.StatModifier)]
    [Tooltip("目标属性名称")]
    public string targetStat = "Damage";
    
    [ShowIf("effectType", SkillEffectType.StatModifier)]
    [Tooltip("修改倍数（2.0 = +100%）")]
    public float modifierValue = 2f;
    
    [ShowIf("effectType", SkillEffectType.StatModifier)]
    [Tooltip("修改器类型")]
    public StatModifierType modifierType = StatModifierType.PercentMult;
    
    /// <summary>
    /// 创建效果实例
    /// </summary>
    public IEffect CreateEffect(IRemovalCondition removalCondition = null)
    {
        switch (effectType)
        {
            case SkillEffectType.StatModifier:
                var statModifierEffect = new StatModifierEffect();
                statModifierEffect.SetModifier(targetStat, modifierValue);
                // 设置移除条件
                if (removalCondition != null)
                {
                    statModifierEffect.SetRemovalCondition(removalCondition);
                }
                return statModifierEffect;
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
    Status,         // 状态效果（暂未实现）
    Resource,       // 资源效果（暂未实现）
    Spawn,          // 生成效果（暂未实现）
    Chain           // 连锁效果（暂未实现）
}
