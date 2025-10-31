using UnityEngine;

/// <summary>
/// 技能伤害修改器 - 支持双模式：创建/删除模式和启用/禁用模式
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
    public bool IsEnabled => isEnabled;
    
    /// <summary>
    /// 处理伤害修改 - 技能伤害修改
    /// </summary>
    /// <param name="attackData">攻击数据（可修改）</param>
    /// <returns>是否成功处理了伤害修改</returns>
    public bool ProcessDamage(ref AttackData attackData)
    {
        // 检查是否启用
        if (!isEnabled)
        {
            return false;
        }
        
        // 检查是否应该移除
        if (ShouldRemove())
        {
            isEnabled = false;
            if (showDebugLog)
            {
                Debug.Log($"[SkillDamageModifier] {modifierName} 条件满足，自动禁用");
            }
            
            // 通知 StatModifierEffect 重置标记
            onRemoved?.Invoke();
            
            return false;
        }
        
        // 只处理玩家攻击的情况
        if (attackData.Attacker == null || !attackData.Attacker.CompareTag("Player"))
            return false;
        
        // 只处理攻击敌人的情况
        if (attackData.Target == null)
        {
            if (showDebugLog)
            {
                Debug.Log($"[SkillDamageModifier] {modifierName} 攻击目标为空，跳过");
            }
            return false;
        }
        
        // 检查目标是否为敌人
        if (!attackData.Target.CompareTag("Enemy"))
        {
            if (showDebugLog)
            {
                Debug.Log($"[SkillDamageModifier] {modifierName} 目标不是敌人（{attackData.Target.tag}），跳过");
            }
            return false;
        }
        
        // 修改伤害值
        float originalDamage = attackData.Damage;
        
        switch (modifierType)
        {
            case StatModifierType.Add:
                attackData.Damage += modifierValue;
                break;
            case StatModifierType.PercentAdd:
                attackData.Damage *= (1f + modifierValue);
                break;
            case StatModifierType.PercentMult:
                attackData.Damage *= modifierValue;
                break;
            default:
                Debug.LogWarning($"[SkillDamageModifier] {modifierName} 未知的修饰器类型: {modifierType}");
                return false;
        }
        
        if (showDebugLog)
        {
            Debug.Log($"[SkillDamageModifier] {modifierName} 修改伤害: {originalDamage} → {attackData.Damage} (类型: {modifierType}, 值: {modifierValue})");
        }
        
        return true; // 成功处理了伤害修改
    }
    
    #endregion
    
    #region 私有字段
    
    private string modifierName;
    private float modifierValue;
    private StatModifierType modifierType;
    private bool showDebugLog;
    private bool isEnabled = true;                    // 是否启用
    private IEffectRemovalCondition removalCondition; // 移除条件
    private System.Action onRemoved;                  // 移除回调
    
    #endregion
    
    #region 构造函数
    
    /// <summary>
    /// 创建技能伤害修改器
    /// </summary>
    /// <param name="name">修改器名称</param>
    /// <param name="value">修改值</param>
    /// <param name="type">修改类型</param>
    /// <param name="removalCondition">移除条件（可选）</param>
    /// <param name="debugLog">是否显示调试日志</param>
    public SkillDamageModifier(string name, float value, StatModifierType type, IEffectRemovalCondition removalCondition = null, bool debugLog = true)
    {
        this.modifierName = name;
        this.modifierValue = value;
        this.modifierType = type;
        this.removalCondition = removalCondition;
        this.showDebugLog = debugLog;
        this.isEnabled = true;
        
        if (showDebugLog)
        {
            Debug.Log($"[SkillDamageModifier] 创建修改器: {modifierName}, 值: {modifierValue}, 类型: {modifierType}");
        }
    }
    
    #endregion
    
    #region 公共方法
    
    /// <summary>
    /// 设置修改值
    /// </summary>
    /// <param name="value">修改值</param>
    public void SetModifierValue(float value)
    {
        modifierValue = value;
        if (showDebugLog)
        {
            Debug.Log($"[SkillDamageModifier] {modifierName} 设置修改值: {value} (类型: {modifierType})");
        }
    }
    
    /// <summary>
    /// 设置修改类型和值
    /// </summary>
    /// <param name="value">修改值</param>
    /// <param name="type">修改类型</param>
    public void SetModifier(float value, StatModifierType type)
    {
        modifierValue = value;
        modifierType = type;
        if (showDebugLog)
        {
            Debug.Log($"[SkillDamageModifier] {modifierName} 设置修改器: 值={value}, 类型={type}");
        }
    }
    
    /// <summary>
    /// 设置修改器启用状态
    /// </summary>
    /// <param name="enabled">是否启用</param>
    public void SetEnabled(bool enabled)
    {
        isEnabled = enabled;
        if (showDebugLog)
        {
            Debug.Log($"[SkillDamageModifier] {modifierName} 设置启用状态: {enabled}");
        }
    }
    
    /// <summary>
    /// 设置移除回调
    /// </summary>
    /// <param name="callback">移除时调用的回调</param>
    public void SetOnRemovedCallback(System.Action callback)
    {
        onRemoved = callback;
        if (showDebugLog)
        {
            Debug.Log($"[SkillDamageModifier] {modifierName} 设置移除回调");
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
        
        var args = SkillArgs.FromEventData(currentHealth);
        return removalCondition.ShouldRemoveEffect(args);
    }
    
    #endregion
}