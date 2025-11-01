# 新伤害系统迁移指南

> **目的**：从旧的硬编码伤害系统逐步迁移到规则驱动的新伤害系统
>
> **策略**：分阶段迁移，新旧系统并存，逐步替换，确保稳定性

---

## 📋 迁移概览

### 当前状态
- ✅ 新伤害系统核心已完成
- ✅ Blackboard 基础设施已就绪
- ⚠️ 旧系统仍在运行（会导致双重伤害）

### 迁移目标
- ✅ 玩家和敌人都使用新伤害系统
- ✅ 通过 Blackboard 状态控制攻击时机
- ✅ 规则配置驱动伤害逻辑
- ✅ 避免阶段冲突和双重伤害

---

## 🎯 迁移执行顺序

### **阶段 1：玩家碰撞攻击迁移（1-2 天）**✅ 已完成

**目标**：玩家主动攻击敌人使用新系统

#### **Step 1.1：创建规则配置（Unity）**
- [x] 创建 `DamageRule_PlayerHitEnemy`
- [x] 创建 `DamageProfile_Player`

#### **Step 1.2：修改 PlayerData（代码）**
- [x] 添加 `DamageProfile damageProfile` 字段
- [x] 在 Inspector 中配置 DamageProfile

#### **Step 1.3：修改 PlayerBehavior（代码）**
- [x] 在 Start() 注册到 DamageSystem
- [x] 简化 OnCollisionEnter2D（保留碰撞发布，禁用旧判断）
- [x] 实现 IDamageable 接口

#### **Step 1.4：修改 PlayerStateMachine（代码）**
- [x] 在状态切换时设置 Blackboard 状态
- [x] Moving 状态 → `CanAttack = true`
- [x] 其他状态 → `CanAttack = false`

#### **Step 1.5：禁用旧系统（代码）**
- [x] PlayerAttackManager.HandleCollisionAttack() 已禁用

#### **Step 1.6：特效系统集成（代码）**
- [x] EffectManager 订阅 OnDamage 事件
- [x] 支持父对象特效查找（处理子对象碰撞）

**验收标准**：
- ✅ 玩家在 Moving 状态撞敌人 → 敌人受伤
- ✅ 玩家在 Idle 状态撞敌人 → 敌人不受伤
- ✅ 没有双重伤害
- ✅ 特效正常触发（攻击、受击、全局）

---

### **阶段 2：敌人近战攻击迁移（2-3 天）**✅ 已完成

**目标**：敌人近战攻击（范围攻击）使用新系统

**关键设计**：
- ✅ 通过 `EnemyAttackRange` Tag 区分攻击来源（AttackRange vs 敌人主体）
- ✅ **主动检测**：在 Attack 阶段调用 `GetTargetsInRange()` 主动检测玩家
- ✅ 复用 CollisionEvent 机制（新增 CreateFromTrigger 方法）
- ✅ 通过 Blackboard 的 `CanAttack` 状态控制攻击时机
- ✅ BlackboardExtensions 添加 `TryGetBlackboard()`，避免自动创建

#### **Step 2.1：Unity 配置**✅ 已完成
- [x] 给所有敌人的 `AttackArea` 添加 Tag：`EnemyAttackRange`
- [x] 创建 `DamageRule_EnemyMeleeAttack`
  - Trigger Type: Collision
  - Source Tag: **EnemyAttackRange**
  - Target Tag: Player
  - Require Source State: CanAttack
  - Base Damage: 10
- [x] 添加到 `DamageProfile_Enemy`
- [x] 给 `EnemyData` 配置 `damageProfile`

#### **Step 2.2：添加 CollisionEvent.CreateFromTrigger（代码）**✅ 已完成
- [x] 在 `DamageSystemEvents.cs` 添加静态方法
- [x] 处理范围检测转换为 CollisionEvent

#### **Step 2.3：修改 DamageSystem（代码）**✅ 已完成
- [x] 添加 `GetBlackboard()` 辅助方法，使用 `TryGetBlackboard()` 避免自动创建
- [x] 支持向上查找父级 Blackboard（循环查找所有父级）
- [x] 清理冗余日志

#### **Step 2.4：修改 BlackboardExtensions（代码）**✅ 已完成
- [x] 添加 `TryGetBlackboard()` 方法（不自动创建）
- [x] `GetBlackboard()` 创建时调用 `SetOwner()`

