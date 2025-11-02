using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 伤害配置组合 - 实体的完整伤害规则集
/// </summary>
[CreateAssetMenu(fileName = "DamageProfile", menuName = "Game/Damage/Damage Profile")]
public class DamageProfile : ScriptableObject
{
    [Header("配置信息")]
    [Tooltip("配置名称")]
    public string profileName = "未命名配置";
    
    [Tooltip("配置描述")]
    [TextArea(2, 4)]
    public string description = "";
    
    [Header("伤害规则列表")]
    [Tooltip("多个规则组合，按优先级执行")]
    public List<DamageRuleConfig> rules = new List<DamageRuleConfig>();
    
    /// <summary>
    /// 获取匹配的规则
    /// </summary>
    public List<DamageRuleConfig> GetMatchingRules(DamageTriggerType triggerType, string targetTag)
    {
        List<DamageRuleConfig> matchingRules = new List<DamageRuleConfig>();
        
        foreach (var rule in rules)
        {
            if (rule == null) continue;
            if (rule.triggerType != triggerType) continue;
            if (!string.IsNullOrEmpty(rule.targetTag) && rule.targetTag != targetTag) continue;
            
            matchingRules.Add(rule);
        }
        
        // 按优先级排序
        matchingRules.Sort((a, b) => a.priority.CompareTo(b.priority));
        
        return matchingRules;
    }
    
    /// <summary>
    /// 验证所有规则
    /// </summary>
    public bool Validate()
    {
        if (rules == null || rules.Count == 0)
        {
            Debug.LogWarning($"[DamageProfile] {profileName}: 没有配置任何规则");
            return false;
        }
        
        bool allValid = true;
        foreach (var rule in rules)
        {
            if (rule != null && !rule.Validate())
            {
                allValid = false;
            }
        }
        
        return allValid;
    }
}

