# 技能系统Reset语义重构计划

## 问题背景

当前技能系统存在严重的语义混乱问题：

1. **概念混淆**：ResetCondition、EffectRemovalCondition、isApplied 三个概念职责不清
2. **接口违反**：ShouldReset() 方法既判断又执行，违反接口语义
3. **逻辑纠缠**：isApplied 既管状态又管权限，导致逻辑复杂
4. **边界模糊**：重置条件和移除条件的功能重叠
5. **核心错误**：将"重置条件"和"移除效果"两个独立概念混在一起处理

## 核心问题分析

### 1. 语义不一致问题

#### ResetCondition 接口违反
```csharp
// 当前错误实现（OnEffectRemovalResetCondition）
public bool ShouldReset(object eventData)
{
    bool shouldRemove = effectRemovalCondition.ShouldRemoveEffect(eventData);
    if (shouldRemove) {
        condition?.Reset();  // ❌ 执行操作
        effect?.Reset();     // ❌ 执行操作
        return true;
    }
    return false;
}

// 正确实现应该是
public bool ShouldReset(object eventData)
{
    // 只判断，不执行
    return effectRemovalCondition.ShouldRemoveEffect(eventData);
}
```

#### isApplied 职责混乱
```csharp
// 当前问题：isApplied 既管状态又管权限
if (isApplied) {
    Debug.Log("效果已应用，跳过重复执行"); // ❌ 阻止了合理的重复执行
    return true;
}
```

### 2. 概念边界不清

| 概念 | 当前职责 | 应该职责 | 问题 |
|------|----------|----------|------|
| ResetCondition | 重置触发条件 + 移除效果 | 只重置触发条件 | 职责过重 |
| EffectRemovalCondition | 判断是否移除效果 | 只判断是否移除效果 | 职责正确 |
| isApplied | 状态标记 + 执行权限 | 只标记效果状态 | 职责混乱 |

### 3. 核心概念混淆

#### 当前错误的处理方式
```csharp
// ProcessEvent 第二步 - 错误地将两个独立概念混在一起
if (resetCondition.ShouldReset(eventData)) {
    condition.Reset();  // 重置触发条件
    effect.Reset();     // ❌ 错误：不应该重置效果
    return false;
}
```

#### 正确的处理方式应该是
```csharp
// 分离处理两个独立概念
// 1. 重置触发条件（让技能可以再次触发）
if (resetCondition.ShouldReset(eventData)) {
    condition.Reset();  // 只重置触发条件
    return false;
}

// 2. 移除效果（移除已应用的效果）
if (effectRemovalCondition?.ShouldRemoveEffect(eventData) == true) {
    effect.Reset();     // 只移除效果
    return false;
}
```

## 重构目标

### 1. 语义清晰化
- **ResetCondition**：只负责"何时重置触发条件"
- **EffectRemovalCondition**：只负责"何时移除效果"
- **isApplied**：只负责"效果是否已应用"
- **canExecute**：新增"是否允许执行"

### 2. 接口一致性
- 所有 `Should*` 方法只判断，不执行
- 执行操作统一由调用方处理
- 接口行为可预测

### 3. 职责分离
- 每个组件职责单一
- 逻辑清晰，易于维护
- 扩展性好

## 重构计划

### 阶段1：最小化修改 - 分离重置条件和移除条件

#### 1.1 修正 ProcessEvent 方法
**目标**：分离重置条件和移除条件两个独立概念

**当前错误实现**：
```csharp
// 第二步：检查重置条件（包括效果移除条件）
if (resetCondition.ShouldReset(eventData)) {
    condition.Reset();  // 重置触发条件
    effect.Reset();     // ❌ 错误：不应该重置效果
    return false;
}
```

