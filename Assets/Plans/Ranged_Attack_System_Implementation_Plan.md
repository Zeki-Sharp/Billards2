# 远程攻击系统实现计划

## 1. 需求概述

### 1.1 目标
实现一种远程攻击方式，敌人可以在一定范围内将攻击范围投射到玩家附近位置，而不是在敌人自身位置进行攻击。

### 1.2 核心机制
- 敌人在检测范围内发现玩家
- 计算玩家附近的投射位置
- 将攻击范围移动到投射位置
- 在投射位置执行攻击逻辑

## 2. 系统架构设计

### 2.1 设计原则
- **分离关注点**：攻击方式与移动方式独立配置
- **行为模式一致性**：攻击系统采用与移动系统相同的行为模式架构
- **向后兼容**：不影响现有的近战攻击逻辑
- **利用现有架构**：使用现有的配置系统和条件字段特性

### 2.2 攻击类型定义
创建两种攻击类型：
- **Melee（近战）**：现有的攻击方式，在敌人位置执行攻击
- **Ranged（远程）**：新增的攻击方式，在投射位置执行攻击

### 2.3 攻击行为系统架构
参考现有的移动行为系统（IMovementBehavior），创建攻击行为系统：

```
IAttackBehavior (攻击行为接口)
├── BaseAttackBehavior (攻击行为基类)
├── MeleeAttackBehavior (近战攻击行为)
└── RangedAttackBehavior (远程攻击行为)

AttackBehaviorFactory (攻击行为工厂)
└── CreateAttackBehavior(AttackType) → IAttackBehavior
```

**优势：**
- 职责分离：每种攻击类型独立实现
- 易于扩展：未来可轻松添加新攻击类型
- 代码清晰：攻击逻辑不混在 EnemyBehavior 中
- 架构一致：与移动系统保持相同的设计模式

### 2.4 投射机制设计
**关键问题：敌人被撞击后，攻击位置应该如何变化？**

| 攻击类型 | 父子关系 | 投射方式 | 受敌人移动影响 |
|---------|---------|---------|---------------|
| **Melee** | 保持父子关系 | localPosition = (0,0) | ✅ 是（跟随敌人） |
| **Ranged** | 临时解除父子关系 | 使用世界坐标 | ❌ 否（位置固定） |

**设计决策：**
- **近战攻击**：攻击范围跟随敌人移动，敌人被撞飞时攻击位置也改变
- **远程攻击**：攻击范围在投射位置固定，即使敌人被撞飞也不影响攻击位置

**实现方式：**
- 近战：AttackRange 保持为 Enemy 的子物体
- 远程：预告阶段临时解除父子关系（SetParent(null)），使用世界坐标定位，攻击完成后恢复父子关系

### 2.5 与移动系统的关系
- 攻击方式与移动方式完全独立
- 远程攻击的敌人仍然可以配置任意移动方式（FollowPlayer 或 Flee）
- 通过组合方式实现不同的敌人行为模式

## 3. 配置系统扩展

### 3.1 攻击类型枚举
在 `CombatEnums.cs` 中定义：
- 移除废弃的 `AttackType` 特性标记
- 重新定义 `AttackType` 枚举：Melee, Ranged

### 3.2 远程攻击配置
在 `MovementConfig.cs` 中添加 `RangedAttackConfig` 类，包含：
- `detectionRange`：检测玩家的范围
- `projectionDistance`：投射到玩家附近的距离
- `cooldown`：攻击冷却时间
- `useRandomOffset`：是否使用随机偏移
- `randomOffsetRange`：随机偏移范围

**注意**：攻击范围的大小和形状由 `AttackRange` 预制体本身定义，不需要在配置中设置。

### 3.3 EnemyData 扩展
添加字段：
- `attackType`：攻击类型（Melee/Ranged）
- `rangedConfig`：远程攻击配置（使用条件字段，仅在选择 Ranged 时显示）

## 4. 核心组件修改

### 4.1 攻击行为系统（新增）

#### 4.1.1 IAttackBehavior 接口
**职责**：定义攻击行为的标准契约

**方法：**
- `ExecuteTelegraph()`：执行预告阶段逻辑
- `ExecuteAttack()`：执行攻击阶段逻辑
- `CleanupAttack()`：清理攻击状态（恢复 AttackRange 位置等）

