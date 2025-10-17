using UnityEngine;

/// <summary>
/// 技能伤害修改器 - 通用的技能伤害修改实现
/// 所有需要修改攻击力的技能都使用这个通用修改器
/// </summary>
public class SkillDamageModifier : IDamageModifier
{
    #region IDamageModifier 实现
    
    /// <summary>
    /// 修改器优先级 - 普通优先级，在弱点判定后执行
    /// </summary>
    public EventPriority Priority => EventPriority.Normal;
    
    /// <summary>
    /// 修改器名称
    /// </summary>
    public string ModifierName => modifierName;
    
    /// <summary>
    /// 是否启用此修改器
    /// </summary>
    public bool IsEnabled => isEnabled && !ShouldRemove();
    
    /// <summary>
    /// 处理伤害修改 - 技能伤害修改
    /// </summary>
    /// <param name="attackData">攻击数据（可修改）</param>
    /// <returns>是否成功处理了伤害修改</returns>
    public bool ProcessDamage(ref AttackData attackData)
    {
        if (!IsEnabled)
            return false;
        
        // 只处理玩家攻击的情况
        if (attackData.Attacker == null || !attackData.Attacker.CompareTag("Player"))
            return false;
        
        // 检查移除条件
        if (ShouldRemove())
        {
            if (showDebugLog)
            {
                Debug.Log($"[SkillDamageModifier] {modifierName} 满足移除条件，跳过处理");
            }
            return false;
        }
        
        // 修改伤害值
        float originalDamage = attackData.Damage;
        attackData.Damage *= damageMultiplier;
        
        if (showDebugLog)
        {
            Debug.Log($"[SkillDamageModifier] {modifierName} 修改伤害: {originalDamage} → {attackData.Damage}");
        }
        
        return true; // 成功处理了伤害修改
    }
    
    #endregion
    
    #region 私有字段
    
    private string modifierName;
    private float damageMultiplier;
    private bool isEnabled;
    private IEffectRemovalCondition removalCondition;
    private bool showDebugLog;
    
    #endregion
    
    #region 构造函数
    
    /// <summary>
    /// 创建技能伤害修改器
    /// </summary>
    /// <param name="name">修改器名称</param>
    /// <param name="multiplier">伤害倍率</param>
    /// <param name="removalCondition">移除条件</param>
    /// <param name="debugLog">是否显示调试日志</param>
    public SkillDamageModifier(string name, float multiplier, IEffectRemovalCondition removalCondition = null, bool debugLog = true)
    {
        this.modifierName = name;
        this.damageMultiplier = multiplier;
        this.removalCondition = removalCondition;
        this.isEnabled = true;
        this.showDebugLog = debugLog;
        
        if (showDebugLog)
        {
            Debug.Log($"[SkillDamageModifier] 创建修改器: {modifierName}, 倍率: {damageMultiplier}");
        }
    }
    
    #endregion
    
    #region 私有方法
    
    /// <summary>
    /// 检查是否应该移除修改器
    /// </summary>
    /// <returns>是否应该移除</returns>
    private bool ShouldRemove()
    {
        if (removalCondition == null)
            return false;
        
        // 直接读取玩家当前血量，构造 HealthStateData
        PlayerCore playerCore = Object.FindFirstObjectByType<PlayerCore>();
        if (playerCore == null)
        {
            if (showDebugLog)
            {
                Debug.LogWarning($"[SkillDamageModifier] {modifierName} 未找到 PlayerCore，无法检查移除条件");
            }
            return false;
        }
        
        HealthStateData currentHealth = new HealthStateData
        {
            CurrentHealth = playerCore.GetCurrentHealth(),
            MaxHealth = playerCore.GetMaxHealth()
        };
        
        if (showDebugLog)
        {
            Debug.Log($"[SkillDamageModifier] {modifierName} 检查移除条件 - 血量: {currentHealth.CurrentHealth}/{currentHealth.MaxHealth} ({currentHealth.HealthPercentage:P1})");
        }
        
        return removalCondition.ShouldRemoveEffect(currentHealth);
    }
    
    #endregion
    
    #region 公共方法
    
    /// <summary>
    /// 设置伤害倍率
    /// </summary>
    /// <param name="multiplier">伤害倍率</param>
    public void SetDamageMultiplier(float multiplier)
    {
        damageMultiplier = multiplier;
        if (showDebugLog)
        {
            Debug.Log($"[SkillDamageModifier] {modifierName} 设置伤害倍率: {multiplier}");
        }
    }
    
    /// <summary>
    /// 启用/禁用修改器
    /// </summary>
    /// <param name="enabled">是否启用</param>
    public void SetEnabled(bool enabled)
    {
        isEnabled = enabled;
        if (showDebugLog)
        {
            Debug.Log($"[SkillDamageModifier] {modifierName} {(enabled ? "启用" : "禁用")}修改器");
        }
    }
    
    /// <summary>
    /// 手动移除修改器
    /// </summary>
    public void Remove()
    {
        isEnabled = false;
        if (showDebugLog)
        {
            Debug.Log($"[SkillDamageModifier] {modifierName} 手动移除修改器");
        }
    }
    
    #endregion
}