**正确实现**：
```csharp
public bool ProcessEvent(object eventData)
{
    // 第一步：检查触发器
    if (!trigger.CheckEvent(eventData)) return false;
    
    // 第二步：检查重置条件（只重置触发条件）
    if (resetCondition?.ShouldReset(eventData) == true) {
        condition.Reset();  // 只重置触发条件
        return false;
    }
    
    // 第三步：检查移除条件（独立处理）
    if (effectRemovalCondition?.ShouldRemoveEffect(eventData) == true) {
        effect.Reset();     // 只移除效果
        return false;
    }
    
    // 第四步：检查条件是否满足
    if (!condition.CheckCondition(eventData)) return false;
    
    // 第五步：执行效果
    return effect.ExecuteEffect(eventData);
}
```

#### 1.2 修正 OnEffectRemovalResetCondition
**目标**：让 ShouldReset() 只判断，不执行

**修改内容**：
```csharp
public bool ShouldReset(object eventData)
{
    if (effectRemovalCondition == null) return false;
    
    // 只判断，不执行
    return effectRemovalCondition.ShouldRemoveEffect(eventData);
}
```

#### 1.3 统一所有 ResetCondition 实现
**目标**：确保所有 ShouldReset() 实现只判断，不执行

**检查清单**：
- [ ] ImmediateResetCondition
- [ ] OnPhaseEndedResetCondition
- [ ] OnEffectRemovalResetCondition
- [ ] CompositeResetCondition

### 阶段2：引入双标记系统

#### 2.1 扩展 IEffect 接口
**目标**：引入 canExecute 概念，完全由重置条件控制

**接口修改**：
```csharp
public interface IEffect
{
    string EffectName { get; }
    void Initialize();
    bool ExecuteEffect(object eventData);
    void Reset();
    
    // 新增：执行权限，完全由重置条件控制
    bool CanExecute { get; }
    void SetCanExecute(bool canExecute);
}
```

#### 2.2 修改 StatModifierEffect
**目标**：实现双标记系统，canExecute 完全由重置条件控制

**实现内容**：
```csharp
public class StatModifierEffect : IEffect
{
    private bool isApplied = false;     // 效果是否已应用（由移除条件控制）
    private bool canExecute = true;     // 是否允许执行（完全由重置条件控制）
    
    public bool CanExecute => canExecute;
    
    public void SetCanExecute(bool canExecute)
    {
        this.canExecute = canExecute;
    }
    
    public bool ExecuteEffect(object eventData)
    {
        // 只检查执行权限（完全由重置条件控制）
        if (!canExecute) {
            Debug.Log("不允许执行效果");
            return false;
        }
        
        // 执行效果逻辑（不管是否已应用，只要canExecute为true就执行）
        bool result = ExecuteEffectInternal(eventData);
        if (result) {
            isApplied = true;
        }
        
        return result;
    }
}
```

#### 2.3 重置条件和移除条件的职责分离
**目标**：明确 canExecute 和 isApplied 的控制方

**职责分工**：
- **重置条件**：控制 `canExecute`，决定技能是否可以再次执行
- **移除条件**：控制 `isApplied`，决定效果是否还要应用
- **两者独立**：canExecute 和 isApplied 可以独立变化
- **关键理解**：isApplied 只标记效果状态，不应该阻止重新执行；只有 canExecute 控制是否允许执行

**实现示例**：
```csharp
// 重置条件满足时：只控制 canExecute
if (resetCondition.ShouldReset(eventData)) {
    condition.Reset();        // 重置触发条件
    effect.SetCanExecute(true); // 重新允许执行
    // isApplied 不受影响
}

// 移除条件满足时：只控制 isApplied
if (effectRemovalCondition.ShouldRemoveEffect(eventData)) {
    effect.Reset();           // 移除效果
    // canExecute 不受影响
}
```

### 阶段3：优化执行后重置逻辑

#### 3.1 处理立即重置场景
**目标**：在效果执行后检查立即重置条件

**实现内容**：
```csharp
public bool ProcessEvent(object eventData)
{
    // ... 前面的检查逻辑 ...
    
    // 第五步：执行效果
    bool executed = effect.ExecuteEffect(eventData);
    
    // 第六步：执行后检查立即重置条件
    if (executed && resetCondition is ImmediateResetCondition) {
        condition.Reset(); // 重置触发条件
        effect.SetCanExecute(true); // 重新允许执行
    }
    
    return executed;
}
```

