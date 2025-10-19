using UnityEngine;
using Sirenix.OdinInspector;
using System.Collections.Generic;
#if UNITY_EDITOR
using System.Linq;
#endif

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
    [LabelText("技能标签")]
    [Tooltip("技能所属的标签，用于区分通用技能和角色专属技能")]
    [ValueDropdown("GetAvailableTags")]
    public string skillTag = "default";
    
    // 技能图标暂时移除，简化配置界面
    // [BoxGroup("技能基本信息")]
    // [LabelText("技能图标")]
    // [Tooltip("技能图标")]
    // public Sprite skillIcon;
    
    [BoxGroup("触发器配置")]
    [LabelText("触发器配置")]
    [Tooltip("什么时候触发技能（如：击杀敌人、碰撞墙壁等）")]
    [Required]
    public TriggerConfig triggerConfig = new TriggerConfig();
    
    [BoxGroup("条件配置")]
    [LabelText("条件配置")]
    [Tooltip("触发条件（如：生命值满血、击杀数量等）")]
    [ShowIf("@triggerConfig.triggerType != TriggerType.AlwaysTrue")]
    [InfoBox("AlwaysTrue触发器不需要条件配置，会始终触发", InfoMessageType.Info, "@triggerConfig.triggerType == TriggerType.AlwaysTrue")]
    [Required]
    public ConditionConfig conditionConfig = new ConditionConfig();
    
    [BoxGroup("效果配置")]
    [LabelText("效果配置")]
    [Tooltip("产生什么效果（如：伤害提升、恢复生命值等）")]
    [Required]
    public SkillEffectConfig effectConfig = new SkillEffectConfig();
    
    [BoxGroup("重置条件配置")]
    [LabelText("重置条件配置")]
    [Tooltip("什么时候可以再次触发技能（所有技能都需要）")]
    [Required]
    public ResetConditionConfig resetConditionConfig = new ResetConditionConfig();
    
    [BoxGroup("效果移除配置")]
    [LabelText("效果移除配置")]
    [Tooltip("持续效果何时移除（仅持续效果需要配置）")]
    [ShowIf("@effectConfig.effectType == SkillEffectType.StatModifier")]
    [InfoBox("只有持续效果（属性修改）才需要配置移除条件", InfoMessageType.Info, "@effectConfig.effectType == SkillEffectType.StatModifier")]
    public EffectRemovalConfig effectRemovalConfig = new EffectRemovalConfig();
    
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
    public virtual SkillInstance CreateSkillInstance()
    {
        if (!isActive)
        {
            Debug.LogWarning($"技能 {skillName} 未激活");
            return null;
        }
        
        // 创建组件实例
        var trigger = triggerConfig?.CreateTrigger();
        var condition = conditionConfig?.CreateCondition();
        var resetCondition = resetConditionConfig?.CreateResetCondition();
        
        // 根据效果类型创建相应的移除条件
        IEffectRemovalCondition effectRemovalCondition = null;
        if (IsPropertyEffect())
        {
            effectRemovalCondition = effectRemovalConfig?.CreateEffectRemovalCondition();
        }
        
        var effect = effectConfig?.CreateEffect(effectRemovalCondition);
        
        
        if (trigger == null || condition == null || effect == null || resetCondition == null)
        {
            Debug.LogError($"技能 {skillName} 基础组件创建失败");
            return null;
        }
        
        // 持续效果需要效果移除条件
        if (IsPropertyEffect() && effectRemovalCondition == null)
        {
            Debug.LogError($"技能 {skillName} 缺少效果移除配置");
            return null;
        }
        
        var skillInstance = new SkillInstance(this, trigger, condition, effect, resetCondition, effectRemovalCondition);
        return skillInstance;
    }
    
    /// <summary>
    /// 判断是否为持续效果（PropertyEffect）
    /// </summary>
    private bool IsPropertyEffect()
    {
        return effectConfig?.effectType == SkillEffectType.StatModifier;
    }
    
    /// <summary>
    /// 验证配置是否有效
    /// </summary>
    public virtual bool IsValid()
    {
        bool basicValid = !string.IsNullOrEmpty(skillName) && 
                         triggerConfig != null && 
                         conditionConfig != null && 
                         effectConfig != null;
        
        if (!basicValid) return false;
        
        // 检查重置条件配置
        bool resetConditionValid = resetConditionConfig != null;
        
        // 检查效果移除配置（仅持续效果需要）
        bool effectRemovalValid = true;
        if (IsPropertyEffect())
        {
            effectRemovalValid = effectRemovalConfig != null;
        }
        
        return resetConditionValid && effectRemovalValid;
    }
    
    /// <summary>
    /// 获取调试信息
    /// </summary>
    public virtual string GetDebugInfo()
    {
        string info = $"技能: {skillName}\n" +
                     $"- 触发器: {triggerConfig?.GetDebugInfo()}\n" +
                     $"- 条件: {conditionConfig?.GetDebugInfo()}\n" +
                     $"- 效果: {effectConfig?.GetDebugInfo()}\n" +
                     $"- 重置条件: {resetConditionConfig?.GetDebugInfo()}";
        
        if (IsPropertyEffect())
        {
            info += $"\n- 效果移除: {effectRemovalConfig?.GetDebugInfo()}";
        }
        
        return info;
    }
    
