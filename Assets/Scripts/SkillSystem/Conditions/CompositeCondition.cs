using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// 复合条件 - 支持多个条件的逻辑组合
/// 支持 AND 和 OR 逻辑
/// </summary>
public class CompositeCondition : ICondition
{
    public string ConditionName => "CompositeCondition";
    
    private List<ICondition> conditions = new List<ICondition>();
    private ConditionLogicType logicType = ConditionLogicType.And;
    
    /// <summary>
    /// 设置逻辑类型
    /// </summary>
    /// <param name="logicType">逻辑类型</param>
    public void SetLogicType(ConditionLogicType logicType)
    {
        this.logicType = logicType;
    }
    
    /// <summary>
    /// 添加条件
    /// </summary>
    /// <param name="condition">要添加的条件</param>
    public void AddCondition(ICondition condition)
    {
        if (condition != null)
        {
            conditions.Add(condition);
        }
    }
    
    /// <summary>
    /// 初始化条件
    /// </summary>
    public void Initialize()
    {
        foreach (var condition in conditions)
        {
            condition?.Initialize();
        }
    }
    
    /// <summary>
    /// 检查条件是否满足
    /// </summary>
    /// <param name="eventData">事件数据</param>
    /// <returns>根据逻辑类型判断条件是否满足</returns>
    public bool CheckCondition(object eventData)
    {
        if (conditions.Count == 0)
        {
            Debug.LogWarning($"[{ConditionName}] 没有条件，默认返回true");
            return true;
        }
        
        bool result;
        
        if (logicType == ConditionLogicType.And)
        {
            // AND 逻辑：所有条件都必须满足
            result = conditions.All(condition => condition.CheckCondition(eventData));
        }
        else
        {
            // OR 逻辑：任一条件满足即可
            result = conditions.Any(condition => condition.CheckCondition(eventData));
        }
        
        return result;
    }
    
    /// <summary>
    /// 重置条件状态
    /// </summary>
    public void Reset()
    {
        foreach (var condition in conditions)
        {
            condition?.Reset();
        }
    }
    
    /// <summary>
    /// 回合结束时重置条件状态
    /// </summary>
    public void ResetOnPhaseEnd()
    {
        foreach (var condition in conditions)
        {
            condition?.ResetOnPhaseEnd();
        }
    }
}
