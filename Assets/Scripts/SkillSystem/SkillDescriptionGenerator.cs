using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// 技能描述生成器 - 根据技能配置动态生成描述文字
/// 支持模板占位符替换，如 "杀死{0}个敌人后伤害上升{1}"
/// </summary>
public static class SkillDescriptionGenerator
{
    /// <summary>
    /// 为技能生成动态描述
    /// </summary>
    /// <param name="skill">技能配置</param>
    /// <returns>动态生成的描述文字</returns>
    public static string GenerateDescription(SkillConfig skill)
    {
        if (skill == null)
            return "无效技能";

        // 获取描述模板
        string template = GetDescriptionTemplate(skill);
        
        // 提取数值
        object[] values = ExtractDescriptionValues(skill);
        
        // 格式化描述
        try
        {
            return string.Format(template, values);
        }
        catch (System.FormatException)
        {
            Debug.LogWarning($"技能 {skill.skillName} 描述模板格式化失败: {template}");
            return skill.description; // 回退到原始描述
        }
    }

    /// <summary>
    /// 根据技能特征获取描述模板
    /// </summary>
    private static string GetDescriptionTemplate(SkillConfig skill)
    {
        // 击杀提升伤害技能
        if (IsKillBoostDamageSkill(skill))
        {
            return "杀死{0}个敌人后{1}";
        }
        
        // 碰撞连击技能
        if (IsCollisionComboSkill(skill))
        {
            return "碰撞{0}次后{1}";
        }
        
        // 碰撞治疗技能
        if (IsCollisionHealSkill(skill))
        {
            return "碰撞{0}{1}次后，恢复{2}点生命值";
        }
        
        // 治疗技能
        if (IsHealSkill(skill))
        {
            return "恢复{0}点生命值";
        }
        
        // 血量条件技能
        if (IsHealthConditionSkill(skill))
        {
            return "生命值{0}时{1}";
        }
        
        // 掉落物品技能
        if (IsDropItemSkill(skill))
        {
            return "杀死{0}个敌人后掉落{1}";
        }
        
        // 弱点攻击技能
        if (IsWeakPointSkill(skill))
        {
            return "在敌人周围生成一个弱点，攻击弱点{0}";
        }
        
        // 默认模板（根据技能类型动态生成）
        return GenerateDefaultTemplate(skill);
    }

    /// <summary>
    /// 从技能配置中提取描述所需的数值
    /// </summary>
    private static object[] ExtractDescriptionValues(SkillConfig skill)
    {
        var values = new List<object>();

        // 击杀提升伤害技能
        if (IsKillBoostDamageSkill(skill))
        {
            values.Add(GetRequiredCount(skill));      // {0} - 需要击杀数量
            values.Add(GetStatModifierDescription(skill)); // {1} - 伤害提升描述
        }
        // 碰撞连击技能
        else if (IsCollisionComboSkill(skill))
        {
            values.Add(GetRequiredCount(skill));      // {0} - 需要碰撞次数
            values.Add(GetStatModifierDescription(skill)); // {1} - 攻击力提升描述
        }
        // 碰撞治疗技能
        else if (IsCollisionHealSkill(skill))
        {
            values.Add(GetCollisionTarget(skill));    // {0} - 碰撞目标
            values.Add(GetRequiredCount(skill));      // {1} - 需要碰撞次数
            values.Add(GetHealAmount(skill));        // {2} - 治疗量
        }
        // 治疗技能
        else if (IsHealSkill(skill))
        {
            values.Add(GetHealAmount(skill));         // {0} - 治疗量
        }
        // 血量条件技能
        else if (IsHealthConditionSkill(skill))
        {
            values.Add(GetHealthCondition(skill));    // {0} - 血量条件
            values.Add(GetEffectDescription(skill)); // {1} - 效果描述
        }
        // 掉落物品技能
        else if (IsDropItemSkill(skill))
        {
            values.Add(GetRequiredCount(skill));      // {0} - 需要击杀数量
            values.Add(GetDropItemDescription(skill)); // {1} - 掉落物品描述
        }
        // 弱点攻击技能
        else if (IsWeakPointSkill(skill))
        {
            values.Add(GetWeakPointDamageDescription(skill)); // {0} - 伤害描述
        }
        // 默认提取
        else
        {
            values.AddRange(ExtractDefaultValues(skill));
        }

        return values.ToArray();
    }

    #region 技能类型判断

    /// <summary>
    /// 判断是否为击杀提升伤害技能
    /// </summary>
    private static bool IsKillBoostDamageSkill(SkillConfig skill)
    {
        return skill.triggerConfig.triggerType == TriggerType.Kill &&
               skill.effectConfig.effectType == SkillEffectType.StatModifier &&
               skill.effectConfig.targetStat.ToLower().Contains("damage");
    }

    /// <summary>
    /// 判断是否为碰撞连击技能
    /// </summary>
    private static bool IsCollisionComboSkill(SkillConfig skill)
    {
        return skill.triggerConfig.triggerType == TriggerType.Collision &&
               skill.effectConfig.effectType == SkillEffectType.StatModifier;
    }

