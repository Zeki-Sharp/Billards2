using UnityEngine;

/// <summary>
/// 数值调整效果 - 单模式 + 叠加控制
/// 使用修饰器系统修改玩家的某个属性（如攻击力+50%）
/// 支持临时效果和基于条件的移除
/// </summary>
public class StatModifierEffect : IEffect
{
    public string EffectName => "StatModifierEffect";
    
    private string targetStat = "Damage"; // 默认修改攻击力（使用新命名）
    private float modifierValue = 1.5f;   // 默认+50%
    private StatModifierType modifierType = StatModifierType.PercentMult; // 默认百分比乘算
    private bool canExecute = true;       // 是否允许执行（完全由重置条件控制）
    private PlayerCore targetPlayer;      // 目标玩家
    private PlayerStatsManager statsManager; // 属性管理器
    
    // 叠加控制字段
    private bool allowStacking = true;    // 是否允许叠加
    private bool hasTriggered = false;    // 是否已经触发（用于非叠加效果）
    
    // 修改器管理字段
    private System.Collections.Generic.List<object> appliedModifiers = new System.Collections.Generic.List<object>(); // 应用的修饰器列表
    
    /// <summary>
    /// 是否允许执行（完全由重置条件控制）
    /// </summary>
    public bool CanExecute => canExecute;
    
    /// <summary>
    /// 设置是否允许执行（完全由重置条件控制）
    /// </summary>
    public void SetCanExecute(bool canExecute)
    {
        this.canExecute = canExecute;
    }
    
    /// <summary>
    /// 设置是否允许叠加
    /// </summary>
    /// <param name="allowStacking">是否允许叠加</param>
    public void SetAllowStacking(bool allowStacking)
    {
        this.allowStacking = allowStacking;
        Debug.Log($"[{EffectName}] 设置允许叠加: {allowStacking}");
    }
    
    /// <summary>
    /// 获取是否允许叠加
    /// </summary>
    /// <returns>是否允许叠加</returns>
    public bool GetAllowStacking()
    {
        return allowStacking;
    }
    
    /// <summary>
    /// 设置修改参数
    /// </summary>
    /// <param name="stat">要修改的属性名</param>
    /// <param name="modifier">修改值</param>
    public void SetModifier(string stat, float modifier)
    {
        targetStat = stat;
        modifierValue = modifier;
    }
    
    /// <summary>
    /// 设置修改参数（包含类型）
    /// </summary>
    /// <param name="stat">要修改的属性名</param>
    /// <param name="modifier">修改值</param>
    /// <param name="type">修改器类型</param>
    public void SetModifier(string stat, float modifier, StatModifierType type)
    {
        targetStat = stat;
        modifierValue = modifier;
        modifierType = type;
    }
    
    /// <summary>
    /// 设置效果移除条件（新接口）
    /// </summary>
    /// <param name="condition">效果移除条件</param>
    public void SetEffectRemovalCondition(IEffectRemovalCondition condition)
    {
        effectRemovalCondition = condition;
    }
    
    private IEffectRemovalCondition effectRemovalCondition; // 新的效果移除条件
    
    /// <summary>
    /// 清理已失效的修改器
    /// </summary>
    private void CleanupInvalidModifiers()
    {
        for (int i = appliedModifiers.Count - 1; i >= 0; i--)
        {
            var modifier = appliedModifiers[i];
            bool shouldRemove = false;
            
            // 如果是攻击力修改器，检查是否应该移除
            if (modifier is SkillDamageModifier skillModifier)
            {
                // 简化版本：SkillDamageModifier 总是启用，不需要检查
                // 移除条件由 StatModifierEffect 的 Reset() 方法处理
            }
            // 如果是属性修改器，检查是否还在活跃列表中
            else if (modifier is StatModifier statModifier && statsManager != null)
            {
                if (!statsManager.HasModifier(statModifier))
                {
                    shouldRemove = true;
                }
            }
            
            if (shouldRemove)
            {
                appliedModifiers.RemoveAt(i);
            }
        }
    }
    
