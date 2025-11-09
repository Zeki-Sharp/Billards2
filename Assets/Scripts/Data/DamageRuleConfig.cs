using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 攻击范围形状类型
/// </summary>
public enum RangeShapeType
{
    Circle,    // 圆形范围（默认）
    Triangle   // 三角形范围（需要轨迹记录）
}

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
    
    [Tooltip("要求攻击者不处于特定状态（Blackboard 键名，留空表示无要求）\n例如：CanAttack（排除主动攻击），用于反弹伤害")]
    public string requireSourceNotState = "";
    
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
    [Tooltip("攻击范围形状类型")]
    public RangeShapeType rangeShape = RangeShapeType.Circle;
    
    [Tooltip("攻击范围（仅 Stopped 类型使用，0 表示从 PlayerData 读取）")]
    public float attackRange = 0f;
    
    [Header("伤害计算")]
    [Tooltip("基础伤害（0 = 从 PlayerData.attackPower 读取）")]
    public float baseDamage = 10f;
    
    [Tooltip("伤害倍率（应用于基础伤害）")]
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
    /// 获取最终的基础伤害值
    /// 如果 baseDamage = 0，则从 PlayerData.attackPower 读取
    /// </summary>
    public float GetBaseDamage(GameObject source)
    {
        // 如果配置了固定值，直接使用
        if (baseDamage > 0f)
        {
            return baseDamage;
        }
        
        // 否则优先从实时属性系统读取最终攻击力
        PlayerStats playerStats = source?.GetComponent<PlayerStats>();
        if (playerStats == null && source != null && source.transform.parent != null)
        {
            playerStats = source.transform.parent.GetComponent<PlayerStats>();
        }
        
        if (playerStats != null)
        {
            return playerStats.FinalDamage;
        }
        
        // 回退：使用 PlayerBehavior 的 PlayerData（静态基础值）
        var playerBehavior = source?.GetComponent<PlayerBehavior>();
        if (playerBehavior?.PlayerData != null)
        {
            return playerBehavior.PlayerData.attackPower;
        }
        
        if (source != null && source.transform.parent != null)
        {
            playerBehavior = source.transform.parent.GetComponent<PlayerBehavior>();
            if (playerBehavior?.PlayerData != null)
            {
                return playerBehavior.PlayerData.attackPower;
            }
        }
        
        Debug.LogWarning($"[DamageRuleConfig] {ruleName}: 无法获取攻击力，使用默认值 0");
        return 0f;
    }
    
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