#### 4.1.2 BaseAttackBehavior 基类
**职责**：提供攻击行为的通用实现

**通用功能：**
- 玩家检测
- 伤害计算和应用
- 攻击特效播放
- 参数验证

#### 4.1.3 MeleeAttackBehavior 近战攻击
**职责**：实现近战攻击逻辑

**预告阶段：**
- AttackRange 保持为 Enemy 子物体
- 设置 localPosition = (0, 0)
- 朝向玩家方向

**攻击阶段：**
- 在敌人位置执行攻击
- 检测攻击范围内目标
- 造成伤害

#### 4.1.4 RangedAttackBehavior 远程攻击
**职责**：实现远程攻击逻辑

**预告阶段：**
- 检测玩家是否在 detectionRange 范围内
- 计算投射位置（玩家附近 + 随机偏移）
- **解除父子关系**：`attackRange.transform.SetParent(null)`
- 设置世界坐标：`attackRange.transform.position = projectionPos`
- 设置朝向玩家方向

**攻击阶段：**
- 在投射位置执行攻击（不受敌人移动影响）
- 检测投射位置攻击范围内目标
- 造成伤害

**清理阶段：**
- **恢复父子关系**：`attackRange.transform.SetParent(enemy.transform)`
- 恢复 localPosition = (0, 0)

#### 4.1.5 AttackBehaviorFactory 工厂
**职责**：根据攻击类型创建对应的攻击行为实例

**方法：**
```
CreateAttackBehavior(AttackType attackType) → IAttackBehavior
```

### 4.2 AttackRange 组件（扩展）
**职责**：支持攻击范围的位置投射和父子关系管理

**现有机制**：
- AttackRange 是一个预制体，包含 Image 子物体和碰撞体
- 攻击范围的大小和形状由预制体中的碰撞体定义
- 当前只支持在敌人位置显示和检测

**新增功能**：
- 支持临时解除和恢复父子关系
- 支持世界坐标定位
- 保存原始父物体引用

**实现要点**：
- 保持现有的碰撞检测和目标获取逻辑（基于 Image 子物体的碰撞体）
- 攻击范围大小和形状完全由预制体决定，不需要代码配置
- 不需要新增特殊方法，由攻击行为直接操作 transform

### 4.3 EnemyBehavior 组件（修改）
**职责**：协调攻击行为的执行

**修改点**：
- 添加 `IAttackBehavior attackBehavior` 字段
- `InitializeBehavior()`：同时初始化移动行为和攻击行为
- `ExecuteTelegraphPhase()`：调用 `attackBehavior.ExecuteTelegraph()`
- `ExecuteAttackPhase()`：调用 `attackBehavior.ExecuteAttack()`

**简化后的逻辑**：
```
// 预告阶段
attackBehavior.ExecuteTelegraph(transform, player, enemyData, attackRange);

// 攻击阶段
attackBehavior.ExecuteAttack(transform, player, enemyData, attackRange);

// 清理
attackBehavior.CleanupAttack(attackRange);
```

## 5. 实现步骤

### 5.1 阶段一：基础架构 ✅
1. 在 `CombatEnums.cs` 中定义 `AttackType` 枚举
2. 在 `MovementConfig.cs` 中创建 `RangedAttackConfig` 配置类
3. 在 `EnemyData.cs` 中添加攻击类型字段和远程攻击配置

### 5.2 阶段二：攻击行为系统
1. 创建 `IAttackBehavior` 接口
2. 创建 `BaseAttackBehavior` 基类
3. 创建 `MeleeAttackBehavior` 近战攻击行为
4. 创建 `RangedAttackBehavior` 远程攻击行为
5. 扩展 `BehaviorFactory` 

### 5.3 阶段三：组件集成
1. 修改 `EnemyBehavior` 集成攻击行为系统
2. 重构 `ExecuteTelegraphPhase()` 使用攻击行为
3. 重构 `ExecuteAttackPhase()` 使用攻击行为
4. 测试近战攻击向后兼容性

### 5.4 阶段四：远程攻击实现
1. 实现投射位置计算逻辑
2. 实现父子关系临时解除和恢复
3. 实现远程攻击预告显示
4. 实现远程攻击执行和清理

