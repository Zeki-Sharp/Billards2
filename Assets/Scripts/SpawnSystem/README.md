# 生成器系统 (SpawnSystem) 架构文档

## 概述

SpawnSystem是一个基于三层架构的统一生成器系统，用于管理游戏中各种对象的生成逻辑。系统将配置、决策、执行完全分离，实现了高度解耦和可扩展的架构设计。

## 三层架构

### 1. 配置层 (Configuration Layer)
**职责：** 数据管理与查询

**核心组件：**
- `SpawnConfigProvider<T>` - 配置提供者接口
- 具体实现：`WaveConfigProvider`、`DropTableProvider`等

**功能：**
- 存储和管理配置数据
- 提供统一的查询接口
- 执行概率计算和随机抽取
- 支持动态配置调整

### 2. 决策层 (Strategy Layer)
**职责：** 触发逻辑与生成决策

**核心组件：**
- `SpawnTrigger<T>` - 触发器抽象基类
- 具体实现：`WaveSpawnTrigger`、`DeathDropTrigger`等

**功能：**
- 订阅游戏事件或接收外部调用
- 查询配置层获取生成数据
- 决定何时、何地、生成什么
- 调用执行层完成实际生成

### 3. 执行层 (Execution Layer)
**职责：** 对象实例化与生成

**核心组件：**
- `BaseSpawner<T>` - 生成器抽象基类
- 具体实现：`EnemySpawner`、`ItemSpawner`等

**功能：**
- 位置计算和验证
- 对象实例化
- 生成后处理

## 核心接口和类

### SpawnConfigProvider<T>
```csharp
public interface SpawnConfigProvider<T>
{
    List<T> GetSpawnData();
    bool ShouldSpawn();
    int GetSpawnCount();
    void Initialize();
    void Reset();
}
```

### SpawnTrigger<T>
```csharp
public abstract class SpawnTrigger<T> : MonoBehaviour
{
    protected SpawnConfigProvider<T> configProvider;
    protected BaseSpawner<T> spawner;
    
    protected abstract void SubscribeEvents();
    protected abstract void UnsubscribeEvents();
    protected void RequestSpawn(T data, Vector3 position);
}
```

### BaseSpawner<T>
```csharp
public abstract class BaseSpawner<T> : MonoBehaviour
{
    public virtual void Spawn(T data, Vector3? position = null);
    public virtual void SpawnBatch(List<T> dataList, List<Vector3> positions);
    protected abstract GameObject InstantiateObject(T data);
    protected virtual void OnPostSpawn(GameObject spawnedObject, T data);
}
```

## 使用流程

### 基本使用流程
```
1. 创建配置提供者 (ConfigProvider)
   ↓
2. 创建生成器 (Spawner)
   ↓
3. 创建触发器 (Trigger)
   ↓
4. 配置触发器引用配置提供者和生成器
   ↓
5. 触发器监听事件，自动生成对象
```

### 配置示例
```csharp
// 在Inspector中配置
WaveSpawnTrigger
├── configProvider: WaveConfigProvider (拖入组件)
├── spawner: EnemySpawner (拖入组件)
└── isActive: true

DeathDropTrigger  
├── configProvider: DropTableProvider (拖入组件)
├── spawner: ItemSpawner (拖入组件)
└── isActive: true
```

## 扩展指南

### 添加新的配置提供者
1. 实现`SpawnConfigProvider<T>`接口
2. 添加配置数据管理逻辑
3. 实现查询和计算方法

### 添加新的触发器
1. 继承`SpawnTrigger<T>`抽象类
2. 实现`SubscribeEvents()`和`UnsubscribeEvents()`
3. 在事件处理中调用`RequestSpawn()`

### 添加新的生成器
1. 继承`BaseSpawner<T>`抽象类
2. 实现`InstantiateObject()`方法
3. 可选重写`OnPostSpawn()`方法

## 与现有系统集成

### 技能系统集成
- 触发器可以查询`SkillManager`获取玩家激活技能
- 根据技能状态调整生成逻辑
- 支持技能影响掉落率、生成数量等

### 事件系统集成
- 触发器订阅`GameEventBus`事件
- 支持多种触发方式：事件驱动、定时触发、条件触发等

## 优势

1. **职责分离** - 配置、决策、执行完全解耦
2. **高度复用** - 通用逻辑在基类中实现
3. **易于扩展** - 添加新功能只需实现新的Provider或Trigger
4. **易于测试** - 各层独立，可单独测试
5. **配置灵活** - 支持可视化配置和动态调整

## 注意事项

1. **类型安全** - 使用泛型确保类型安全
2. **生命周期管理** - 正确订阅和取消事件
3. **性能考虑** - 大量生成时考虑对象池
4. **错误处理** - 添加适当的错误检查和日志

## 未来扩展

- 支持更复杂的生成策略
- 集成对象池系统
- 添加生成动画和特效
- 支持网络同步生成
