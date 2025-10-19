# 技能系统事件驱动重构计划 v2.0

## 问题背景

当前技能系统存在以下问题：

1. **执行流程混合**：重置条件和移除条件被错误地放在执行流程中
2. **事件隔离缺失**：技能执行完毕事件会被所有技能实例接收，导致事件混淆
3. **条件匹配混乱**：重置条件无法准确匹配到对应的技能实例

## 核心问题分析

### 1. 事件混淆问题

#### 问题场景
```
玩家携带技能A和技能B：
- 技能A执行完毕 → 发布全局 SkillExecutedEvent
- 技能B的 ImmediateResetCondition 收到事件 → 错误重置技能B
- 技能A的 ImmediateResetCondition 可能没有响应
```

#### 根本原因
- **事件广播范围过广**：全局事件被所有技能实例接收
- **缺乏事件过滤机制**：重置条件无法区分事件来源
- **条件匹配逻辑缺失**：无法确保重置条件只响应对应技能的事件

### 2. 当前设计的错误

#### 执行流程混合问题
```csharp
// ❌ 当前错误设计：在执行流程中检查事件驱动的重置/移除
public bool ProcessEvent(object eventData)
{
    // 触发器检测 → 条件检查 → 效果执行
    
    // 第二步：检查重置条件 ❌ 这里检查的是事件驱动的重置！
    if (resetCondition?.ShouldReset(eventData) == true) {
        // 这里检查的可能是"回合结束"事件，但我们在执行流程中！
        return false; // 阻止执行！
    }
}
```

#### 事件隔离缺失问题
```csharp
// ❌ 问题：全局事件被所有技能实例接收
public void PublishSkillExecutedEvent(SkillExecutedEventData eventData)
{
    // 所有技能实例都会收到这个事件
    foreach (var skillInstance in allSkillInstances) {
        skillInstance.HandleSkillExecutedEvent(eventData);
    }
}
```

## 设计目标

### 主要目标
1. **事件隔离**：确保每个技能实例只响应自己的事件
2. **条件匹配**：重置条件只能重置对应的技能实例
3. **流程分离**：执行流程和事件处理流程完全分离
4. **语义清晰**：重置条件和移除条件的职责明确

### 次要目标
1. **扩展性好**：可以轻松添加新的事件类型
2. **可维护性高**：每个事件处理逻辑独立且清晰
3. **可测试性强**：可以独立测试每个事件处理逻辑

## 解决方案设计

### 方案1：事件过滤机制（推荐）

#### 1.1 事件数据结构增强
```csharp
public class SkillExecutedEventData
{
    public string SkillName { get; set; }           // 技能名称
    public string SkillInstanceId { get; set; }     // 技能实例ID
    public object OriginalEventData { get; set; }   // 原始事件数据
    public bool Success { get; set; }               // 执行是否成功
    public float Timestamp { get; set; }            // 时间戳
}
```

#### 1.2 技能实例ID机制
```csharp
public class SkillInstance
{
    public string InstanceId { get; private set; }  // 唯一实例ID
    
    public SkillInstance(SkillConfig config, ...)
    {
        this.InstanceId = $"{config.skillName}_{Guid.NewGuid()}";
        // ... 其他初始化
    }
}
```

#### 1.3 重置条件过滤逻辑
```csharp
public class ImmediateResetCondition : IResetCondition
{
    private string targetSkillInstanceId;  // 目标技能实例ID
    
    public void SetTargetSkillInstance(string instanceId)
    {
        this.targetSkillInstanceId = instanceId;
    }
    
    public bool ShouldReset(object eventData)
    {
        if (eventData is SkillExecutedEventData skillEvent)
        {
            // 只响应对应技能实例的事件
            if (skillEvent.SkillInstanceId == targetSkillInstanceId)
            {
                Debug.Log($"[{ConditionName}] 响应技能 {skillEvent.SkillName} 执行完毕事件");
                return true;
            }
        }
        
        return false;
    }
}
```

#### 1.4 技能实例事件处理
```csharp
public class SkillInstance
{
    public bool ProcessEvent(object eventData)
    {
        // 第一步：检查触发器
        if (!trigger.CheckEvent(eventData)) return false;
        
        // 第二步：检查条件是否满足
        if (!condition.CheckCondition(eventData)) return false;
        
        // 第三步：执行效果
        bool effectExecuted = effect.ExecuteEffect(eventData);
        
        // 第四步：发布技能执行完毕事件（带实例ID）
        if (effectExecuted) {
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
        
        return effectExecuted;
    }
    
    public void HandleSkillExecutedEvent(object eventData)
    {
        if (eventData is SkillExecutedEventData skillEvent)
        {
            // 只处理自己的事件
            if (skillEvent.SkillInstanceId == this.InstanceId)
            {
                if (resetCondition?.ShouldReset(eventData) == true) {
                    condition.Reset();
                    effect.SetCanExecute(true);
                    Debug.Log($"[SkillInstance] 🔄 技能执行完毕，立即重置 - {config.skillName}");
                }
            }
        }
    }
}
```

### 方案2：私有事件机制（备选）

