using UnityEngine;
using Sirenix.OdinInspector;
using System.Collections.Generic;

/// <summary>
/// 属性修改效果配置 - 修改玩家属性（如攻击力、速度等）
/// </summary>
[System.Serializable]
public class StatModifierEffectConfig : EffectBase
{
    /// <summary>
    /// 获取可选的属性名称列表（带中文标签）
    /// </summary>
    private static IEnumerable<ValueDropdownItem<string>> GetAvailableStats()
    {
        return new ValueDropdownList<string>
        {
            { "攻击力", "Damage" },
            { "最大血量", "MaxHealth" },
            { "微调移动速度", "MicroMoveSpeed" },
            { "攻击范围半径", "AreaRadius" }
        };
    }

    [LabelText("目标属性名称")]
    [Tooltip("要修改的属性名称")]
    [ValueDropdown("GetAvailableStats")]
    public string targetStat = "Damage";

    [LabelText("修改倍数")]
    [Tooltip("修改倍数（支持动态值，如固定值、随机值、基于属性等）")]
    [SerializeReference]
    public PropertyGetFloat modifierValue = new ConstantFloat(2f);

    [LabelText("修改器类型")]
    [Tooltip("修改器的计算方式")]
    public StatModifierType modifierType = StatModifierType.PercentMult;

    [LabelText("允许叠加")]
    [Tooltip("是否允许效果叠加（如击杀增加攻击力）")]
    [InfoBox("✓ 允许叠加：每次触发都创建新修改器，支持叠加效果\n✗ 不允许叠加：只创建一次修改器，后续触发跳过", InfoMessageType.Info)]
    public bool allowStacking = true;

    public override IEffect CreateEffect(IEffectRemovalCondition effectRemovalCondition = null)
    {
        var statModifierEffect = new StatModifierEffect();
        
        // 兼容性：如果没有设置 Property，使用默认固定值 2.0
        var modifierProp = modifierValue ?? new ConstantFloat(2f);
        statModifierEffect.SetModifier(targetStat, modifierProp, modifierType);
        statModifierEffect.SetAllowStacking(allowStacking);
        
        // 设置效果移除条件
        if (effectRemovalCondition != null)
        {
            statModifierEffect.SetEffectRemovalCondition(effectRemovalCondition);
        }
        
        return statModifierEffect;
    }

    public override string GetDebugInfo()
    {
        return $"属性修改: {targetStat} x{modifierValue} ({modifierType})";
    }
}

