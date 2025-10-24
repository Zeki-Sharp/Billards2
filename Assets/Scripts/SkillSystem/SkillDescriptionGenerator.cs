using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// 技能描述生成器 - 根据技能配置动态生成描述文字
/// 支持多等级系统，显示格式：等级1显示 x，等级>1显示 x(y)
/// 其中 x 为当前等级数值，y 为前一等级数值
/// </summary>
public static class SkillDescriptionGenerator
{
    /// <summary>
    /// 智能格式化数值 - 整数不显示小数点，有小数则显示
    /// </summary>
    /// <param name="value">要格式化的数值</param>
    /// <returns>格式化后的字符串</returns>
    private static string FormatNumber(float value)
    {
        // 如果是整数，不显示小数点
        if (Mathf.Approximately(value, Mathf.Round(value)))
        {
            return Mathf.RoundToInt(value).ToString();
        }
        // 有小数，显示小数（最多2位）
        return value.ToString("0.##");
    }
    
    /// <summary>
    /// 为指定等级的技能生成动态描述
    /// </summary>
    /// <param name="skill">技能配置</param>
    /// <param name="currentLevel">当前等级</param>
    /// <returns>动态生成的描述文字</returns>
    public static string GenerateDescription(SkillConfig skill, int currentLevel)
    {
        if (skill == null)
            return "无效技能";

        // 获取当前等级配置
        var currentLevelConfig = skill.GetLevelConfig(currentLevel);
        if (currentLevelConfig == null)
        {
            Debug.LogWarning($"技能 {skill.skillName} 没有找到等级 {currentLevel} 的配置");
            return "无效等级";
        }

        // 获取前一等级配置（用于数值对比）
        SkillLevelConfig previousLevelConfig = null;
        if (currentLevel > 1)
        {
            previousLevelConfig = skill.GetLevelConfig(currentLevel - 1);
        }

        // 获取描述模板
        string template = GetDescriptionTemplate(currentLevelConfig);
        
        // 提取数值（带对比）
        object[] values = ExtractDescriptionValues(currentLevelConfig, previousLevelConfig);
        
        // 格式化描述
        try
        {
            return string.Format(template, values);
        }
        catch (System.FormatException)
        {
            Debug.LogWarning($"技能 {skill.skillName} 等级 {currentLevel} 描述模板格式化失败: {template}");
            return currentLevelConfig.levelDescription; // 回退到等级描述
        }
    }

    /// <summary>
    /// 根据技能特征获取描述模板
    /// </summary>
    private static string GetDescriptionTemplate(SkillLevelConfig levelConfig)
    {
        // 击杀提升伤害技能
        if (IsKillBoostDamageSkill(levelConfig))
        {
            return "一局内，每击杀{0}个敌人后{1}";
        }
        
        // 碰撞连击技能
        if (IsCollisionComboSkill(levelConfig))
        {
            return "碰撞{0}次后{1}";
        }
        
        // 碰撞治疗技能
        if (IsCollisionHealSkill(levelConfig))
        {
            return "碰撞{0}{1}次后，恢复{2}点生命值";
        }
        
        // 治疗技能
        if (IsHealSkill(levelConfig))
        {
            return "恢复{0}点生命值";
        }
        
        // 血量条件技能
        if (IsHealthConditionSkill(levelConfig))
        {
            return "生命值{0}时{1}";
        }
        
        // 掉落物品技能
        if (IsDropItemSkill(levelConfig))
        {
            return "杀死{0}个敌人后掉落{1}";
        }
        
        // 弱点攻击技能
        if (IsWeakPointSkill(levelConfig))
        {
            return "在敌人周围生成一个弱点，攻击弱点{0}";
        }
        
        // Transition 技能
        if (IsTransitionSkill(levelConfig))
        {
            return "蓄力后进入过渡状态，持续时间{0}秒";
        }
        
        // 默认模板（根据技能类型动态生成）
        return GenerateDefaultTemplate(levelConfig);
    }

    /// <summary>
    /// 从技能配置中提取描述所需的数值（带等级对比）
    /// </summary>
    private static object[] ExtractDescriptionValues(SkillLevelConfig currentLevel, SkillLevelConfig previousLevel)
    {
        var values = new List<object>();