### 5.5 阶段五：测试和调优
1. 创建测试用 EnemyData 配置
2. 测试近战和远程攻击的表现
3. 测试敌人被撞击时的攻击位置行为
4. 调整配置参数（范围、偏移等）
5. 验证与现有系统的兼容性

## 6. 配置示例

### 6.1 近战敌人配置
```
attackType: Melee
movementType: FollowPlayer
```

### 6.2 远程追击敌人配置
```
attackType: Ranged
movementType: FollowPlayer
rangedConfig:
  - detectionRange: 10
  - projectionDistance: 2
  - cooldown: 2
```
注：攻击范围形状由预制体定义

### 6.3 远程逃跑敌人配置
```
attackType: Ranged
movementType: Flee
rangedConfig:
  - detectionRange: 8
  - projectionDistance: 1.5
  - useRandomOffset: true
  - randomOffsetRange: 1.0
```
注：攻击范围形状由预制体定义

## 7. 技术要点

### 7.1 投射位置计算
- 基础位置：玩家位置向敌人方向偏移指定距离
- 随机偏移：在基础位置周围添加随机偏移
- 边界检查：确保投射位置在有效范围内

### 7.2 攻击范围管理和父子关系
**近战攻击（Melee）：**
- AttackRange 保持为 Enemy 的子物体
- 使用 localPosition = (0, 0) 定位
- 攻击范围跟随敌人移动
- 敌人被撞击时，攻击位置也随之改变

**远程攻击（Ranged）：**
- **预告阶段**：解除父子关系 `SetParent(null)`，使用世界坐标定位
- **攻击阶段**：AttackRange 位置固定，不受敌人移动影响
- **清理阶段**：恢复父子关系 `SetParent(enemy.transform)`，恢复 localPosition = (0, 0)

**关键设计决策：**
- 远程攻击使用世界坐标确保攻击位置独立于敌人位置
- 即使敌人被玩家撞飞，远程攻击位置也不会改变
- 增加游戏策略深度：玩家可以通过撞击改变近战攻击位置，但无法改变远程攻击位置

**实现细节：**
- AttackRange 的大小和形状由预制体中的 Image 子物体的 Collider2D 组件定义
- 保持与现有碰撞检测系统的兼容（使用 Image 子物体的 Collider2D.Overlap）
- 不需要在代码中配置攻击范围大小，完全由预制体决定

### 7.3 时序控制
- 预告阶段：显示投射位置的攻击范围
- 等待时间：给玩家反应时间
- 攻击执行：在投射位置检测目标并造成伤害
- 冷却时间：控制攻击频率

## 8. 扩展性考虑

### 8.1 未来可能的扩展
- 多段投射攻击
- 追踪型投射攻击
- 区域轰炸攻击
- 召唤型攻击

### 8.2 扩展点设计
- 保持攻击类型枚举的可扩展性
- 配置类支持继承和扩展
- 攻击执行逻辑支持多态

## 9. 注意事项

### 9.1 性能考虑
- 投射位置计算频率控制
- 避免频繁的攻击范围移动
- 合理的冷却时间设置

### 9.2 游戏平衡
- 远程攻击范围不宜过大
- 预告时间需要足够让玩家反应
- 随机偏移范围要适中

### 9.3 调试支持
- 使用 Debug.Log 输出关键信息
- Gizmos 绘制投射位置和检测范围
- 支持开关调试信息的显示

## 10. 验收标准

### 10.1 功能验收
- [ ] 远程攻击可以在投射位置执行
- [ ] 近战攻击不受影响（向后兼容）
- [ ] 配置系统正常工作
- [ ] 预告系统正确显示投射位置
- [ ] **敌人被撞击时**：近战攻击位置跟随改变，远程攻击位置固定不变
- [ ] 攻击行为系统与移动行为系统架构一致

### 10.2 兼容性验收
- [ ] 现有敌人配置继续有效（默认为 Melee）
- [ ] 移动系统与攻击系统独立工作
- [ ] 不影响现有的攻击特效系统
- [ ] AttackRange 预制体无需修改

### 10.3 质量验收
- [ ] 代码结构清晰，职责分明
- [ ] 使用行为模式，易于扩展
- [ ] 配置简单直观
- [ ] 调试信息完善
- [ ] 性能表现良好
