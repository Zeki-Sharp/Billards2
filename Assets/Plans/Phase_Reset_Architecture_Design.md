# 回合重置架构设计

## 问题描述

当前技能系统存在跨回合计数累积问题：
- 第一回合撞墙1次
- 第二回合撞墙1次  
- 被系统误认为连续撞墙2次，触发技能

## 设计原则

### 核心原则
**所有技能条件都应该在回合结束时自动重置**

### 理由
1. **游戏逻辑一致性**：台球游戏是回合制的，每回合应该独立
2. **用户体验**：符合玩家直觉，每回合重新开始
3. **平衡性**：避免跨回合累积导致的技能过于强大
4. **配置简化**：减少配置复杂度

## 架构设计

### 1. 条件重置机制

#### 自动回合重置
```csharp
// 所有条件类都应该实现回合重置
public interface ICondition
{
    void Reset();                    // 通用重置
    void ResetOnPhaseEnd();         // 回合结束重置（新增）
}
```

#### 回合重置管理器
```csharp
public class PhaseResetManager : MonoBehaviour
{
    void Start()
    {
        GameEventBus.OnGameFlowStateChanged += OnGameFlowStateChanged;
    }
    
    void OnGameFlowStateChanged(GameFlowStateChangedData data)
    {
        if (data.NewState == GameFlowState.PlayerPhaseEnd)
        {
            ResetAllSkillConditions();
        }
    }
    
    void ResetAllSkillConditions()
    {
        foreach (var skillInstance in skillManager.GetAllSkillInstances())
        {
            skillInstance.condition.ResetOnPhaseEnd();
        }
    }
}
```

### 2. 条件类更新

#### CountCondition 更新
```csharp
public class CountCondition : ICondition
{
    private int currentCount = 0;
    private int requiredCount = 3;
    private bool allowCrossPhase = false;  // 默认不允许跨回合
    
    public void ResetOnPhaseEnd()
    {
        if (!allowCrossPhase)
        {
            Debug.Log($"[{ConditionName}] 🔄 回合结束，重置计数: {currentCount} → 0");
            currentCount = 0;
        }
    }
    
    // 构造函数支持跨回合选项
    public CountCondition(bool allowCrossPhase = false)
    {
        this.allowCrossPhase = allowCrossPhase;
    }
}
```

### 3. 触发重置类型重新定义

#### 新的触发重置类型
```csharp
public enum TriggerResetType
{
    Immediate,              // 立即重置（回合内）
    OnPhaseEnd,            // 回合结束时重置
    CrossPhase,            // 跨回合（明确表示）
    Never                  // 永不复位
}
```

#### 配置更新
```csharp
public class TriggerResetConfig
{
    public TriggerResetType resetType = TriggerResetType.Immediate;
    
    [ShowIf("resetType", TriggerResetType.CrossPhase)]
    public bool allowCrossPhase = false;  // 跨回合开关
    
    public ITriggerResetCondition CreateTriggerResetCondition()
    {
        switch (resetType)
        {
            case TriggerResetType.Immediate:
                return new ImmediateTriggerResetCondition();
            case TriggerResetType.OnPhaseEnd:
                return new OnPhaseEndedTriggerResetCondition();
            case TriggerResetType.CrossPhase:
                return new CrossPhaseTriggerResetCondition(allowCrossPhase);
            case TriggerResetType.Never:
                return new NeverTriggerResetCondition();
        }
    }
}
```

### 4. 向后兼容性

#### 现有配置迁移
- 所有现有的 `Immediate` 配置保持不变
- 自动应用回合重置逻辑
- 如果需要跨回合，需要明确选择 `CrossPhase` 类型

#### 迁移策略
1. 保持现有配置不变
2. 在技能初始化时自动应用回合重置
3. 提供配置迁移工具（可选）

## 实施步骤

### 阶段1：核心架构更新
1. 更新 `ICondition` 接口，添加 `ResetOnPhaseEnd()` 方法
2. 更新所有条件类实现回合重置逻辑
3. 创建 `PhaseResetManager`

### 阶段2：触发重置类型重构
1. 添加 `CrossPhase` 触发重置类型
2. 更新配置界面
3. 实现跨回合重置条件

### 阶段3：集成测试
1. 测试回合重置逻辑
2. 验证现有技能行为
3. 测试跨回合技能（如果需要）

## 配置示例

### 标准技能（回合重置）
```
撞墙回复生命值：
- 触发重置类型: Immediate
- 行为: 技能执行后立即重置，回合结束时也重置
```

### 跨回合技能（特殊需求）
```
累积伤害技能：
- 触发重置类型: CrossPhase
- 行为: 允许跨回合累积，直到技能触发
```

## 总结

这个设计解决了跨回合计数问题，同时保持了系统的灵活性和可扩展性。通过默认的回合重置机制，确保大多数技能行为符合游戏逻辑，同时为特殊需求提供明确的选择。