        // 击杀提升伤害技能
        if (IsKillBoostDamageSkill(currentLevel))
        {
            values.Add(GetRequiredCountWithComparison(currentLevel, previousLevel));      // {0} - 需要击杀数量
            values.Add(GetStatModifierDescriptionWithComparison(currentLevel, previousLevel)); // {1} - 伤害提升描述
        }
        // 碰撞连击技能
        else if (IsCollisionComboSkill(currentLevel))
        {
            values.Add(GetRequiredCountWithComparison(currentLevel, previousLevel));      // {0} - 需要碰撞次数
            values.Add(GetStatModifierDescriptionWithComparison(currentLevel, previousLevel)); // {1} - 攻击力提升描述
        }
        // 碰撞治疗技能
        else if (IsCollisionHealSkill(currentLevel))
        {
            values.Add(GetCollisionTarget(currentLevel));    // {0} - 碰撞目标
            values.Add(GetRequiredCountWithComparison(currentLevel, previousLevel));      // {1} - 需要碰撞次数
            values.Add(GetHealAmountWithComparison(currentLevel, previousLevel));        // {2} - 治疗量
        }
        // 治疗技能
        else if (IsHealSkill(currentLevel))
        {
            values.Add(GetHealAmountWithComparison(currentLevel, previousLevel));         // {0} - 治疗量
        }
        // 血量条件技能
        else if (IsHealthConditionSkill(currentLevel))
        {
            values.Add(GetHealthCondition(currentLevel));    // {0} - 血量条件
            values.Add(GetEffectDescriptionWithComparison(currentLevel, previousLevel)); // {1} - 效果描述
        }
        // 掉落物品技能
        else if (IsDropItemSkill(currentLevel))
        {
            values.Add(GetRequiredCountWithComparison(currentLevel, previousLevel));      // {0} - 需要击杀数量
            values.Add(GetDropItemDescription(currentLevel)); // {1} - 掉落物品描述
        }
        // 弱点攻击技能
        else if (IsWeakPointSkill(currentLevel))
        {
            values.Add(GetWeakPointDamageDescriptionWithComparison(currentLevel, previousLevel)); // {0} - 伤害描述
        }
        // Transition 技能
        else if (IsTransitionSkill(currentLevel))
        {
            values.Add(GetTransitionTimeDescriptionWithComparison(currentLevel, previousLevel)); // {0} - 时间描述
        }
        // 默认提取
        else
        {
            values.AddRange(ExtractDefaultValues(currentLevel, previousLevel));
        }

        return values.ToArray();
    }

    #region 技能类型判断

    /// <summary>
    /// 判断是否为击杀提升伤害技能
    /// </summary>
    private static bool IsKillBoostDamageSkill(SkillLevelConfig levelConfig)
    {
        return levelConfig.triggerConfig.triggerType == TriggerType.Kill &&
               levelConfig.effectConfig.effectType == SkillEffectType.StatModifier &&
               levelConfig.effectConfig.targetStat.ToLower().Contains("damage");
    }

    /// <summary>
    /// 判断是否为碰撞连击技能
    /// </summary>
    private static bool IsCollisionComboSkill(SkillLevelConfig levelConfig)
    {
        return levelConfig.triggerConfig.triggerType == TriggerType.Collision &&
               levelConfig.effectConfig.effectType == SkillEffectType.StatModifier;
    }

    /// <summary>
    /// 判断是否为碰撞治疗技能
    /// </summary>
    private static bool IsCollisionHealSkill(SkillLevelConfig levelConfig)
    {
        return levelConfig.triggerConfig.triggerType == TriggerType.Collision &&
               levelConfig.effectConfig.effectType == SkillEffectType.Heal;
    }

    /// <summary>
    /// 判断是否为治疗技能
    /// </summary>
    private static bool IsHealSkill(SkillLevelConfig levelConfig)
    {
        return levelConfig.effectConfig.effectType == SkillEffectType.Heal;
    }

    /// <summary>
    /// 判断是否为血量条件技能
    /// </summary>
    private static bool IsHealthConditionSkill(SkillLevelConfig levelConfig)
    {
        return levelConfig.conditionConfig.conditions.Any(c => 
            c.conditionType == ConditionType.ValueComparison &&
            c.dataExtractorType == DataExtractorType.Health);
    }

    /// <summary>
    /// 判断是否为掉落物品技能
    /// </summary>
    private static bool IsDropItemSkill(SkillLevelConfig levelConfig)
    {
        return levelConfig.effectConfig.effectType == SkillEffectType.DropItem;
    }

    /// <summary>
    /// 判断是否为弱点攻击技能
    /// </summary>
    private static bool IsWeakPointSkill(SkillLevelConfig levelConfig)
    {
        return levelConfig.effectConfig.effectType == SkillEffectType.WeakPoint;
    }

    /// <summary>
    /// 判断是否为 Transition 技能
    /// </summary>
    private static bool IsTransitionSkill(SkillLevelConfig levelConfig)
    {
        return levelConfig.effectConfig.effectType == SkillEffectType.Transition;
    }

    #endregion

    #region 数值提取方法（带对比）

    /// <summary>
    /// 获取需要达到的计数（带对比）
    /// </summary>
    private static string GetRequiredCountWithComparison(SkillLevelConfig current, SkillLevelConfig previous)
    {
        int currentCount = GetRequiredCount(current);
        
        if (previous != null)
        {
            int previousCount = GetRequiredCount(previous);
            if (currentCount != previousCount)
            {
                return $"{currentCount}({previousCount})";
            }
        }
        
        return currentCount.ToString();
    }