    /// <summary>
    /// 初始化效果
    /// </summary>
    public void Initialize()
    {
        // 延迟初始化：不在初始化时查找玩家，而是在执行时动态查找
    }
    
    /// <summary>
    /// 执行效果
    /// </summary>
    /// <param name="eventData">事件数据</param>
    /// <returns>效果是否执行成功</returns>
    public bool ExecuteEffect(object eventData)
    {
        // 只检查执行权限（完全由重置条件控制）
        if (!canExecute)
        {
            return false;
        }
        
        // 叠加控制检查：如果不允许叠加且已经触发，则跳过执行
        if (!allowStacking && hasTriggered)
        {
            Debug.Log($"[{EffectName}] 不允许叠加且已触发，跳过执行");
            return false;
        }
        
        // 动态查找目标玩家
        if (!GetTargetPlayer())
        {
            Debug.LogError($"[{EffectName}] 无法找到目标玩家，无法执行效果");
            return false;
        }
        
        // 清理已失效的修改器
        CleanupInvalidModifiers();
        
        // 统一执行效果逻辑（总是创建新修改器）
        bool result;
        if (targetStat == "Damage")
        {
            result = ExecuteDamageModification();
        }
        else
        {
            result = ExecuteStatModification();
        }
        
        // 更新触发状态
        if (result && !allowStacking)
        {
            hasTriggered = true;
            Debug.Log($"[{EffectName}] 标记已触发，后续不允许叠加");
        }
        
        // 执行成功后，禁止再次执行（由重置条件重新允许）
        if (result)
        {
            canExecute = false;
        }
        
        return result;
    }
    
    /// <summary>
    /// 执行攻击力修改 - 委托给 DamageProcessor
    /// </summary>
    /// <returns>是否执行成功</returns>
    private bool ExecuteDamageModification()
    {
        // 查找 DamageProcessor
        DamageProcessor damageProcessor = DamageProcessor.Instance;
        if (damageProcessor == null)
        {
            Debug.LogError($"[{EffectName}] 未找到 DamageProcessor，无法处理攻击力修改");
            return false;
        }
        
        // 总是创建新的技能伤害修改器（支持叠加）
        string modifierName = $"技能攻击力修改_{targetStat}_{System.Guid.NewGuid().ToString("N").Substring(0, 8)}";
        SkillDamageModifier damageModifier = new SkillDamageModifier(
            modifierName,
            modifierValue,
            modifierType,
            effectRemovalCondition,
            true
        );
        
        // 设置移除回调
        damageModifier.SetOnRemovedCallback(() => {
            // 只重置标记，不删除修改器（因为修改器已经被禁用了）
            if (!allowStacking)
            {
                hasTriggered = false;
                Debug.Log($"[{EffectName}] 修改器被移除，重置触发标记，允许重新触发");
            }
        });
        
        // 注册到 DamageProcessor
        damageProcessor.RegisterDamageModifier(damageModifier);
        
        // 保存引用用于后续移除
        appliedModifiers.Add(damageModifier);
        
        Debug.Log($"[{EffectName}] 创建新的攻击力修改器: {modifierName}, 当前修改器数量: {appliedModifiers.Count}");
        
        // 触发表现效果
        TriggerVisualEffect();
        
        return true;
    }
    