#### 2.1 技能实例私有事件
```csharp
public class SkillInstance
{
    private event Action<SkillExecutedEventData> OnSkillExecuted;
    
    public bool ProcessEvent(object eventData)
    {
        // ... 执行逻辑 ...
        
        if (effectExecuted) {
            var skillEvent = new SkillExecutedEventData
            {
                SkillName = config.skillName,
                SkillInstanceId = this.InstanceId,
                OriginalEventData = eventData,
                Success = true,
                Timestamp = Time.time
            };
            
            // 只触发自己的私有事件
            OnSkillExecuted?.Invoke(skillEvent);
        }
        
        return effectExecuted;
    }
}
```

#### 2.2 重置条件订阅私有事件
```csharp
public class ImmediateResetCondition : IResetCondition
{
    private SkillInstance targetSkillInstance;
    
    public void SetTargetSkillInstance(SkillInstance skillInstance)
    {
        this.targetSkillInstance = skillInstance;
        // 订阅私有事件
        skillInstance.OnSkillExecuted += HandleSkillExecuted;
    }
    
    private void HandleSkillExecuted(SkillExecutedEventData eventData)
    {
        if (ShouldReset(eventData)) {
            targetSkillInstance.condition.Reset();
            targetSkillInstance.effect.SetCanExecute(true);
        }
    }
}
```

## 实施计划

### 阶段1：事件隔离机制

#### 1.1 新增事件数据结构
- [ ] 创建 `SkillExecutedEventData` 类
- [ ] 添加技能实例ID字段
- [ ] 添加事件过滤逻辑

#### 1.2 修改技能实例
- [ ] 为每个技能实例生成唯一ID
- [ ] 修改 `ProcessEvent` 方法，发布带实例ID的事件
- [ ] 修改事件处理方法，只处理自己的事件

#### 1.3 修改重置条件
- [ ] 为重置条件添加目标技能实例ID
- [ ] 修改 `ShouldReset` 方法，添加事件过滤逻辑
- [ ] 确保重置条件只能重置对应的技能实例

### 阶段2：流程分离

#### 2.1 清理执行流程
- [ ] 从 `ProcessEvent` 中移除事件驱动的重置/移除检查
- [ ] 只保留执行逻辑：触发器 → 条件检查 → 效果执行 → 发布事件
- [ ] 移除执行流程中的重置/移除逻辑

#### 2.2 完善事件处理流程
- [ ] 实现 `HandleSkillExecutedEvent` 方法
- [ ] 实现 `HandlePhaseEndEvent` 方法
- [ ] 实现其他事件处理方法

#### 2.3 统一事件驱动架构
- [ ] 所有重置和移除都通过事件驱动
- [ ] 每个事件处理方法职责单一
- [ ] 事件处理逻辑独立且可测试

### 阶段3：测试验证

#### 3.1 功能测试
- [ ] 测试技能执行完毕事件隔离
- [ ] 测试重置条件只响应对应技能的事件
- [ ] 测试多个技能实例的事件处理

#### 3.2 场景测试
- [ ] 碰撞连击技能（立即重置+回合结束移除）
- [ ] 碰撞连击技能（回合结束重置+回合结束移除）
- [ ] 多个技能同时存在的场景

#### 3.3 回归测试
- [ ] 现有技能功能不受影响
- [ ] 性能没有显著下降
- [ ] 边界情况处理正确

## 风险控制

### 1. 向后兼容性
- [ ] 保持现有接口不变
- [ ] 渐进式迁移
- [ ] 保留原始实现的备份

### 2. 测试覆盖
- [ ] 每个阶段都有完整的测试
- [ ] 关键路径的回归测试
- [ ] 边界情况测试

### 3. 回滚计划
- [ ] 每个阶段都可以独立回滚
- [ ] 保留原始实现的备份
- [ ] 分阶段部署

## 预期收益

### 1. 设计优势
- **事件隔离**：每个技能实例只响应自己的事件
- **流程分离**：执行流程和事件处理流程完全分离
- **职责清晰**：重置条件和移除条件的职责明确
- **扩展性好**：可以轻松添加新的事件类型

### 2. 维护优势
- **可测试性**：每个事件处理逻辑独立且可测试
- **可维护性**：每个事件处理方法职责单一
- **可扩展性**：可以轻松添加新的事件处理逻辑

### 3. 性能优势
- **事件过滤**：减少不必要的事件处理
- **逻辑简化**：执行流程更加简洁
- **内存优化**：减少事件对象的创建和传递

## 后续优化

### 1. 事件系统优化
- [ ] 实现事件优先级机制
- [ ] 添加事件延迟处理
- [ ] 优化事件传递性能

### 2. 功能扩展
- [ ] 支持更复杂的事件处理逻辑
- [ ] 添加事件监听器机制
- [ ] 实现事件链式处理

### 3. 监控与诊断
- [ ] 添加事件处理性能监控
- [ ] 增加事件处理调试信息
- [ ] 完善事件处理日志系统

## 总结

这个重构计划通过引入事件隔离机制和流程分离，解决了当前技能系统的核心问题：

1. **事件混淆问题**：通过技能实例ID和事件过滤机制解决
2. **流程混合问题**：通过执行流程和事件处理流程分离解决
3. **条件匹配混乱**：通过重置条件只能响应对应技能事件解决

重构后的系统将具有更好的设计一致性、可维护性和扩展性，为后续功能开发奠定坚实基础。