    /// <summary>
    /// 判断是否为碰撞治疗技能
    /// </summary>
    private static bool IsCollisionHealSkill(SkillConfig skill)
    {
        return skill.triggerConfig.triggerType == TriggerType.Collision &&
               skill.effectConfig.effectType == SkillEffectType.Heal;
    }

    /// <summary>
    /// 判断是否为治疗技能
    /// </summary>
    private static bool IsHealSkill(SkillConfig skill)
    {
        return skill.effectConfig.effectType == SkillEffectType.Heal;
    }

    /// <summary>
    /// 判断是否为血量条件技能
    /// </summary>
    private static bool IsHealthConditionSkill(SkillConfig skill)
    {
        return skill.conditionConfig.conditions.Any(c => 
            c.conditionType == ConditionType.ValueComparison &&
            c.dataExtractorType == DataExtractorType.Health);
    }

    /// <summary>
    /// 判断是否为掉落物品技能
    /// </summary>
    private static bool IsDropItemSkill(SkillConfig skill)
    {
        return skill.effectConfig.effectType == SkillEffectType.DropItem;
    }

    /// <summary>
    /// 判断是否为弱点攻击技能
    /// </summary>
    private static bool IsWeakPointSkill(SkillConfig skill)
    {
        return skill.effectConfig.effectType == SkillEffectType.WeakPoint;
    }

    #endregion

    #region 数值提取方法

    /// <summary>
    /// 获取碰撞目标描述
    /// </summary>
    private static string GetCollisionTarget(SkillConfig skill)
    {
        // 从触发器配置中获取碰撞目标标签
        if (skill.triggerConfig.triggerType == TriggerType.Collision)
        {
            return GetTargetDisplayName(skill.triggerConfig.targetTag);
        }
        
        return "目标"; // 默认描述
    }
    
    /// <summary>
    /// 获取目标显示名称
    /// </summary>
    private static string GetTargetDisplayName(string targetTag)
    {
        switch (targetTag.ToLower())
        {
            case "wall":
                return "墙壁";
            case "enemy":
                return "敌人";
            case "player":
                return "玩家";
            case "obstacle":
                return "障碍物";
            default:
                return targetTag; // 如果没有映射，返回原名
        }
    }

    /// <summary>
    /// 获取需要达到的计数
    /// </summary>
    private static int GetRequiredCount(SkillConfig skill)
    {
        var countCondition = skill.conditionConfig.conditions
            .FirstOrDefault(c => c.conditionType == ConditionType.Count);
        
        return countCondition?.requiredCount ?? 1;
    }

    /// <summary>
    /// 获取修改器数值
    /// </summary>
    private static float GetModifierValue(SkillConfig skill)
    {
        return skill.effectConfig.modifierValue;
    }

    /// <summary>
    /// 获取修改器百分比（转换为百分比显示）
    /// </summary>
    private static int GetModifierPercentage(SkillConfig skill)
    {
        float value = skill.effectConfig.modifierValue;
        if (skill.effectConfig.modifierType == StatModifierType.PercentMult)
        {
            return Mathf.RoundToInt((value - 1) * 100); // 2.0 -> 100%
        }
        else if (skill.effectConfig.modifierType == StatModifierType.Add)
        {
            return Mathf.RoundToInt(value); // 直接显示数值
        }
        return Mathf.RoundToInt(value);
    }

    /// <summary>
    /// 获取治疗量
    /// </summary>
    private static float GetHealAmount(SkillConfig skill)
    {
        return skill.effectConfig.healAmount;
    }

    /// <summary>
    /// 获取血量条件描述
    /// </summary>
    private static string GetHealthCondition(SkillConfig skill)
    {
        var healthCondition = skill.conditionConfig.conditions
            .FirstOrDefault(c => c.conditionType == ConditionType.ValueComparison &&
                                c.dataExtractorType == DataExtractorType.Health);
        
        if (healthCondition == null) return "未知";

        switch (healthCondition.comparisonType)
        {
            case ComparisonType.LessThan:
                return $"低于{FormatHealthPercentage(healthCondition.targetValue)}";
            case ComparisonType.GreaterThan:
                return $"高于{FormatHealthPercentage(healthCondition.targetValue)}";
            case ComparisonType.GreaterThanOrEqual:
                return $"不低于{FormatHealthPercentage(healthCondition.targetValue)}";
            case ComparisonType.LessThanOrEqual:
                return $"不高于{FormatHealthPercentage(healthCondition.targetValue)}";
            case ComparisonType.Equal:
                return $"等于{FormatHealthPercentage(healthCondition.targetValue)}";
            case ComparisonType.InRange:
                return $"在{FormatHealthPercentage(healthCondition.minValue)}-{FormatHealthPercentage(healthCondition.maxValue)}之间";
            default:
                return "未知条件";
        }
    }
    
