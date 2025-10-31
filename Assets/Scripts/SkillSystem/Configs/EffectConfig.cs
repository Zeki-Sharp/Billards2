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
    [Tooltip("修改倍数（支持动态值）")]
    [SerializeReference]
    public PropertyGetFloat modifierValue;
    
    [ShowIf("effectType", SkillEffectType.StatModifier)]
    [LabelText("修改器类型")]
    [Tooltip("修改器类型")]
    public StatModifierType modifierType = StatModifierType.PercentMult;
    
    [ShowIf("effectType", SkillEffectType.StatModifier)]
    [LabelText("允许叠加")]
    [Tooltip("是否允许效果叠加（如击杀增加攻击力）")]
    [InfoBox("✓ 允许叠加：每次触发都创建新修改器，支持叠加效果\n✗ 不允许叠加：只创建一次修改器，后续触发跳过", InfoMessageType.Info)]
    public bool allowStacking = true;
    
    [ShowIf("effectType", SkillEffectType.Heal)]
    [LabelText("治疗量")]
    [Tooltip("治疗量（支持动态值，如固定值、随机值、基于属性等）")]
    [SerializeReference]
    public PropertyGetFloat healAmount;
    
    [ShowIf("effectType", SkillEffectType.Transition)]
    [LabelText("最小 Transition 时间")]
    [Tooltip("Transition 的最小持续时间（秒）")]
    [Range(0.1f, 5f)]
    public float minTransitionTime = 1f;
    
    [ShowIf("effectType", SkillEffectType.Transition)]
    [LabelText("最大 Transition 时间")]
    [Tooltip("Transition 的最大持续时间（秒）")]
    [Range(0.1f, 10f)]
    public float maxTransitionTime = 5f;
    
    [ShowIf("effectType", SkillEffectType.Transition)]
    [LabelText("Transition 门槛值")]
    [Tooltip("触发 Transition 所需的最小蓄力进度（0-1）")]
    [Range(0f, 1f)]
    public float transitionThreshold = 0.3f;
    
    [ShowIf("effectType", SkillEffectType.Transition)]
    [LabelText("蓄力到 Transition 映射曲线")]
    [Tooltip("将蓄力进度映射到 Transition 时长的曲线")]
    public AnimationCurve chargingToTransitionCurve;
    
    // ========== 弱点攻击配置 ==========
    [ShowIf("effectType", SkillEffectType.WeakPoint)]
    [BoxGroup("弱点攻击配置")]
    [LabelText("弱点标记预制体")]
    [Tooltip("弱点标记的UI预制体，将显示在敌人身上")]
    [AssetsOnly]
    [Required]
    public GameObject weakPointMarkerPrefab;
    
    [ShowIf("effectType", SkillEffectType.WeakPoint)]
    [BoxGroup("弱点攻击配置")]
    [LabelText("判定半径")]
    [Tooltip("弱点判定的半径（单位）")]
    [Range(0.1f, 2f)]
    public float weakPointRadius = 0.5f;
    
    // ========== 掉落物品配置 ==========
    [ShowIf("effectType", SkillEffectType.DropItem)]
    [BoxGroup("掉落物品配置")]
    [LabelText("掉落物品配置")]
    [Tooltip("要掉落的物品配置")]
    [Required]
    public ItemConfig dropItemConfig;
    
    [ShowIf("effectType", SkillEffectType.DropItem)]
    [BoxGroup("掉落物品配置")]
    [LabelText("掉落概率")]
    [Tooltip("掉落此物品的概率（0-1）")]
    [Range(0f, 1f)]
    public float dropChance = 1.0f;
    
    [ShowIf("effectType", SkillEffectType.DropItem)]
    [BoxGroup("掉落物品配置")]
    [LabelText("掉落范围配置")]
    [Tooltip("掉落位置的范围配置")]
    public DropRangeConfig dropRangeConfig = new DropRangeConfig();
    
    [ShowIf("effectType", SkillEffectType.WeakPoint)]
    [BoxGroup("弱点攻击配置")]
    [LabelText("伤害倍率")]
    [Tooltip("命中弱点时的伤害倍率")]
    [Range(1.0f, 5.0f)]
    public float weakPointDamageMultiplier = 1.5f;
    
    [ShowIf("effectType", SkillEffectType.WeakPoint)]
    [BoxGroup("弱点攻击配置")]
    [LabelText("击中后刷新")]
    [Tooltip("命中弱点后是否立即刷新位置")]
    public bool weakPointRefreshOnHit = true;
    
    /// <summary>
    /// 创建效果实例
    /// </summary>
    public IEffect CreateEffect(IEffectRemovalCondition effectRemovalCondition = null)
    {
        switch (effectType)
        {
            case SkillEffectType.StatModifier:
                var statModifierEffect = new StatModifierEffect();
                // ✅ 兼容性：如果没有设置 Property，使用默认固定值 2.0
                var modifierProp = modifierValue ?? new ConstantFloat(2f);
                statModifierEffect.SetModifier(targetStat, modifierProp, modifierType);
                statModifierEffect.SetAllowStacking(allowStacking); // 设置是否允许叠加
                // 设置新的效果移除条件
                if (effectRemovalCondition != null)
                {
                    statModifierEffect.SetEffectRemovalCondition(effectRemovalCondition);
                }
                return statModifierEffect;
                
            case SkillEffectType.Heal:
                var healEffect = new HealEffect();
                // ✅ 兼容性：如果没有设置 Property，使用默认固定值 20
                var healProp = healAmount ?? new ConstantFloat(20f);
                healEffect.SetHealAmount(healProp);
                // 治疗是瞬时效果，不需要移除条件
                return healEffect;
                
            case SkillEffectType.Transition:
                var transitionEffect = new TransitionEffect();
                // 设置 Transition 专用参数
                transitionEffect.SetParameters(
                    minTransitionTime,
                    maxTransitionTime,
                    transitionThreshold,
                    chargingToTransitionCurve
                );
                // Transition效果是瞬时效果，不需要移除条件
                return transitionEffect;
                
            case SkillEffectType.WeakPoint:
                var weakPointEffect = new WeakPointEffect();
                // 设置弱点攻击专用参数
                weakPointEffect.SetParameters(
                    weakPointMarkerPrefab,
                    weakPointRadius,
                    weakPointDamageMultiplier,
                    weakPointRefreshOnHit
                );
                // 弱点效果是持续效果，生命周期由技能管理
                return weakPointEffect;
                
            case SkillEffectType.DropItem:
                var dropItemEffect = new DropItemEffect();
                // 设置掉落物品专用参数
                dropItemEffect.SetDropConfig(dropItemConfig, dropChance, dropRangeConfig);
                // 掉落效果是瞬时效果，不需要移除条件
                return dropItemEffect;
                
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
            case SkillEffectType.Transition:
                return $"Transition: {minTransitionTime:F1}s-{maxTransitionTime:F1}s (门槛:{transitionThreshold:F2})";
            case SkillEffectType.WeakPoint:
                return $"弱点攻击: {weakPointDamageMultiplier:F1}x伤害 (半径:{weakPointRadius:F1})";
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
    Transition,     // Transition 过渡效果
    WeakPoint,      // 弱点攻击效果
    DropItem        // 掉落物品效果
}
