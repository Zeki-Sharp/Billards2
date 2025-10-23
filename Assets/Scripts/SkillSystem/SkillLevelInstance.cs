using UnityEngine;

/// <summary>
/// 技能等级实例 - 包含等级配置和运行时组件
/// </summary>
public class SkillLevelInstance
{
    public SkillLevelConfig config;
    public string parentSkillName;
    public ITrigger trigger;
    public ICondition condition;
    public IEffect effect;
    
    // 新的分离组件
    public IResetCondition resetCondition;  // 所有技能都有
    public IEffectRemovalCondition effectRemovalCondition; // 只有PropertyEffect有
    
    /// <summary>
    /// 技能等级实例唯一ID
    /// </summary>
    public string InstanceId { get; private set; }
    
    public SkillLevelInstance(SkillLevelConfig config, string parentSkillName, ITrigger trigger, ICondition condition, IEffect effect, 
                        IResetCondition resetCondition, 
                        IEffectRemovalCondition effectRemovalCondition = null)
    {
        this.config = config;
        this.parentSkillName = parentSkillName;
        this.trigger = trigger;
        this.condition = condition;
        this.effect = effect;
        this.resetCondition = resetCondition;
        this.effectRemovalCondition = effectRemovalCondition;
        
        // 生成唯一实例ID
        this.InstanceId = $"{parentSkillName}_Lv{config.level}_{System.Guid.NewGuid()}";
        
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
    /// 重置技能等级状态
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
            Debug.Log($"[技能等级] {parentSkillName} Lv{config.level} 触发成功！");
            
            // 第四步：发布技能执行完毕事件（带实例ID）
            var skillEvent = new SkillExecutedEventData
            {
                SkillName = parentSkillName,
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
            Debug.LogError($"[SkillLevelInstance] ❌ 技能等级执行失败 - 技能: {parentSkillName} Lv{config.level}");
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
