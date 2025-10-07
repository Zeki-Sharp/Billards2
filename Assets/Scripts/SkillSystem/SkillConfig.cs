using UnityEngine;

/// <summary>
/// 技能配置 ScriptableObject - 用于在 Inspector 中配置技能
/// 支持可视化配置，替代硬编码的技能定义
/// </summary>
[CreateAssetMenu(fileName = "SkillConfig", menuName = "Game/Skill Config")]
public class SkillConfig : ScriptableObject
{
    [Header("技能基本信息")]
    [Tooltip("技能名称")]
    public string skillName = "碰撞连击";
    
    [Tooltip("技能描述")]
    [TextArea(3, 5)]
    public string description = "碰撞敌人2次后，攻击力提升100%";
    
    [Tooltip("技能图标")]
    public Sprite skillIcon;
    
    [Header("技能组件配置")]
    [Tooltip("触发器配置")]
    public TriggerConfig triggerConfig = new TriggerConfig();
    
    [Tooltip("条件配置")]
    public ConditionConfig conditionConfig = new ConditionConfig();
    
    [Tooltip("效果配置")]
    public SkillEffectConfig effectConfig = new SkillEffectConfig();
    
    [Header("技能属性")]
    [Tooltip("技能类型")]
    public SkillType skillType = SkillType.Passive;
    
    [Tooltip("所需等级")]
    public int requiredLevel = 1;
    
    [Tooltip("是否激活")]
    public bool isActive = true;
    
    /// <summary>
    /// 创建技能实例
    /// </summary>
    public SkillInstance CreateSkillInstance()
    {
        if (!isActive)
        {
            Debug.LogWarning($"技能 {skillName} 未激活");
            return null;
        }
        
        // 创建组件实例
        var trigger = triggerConfig?.CreateTrigger();
        var condition = conditionConfig?.CreateCondition();
        var effect = effectConfig?.CreateEffect();
        
        if (trigger == null || condition == null || effect == null)
        {
            Debug.LogError($"技能 {skillName} 组件创建失败");
            return null;
        }
        
        // 初始化组件
        trigger.Initialize();
        condition.Initialize();
        effect.Initialize();
        
        return new SkillInstance(this, trigger, condition, effect);
    }
    
    /// <summary>
    /// 验证配置是否有效
    /// </summary>
    public bool IsValid()
    {
        return !string.IsNullOrEmpty(skillName) && 
               triggerConfig != null && 
               conditionConfig != null && 
               effectConfig != null;
    }
    
    /// <summary>
    /// 获取调试信息
    /// </summary>
    public string GetDebugInfo()
    {
        return $"技能: {skillName}\n" +
               $"- 触发器: {triggerConfig?.GetDebugInfo()}\n" +
               $"- 条件: {conditionConfig?.GetDebugInfo()}\n" +
               $"- 效果: {effectConfig?.GetDebugInfo()}";
    }
}

/// <summary>
/// 技能类型枚举
/// </summary>
public enum SkillType
{
    Passive,    // 被动技能
    Active,     // 主动技能（暂未实现）
    Ultimate    // 终极技能（暂未实现）
}

/// <summary>
/// 技能实例 - 包含配置和运行时组件
/// </summary>
public class SkillInstance
{
    public SkillConfig config;
    public ITrigger trigger;
    public ICondition condition;
    public IEffect effect;
    
    public SkillInstance(SkillConfig config, ITrigger trigger, ICondition condition, IEffect effect)
    {
        this.config = config;
        this.trigger = trigger;
        this.condition = condition;
        this.effect = effect;
    }
    
    /// <summary>
    /// 重置技能状态
    /// </summary>
    public void Reset()
    {
        trigger?.Reset();
        condition?.Reset();
        effect?.Reset();
    }
    
    /// <summary>
    /// 处理事件
    /// </summary>
    public bool ProcessEvent(object eventData)
    {
        // 第一步：检查触发器是否检测到事件
        bool eventDetected = trigger.CheckEvent(eventData);
        if (!eventDetected)
        {
            return false;
        }
        
        // 第二步：检查条件是否满足
        bool conditionMet = condition.CheckCondition(eventData);
        if (!conditionMet)
        {
            return false;
        }
        
        // 第三步：执行效果
        bool effectExecuted = effect.ExecuteEffect(eventData);
        
        if (effectExecuted)
        {
            Debug.Log($"[SkillInstance] 技能 {config.skillName} 执行成功！");
        }
        
        return effectExecuted;
    }
}
