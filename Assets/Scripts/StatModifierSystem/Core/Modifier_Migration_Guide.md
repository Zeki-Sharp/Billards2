# Modifier 轻量化迁移指南

## 概述

本指南说明如何从旧的 `StatModifier` 系统迁移到新的轻量级 `Modifier` 系统。

---

## 核心变化

### 旧系统 vs 新系统

| 特性 | 旧系统 (StatModifier) | 新系统 (Modifier) |
|------|---------------------|------------------|
| 数据类型 | `class` | `struct` ⭐ |
| GC 压力 | 高（每次创建分配堆内存） | 低（值类型） |
| 职责 | 数据 + 生命周期混合 | 纯数据 |
| 字段数量 | 8+ 个字段 | 2 个字段 ⭐ |
| 访问速度 | O(n) 计算 | O(1) 缓存 ⭐ |

---

## 新系统架构

### 三层设计

```
1. Modifier (struct)          - 纯数据（StatID + Value）
2. ModifierHandle (class)     - 生命周期管理（时间、条件、来源）
3. ModifierList (class)       - 集合管理（列表 + 缓存）
4. Modifiers (class)          - 容器（Constant + Percent）
5. RuntimeStat (class)        - 单个属性管理
6. RuntimeStatsManager (class)- 所有属性管理
```

---

## 使用对比

### 创建修改器

**旧系统：**
```csharp
// 创建一个 class 实例（堆分配）
StatModifier modifier = new StatModifier(
    "Damage",                    // targetStat
    StatModifierType.PercentAdd, // type
    0.5f,                        // value (+50%)
    this                         // source
);

playerStatsManager.ApplyModifier(modifier);
```

**新系统（推荐）：**
```csharp
// 创建轻量级 struct（栈分配或内联）
ModifierHandle handle = playerStatsManagerV2.AddPercent(
    "Damage",  // statID
    0.5f,      // value (+50%)
    this       // source
);

// 不再需要保存 modifier 引用，只保存 handle
```

---

### 移除修改器

**旧系统：**
```csharp
// 需要保存修改器引用
StatModifier modifier = new StatModifier(...);
playerStatsManager.ApplyModifier(modifier);

// 移除时需要引用
playerStatsManager.RemoveModifier(modifier);
```

**新系统：**
```csharp
// 保存句柄
ModifierHandle handle = playerStatsManagerV2.AddPercent("Damage", 0.5f);

// 使用句柄移除
playerStatsManagerV2.RemoveModifier("Damage", handle);
```

---

### 获取最终值

**旧系统：**
```csharp
// O(n) 计算：遍历所有修改器
float finalDamage = playerStatsManager.FinalDamage;
```

**新系统：**
```csharp
// O(1) 访问：使用缓存总值
float finalDamage = playerStatsManagerV2.FinalDamage;
```

---

## 性能对比

### 内存分配

**旧系统：**
- 每个修改器：~100 bytes（class + 8个字段）
- 10个修改器：~1KB 堆内存
- GC 压力：高

**新系统：**
- 每个 Modifier：16 bytes（struct：string + float）
- 每个 ModifierHandle：~60 bytes（管理信息）
- 10个修改器：~760 bytes
- GC 压力：低（Modifier 不分配堆内存）

### 访问速度

**旧系统：**
```
获取最终值 = O(n) 遍历所有修改器
10个修改器 ≈ 10次循环
```

**新系统：**
```
获取最终值 = O(1) 读取缓存
10个修改器 ≈ 1次访问
```

---

## 迁移步骤

### 步骤 1: 使用 PlayerStatsManagerV2

**在 Player.cs 或相关脚本中：**
```csharp
// 旧版本
public PlayerStatsManager statsManager; // 移除

// 新版本
public PlayerStatsManagerV2 statsManager; // 使用新版本
```

### 步骤 2: 更新技能效果

**旧版本（StatModifierEffect）：**
```csharp
StatModifier modifier = new StatModifier(
    targetStat, 
    modifierType, 
    modifierValue, 
    effectRemovalCondition, 
    this
);
```

**新版本：**
```csharp
// 方式1：使用新接口（推荐）
ModifierHandle handle = statsManager.AddPercent(
    "Damage",
    0.5f,    // +50%
    this
);

// 方式2：兼容旧接口（过渡期）
StatModifier oldModifier = new StatModifier(...);
statsManager.ApplyModifier(oldModifier); // 自动转换为新系统
```

### 步骤 3: 更新移除逻辑

**旧版本：**
```csharp
private StatModifier appliedModifier;

public void RemoveEffect()
{
    statsManager.RemoveModifier(appliedModifier);
}
```

**新版本：**
```csharp
private ModifierHandle modifierHandle;

public void RemoveEffect()
{
    statsManager.RemoveModifier("Damage", modifierHandle);
}
```

---

## 兼容性说明

### ✅ 完全兼容

新系统的 `PlayerStatsManagerV2` 保持了旧系统的所有公共接口：
- `ApplyModifier(StatModifier)` - 自动转换
- `RemoveModifier(StatModifier)` - 自动转换
- `RemoveModifiersBySource(object)` - 完全兼容
- `FinalMaxHealth`, `FinalDamage` 等属性 - 完全兼容

### ⚠️ 建议迁移

虽然兼容，但建议逐步迁移到新接口：
- 使用 `AddPercent()` / `AddConstant()` 替代 `ApplyModifier()`
- 保存 `ModifierHandle` 替代保存 `StatModifier`
- 享受更好的性能和更清晰的代码

---

## 测试检查清单

### 功能测试
- [ ] 技能效果正常应用
- [ ] 技能效果正常移除
- [ ] 属性最终值计算正确
- [ ] 临时效果正常过期
- [ ] 条件移除正常工作

### 性能测试
- [ ] GC Alloc 减少（Profiler 检查）
- [ ] 属性访问速度提升（Profiler 检查）
- [ ] 无性能退化

### 兼容性测试
- [ ] 所有现有技能正常运行
- [ ] 旧的 ApplyModifier 接口正常工作
- [ ] 属性值与旧系统一致

---

## 未来计划

新系统为以下功能打下基础：
- ✅ 三层属性系统（Stats/Attributes/StatusEffects）
- ✅ Property 动态值系统
- ✅ Class/Instance 分离

---

## 总结

**核心优势：**
1. ⭐ Modifier 改为 struct - 减少 GC 压力
2. ⭐ ModifierList 缓存总值 - O(1) 访问
3. ⭐ 职责分离 - 数据/生命周期分离
4. ⭐ 完全兼容 - 旧接口仍然可用

**迁移建议：**
- 渐进式迁移，不着急一次性替换
- 新代码使用新接口
- 旧代码可以继续使用旧接口（自动适配）
- 根据性能测试结果决定是否全面迁移

