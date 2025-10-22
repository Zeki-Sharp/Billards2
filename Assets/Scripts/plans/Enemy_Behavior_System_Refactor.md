# 敌人行为系统重构计划

## 📋 概述

本文档详细规划了敌人行为系统的重构方案。目标是建立一个**统一的、可扩展的敌人行为架构**，解决当前阶段流程混乱和伤害触发逻辑分散的问题。

**核心思想**：分离"行为编排"和"伤害触发"，创建清晰的架构分层。

---

## 🎯 重构目标

- **简化阶段流程**：从 4 个阶段减少到 3 个阶段
- **统一行为编排**：消除硬编码的特殊判断
- **统一伤害触发**：消除玩家和敌人之间的重复代码
- **消除特殊处理**：移除 TrapMode、DealTrapDamageToPlayer 等特殊代码
- **提高扩展性**：新增敌人类型和伤害触发方式无需修改现有代码

---

## 📊 当前问题分析

### **问题 1：敌人行为阶段流程混乱**

**当前阶段流程**：`Telegraph → Attack → Move → Spawn`

**具体问题**：
- **Thorn 敌人**：Attack 阶段无意义，攻击通过碰撞被动触发
- **Charge 敌人**：Attack 阶段无意义，攻击通过移动中碰撞触发
- **阶段不匹配**：预告阶段锁定方向，Attack 阶段对某些敌人是空操作
- **硬编码判断**：分散在各处的特殊处理逻辑

### **问题 2：伤害触发逻辑分散**

**玩家的伤害触发**（PlayerAttackManager）
- 碰撞伤害：`HandleCollisionAttack()` - 硬编码碰撞检测
- 范围伤害：`HandleAreaAttack()` - 硬编码范围检测

**敌人的伤害触发**（多个类）
- Melee/Ranged：`BaseAttackBehavior.DealDamageToPlayer()` - 通过 AttackRange 检测
- Thorn：碰撞触发 + 特殊处理（`IsTrapMode` 判断）

**问题**：
- ❌ 范围检测逻辑重复实现（玩家 + 敌人）
- ❌ 碰撞检测逻辑重复实现（玩家 + 敌人）
- ❌ 两套受击接口：`TakeDamage` vs `TakeDamageIgnorePhase`
- ❌ PlayerCore 需要判断碰撞语义（是我打敌人，还是敌人打我）

---

## 🏗️ 重构方案

### **方案 1：行为阶段重构**

#### **核心思想**
所有敌人在 Action 阶段都是**移动**，只是移动过程中或移动前后的伤害触发方式不同。

#### **新的阶段流程**
```
Telegraph → Action → Spawn → Telegraph → ...
```
- **Telegraph**：所有敌人预告下一轮行为
- **Action**：所有敌人执行各自的行为（攻击+移动 或 只移动）
- **Spawn**：生成新敌人

#### **两种行为模式**

| 敌人类型 | Action阶段 | 伤害触发时机 | 伤害触发方式 | 是否需要攻击阶段 |
|---------|------------|-------------|-------------|----------------|
| **Melee** | 攻击 → 移动 | 攻击时 | 范围检测 | ✅ 需要 |
| **Ranged** | 攻击 → 移动 | 攻击时 | 范围检测 | ✅ 需要 |
| **Thorn** | 只移动 | 移动后 | 玩家碰撞敌人 | ❌ 不需要 |
| **Charge** | 只移动 | 移动中 | 敌人碰撞玩家 | ❌ 不需要 |

#### **实现方案**

**1. 创建行为模式枚举**
```csharp
public enum EnemyActionMode
{
    ActiveAttack,    // 主动攻击模式：攻击 → 移动（Melee、Ranged）
    PassiveAttack   // 被动攻击模式：只移动（Thorn、Charge）
}
```

**2. 实现 Action 阶段编排**
```csharp
public void ExecuteActionPhase()
{
    switch (GetActionMode(enemyData.attackType))
    {
        case EnemyActionMode.ActiveAttack:
            // Melee/Ranged：攻击 → 移动
            ExecuteAttackPhase();
            ExecuteMovePhase();
            break;
            
        case EnemyActionMode.PassiveAttack:
            // Thorn/Charge：只移动
            ExecuteMovePhase();
            break;
    }
}
```

**3. 修改 EnemyPhaseController**
- 更新阶段序列：`Telegraph → Action → Spawn`
- 在 Action 阶段等待所有敌人完成各自的行为