    /// <summary>
    /// 获取治疗量（带对比）
    /// </summary>
    private static string GetHealAmountWithComparison(SkillLevelConfig current, SkillLevelConfig previous)
    {
        float currentAmount = current.effectConfig.healAmount;
        
        if (previous != null)
        {
            float previousAmount = previous.effectConfig.healAmount;
            if (!Mathf.Approximately(currentAmount, previousAmount))
            {
                return $"{FormatNumber(currentAmount)}({FormatNumber(previousAmount)})";
            }
        }
        
        return FormatNumber(currentAmount);
    }

    /// <summary>
    /// 获取属性修改器描述（带对比）
    /// </summary>
    private static string GetStatModifierDescriptionWithComparison(SkillLevelConfig current, SkillLevelConfig previous)
    {
        string statName = GetStatDisplayName(current.effectConfig.targetStat);
        float currentValue = current.effectConfig.modifierValue;
        
        if (previous != null && previous.effectConfig.effectType == SkillEffectType.StatModifier)
        {
            float previousValue = previous.effectConfig.modifierValue;
            
            switch (current.effectConfig.modifierType)
            {
                case StatModifierType.PercentMult:
                    int currentPercentage = Mathf.RoundToInt(currentValue * 100);
                    int previousPercentage = Mathf.RoundToInt(previousValue * 100);
                    
                    if (currentPercentage != previousPercentage)
                    {
                        return $"{statName}提升为{currentPercentage}%({previousPercentage}%)";
                    }
                    return $"{statName}提升为{currentPercentage}%";
                    
                case StatModifierType.PercentAdd:
                    int currentAddPercentage = Mathf.RoundToInt(currentValue * 100);
                    int previousAddPercentage = Mathf.RoundToInt(previousValue * 100);
                    
                    if (currentAddPercentage != previousAddPercentage)
                    {
                        return $"{statName}提升{currentAddPercentage}%({previousAddPercentage}%)";
                    }
                    return $"{statName}提升{currentAddPercentage}%";
                    
                case StatModifierType.Add:
                    if (!Mathf.Approximately(currentValue, previousValue))
                    {
                        return $"{statName}提升{FormatNumber(currentValue)}({FormatNumber(previousValue)})";
                    }
                    return $"{statName}提升{FormatNumber(currentValue)}";
            }
        }
        
        // 没有前一等级时，使用标准格式
        return GetStatModifierDescription(current);
    }

    /// <summary>
    /// 获取效果描述（带对比）
    /// </summary>
    private static string GetEffectDescriptionWithComparison(SkillLevelConfig current, SkillLevelConfig previous)
    {
        switch (current.effectConfig.effectType)
        {
            case SkillEffectType.StatModifier:
                return GetStatModifierDescriptionWithComparison(current, previous);
            case SkillEffectType.Heal:
                return $"恢复{GetHealAmountWithComparison(current, previous)}点生命值";
            case SkillEffectType.DropItem:
                return GetDropItemDescription(current);
            default:
                return "产生效果";
        }
    }

    /// <summary>
    /// 获取弱点攻击伤害描述（带对比）
    /// </summary>
    private static string GetWeakPointDamageDescriptionWithComparison(SkillLevelConfig current, SkillLevelConfig previous)
    {
        float currentMultiplier = current.effectConfig.weakPointDamageMultiplier;
        
        if (previous != null && previous.effectConfig.effectType == SkillEffectType.WeakPoint)
        {
            float previousMultiplier = previous.effectConfig.weakPointDamageMultiplier;
            
            if (!Mathf.Approximately(currentMultiplier, previousMultiplier))
            {
                return $"造成{FormatNumber(currentMultiplier)}({FormatNumber(previousMultiplier)})倍伤害";
            }
        }
        
        return $"造成{FormatNumber(currentMultiplier)}倍伤害";
    }

    /// <summary>
    /// 获取 Transition 时间描述（带对比）
    /// </summary>
    private static string GetTransitionTimeDescriptionWithComparison(SkillLevelConfig current, SkillLevelConfig previous)
    {
        float currentMin = current.effectConfig.minTransitionTime;
        float currentMax = current.effectConfig.maxTransitionTime;
        
        if (previous != null && previous.effectConfig.effectType == SkillEffectType.Transition)
        {
            float previousMin = previous.effectConfig.minTransitionTime;
            float previousMax = previous.effectConfig.maxTransitionTime;
            
            bool minChanged = !Mathf.Approximately(currentMin, previousMin);
            bool maxChanged = !Mathf.Approximately(currentMax, previousMax);
            
            if (minChanged && maxChanged)
            {
                return $"{FormatNumber(currentMin)}-{FormatNumber(currentMax)}({FormatNumber(previousMin)}-{FormatNumber(previousMax)})";
            }
            else if (minChanged)
            {
                return $"{FormatNumber(currentMin)}({FormatNumber(previousMin)})-{FormatNumber(currentMax)}";
            }
            else if (maxChanged)
            {
                return $"{FormatNumber(currentMin)}-{FormatNumber(currentMax)}({FormatNumber(previousMax)})";
            }
        }
        
        return $"{FormatNumber(currentMin)}-{FormatNumber(currentMax)}";
    }