    /// <summary>
    /// 执行其他属性修改 - 使用原来的 StatModifier 方式
    /// </summary>
    /// <returns>是否执行成功</returns>
    private bool ExecuteStatModification()
    {
        if (statsManager == null)
        {
            Debug.LogError($"[{EffectName}] 属性管理器为空，无法执行效果");
            return false;
        }
        
        // 创建修饰器 - 使用配置中的实际类型和值
        StatModifier statModifier = new StatModifier(
            targetStat,                                    // 目标属性
            modifierType,                                  // 从配置读取的类型
            modifierValue,                                 // 从配置读取的值
            this                                           // 来源
        );
        
        // 设置移除条件（使用新接口）
        if (effectRemovalCondition != null)
        {
            statModifier.SetEffectRemovalCondition(effectRemovalCondition);
        }
        
        // 应用修饰器
        statsManager.ApplyModifier(statModifier);
        
        // 保存引用
        appliedModifiers.Add(statModifier);
        
        Debug.Log($"[{EffectName}] 创建新的属性修改器: {targetStat}, 当前修改器数量: {appliedModifiers.Count}");
        
        // 触发表现效果
        TriggerVisualEffect();
        
        return true;
    }
    
    /// <summary>
    /// 触发表现效果
    /// </summary>
    private void TriggerVisualEffect()
    {
        // 触发攻击力提升的表现特效
    }
    
    /// <summary>
    /// 动态获取目标玩家
    /// </summary>
    private bool GetTargetPlayer()
    {
        if (targetPlayer == null)
        {
            targetPlayer = Object.FindFirstObjectByType<PlayerCore>();
            if (targetPlayer != null)
            {
                // 查找属性管理器
                statsManager = targetPlayer.GetComponent<PlayerStatsManager>();
                if (statsManager == null)
                {
                    Debug.LogError($"[{EffectName}] 未找到PlayerStatsManager，无法应用效果");
                    targetPlayer = null;
                    return false;
                }
                
                Debug.Log($"[{EffectName}] 动态找到目标玩家: {targetPlayer.name}");
            }
            else
            {
                Debug.LogWarning($"[{EffectName}] 未找到PlayerCore，可能玩家还未初始化");
                return false;
            }
        }
        
        // 检查玩家是否就绪
        if (!IsPlayerReady(targetPlayer))
        {
            Debug.LogWarning($"[{EffectName}] 玩家未就绪，重置引用并重试");
            targetPlayer = null;
            statsManager = null;
            return false;
        }
        
        return true;
    }
    
    /// <summary>
    /// 检查玩家是否就绪
    /// </summary>
    private bool IsPlayerReady(PlayerCore player)
    {
        return player != null && player.enabled && player.gameObject.activeInHierarchy;
    }
    
    /// <summary>
    /// 移除效果（删除所有修改器，重置 hasTriggered 标志）
    /// 注意：不重置 canExecute，因为它完全由重置条件控制
    /// </summary>
    public void RemoveEffect()
    {
        Debug.Log($"[{EffectName}] 重置效果，删除所有修改器，当前数量: {appliedModifiers.Count}");
        
        // 删除所有应用的修改器
        for (int i = appliedModifiers.Count - 1; i >= 0; i--)
        {
            var modifier = appliedModifiers[i];
            
            // 如果是攻击力修改，从 DamageProcessor 中移除
            if (modifier is SkillDamageModifier skillModifier)
            {
                DamageProcessor damageProcessor = DamageProcessor.Instance;
                if (damageProcessor != null)
                {
                    damageProcessor.UnregisterDamageModifier(skillModifier);
                    Debug.Log($"[{EffectName}] 删除攻击力修改器: {skillModifier.ModifierName}");
                }
            }
            // 如果是其他属性修改，从 PlayerStatsManager 中移除
            else if (modifier is StatModifier statModifier && statsManager != null)
            {
                statsManager.RemoveModifier(statModifier);
                Debug.Log($"[{EffectName}] 删除属性修改器: {statModifier.targetStat}");
            }
        }
        
        // 清空修改器列表
        appliedModifiers.Clear();
        
        // 重置触发状态（效果被移除时重置，允许重新触发）
        hasTriggered = false;
        Debug.Log($"[{EffectName}] 效果被移除，重置触发标记，允许重新触发");
        
        // 注意：不重置 canExecute，因为它完全由重置条件控制
    }
}