    /// <summary>
    /// 格式化生命值百分比
    /// </summary>
    private static string FormatHealthPercentage(float value)
    {
        // 生命值通常在0-1之间，转换为百分比
        int percentage = Mathf.RoundToInt(value * 100);
        return $"{percentage}%";
    }

    /// <summary>
    /// 获取效果描述
    /// </summary>
    private static string GetEffectDescription(SkillConfig skill)
    {
        switch (skill.effectConfig.effectType)
        {
            case SkillEffectType.StatModifier:
                return GetStatModifierDescription(skill);
            case SkillEffectType.Heal:
                return $"恢复{skill.effectConfig.healAmount}点生命值";
            case SkillEffectType.DropItem:
                return GetDropItemDescription(skill);
            default:
                return "产生效果";
        }
    }
    
    /// <summary>
    /// 获取掉落物品描述
    /// </summary>
    private static string GetDropItemDescription(SkillConfig skill)
    {
        if (skill.effectConfig.dropItemConfig == null)
        {
            return "未知物品";
        }
        
        string itemName = skill.effectConfig.dropItemConfig.itemName;
        
        // 根据物品类型生成描述
        if (skill.effectConfig.dropItemConfig.itemSkill != null)
        {
            // 如果物品有关联技能，显示技能效果
            var itemSkill = skill.effectConfig.dropItemConfig.itemSkill;
            if (itemSkill.effectConfig.effectType == SkillEffectType.Heal)
            {
                return $"恢复{itemSkill.effectConfig.healAmount}点生命值的{itemName}";
            }
            else if (itemSkill.effectConfig.effectType == SkillEffectType.StatModifier)
            {
                string statName = GetStatDisplayName(itemSkill.effectConfig.targetStat);
                return $"{statName}提升的{itemName}";
            }
        }
        
        // 默认显示物品名称
        return itemName;
    }
    
    /// <summary>
    /// 获取弱点攻击伤害描述
    /// </summary>
    private static string GetWeakPointDamageDescription(SkillConfig skill)
    {
        float multiplier = skill.effectConfig.weakPointDamageMultiplier;
        if (multiplier > 1.0f)
        {
            return $"造成{multiplier:F1}倍伤害";
        }
        else if (multiplier < 1.0f)
        {
            return $"造成{multiplier:F1}倍伤害";
        }
        else
        {
            return "造成正常伤害";
        }
    }
    
    /// <summary>
    /// 获取弱点攻击伤害倍率
    /// </summary>
    private static string GetWeakPointDamageMultiplier(SkillConfig skill)
    {
        float multiplier = skill.effectConfig.weakPointDamageMultiplier;
        return $"{multiplier:F1}";
    }
    
    /// <summary>
    /// 获取弱点攻击判定半径
    /// </summary>
    private static string GetWeakPointRadius(SkillConfig skill)
    {
        float radius = skill.effectConfig.weakPointRadius;
        return $"{radius:F1}";
    }
    
    /// <summary>
    /// 获取属性修改器描述
    /// </summary>
    private static string GetStatModifierDescription(SkillConfig skill)
    {
        string statName = GetStatDisplayName(skill.effectConfig.targetStat);
        float value = skill.effectConfig.modifierValue;
        
        switch (skill.effectConfig.modifierType)
        {
            case StatModifierType.PercentMult:
                // PercentMult: 1.5 -> 150%
                int percentage = Mathf.RoundToInt(value * 100);
                return $"{statName}提升为{percentage}%";
                
            case StatModifierType.PercentAdd:
                // PercentAdd: 0.5 -> +50%
                int addPercentage = Mathf.RoundToInt(value * 100);
                return $"{statName}提升{addPercentage}%";
                
            case StatModifierType.Add:
                // Add: 直接数值
                return $"{statName}提升{value}";
                
            default:
                return $"{statName}提升";
        }
    }
    
    /// <summary>
    /// 获取属性显示名称
    /// </summary>
    private static string GetStatDisplayName(string statName)
    {
        switch (statName.ToLower())
        {
            case "damage":
                return "伤害";
            case "health":
                return "生命值";
            case "speed":
                return "速度";
            case "defense":
                return "防御";
            case "attack":
                return "攻击力";
            default:
                return statName; // 如果没有映射，返回原名
        }
    }

    #endregion

    #region 默认处理

    /// <summary>
    /// 生成默认模板
    /// </summary>
    private static string GenerateDefaultTemplate(SkillConfig skill)
    {
        // 根据触发器类型生成基础模板
        switch (skill.triggerConfig.triggerType)
        {
            case TriggerType.Kill:
                return "击杀敌人后{0}";
            case TriggerType.Collision:
                return "碰撞后{0}";
            case TriggerType.AlwaysTrue:
                return "{0}";
            default:
                return skill.description; // 回退到原始描述
        }
    }

    /// <summary>
    /// 提取默认数值
    /// </summary>
    private static object[] ExtractDefaultValues(SkillConfig skill)
    {
        var values = new List<object>();
        
        // 添加效果描述
        values.Add(GetEffectDescription(skill));
        
        return values.ToArray();
    }

    #endregion
}