    #endregion

    #region 基础数值提取方法

    /// <summary>
    /// 获取碰撞目标描述
    /// </summary>
    private static string GetCollisionTarget(SkillLevelConfig levelConfig)
    {
        if (levelConfig.triggerConfig.triggerType == TriggerType.Collision)
        {
            return GetTargetDisplayName(levelConfig.triggerConfig.targetTag);
        }
        
        return "目标";
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
                return targetTag;
        }
    }

    /// <summary>
    /// 获取需要达到的计数
    /// </summary>
    private static int GetRequiredCount(SkillLevelConfig levelConfig)
    {
        var countCondition = levelConfig.conditionConfig.conditions
            .FirstOrDefault(c => c.conditionType == ConditionType.Count);
        
        return countCondition?.requiredCount ?? 1;
    }

    /// <summary>
    /// 获取血量条件描述
    /// </summary>
    private static string GetHealthCondition(SkillLevelConfig levelConfig)
    {
        var healthCondition = levelConfig.conditionConfig.conditions
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
        int percentage = Mathf.RoundToInt(value * 100);
        return $"{percentage}%";
    }

    /// <summary>
    /// 获取掉落物品描述
    /// </summary>
    private static string GetDropItemDescription(SkillLevelConfig levelConfig)
    {
        if (levelConfig.effectConfig.dropItemConfig == null)
        {
            return "未知物品";
        }
        
        string itemName = levelConfig.effectConfig.dropItemConfig.itemName;
        
        // 根据物品类型生成描述
        if (levelConfig.effectConfig.dropItemConfig.itemSkill != null)
        {
            var itemSkill = levelConfig.effectConfig.dropItemConfig.itemSkill;
            var firstLevel = itemSkill.skillLevels.FirstOrDefault();
            
            if (firstLevel != null)
            {
                if (firstLevel.effectConfig.effectType == SkillEffectType.Heal)
                {
                    return $"恢复{FormatNumber(firstLevel.effectConfig.healAmount)}点生命值的{itemName}";
                }
                else if (firstLevel.effectConfig.effectType == SkillEffectType.StatModifier)
                {
                    string statName = GetStatDisplayName(firstLevel.effectConfig.targetStat);
                    return $"{statName}提升的{itemName}";
                }
            }
        }
        
        return itemName;
    }
    
    /// <summary>
    /// 获取属性修改器描述（不带对比）
    /// </summary>
    private static string GetStatModifierDescription(SkillLevelConfig levelConfig)
    {
        string statName = GetStatDisplayName(levelConfig.effectConfig.targetStat);
        float value = levelConfig.effectConfig.modifierValue;
        
        switch (levelConfig.effectConfig.modifierType)
        {
            case StatModifierType.PercentMult:
                int percentage = Mathf.RoundToInt(value * 100);
                return $"{statName}提升为{percentage}%";
                
            case StatModifierType.PercentAdd:
                int addPercentage = Mathf.RoundToInt(value * 100);
                return $"{statName}提升{addPercentage}%";
                
            case StatModifierType.Add:
                return $"{statName}提升{FormatNumber(value)}";
                
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
            case "arearadius":
                return "攻击范围";
            default:
                return statName;
        }
    }

    #endregion

    #region 默认处理

    /// <summary>
    /// 生成默认模板
    /// </summary>
    private static string GenerateDefaultTemplate(SkillLevelConfig levelConfig)
    {
        switch (levelConfig.triggerConfig.triggerType)
        {
            case TriggerType.Kill:
                return "击杀敌人后{0}";
            case TriggerType.Collision:
                return "碰撞后{0}";
            case TriggerType.AlwaysTrue:
                return "{0}";
            case TriggerType.MovingEnd:
                return "球停止后{0}";
            default:
                return levelConfig.levelDescription;
        }
    }

    /// <summary>
    /// 提取默认数值（带对比）
    /// </summary>
    private static object[] ExtractDefaultValues(SkillLevelConfig current, SkillLevelConfig previous)
    {
        var values = new List<object>();
        
        values.Add(GetEffectDescriptionWithComparison(current, previous));
        
        return values.ToArray();
    }

    #endregion
}