---

### **方案 2：伤害触发系统重构**

#### **核心思想**
将"如何触发伤害"从具体的攻击逻辑中分离出来，创建通用的伤害触发器系统。

#### **架构分层**

```
业务层（上层）
    PlayerAttackManager     - 玩家攻击表现
    IAttackBehavior         - 敌人攻击表现
            ↓ 调用
伤害层（通用底层）
    IDamageTrigger          - 伤害触发器接口
    ├─ ActiveRangeDamageTrigger      （主动范围检测）
    ├─ PassiveCollisionDamageTrigger （被动碰撞触发）
    ├─ ActiveCollisionDamageTrigger  （主动碰撞触发）
    └─ AreaDamageTrigger             （区域范围触发）
            ↓ 触发伤害
受击层（统一接口）
    IDamageable.TakeDamage(damageData, ignorePhase)
```

#### **四种触发器设计**

**1. 主动范围检测触发器**
- **使用者**：Melee/Ranged 敌人
- **工作方式**：主动调用检测范围内的目标并造成伤害
- **阶段限制**：有（遵守游戏阶段）
- **实现要点**：依赖 `AttackRange` 组件，提供 `TriggerDamage()` 方法

**2. 被动碰撞触发器**
- **使用者**：Thorn 敌人
- **工作方式**：目标碰撞到触发器时，持续造成伤害（带冷却）
- **阶段限制**：无（忽略游戏阶段）
- **实现要点**：MonoBehaviour 组件，使用 `OnTriggerStay2D` 持续检测

**3. 主动碰撞触发器**
- **使用者**：Charge 敌人、玩家碰撞模式
- **工作方式**：启用后，与目标碰撞时造成伤害（一次性）
- **阶段限制**：无（移动中碰撞）
- **实现要点**：MonoBehaviour 组件，使用 `OnCollisionEnter2D` 检测碰撞

**4. 区域范围触发器**
- **使用者**：玩家 Area 模式
- **工作方式**：在指定位置检测范围内的所有目标并造成伤害
- **阶段限制**：有（遵守游戏阶段）
- **实现要点**：提供 `TriggerAtPosition(position)` 方法

#### **统一接口设计**

**伤害触发器接口（IDamageTrigger）**
- `Initialize(owner, damageSource)` - 初始化触发器
- `Enable()` - 启用触发器
- `Disable()` - 禁用触发器
- `Cleanup()` - 清理触发器

**伤害数据结构（DamageData）**
- `float Amount` - 伤害值
- `GameObject Attacker` - 攻击者
- `Vector3 HitPosition` - 击中位置

**可受伤接口（IDamageable）**
- `TakeDamage(DamageData damageData, bool ignorePhaseRestriction)` - 受到伤害

---

## 🚀 实施计划

### **阶段 1：行为阶段重构（2-3周）**

#### **第 1 周：行为模式分析和实现**
- 分析现有敌人类型的行为模式
- 创建 `EnemyActionMode` 枚举（ActiveAttack, PassiveAttack）
- 实现 `ExecuteActionPhase()` 方法
- 添加 `GetActionMode(AttackType)` 方法
- 单元测试

#### **第 2 周：EnemyBehavior 重构**
- 重构 `EnemyBehavior` 添加 `ExecuteActionPhase()` 方法
- 移除硬编码的阶段流程判断
- 更新现有敌人类型的配置
- 集成测试

#### **第 3 周：EnemyPhaseController 重构**
- 简化阶段流程：`Telegraph → Action → Spawn`
- 移除独立的 Attack 和 Move 阶段
- 更新阶段同步机制
- 回归测试

**预期成果**：
- ✅ 阶段从 4 个减少到 3 个
- ✅ 架构简洁，只有两种行为模式
- ✅ 消除硬编码的特殊判断

---

### **阶段 2：伤害触发系统重构（3周）**

#### **第 1 周：核心接口和基类**
- 创建 `IDamageTrigger` 接口
- 创建 `DamageData` 结构
- 创建 `IDamageSource` 接口
- 更新 `IDamageable` 接口
- 实现 4 种基础触发器
- 单元测试

#### **第 2 周：敌人系统集成**
- 重构 `EnemyBehavior` 实现 `IDamageSource`
- 添加触发器创建逻辑
- 移除 `SetTrapMode()`、`DealTrapDamageToPlayer()`
- 重构 `ThornAttackBehavior`
- 更新其他攻击行为
- 集成测试

