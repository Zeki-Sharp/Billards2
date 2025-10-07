using UnityEngine;
using Sirenix.OdinInspector;

/// <summary>
/// 技能配置 ScriptableObject - 用于在 Inspector 中配置技能
/// 支持可视化配置，替代硬编码的技能定义
/// </summary>
[CreateAssetMenu(fileName = "SkillConfig", menuName = "Game/Skill Config")]
public class SkillConfig : ScriptableObject
{
    [BoxGroup("技能基本信息")]
    [LabelText("技能名称")]
    [Tooltip("技能名称")]
    public string skillName = "碰撞连击";
    
    [BoxGroup("技能基本信息")]
    [LabelText("技能描述")]
    [Tooltip("技能描述")]
    [TextArea(3, 5)]
    public string description = "碰撞敌人2次后，攻击力提升100%";
    
    [BoxGroup("技能基本信息")]
    [LabelText("技能图标")]
    [Tooltip("技能图标")]
    public Sprite skillIcon;
    
    [BoxGroup("技能组件配置")]
    [LabelText("触发器配置")]
    [Tooltip("触发器配置")]
    [Required]
    public TriggerConfig triggerConfig = new TriggerConfig();
    
    [BoxGroup("技能组件配置")]
    [LabelText("条件配置")]
    [Tooltip("条件配置")]
    [Required]
    public ConditionConfig conditionConfig = new ConditionConfig();
    
    [BoxGroup("技能组件配置")]
    [LabelText("效果配置")]
    [Tooltip("效果配置")]
    [Required]
    public SkillEffectConfig effectConfig = new SkillEffectConfig();
    
    [BoxGroup("技能组件配置")]
    [LabelText("移除条件配置")]
    [Tooltip("移除条件配置")]
    [Required]
    public RemovalConditionConfig removalConditionConfig = new RemovalConditionConfig();
    
    [BoxGroup("技能属性")]
    [LabelText("技能类型")]
    [Tooltip("技能类型")]
    public SkillType skillType = SkillType.Passive;
    
    [BoxGroup("技能属性")]
    [LabelText("所需等级")]
    [Tooltip("所需等级")]
    [MinValue(1)]
    public int requiredLevel = 1;
    
    [BoxGroup("技能属性")]
    [LabelText("是否激活")]
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
        var removalCondition = removalConditionConfig?.CreateRemovalCondition();
        var effect = effectConfig?.CreateEffect(removalCondition);
        
        if (trigger == null || condition == null || effect == null || removalCondition == null)
        {
            Debug.LogError($"技能 {skillName} 组件创建失败");
            return null;
        }
        
        // 初始化组件
        trigger.Initialize();
        condition.Initialize();
        effect.Initialize();
        removalCondition.Initialize();
        
        // 如果是反向条件检查，设置原始条件
        if (removalCondition is InverseConditionCheck inverseCheck)
        {
            inverseCheck.SetOriginalCondition(condition);
        }
        
        return new SkillInstance(this, trigger, condition, effect, removalCondition);
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
    public IRemovalCondition removalCondition;
    
    public SkillInstance(SkillConfig config, ITrigger trigger, ICondition condition, IEffect effect, IRemovalCondition removalCondition)
    {
        this.config = config;
        this.trigger = trigger;
        this.condition = condition;
        this.effect = effect;
        this.removalCondition = removalCondition;
        
        // 初始化所有组件
        this.trigger?.Initialize();
        this.condition?.Initialize();
        this.effect?.Initialize();
        this.removalCondition?.Initialize();
    }
    
    /// <summary>
    /// 重置技能状态
    /// </summary>
    public void Reset()
    {
        trigger?.Reset();
        condition?.Reset();
        effect?.Reset();
        removalCondition?.Reset();
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
