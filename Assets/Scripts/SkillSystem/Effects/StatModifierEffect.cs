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
    private bool isApplied = false;       // 是否已应用
    private PlayerCore targetPlayer;      // 目标玩家
    private PlayerStatsManager statsManager; // 属性管理器
    private StatModifier appliedModifier; // 应用的修饰器
    
    /// <summary>
    /// 设置修改参数
    /// </summary>
    /// <param name="stat">要修改的属性名</param>
    /// <param name="modifier">修改值（倍数）</param>
    public void SetModifier(string stat, float modifier)
    {
        targetStat = stat;
        modifierValue = modifier;
        Debug.Log($"[{EffectName}] 设置修改参数: {targetStat} * {modifierValue}");
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
        if (isApplied && appliedModifier != null && statsManager != null)
        {
            // 检查修饰器是否还在活跃列表中
            if (!statsManager.HasModifier(appliedModifier))
            {
                Debug.Log($"[{EffectName}] 检测到修饰器已被移除，重置效果状态");
                isApplied = false;
                appliedModifier = null;
            }
        }
    }
    
    /// <summary>
    /// 初始化效果
    /// </summary>
    public void Initialize()
    {
        // 查找目标玩家
        targetPlayer = Object.FindFirstObjectByType<PlayerCore>();
        if (targetPlayer == null)
        {
            Debug.LogError($"[{EffectName}] 未找到PlayerCore，无法应用效果");
            return;
        }
        
        // 查找属性管理器
        statsManager = targetPlayer.GetComponent<PlayerStatsManager>();
        if (statsManager == null)
        {
            Debug.LogError($"[{EffectName}] 未找到PlayerStatsManager，无法应用效果");
            return;
        }
        
        Debug.Log($"[{EffectName}] 初始化完成，目标玩家: {targetPlayer.name}");
    }
    
    /// <summary>
    /// 执行效果
    /// </summary>
    /// <param name="eventData">事件数据</param>
    /// <returns>效果是否执行成功</returns>
    public bool ExecuteEffect(object eventData)
    {
        if (targetPlayer == null || statsManager == null)
        {
            Debug.LogError($"[{EffectName}] 目标玩家或属性管理器为空，无法执行效果");
            return false;
        }
        
        // 检查修饰器是否仍然存在
        CheckModifierStatus();
        
        if (isApplied)
        {
            Debug.Log($"[{EffectName}] 效果已应用，跳过重复执行");
            return true;
        }
        
        // 创建修饰器
        appliedModifier = new StatModifier(
            targetStat,                                    // 目标属性
            StatModifierType.PercentAdd,                  // 百分比增加类型
            modifierValue - 1f,                           // 如果modifierValue是1.5，则Value是0.5 (50%增加)
            this                                           // 来源
        );
        
        // 设置移除条件（使用新接口）
        if (effectRemovalCondition != null)
        {
            appliedModifier.SetEffectRemovalCondition(effectRemovalCondition);
        }
        
        // 应用修饰器
        statsManager.ApplyModifier(appliedModifier);
        
        // 获取修改前后的值用于日志
        float finalValue = statsManager.GetFinalStat(targetStat);
        float baseValue = statsManager.GetBaseStat(targetStat);
        
        Debug.Log($"[{EffectName}] 属性修改成功: {targetStat} {baseValue} -> {finalValue} (x{modifierValue})");
        
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
    /// 重置效果状态
    /// </summary>
    public void Reset()
    {
        // 移除应用的修饰器
        if (isApplied && appliedModifier != null && statsManager != null)
        {
            statsManager.RemoveModifier(appliedModifier);
            Debug.Log($"[{EffectName}] 移除修饰器: {appliedModifier.GetDebugInfo()}");
        }
        
        isApplied = false;
        appliedModifier = null;
        Debug.Log($"[{EffectName}] 效果重置完成");
    }
}