#### **第 3 周：玩家系统集成**
- 重构 `PlayerCore` 统一受击接口
- 移除 `TakeDamageIgnorePhase()`、`HandleTrapCollision()`
- 重构 `PlayerAttackManager` 实现 `IDamageSource`
- 移除硬编码的攻击逻辑
- 全面测试和优化

**预期成果**：
- ✅ 消除所有特殊处理代码
- ✅ 统一为单一受击接口
- ✅ 玩家和敌人共用触发器
- ✅ 新触发方式零修改添加

---

## 📈 重构收益对比

### **代码简化**

| 项目 | 重构前 | 重构后 | 改善 |
|-----|-------|-------|------|
| **阶段数量** | 4个 | 3个 | ✅ 减少25% |
| **行为模式** | 硬编码判断 | 2种模式 | ✅ 统一管理 |
| **受击接口数量** | 2个 | 1个 | ✅ 减少50% |
| **特殊处理方法** | 3个 | 0个 | ✅ 完全消除 |
| **碰撞判断逻辑** | 分散在多处 | 封装在触发器 | ✅ 集中管理 |
| **范围检测逻辑** | 玩家+敌人重复 | 统一触发器 | ✅ 消除重复 |

### **消除的特殊处理代码**

**EnemyBehavior**
- ❌ `bool isTrapMode` 字段
- ❌ `SetTrapMode(bool)` 方法
- ❌ `DealTrapDamageToPlayer(GameObject, Vector3)` 方法

**PlayerCore**
- ❌ `TakeDamageIgnorePhase(float)` 方法
- ❌ `HandleTrapCollision(Collision2D)` 方法
- ❌ `if (enemy.IsTrapMode)` 判断逻辑

**PlayerAttackManager**
- ❌ `HandleCollisionAttack(Collision2D)` 硬编码逻辑
- ❌ `HandleAreaAttack(Vector3)` 硬编码逻辑

---

## ⚠️ 风险评估

### **主要风险**

| 风险 | 影响 | 缓解措施 |
|-----|------|---------|
| **接口变更破坏现有功能** | 高 | 分阶段实施，充分测试，保留备份 |
| **碰撞逻辑变更引入新 bug** | 高 | 优先测试碰撞相关功能 |

### **缓解策略**
1. **渐进式重构**：每周独立完成，避免大爆炸式重写
2. **充分测试**：每周完成后进行全面测试
3. **代码备份**：使用 Git 分支管理，保留回滚点
4. **文档先行**：先制定计划，再动手实施

---

## 🎯 成功指标

### **功能指标**
- ✅ 所有敌人类型正常工作（Melee、Ranged、Thorn）
- ✅ 玩家攻击模式正常工作（Collision、Area）
- ✅ 伤害数值准确无误
- ✅ 阶段限制正确生效

### **代码质量指标**
- ✅ 移除所有特殊处理代码
- ✅ 统一为单一受击接口
- ✅ 消除玩家和敌人的重复逻辑
- ✅ 新增触发器类型零修改现有代码

---

## 📅 时间线总览

```
Week 0  ████████ 前置准备 ✅
Week 1  ████████ 阶段 1：行为模式分析 (1/3)
Week 2  ████████ 阶段 1：EnemyBehavior重构 (2/3)
Week 3  ████████ 阶段 1：EnemyPhaseController重构 (3/3)
Week 4  ████████ 阶段 2：IDamageTrigger (1/3)
Week 5  ████████ 阶段 2：IDamageTrigger (2/3)
Week 6  ████████ 阶段 2：IDamageTrigger (3/3) 🎯
```

**总计**：约 6 周

---

## 📝 总结

通过引入**行为阶段重构**和**伤害触发系统**，我们将：

1. **简化阶段流程**：从 4 个阶段减少到 3 个阶段
2. **统一行为编排**：只有两种行为模式，易于理解和维护
3. **统一伤害触发**：玩家和敌人共用相同的伤害触发器
4. **消除特殊处理**：移除所有硬编码的特殊逻辑
5. **提高扩展性**：新增敌人类型和伤害触发方式零修改添加

这是一次**底层架构重构**，为整个战斗系统打下坚实基础，为 Charge 敌人等新功能铺平道路。

---

*最后更新：2024年10月22日*