#### 3.2 处理回合结束重置场景
**目标**：在回合结束时检查重置条件

**实现内容**：
```csharp
public void HandlePhaseEndEvent(object eventData)
{
    // 重置条件满足时：只控制 canExecute
    if (resetCondition?.ShouldReset(eventData) == true) {
        condition.Reset(); // 重置触发条件
        effect.SetCanExecute(true); // 重新允许执行
    }
    
    // 移除条件满足时：只控制 isApplied
    if (effectRemovalCondition?.ShouldRemoveEffect(eventData) == true) {
        effect.Reset(); // 移除效果
    }
}
```

### 阶段4：测试与验证

#### 4.1 功能测试
**测试场景**：
- [ ] 重置条件和移除条件分离工作正常
- [ ] 碰撞连击技能（回合结束重置+回合结束移除）：碰撞2次→获得攻击力加成→再碰撞2次→跳过（canExecute=false）→回合结束→移除效果并重置条件
- [ ] 碰撞连击技能（立即重置+回合结束移除）：碰撞2次→获得攻击力加成→重置条件→再碰撞2次→再次获得攻击力加成→回合结束→移除效果
- [ ] canExecute 完全由重置条件控制，isApplied 完全由移除条件控制
- [ ] isApplied 只标记效果状态，不阻止重新执行

#### 4.2 回归测试
**测试内容**：
- [ ] 现有技能功能不受影响
- [ ] 各种技能配置组合的正确性
- [ ] 性能没有显著下降

#### 4.3 边界测试
**测试场景**：
- [ ] 多个重置条件组合
- [ ] 复杂的效果移除条件
- [ ] 异常情况处理

## 实施时间表

### 第1周：最小化修改
- [ ] 修正 ProcessEvent 方法，分离重置条件和移除条件
- [ ] 修正 OnEffectRemovalResetCondition，只判断不执行
- [ ] 检查所有 ResetCondition 实现
- [ ] 测试分离后的逻辑

### 第2周：双标记系统（可选）
- [ ] 扩展 IEffect 接口
- [ ] 修改 StatModifierEffect
- [ ] 修改其他效果实现

### 第3周：优化执行后重置逻辑
- [ ] 处理立即重置场景
- [ ] 处理回合结束重置场景
- [ ] 集成到现有事件处理系统

### 第4周：测试与验证
- [ ] 功能测试
- [ ] 回归测试
- [ ] 性能优化

## 风险控制

### 1. 向后兼容性
- 保持现有接口不变
- 新增功能通过扩展实现
- 渐进式迁移

### 2. 测试覆盖
- 每个阶段都有完整的测试
- 关键路径的回归测试
- 边界情况测试

### 3. 回滚计划
- 每个阶段都可以独立回滚
- 保留原始实现的备份
- 分阶段部署

## 预期收益

### 1. 代码质量提升
- 概念清晰，职责明确
- 接口语义一致
- 易于理解和维护
- 最小化修改，降低风险

### 2. 开发效率提升
- 减少调试时间
- 降低新功能开发复杂度
- 提高代码复用性
- 简单的修改，易于实现

### 3. 系统稳定性
- 减少逻辑错误
- 提高测试覆盖率
- 增强系统健壮性
- 执行、重置、移除逻辑分离

### 4. 架构优势
- **职责分离**：重置条件和移除条件独立处理
- **最小化修改**：在现有框架内解决问题
- **逻辑清晰**：执行、重置、移除三个概念分离
- **易于维护**：修改量小，风险低
- **概念明确**：canExecute 完全由重置条件控制，isApplied 完全由移除条件控制
- **执行控制**：只有 canExecute 控制是否允许执行，isApplied 只标记效果状态

## 后续优化

### 1. 性能优化
- 减少不必要的检查
- 优化事件处理流程
- 缓存常用结果

### 2. 功能扩展
- 支持更复杂的重置逻辑
- 增加更多效果类型
- 优化配置界面

### 3. 监控与诊断
- 添加性能监控
- 增加调试信息
- 完善日志系统