#### **Step 2.5：修改 MeleeAttackBehavior（代码）**✅ 已完成
- [x] 在 `ExecuteAttack()` 设置 `CanAttack = true`
- [x] 主动调用 `GetTargetsInRange()` 检测玩家
- [x] 向检测到的玩家发布 `CollisionEvent`
- [x] 在 `CleanupAttack()` 清理 `CanAttack = false`

#### **Step 2.6：测试和验证**✅ 已完成
- [x] Attack 阶段伤害生效
- [x] Telegraph 阶段不造成伤害（只是预告）
- [x] 没有双重伤害
- [x] 特效正常触发

**验收标准**：
- ✅ 敌人 Attack 阶段，玩家在范围内 → 玩家受伤
- ✅ 敌人 Telegraph 阶段，玩家在范围内 → 玩家不受伤（只是预告）
- ✅ 没有双重伤害（旧系统已禁用）
- ✅ 特效正常触发（攻击、受击、全局）

**关键设计决策**：
- ❌ 不使用 OnTriggerEnter2D 被动检测（会在 Telegraph 阶段误触发）
- ✅ 使用 GetTargetsInRange() 主动检测（精确控制在 Attack 阶段）
- ✅ BlackboardExtensions.TryGetBlackboard() 避免自动创建导致的 Blackboard 冲突

---

### **阶段 3：敌人陷阱攻击迁移（1 天）**

**目标**：陷阱伤害使用新系统

#### **Step 3.1：创建陷阱规则配置（Unity）**
- [ ] 创建 `DamageRule_EnemyTrap`
  - Trigger Type: Collision
  - Source Tag: Enemy
  - Target Tag: Player
  - Require Source State: IsTrap  ← 不同的状态
  - Base Damage: 5

- [ ] 添加规则到 `DamageProfile_Enemy`

#### **Step 3.2：修改 ThornAttackBehavior（代码）**
- [ ] 在激活陷阱时设置 `IsTrap = true`
- [ ] 在清理时设置 `IsTrap = false`
- [ ] 移除旧的 `PublishAttack()` 调用

**验收标准**：
- ✅ 玩家碰到激活的陷阱 → 玩家受伤
- ✅ 玩家碰到未激活的敌人 → 玩家不受伤

---

### **阶段 4：玩家范围攻击迁移（1-2 天）**✅ 已完成

**目标**：球停止后的范围攻击使用新系统

**重要说明**：
- 需要新增 `Stopped` 触发类型（当前系统仅支持 `Collision`）
- 可以延后实现，不影响核心功能迁移
- 建议在阶段 1-3 完成后评估是否必要

#### **Step 4.1：创建范围伤害规则（Unity）**✅ 已完成
- [x] 创建 `DamageRule_PlayerAreaAttack`
  - Trigger Type: Stopped
  - Source Tag: Player
  - Target Tag: Enemy
  - Attack Range: 0（从 PlayerData.areaRadius 读取）
  - Base Damage: 15

- [x] 添加规则到 `DamageProfile_Player`

#### **Step 4.2：实现 Stopped 触发类型（代码）**✅ 已完成
- [x] 在 `DamageSystemEvents.cs` 添加 `StoppedEvent` 数据结构
- [x] 在 `GameEventBus.cs` 添加 `OnStopped` 事件和 `PublishStopped` 方法
- [x] 在 `DamageSystem.cs` 订阅 `OnStopped` 事件
- [x] 实现 `HandleStoppedEvent` 和 `ProcessStoppedDamage` 方法
- [x] 在 `DamageRuleConfig.cs` 添加 `attackRange` 字段
- [x] 在 `PlayerBehavior.cs` 添加 `PlayerData` 公共访问器

#### **Step 4.3：发布 Stopped 事件（代码）**✅ 已完成
- [x] 在 `PlayerAttackManager.ProcessBallStopped` 发布 `StoppedEvent`
- [x] 旧的 `HandleAreaAttack` 暂时保留（测试后可删除）

#### **Step 4.4：修复双重伤害问题（代码）**✅ 已完成
- [x] 禁用 `EnemyBehavior.OnDamageProcessed`（旧系统事件处理）
- [x] 清理冗余日志

**验收标准**：
- ✅ 球停止后范围内敌人受伤
- ✅ 攻击范围从 PlayerData.areaRadius 动态读取
- ✅ 没有双重伤害（旧系统已禁用）
- ✅ 伤害计算正确（20 伤害 = 40 血量 → 20 血量）

