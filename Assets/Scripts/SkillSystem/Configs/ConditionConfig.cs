using UnityEngine;
using Sirenix.OdinInspector;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// 条件配置 - 支持多个条件和逻辑判断
/// 如果没有条件，触发即执行
/// </summary>
[System.Serializable]
public class ConditionConfig
{
    [LabelText("条件逻辑")]
    [Tooltip("多个条件之间的逻辑关系（列表为空时表示无条件，触发即执行）")]
    public ConditionLogicType logicType = ConditionLogicType.And;
    
    [LabelText("条件列表")]
    [Tooltip("条件列表（列表为空时表示无条件，触发即执行）")]
    [ListDrawerSettings(ShowIndexLabels = true)]
    [SerializeReference]
    public List<ConditionBase> conditions = new List<ConditionBase>();
    
    /// <summary>
    /// 创建条件实例
    /// </summary>
    public ICondition CreateCondition()
    {
        // 如果列表为空，返回一个总是返回true的条件（无条件，触发即执行）
        if (conditions.Count == 0)
        {
            return new AlwaysTrueCondition();
        }
        
        // 如果只有一个条件，直接返回该条件
        if (conditions.Count == 1)
        {
            return conditions[0].CreateCondition();
        }
        
        // 如果有多个条件，创建复合条件
        if (conditions.Count > 1)
        {
            var compositeCondition = new CompositeCondition();
            compositeCondition.SetLogicType(logicType);
            
            foreach (var conditionConfig in conditions)
            {
                if (conditionConfig != null)
                {
                    var condition = conditionConfig.CreateCondition();
                    if (condition != null)
                    {
                        compositeCondition.AddCondition(condition);
                    }
                }
            }
            
            return compositeCondition;
        }
        
        // 默认返回总是为true的条件
        return new AlwaysTrueCondition();
    }
    
    /// <summary>
    /// 获取调试信息
    /// </summary>
    public string GetDebugInfo()
    {
        if (conditions.Count == 0)
        {
            return "无条件（触发即执行）";
        }
        
        if (conditions.Count == 1)
        {
            return conditions[0].GetDebugInfo();
        }
        
        string logicText = logicType == ConditionLogicType.And ? "AND" : "OR";
        string conditionTexts = string.Join($" {logicText} ", conditions.Where(c => c != null).Select(c => c.GetDebugInfo()));
        return $"复合条件 ({logicText}): {conditionTexts}";
    }
}

/// <summary>
/// 血量比较类型
/// </summary>
public enum HealthComparisonType
{
    LessThan,    // 小于
    GreaterThan, // 大于
    Equal        // 等于
}

/// <summary>
/// 条件逻辑类型
/// </summary>
public enum ConditionLogicType
{
    And,    // 所有条件都必须满足
    Or      // 任一条件满足即可
}

