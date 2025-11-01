# Blackboard 系统使用说明

## 快速开始

### 基础使用

```csharp
// 1. 获取 Blackboard（自动创建）
Blackboard blackboard = this.GetBlackboard();

// 2. 设置数据
blackboard.Set("IsDashing", true);
blackboard.Set("Speed", 5.5f);
blackboard.Set("LastPosition", transform.position);

// 3. 获取数据
bool isDashing = blackboard.Get<bool>("IsDashing");
float speed = blackboard.Get<float>("Speed");

// 4. 安全获取（推荐）
if (blackboard.TryGet("IsDashing", out bool value)) {
    // 使用 value
}
```

## 核心特性

- ✅ 自动创建：调用 GetBlackboard() 自动创建
- ✅ 类型安全：编译时检查类型
- ✅ 实例隔离：每个 GameObject 独立的 Blackboard
- ✅ 轻量级：Dictionary 存储，性能高

## Phase 0 完成 ✅

Blackboard 基础设施已完成，可以进入 Phase 1（伤害系统核心）

