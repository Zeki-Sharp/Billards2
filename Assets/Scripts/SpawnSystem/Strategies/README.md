# 生成策略系统架构文档

## 概述

生成策略系统是生成器系统的核心抽象层，负责决定"生成什么内容"。它将生成逻辑从配置层和执行层中分离出来，实现了高度的可复用性和可扩展性。

## 架构设计

### 四层架构

```
Layer 1: Trigger（触发层）
    ↓ 决定"何时生成"
Layer 2: Spawn Strategy（生成方式层）★ 本模块
    ↓ 决定"生成什么"
Layer 3: Range Config（范围配置层）
    ↓ 决定"在哪生成"
Layer 4: Spawner（执行层）
    ↓ 决定"如何生成"
```

### 核心接口

#### ISpawnStrategy<T>
```csharp
public interface ISpawnStrategy<T>
{
    List<T> GetSpawnList();     // 获取生成内容
    int GetSpawnCount();        // 获取生成数量
    bool ValidateConfig();      // 验证配置
}
```

**设计原则：**
- **单一职责**：只管"生成什么"，不管其他
- **无状态**：每次调用独立，不保存状态
- **可配置**：通过配置数据驱动行为

## 三种策略类型

### 1. ListSpawnStrategy（列表生成策略）

**用途：** 敌人波次生成
**特点：** 预设完整列表，直接返回

```csharp
// 配置示例
Wave1: [敌人A, 敌人A, 敌人A, 敌人B, 敌人B]
Wave2: [敌人C, 敌人C, 敌人C, 敌人C, 敌人C]
```

**适用场景：**
- 需要精确控制每波次内容的场景
- 敌人波次生成
- 道具池随机选择

### 2. FixedSpawnStrategy（固定生成策略）

**用途：** 技能主动生成道具
**特点：** 固定种类+数量，重复生成

```csharp
// 配置示例
道具种类: 治疗药水
生成数量: 2
→ 每次生成: [治疗药水, 治疗药水]
```

**适用场景：**
- 技能每回合生成固定道具
- 定时刷新的固定内容
- 事件触发的固定奖励

### 3. ConditionalDropStrategy（条件掉落策略）

**用途：** 击杀掉落
**特点：** 技能条件判定 + 可选概率

```csharp
// 配置示例
掉落表:
- 小血瓶: 
    conditionType: Always
    dropChance: 1.0 (100%必掉)
- 特殊材料: 
    conditionType: SkillRequired
    requiredSkillName: "采集技能"
    dropChance: 1.0 (有技能时100%掉落)
```

**适用场景：**
- 击杀掉落
- 条件触发的道具生成
- 技能解锁的特殊掉落

## 使用方式

### 在Trigger中使用策略

```csharp
public class DeathDropTrigger : SpawnTrigger<ItemConfig>
{
    public ConditionalDropStrategy<ItemConfig> dropStrategy;
    
    private void OnEnemyDeath(DeathData deathData)
    {
        // 1. 使用策略获取生成内容
        List<ItemConfig> itemsToDrop = dropStrategy.GetSpawnList();
        
        // 2. 计算生成位置
        List<Vector3> positions = rangeConfig.GetRandomPositions(itemsToDrop.Count);
        
        // 3. 调用生成器执行
        spawner.SpawnBatch(itemsToDrop, positions);
    }
}
```

### 策略配置示例

```csharp
// ListSpawnStrategy配置
public class ListSpawnStrategy<T> : ISpawnStrategy<T>
{
    [Header("列表配置")]
    public List<T> spawnList;
    
    public List<T> GetSpawnList() => new List<T>(spawnList);
    public int GetSpawnCount() => spawnList.Count;
}

// FixedSpawnStrategy配置
public class FixedSpawnStrategy<T> : ISpawnStrategy<T>
{
    [Header("固定配置")]
    public T itemToSpawn;
    public int spawnCount = 2;
    
    public List<T> GetSpawnList()
    {
        List<T> result = new List<T>();
        for (int i = 0; i < spawnCount; i++)
        {
            result.Add(itemToSpawn);
        }
        return result;
    }
}
```

## 扩展指南

### 添加新的策略类型

1. **实现ISpawnStrategy接口**
```csharp
public class NewSpawnStrategy<T> : ISpawnStrategy<T>
{
    public List<T> GetSpawnList() { /* 实现逻辑 */ }
    public int GetSpawnCount() { /* 实现逻辑 */ }
    public bool ValidateConfig() { /* 实现逻辑 */ }
}
```

2. **配置策略参数**
```csharp
[Header("新策略配置")]
public SomeConfigType config;
```

3. **在Trigger中使用**
```csharp
public NewSpawnStrategy<ItemConfig> newStrategy;
```

### 策略组合

策略可以灵活组合使用：

```csharp
// 多个策略组合
public class ComplexSpawnTrigger : SpawnTrigger<ItemConfig>
{
    public ListSpawnStrategy<ItemConfig> listStrategy;
    public FixedSpawnStrategy<ItemConfig> fixedStrategy;
    
    private void OnTrigger()
    {
        List<ItemConfig> items = new List<ItemConfig>();
        
        // 组合多个策略的结果
        items.AddRange(listStrategy.GetSpawnList());
        items.AddRange(fixedStrategy.GetSpawnList());
        
        // 执行生成
        SpawnItems(items);
    }
}
```

## 设计优势

### 1. 职责清晰
- **策略层**：只管"生成什么"
- **触发层**：只管"何时生成"
- **范围层**：只管"在哪生成"
- **执行层**：只管"如何生成"

### 2. 高度复用
- 同一个策略可以被多个Trigger使用
- 策略逻辑与具体场景解耦
- 配置数据与逻辑分离

### 3. 易于扩展
- 添加新策略无需修改现有代码
- 策略可以自由组合
- 配置灵活，支持运行时调整

### 4. 易于测试
- 策略独立，可单独测试
- 无状态，测试简单
- 配置驱动，测试覆盖全面

## 注意事项

### 1. 无状态原则
- 策略不应保存状态
- 每次调用应返回一致的结果
- 如需状态，应在Trigger层管理

### 2. 配置验证
- 实现ValidateConfig()方法
- 在初始化时验证配置有效性
- 提供清晰的错误信息

### 3. 性能考虑
- 避免在策略中进行重量级计算
- 复杂逻辑应在配置层预处理
- 缓存计算结果（如果适用）

## 未来扩展

### 可能的策略类型
- **WeightedSpawnStrategy**：权重随机选择
- **SequentialSpawnStrategy**：按顺序生成
- **ConditionalSpawnStrategy**：基于条件的动态生成
- **PoolSpawnStrategy**：对象池生成

### 高级功能
- **策略链**：多个策略串联执行
- **策略选择器**：根据条件选择不同策略
- **动态策略**：运行时切换策略
- **策略监控**：统计和调试支持

通过这种设计，生成策略系统为游戏提供了强大而灵活的内容生成能力，同时保持了代码的清晰和可维护性。