---

### **阶段 5：清理和优化（1-2 天）**

**目标**：移除旧系统，优化代码结构

#### **Step 5.1：完全移除旧的伤害逻辑**
- [ ] 删除 PlayerAttackManager 中的旧逻辑
- [ ] 删除 BaseAttackBehavior 中的 `PublishAttackEvent()`
- [ ] 清理未使用的代码

#### **Step 5.2：统一规则配置**
- [ ] 为所有敌人类型创建规则配置
- [ ] 整理 DamageProfile 资源
- [ ] 文档化规则命名规范

#### **Step 5.3：性能优化**
- [ ] 检查 DamageSystem 性能
- [ ] 优化规则匹配逻辑
- [ ] 添加调试工具

**验收标准**：
- ✅ 没有旧系统残留代码
- ✅ 所有伤害都通过新系统
- ✅ 性能无明显下降

---

## 🔄 阶段依赖关系

```
阶段 1（玩家碰撞）→ 必须先完成
    ↓
阶段 2（敌人近战）→ 依赖阶段 1 的状态控制经验
    ↓
阶段 3（敌人陷阱）→ 并行于阶段 2，独立状态
    ↓
阶段 4（玩家范围）→ 可选，需要新的触发类型（Stopped），可延后实现
    ↓
阶段 5（清理优化）→ 所有功能迁移完成后
```

**重要说明**：
- ✅ 阶段 1-3 只需 Collision 触发类型（Trigger 也使用 Collision 机制）
- ⚠️ 阶段 4 需要新增 Stopped 触发类型，可以延后实现
- ✅ 不需要实现 Manual/Interval 等复杂触发类型

---

## ⚠️ 关键注意事项

### 1. 双重伤害问题

**问题**：新旧系统并存会导致双重伤害

**解决**：
- 每迁移一个功能，立即禁用对应的旧逻辑
- 添加开关控制新旧系统切换
- 充分测试验证

### 2. 碰撞事件的双向性

**问题**：Unity 碰撞是双向的，两边都会触发

**解决**：
- 通过 Source Tag 和 Target Tag 明确方向
- 使用 Blackboard 状态精确控制时机
- 规则配置中添加状态要求

### 3. 阶段控制

**问题**：玩家和敌人的攻击需要阶段区分

**解决方案 A（推荐）**：
- 使用 Blackboard 状态（`CanAttack`）
- 在状态机/行为中控制状态

**解决方案 B（备选）**：
- 使用全局阶段状态（`InPlayerPhase` / `InEnemyPhase`）
- 在 GameFlowController 中统一管理

### 4. 攻击范围的碰撞（Trigger）

**问题**：AttackRange 使用 Trigger Collider（OnTriggerEnter2D），不是物理碰撞

**解决方案**：
- AttackRange 的 `OnTriggerEnter2D` 获取父级 `EnemyBehavior` 的 GameObject
- 使用父级作为 `CollisionEvent` 的 source，触发对象作为 target
- 复用现有的 `CollisionEvent` 机制（不区分 Trigger 和 Collision）
- 通过 Blackboard 状态（`CanAttack`）控制攻击时机

---

## 📊 迁移时间估算

| 阶段 | 工作量 | 依赖 | 风险 | 优先级 |
|------|--------|------|------|--------|
| **阶段 1** | 1-2 天 | 无 | 低 | ✅ 已完成 |
| **阶段 2** | 2-3 天 | 阶段 1 | 中 | ✅ 已完成 |
| **阶段 3** | 1 天 | 阶段 2 | 低 | ⏳ 待迁移 |
| **阶段 4** | 1-2 天 | 阶段 1 | 中 | ✅ 已完成 |
| **阶段 5** | 1-2 天 | 全部 | 低 | ⏳ 待执行 |
| **总计（已完成）** | **4-6 天** | - | - | - |
| **总计（全部）** | **6-10 天** | - | - | - |

**建议分配**：
- **Week 1**：阶段 1（已完成）+ 阶段 2（敌人近战攻击）⭐
- **Week 2**：阶段 3（陷阱攻击）+ 阶段 5（清理优化）
- **Week 3**：阶段 4（可选，范围攻击）+ 行为系统重构

---

## ✅ 验收清单

