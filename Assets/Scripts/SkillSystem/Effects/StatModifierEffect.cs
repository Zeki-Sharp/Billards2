using UnityEngine;

/// <summary>
/// 数值调整效果 - 技能系统第一阶段最小验证
/// 使用修饰器系统修改玩家的某个属性（如攻击力+50%）
/// 支持临时效果和基于条件的移除
/// </summary>
public class StatModifierEffect : IEffect
{
    public string EffectName => "StatModifierEffect";
    
    private string targetStat = "Damage"; // 默认修改攻击力（使用新命名）
    private float modifierValue = 1.5f;   // 默认+50%
    private StatModifierType modifierType = StatModifierType.PercentMult; // 默认百分比乘算
    private bool isApplied = false;       // 效果是否已应用（由移除条件控制）
    private bool canExecute = true;       // 是否允许执行（完全由重置条件控制）
    private PlayerCore targetPlayer;      // 目标玩家
    private PlayerStatsManager statsManager; // 属性管理器
    private object appliedModifier; // 应用的修饰器（可能是 StatModifier 或 SkillDamageModifier）
    
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
        Debug.Log($"[{EffectName}] SetCanExecute: {canExecute}");
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
        Debug.Log($"[{EffectName}] 设置修改参数: {targetStat} {modifierValue} ({modifierType})");
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
        Debug.Log($"[{EffectName}] 设置修改参数: {targetStat} {modifierValue} ({modifierType})");
    }
    
    /// <summary>
    /// 设置效果移除条件（新接口）
    /// </summary>
    /// <param name="condition">效果移除条件</param>
    public void SetEffectRemovalCondition(IEffectRemovalCondition condition)
    {
        effectRemovalCondition = condition;
        Debug.Log($"[{EffectName}] 设置效果移除条件: {condition?.ConditionName}");
    }
    
    private IEffectRemovalCondition effectRemovalCondition; // 新的效果移除条件
    
    /// <summary>
    /// 检查修饰器是否仍然存在
    /// </summary>
    private void CheckModifierStatus()
    {
        if (isApplied && appliedModifier != null)
        {
            // 如果是攻击力修改器，检查是否应该移除
            if (appliedModifier is SkillDamageModifier skillModifier)
            {
                if (!skillModifier.IsEnabled)
                {
                    Debug.Log($"[{EffectName}] 检测到技能伤害修改器已被禁用，重置效果状态");
                    isApplied = false;
                    appliedModifier = null;
                }
            }
            // 如果是属性修改器，检查是否还在活跃列表中
            else if (appliedModifier is StatModifier statModifier && statsManager != null)
            {
                if (!statsManager.HasModifier(statModifier))
                {
                    Debug.Log($"[{EffectName}] 检测到属性修饰器已被移除，重置效果状态");
                    isApplied = false;
                    appliedModifier = null;
                }
            }
        }
    }
    
    /// <summary>
    /// 初始化效果
    /// </summary>
    public void Initialize()
    {
        // 延迟初始化：不在初始化时查找玩家，而是在执行时动态查找
        Debug.Log($"[{EffectName}] 初始化完成，将在执行时动态查找玩家");
    }
    
    /// <summary>
    /// 执行效果
    /// </summary>
    /// <param name="eventData">事件数据</param>
    /// <returns>效果是否执行成功</returns>
    public bool ExecuteEffect(object eventData)
    {
        Debug.Log($"[{EffectName}] ExecuteEffect 被调用，目标属性: {targetStat}, canExecute: {canExecute}, isApplied: {isApplied}");
        
        // 只检查执行权限（完全由重置条件控制）
        if (!canExecute)
        {
            Debug.Log($"[{EffectName}] 不允许执行效果（canExecute=false）");
            return false;
        }
        
        // 动态查找目标玩家
        if (!GetTargetPlayer())
        {
            Debug.LogError($"[{EffectName}] 无法找到目标玩家，无法执行效果");
            return false;
        }
        
        // 检查修饰器是否仍然存在
        CheckModifierStatus();
        
        // 执行效果逻辑（不管是否已应用，只要canExecute为true就执行）
        bool result;
        
        // 特殊处理：如果目标属性是攻击力，委托给 DamageProcessor 处理
        if (targetStat == "Damage")
        {
            result = ExecuteDamageModification();
        }
        else
        {
            // 其他属性使用原来的 StatModifier 方式
            result = ExecuteStatModification();
        }
        
        // 执行成功后，禁止再次执行（由重置条件重新允许）
        if (result)
        {
            canExecute = false;
            Debug.Log($"[{EffectName}] 效果执行成功，设置 canExecute=false");
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
        
        // 创建技能伤害修改器
        string modifierName = $"技能攻击力修改_{targetStat}";
        SkillDamageModifier damageModifier = new SkillDamageModifier(
            modifierName,
            modifierValue,
            modifierType,
            effectRemovalCondition,
            true
        );
        
        // 注册到 DamageProcessor
        damageProcessor.RegisterDamageModifier(damageModifier);
        
        // 保存引用用于后续移除
        appliedModifier = damageModifier;
        
        Debug.Log($"[{EffectName}] 攻击力修改委托给 DamageProcessor: {targetStat} {modifierType} {modifierValue}");
        
        isApplied = true;
        
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
        
        // 获取修改前后的值用于日志
        float finalValue = statsManager.GetFinalStat(targetStat);
        float baseValue = statsManager.GetBaseStat(targetStat);
        
        Debug.Log($"[{EffectName}] 属性修改成功: {targetStat} {baseValue} -> {finalValue} (x{modifierValue})");
        
        // 保存引用
        appliedModifier = statModifier;
        isApplied = true;
        
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
        // 使用现有的特效类型，比如 "Hit" 或自定义的升级特效
        // GameEventBus.PublishEffectEvent(
        //     "Hit",  // 使用现有的特效类型，或者可以扩展 EffectManager 支持新的特效类型
        //     targetPlayer.transform.position, 
        //     Vector3.up, 
        //     targetPlayer.gameObject, 
        //     "Player"
        // );
        
        Debug.Log($"[{EffectName}] 触发表现效果: 攻击力提升特效 at {targetPlayer.transform.position}");
        
        // TODO: 后续可以在 EffectManager 中添加专门的技能特效类型
        // 如: "SkillUpgrade", "StatBoost" 等，用于技能相关的表现效果
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
    /// 重置效果状态（由移除条件调用）
    /// </summary>
    public void Reset()
    {
        Debug.Log($"[{EffectName}] 🔄 重置前状态 - isApplied: {isApplied}, canExecute: {canExecute}, appliedModifier: {appliedModifier != null}, 时间: {Time.time:F2}");
        
        if (isApplied && appliedModifier != null)
        {
            // 如果是攻击力修改，从 DamageProcessor 中移除
            if (targetStat == "Damage" && appliedModifier is SkillDamageModifier skillModifier)
            {
                DamageProcessor damageProcessor = DamageProcessor.Instance;
                if (damageProcessor != null)
                {
                    damageProcessor.UnregisterDamageModifier(skillModifier);
                    Debug.Log($"[{EffectName}] 从 DamageProcessor 移除攻击力修改器");
                }
            }
            // 如果是其他属性修改，从 PlayerStatsManager 中移除
            else if (statsManager != null && appliedModifier is StatModifier statModifier)
            {
                statsManager.RemoveModifier(statModifier);
                Debug.Log($"[{EffectName}] 移除属性修饰器: {statModifier.GetDebugInfo()}");
            }
        }
        
        isApplied = false;
        appliedModifier = null;
        // 注意：不重置 canExecute，因为它完全由重置条件控制
        Debug.Log($"[{EffectName}] ✅ 重置后状态 - isApplied: {isApplied}, canExecute: {canExecute}, appliedModifier: {appliedModifier != null}, 时间: {Time.time:F2}");
    }
}
