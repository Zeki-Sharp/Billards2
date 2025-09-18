# Telegraph管理器设计方案

## 当前问题分析

### 现有问题
1. **预告阶段概念混乱**：Telegraph阶段包含两种不同的预告
   - 敌人攻击预告（显示现有敌人的攻击范围）
   - 敌人生成预告（预告下一波敌人的生成位置）

2. **职责分散**：两种预告分别由不同组件处理
   - 敌人攻击预告：需要Enemy配合
   - 敌人生成预告：由EnemySpawner处理

3. **阶段完成通知复杂**：需要协调多个组件的完成状态

## 设计方案

### 核心思路
创建一个**TelegraphManager**来统一管理所有预告逻辑，作为预告阶段的唯一入口点。

### 架构设计

```
EnemyPhaseController
    ↓ (Telegraph阶段)
TelegraphManager
    ├── 敌人攻击预告 (AttackTelegraphHandler)
    └── 敌人生成预告 (SpawnTelegraphHandler)
    ↓ (两个预告都完成后)
EnemyPhaseController.OnEnemyPhaseActionComplete()
```

### 组件职责

#### TelegraphManager
- **职责**：统一管理所有预告逻辑
- **功能**：
  - 同时启动两种预告
  - 等待两种预告都完成
  - 通知EnemyPhaseController阶段完成

#### AttackTelegraphHandler
- **职责**：处理敌人攻击预告
- **功能**：
  - 通知所有敌人显示攻击范围
  - 等待所有敌人完成攻击预告
  - 通知TelegraphManager完成

#### SpawnTelegraphHandler
- **职责**：处理敌人生成预告
- **功能**：
  - 调用EnemySpawner的预告功能
  - 等待EnemySpawner完成预告
  - 通知TelegraphManager完成

## 实现方案

### 方案A：独立TelegraphManager
```csharp
public class TelegraphManager : MonoBehaviour
{
    public static TelegraphManager Instance { get; private set; }
    
    private int completedTelegraphs = 0;
    private int totalTelegraphs = 2; // 攻击预告 + 生成预告
    
    public void StartTelegraphPhase()
    {
        completedTelegraphs = 0;
        
        // 同时启动两种预告
        StartAttackTelegraph();
        StartSpawnTelegraph();
    }
    
    private void StartAttackTelegraph()
    {
        // 通知所有敌人显示攻击范围
        // 等待所有敌人完成
    }
    
    private void StartSpawnTelegraph()
    {
        // 调用EnemySpawner的预告功能
        // 等待EnemySpawner完成
    }
    
    public void OnTelegraphComplete()
    {
        completedTelegraphs++;
        if (completedTelegraphs >= totalTelegraphs)
        {
            // 所有预告完成，通知EnemyPhaseController
            EnemyPhaseController.Instance.OnEnemyPhaseActionComplete();
        }
    }
}
```

### 方案B：集成到EnemyPhaseController
```csharp
// 在EnemyPhaseController中添加
void HandleTelegraphPhase()
{
    telegraphManager = new TelegraphManager();
    telegraphManager.StartTelegraphPhase();
}
```

## 优势分析

### 1. 职责清晰
- TelegraphManager专门负责预告逻辑
- 各组件职责单一，易于维护

### 2. 扩展性好
- 未来可以轻松添加新的预告类型
- 预告顺序可以灵活配置

### 3. 调试友好
- 所有预告逻辑集中在一个地方
- 便于跟踪预告状态

### 4. 解耦合
- EnemyPhaseController不需要了解具体预告细节
- 各预告处理器可以独立开发

## 实现步骤

### 第一步：创建TelegraphManager
1. 创建TelegraphManager脚本
2. 实现基本的预告管理逻辑
3. 集成到EnemyPhaseController中

### 第二步：重构攻击预告
1. 将敌人攻击预告逻辑移到AttackTelegraphHandler
2. 确保与TelegraphManager的通信正常

### 第三步：重构生成预告
1. 将敌人生成预告逻辑移到SpawnTelegraphHandler
2. 确保与TelegraphManager的通信正常

### 第四步：测试和优化
1. 测试预告阶段的完整流程
2. 优化性能和用户体验

## 潜在问题

### 1. 复杂度增加
- 增加了新的组件层级
- 需要更多的通信机制

### 2. 初始化顺序
- TelegraphManager需要在合适的时机初始化
- 需要确保所有依赖组件都已准备好

### 3. 错误处理
- 需要处理预告失败的情况
- 需要超时机制防止卡死

## 建议

**推荐使用方案A（独立TelegraphManager）**，因为：
1. 职责分离更清晰
2. 便于独立测试和维护
3. 符合单一职责原则
4. 未来扩展性更好

## 总结

Telegraph管理器方案能够很好地解决当前预告阶段概念混乱的问题，通过统一管理两种预告，使系统架构更加清晰和可维护。建议优先实现这个方案。
