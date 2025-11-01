using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 伤害规则配置 - ScriptableObject
/// 定义实体在什么情况下造成伤害
/// 
/// 【核心思想】：
/// - 规则驱动：伤害条件通过配置定义
/// - 组合式设计：多个规则组合成完整的伤害配置
/// - 状态感知：支持 Blackboard 状态查询
/// </summary>
[CreateAssetMenu(fileName = "DamageRule", menuName = "Game/Damage/Damage Rule Config")]
public class DamageRuleConfig : ScriptableObject
{
    [Header("规则基本信息")]
    [Tooltip("规则名称（调试用）")]
    public string ruleName = "未命名规则";
    
    [Tooltip("规则优先级（数字越小越优先）")]
    public int priority = 0;
    
    [Header("触发条件")]
    [Tooltip("触发类型：碰撞、停止、间隔")]
    public DamageTriggerType triggerType = DamageTriggerType.Collision;
    
    [Tooltip("来源标签（攻击者的 Tag）")]
    public string sourceTag = "";
    
    [Tooltip("目标标签（受击者的 Tag）")]
    public string targetTag = "Player";
    
    [Header("状态要求（可选）")]
    [Tooltip("要求攻击者处于特定状态（Blackboard 键名，留空表示无要求）")]
    public string requireSourceState = "";
    
    [Tooltip("要求目标处于特定状态（Blackboard 键名，留空表示无要求）")]
    public string requireTargetState = "";
    
    [Tooltip("要求目标不处于特定状态（Blackboard 键名，留空表示无要求）\n例如：IsTrap（陷阱无敌）、IsInvincible（无敌技能）")]
    public string requireTargetNotState = "";
    
    [Header("速度要求（可选）")]
    [Tooltip("最小速度要求（0 表示无要求）")]
    public float minVelocity = 0f;
    
    [Tooltip("速度倍率（0 表示不使用速度加成）")]
    public float velocityMultiplier = 0f;
    
    [Header("范围配置（Stopped 类型专用）")]
    [Tooltip("攻击范围（仅 Stopped 类型使用，0 表示从 PlayerData 读取）")]
    public float attackRange = 0f;
    
    [Header("伤害计算")]
    [Tooltip("基础伤害")]
    public float baseDamage = 10f;
    
    [Tooltip("伤害倍率")]
    public float damageMultiplier = 1.0f;
    
    [Tooltip("伤害类型")]
    public DamageType damageType = DamageType.Physical;
    
    [Header("目标过滤")]
    [Tooltip("是否影响玩家")]
    public bool affectPlayer = true;
    
    [Tooltip("是否影响敌人")]
    public bool affectEnemy = false;
    
    [Tooltip("是否对自己造成伤害（如撞墙）")]
    public bool selfDamage = false;
    
    [Header("附加效果")]
    [Tooltip("击退力度")]
    public float knockbackForce = 0f;
    
    [Tooltip("眩晕时长")]
    public float stunDuration = 0f;
    
    [Tooltip("是否可被格挡")]
    public bool canBeBlocked = true;
    
    /// <summary>
    /// 验证规则配置
    /// </summary>
    public bool Validate()
    {
        if (string.IsNullOrEmpty(targetTag))
        {
            Debug.LogWarning($"[DamageRuleConfig] {ruleName}: targetTag 未设置");
            return false;
        }
        
        if (baseDamage < 0)
        {
            Debug.LogWarning($"[DamageRuleConfig] {ruleName}: baseDamage 不能为负数");
            return false;
        }
        
        return true;
    }
}

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