#if UNITY_EDITOR
    /// <summary>
    /// 获取所有可用的技能标签
    /// </summary>
    private IEnumerable<string> GetAvailableTags()
    {
        var tags = new List<string>();
        
        // 添加固定标签
        tags.Add("default");
        tags.Add("common");
        
        // 尝试从 Resources 加载角色选择数据
        var characterSelectionData = UnityEngine.Resources.Load<CharacterSelectionData>("Data/CharacterSelectionData");
        if (characterSelectionData != null && characterSelectionData.availableCharacters != null)
        {
            // 添加所有角色名称
            foreach (var character in characterSelectionData.availableCharacters)
            {
                if (character != null && !string.IsNullOrEmpty(character.playerName))
                {
                    tags.Add(character.playerName);
                }
            }
        }
        else
        {
            // 如果无法加载角色选择数据，尝试从 Resources/Data/Player 目录加载所有 PlayerData
            var allPlayerData = UnityEngine.Resources.LoadAll<PlayerData>("Data/Player");
            if (allPlayerData != null && allPlayerData.Length > 0)
            {
                foreach (var playerData in allPlayerData)
                {
                    if (playerData != null && !string.IsNullOrEmpty(playerData.playerName))
                    {
                        tags.Add(playerData.playerName);
                    }
                }
            }
        }
        
        return tags.Distinct().OrderBy(t => t); // 去重并排序
    }
#endif
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
    
    // 新的分离组件
    public IResetCondition resetCondition;  // 所有技能都有
    public IEffectRemovalCondition effectRemovalCondition; // 只有PropertyEffect有
    
    /// <summary>
    /// 技能实例唯一ID
    /// </summary>
    public string InstanceId { get; private set; }
    
    public SkillInstance(SkillConfig config, ITrigger trigger, ICondition condition, IEffect effect, 
                        IResetCondition resetCondition, 
                        IEffectRemovalCondition effectRemovalCondition = null)
    {
        this.config = config;
        this.trigger = trigger;
        this.condition = condition;
        this.effect = effect;
        this.resetCondition = resetCondition;
        this.effectRemovalCondition = effectRemovalCondition;
        
        // 生成唯一实例ID
        this.InstanceId = $"{config.skillName}_{System.Guid.NewGuid()}";
        
        // 初始化所有组件
        this.trigger?.Initialize();
        this.condition?.Initialize();
        this.effect?.Initialize();
        this.resetCondition?.Initialize();
        this.effectRemovalCondition?.Initialize();
        
        // 设置重置条件的目标技能实例ID和依赖组件
        this.resetCondition?.SetTargetSkillInstanceId(this.InstanceId);
        
        // 如果是值比较重置条件，设置依赖组件
        if (this.resetCondition is ValueComparisonResetCondition valueComparisonResetCondition)
        {
            valueComparisonResetCondition.SetDependencies(this.condition, this.effect);
        }
    }
    
    /// <summary>
    /// 重置技能状态
    /// </summary>
    public void Reset()
    {
        trigger?.Reset();
        condition?.Reset();
        effect?.Reset();
        resetCondition?.Reset();
        effectRemovalCondition?.Reset();
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
            Debug.Log($"[技能] {config.skillName} 触发成功！");
            
            // 第四步：发布技能执行完毕事件（带实例ID）
            var skillEvent = new SkillExecutedEventData
            {
                SkillName = config.skillName,
                SkillInstanceId = this.InstanceId,
                OriginalEventData = eventData,
                Success = true,
                Timestamp = Time.time
            };
            
            // 发布事件，让重置条件响应
            PublishSkillExecutedEvent(skillEvent);
        }
        else
        {
            Debug.LogError($"[SkillInstance] ❌ 技能执行失败 - 技能: {config.skillName}");
        }
        
        return effectExecuted;
    }
    
    /// <summary>
    /// 处理技能执行完毕事件
    /// </summary>
    /// <param name="eventData">技能执行完毕事件数据</param>
    public void HandleSkillExecutedEvent(object eventData)
    {
        if (eventData is SkillExecutedEventData skillEvent)
        {
            // 只处理自己的事件
            if (skillEvent.SkillInstanceId == this.InstanceId)
            {
                // 立即重置条件响应技能执行完毕事件
                if (resetCondition?.ShouldReset(eventData) == true) {
                    condition.Reset();         // 重置触发条件
                    effect.SetCanExecute(true); // 重新允许执行
                }
            }
        }
    }
    
    /// <summary>
    /// 处理回合结束事件
    /// </summary>
    /// <param name="eventData">回合结束事件数据</param>
    public void HandlePhaseEndEvent(object eventData)
    {
        // 重置条件满足时：重置触发条件和 canExecute
        if (resetCondition != null)
        {
            // 检查重置条件是否需要特定类型的事件数据
            bool shouldReset = false;
            
            // 如果是值比较重置条件，需要提供正确的数据
            if (resetCondition is ValueComparisonResetCondition valueComparisonResetCondition)
            {
                // 检查事件是否与数据提取器类型相关
                if (valueComparisonResetCondition.IsEventRelevant(eventData))
                {
                    shouldReset = resetCondition.ShouldReset(eventData);
                }
            }
            else
            {
                // 其他类型的重置条件直接检查
                shouldReset = resetCondition.ShouldReset(eventData);
            }
            
            if (shouldReset)
            {
                condition.Reset();         // 重置触发条件
                effect.SetCanExecute(true); // 重新允许执行
            }
        }
        
        // 移除条件满足时：只移除效果
        if (effectRemovalCondition != null)
        {
            bool shouldRemove = effectRemovalCondition.ShouldRemoveEffect(eventData);
            if (shouldRemove)
            {
                effect.Reset();  // 移除效果（不影响 canExecute）
            }
        }
    }
    
    /// <summary>
    /// 发布技能执行完毕事件
    /// </summary>
    /// <param name="skillEvent">技能执行完毕事件数据</param>
    private void PublishSkillExecutedEvent(SkillExecutedEventData skillEvent)
    {
        // 发布事件，让重置条件响应
        HandleSkillExecutedEvent(skillEvent);
    }
}