### 阶段 1 完成标准
- [ ] 玩家配置了 DamageProfile
- [ ] PlayerStateMachine 设置 CanAttack 状态
- [ ] 旧的碰撞攻击逻辑已禁用
- [ ] 测试：Moving 状态攻击生效，其他状态无效

### 阶段 2 完成标准
- [x] 敌人配置了 DamageProfile
- [x] MeleeAttackBehavior 在 Attack 阶段主动检测并发布 CollisionEvent
- [x] MeleeAttackBehavior 设置 CanAttack 状态（攻击时 true，Move 阶段清理）
- [x] 旧的 DealDamageToPlayer 调用已禁用
- [x] 测试：Attack 阶段玩家受伤，Telegraph 阶段无效

### 阶段 3 完成标准
- [ ] 陷阱规则已配置
- [ ] ThornAttackBehavior 设置 IsTrap 状态
- [ ] 测试：陷阱激活时伤害生效

### 阶段 4 完成标准
- [x] 范围攻击规则已配置
- [x] DamageSystem 支持 Stopped 触发类型
- [x] 从 PlayerData.areaRadius 动态读取攻击范围
- [x] 测试：球停止范围攻击生效
- [x] 修复双重伤害问题（旧系统已禁用）

### 阶段 5 完成标准
- [ ] 所有旧系统代码已清理
- [ ] 规则配置已整理归档
- [ ] 性能测试通过
- [ ] 完整回归测试通过

---

## 🛠️ 迁移工具和辅助

### 调试开关

在迁移过程中，建议添加全局开关：

```csharp
// GameConfig.cs 或 PlayerData.cs
public bool useNewDamageSystem = true;  // 全局开关
```

### 日志追踪

启用详细日志观察伤害流程：

```csharp
DamageSystem.Instance.enableDebugLog = true;
DamageSystem.Instance.showRuleMatching = true;
```

### 测试场景

创建专门的测试场景：
- 简化的玩家和敌人
- 清晰的阶段切换
- 方便观察伤害数值

---

## 📝 迁移记录模板

每完成一个阶段，记录：

```
阶段 X：[功能名称]
完成日期：YYYY-MM-DD
迁移内容：
- 配置了哪些规则
- 修改了哪些代码文件
- 禁用了哪些旧逻辑

遇到的问题：
- 问题描述
- 解决方案

测试结果：
- 测试场景
- 预期结果
- 实际结果
```

---

## 🎯 最终目标

迁移完成后的系统特性：

- ✅ **规则驱动**：所有伤害通过配置定义
- ✅ **状态控制**：精确的攻击时机控制
- ✅ **事件驱动**：松耦合的伤害传递
- ✅ **易于扩展**：新增伤害类型只需配置
- ✅ **可调试**：清晰的日志和状态追踪
- ✅ **性能优化**：规则过滤，无缓存开销

---

**文档版本**：v1.0  
**创建日期**：2025-11-01  
**维护者**：AI Assistant

**下一步**：
- 可选：阶段 3（敌人陷阱攻击迁移）
- 推荐：阶段 5（清理和优化）⭐

---

## 📝 更新日志

### v1.1 (2025-11-01)
- ✅ 更新阶段 2：明确 AttackRange 使用 Trigger Collider
- ✅ 说明复用现有 CollisionEvent 机制（不区分 Trigger 和 Collision）
- ✅ 更新阶段 4：标记为可选，需要新增 Stopped 触发类型
- ✅ 更新依赖关系：说明不需要实现 Manual/Interval 等复杂触发类型
- ✅ 优化时间估算：区分核心功能和可选功能

### v1.2 (2025-11-01)
- ✅ 阶段 2 完成：敌人近战攻击迁移成功
- ✅ 关键修复：从 OnTriggerEnter2D 被动检测改为主动检测
- ✅ 避免 Telegraph 阶段误触发伤害
- ✅ 修复 BlackboardExtensions 自动创建导致的 Blackboard 冲突
- ✅ 删除 AttackRangeTrigger.cs（不再需要）

### v1.3 (2025-11-01)
- ✅ 阶段 4 完成：玩家范围攻击迁移成功
- ✅ 实现 Stopped 触发类型（球停止范围攻击）
- ✅ 添加 StoppedEvent 数据结构和事件系统
- ✅ DamageRuleConfig 添加 attackRange 字段
- ✅ 动态从 PlayerData.areaRadius 读取攻击范围
- ✅ 修复双重伤害问题：禁用 EnemyBehavior.OnDamageProcessed（旧系统）

