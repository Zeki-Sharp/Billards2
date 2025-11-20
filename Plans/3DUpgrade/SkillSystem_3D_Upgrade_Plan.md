# 技能系统 3D 升级规划

> **目标**：将技能系统从 2D 坐标体系完全迁移到 3D 坐标体系，确保所有技能效果（弱点判定、伤害计算、掉落位置、状态效果）在 3D 场景中正确工作。

---

## 一、现状评估

### ✅ 已兼容的部分

1. **核心架构**
   - `SkillManager` 事件订阅机制已适配 3D 事件（`OnCollision`、`OnDamage`、`OnBallStopped`）
   - `SkillArgs` 参数传递系统不依赖 2D/3D 区分
   - 触发器逻辑（`CollisionTrigger`、`MovingEndTrigger`、`DamageTrigger`）基于事件类型和角色 ID，对 3D 不敏感

2. **掉落类技能**
   - `DropItemEffect` 和 `DropItemReplenishEffect` 已使用 `ItemSpawner.Spawn()` 的 3D 接口
   - `DropRangeConfig` 的位置计算已映射到 XZ 平面（`Random.insideUnitCircle` → `x/z`）
   - 道具生成会自动触发 `GroundAlignAnchor` 进行地面对齐

3. **事件数据传递**
   - `CollisionEvent` 已扩展 `ContactPoint3D` 字段
   - `StoppedEvent` 已扩展 `StoppedPosition3D`、`LaunchPosition3D`、`FirstCollisionPoint3D` 字段
   - `DamageEvent` 已扩展 `HitPosition3D` 字段

---

## 二、待升级问题

### 🔴 问题 1：弱点系统仍按 XY 平面对齐

**问题描述**：
- `WeakPointManager.IsWeakPointHit()` 在判定命中时，将 3D 位置强制转换为 `Vector2`，隐式丢弃 Z 轴
- `WeakPointData.GetLocalPosition()` 返回 `Vector2`，导致弱点标记位置计算错误（Y 被当作垂直高度）
- `WeakPointMarker` 使用 `Vector2 localOffset`，无法正确表达 3D 空间中的弱点位置

**影响范围**：
- 弱点判定在 3D 场景中可能失效（方向计算错误）
- 弱点标记 UI 可能显示在错误位置（高度或水平偏移）

**解决方案概述**：
- 将弱点判定改为基于 XZ 平面计算方向（`new Vector2(delta.x, delta.z)`）
- `WeakPointData.GetLocalPosition()` 改为返回 `Vector3`，或明确注释为 XZ 平面坐标
- `WeakPointMarker` 的 `localOffset` 改为 `Vector3`，支持配置高度偏移
- 扇区判定逻辑改为基于 XZ 平面的角度计算

---

### 🔴 问题 2：伤害事件生成仍默认 XY 投影

**问题描述**：
- `CollectorStrikeEffect`、`PoisonStatus`、`BurningStatus` 等效果在构造 `DamageEvent` 时：
  - `HitPosition` 字段为 `Vector2`，当前写法会隐式使用 `position.y` 作为第二分量
  - `HitDirection` 为 `Vector2`，未正确映射到 XZ 平面
  - 未填充 `HitPosition3D` 字段（虽然已存在）

**影响范围**：
- 伤害特效可能显示在错误位置（高度错误）
- 后续基于伤害位置的判定（如范围伤害、状态扩散）可能失效
- 视觉特效与实际伤害位置不匹配

**解决方案概述**：
- 统一修改所有 `DamageEvent` 构造处：
  - `HitPosition = new Vector2(pos.x, pos.z)`（XZ 投影）
  - `HitPosition3D = pos`（真实 3D 位置）
  - `HitDirection = new Vector2(dir.x, dir.z)`（XZ 平面方向）
- 在 `SkillArgs` 中提供便捷方法（如 `GetHitPosition3D()`），引导技能逻辑使用 3D 字段

---

### 🟡 问题 3：StoppedEvent 3D 数据在技能系统中的消费未落地

**问题描述**：
- `StoppedEvent` 已包含 3D 位置数据（`StoppedPosition3D`、`LaunchPosition3D`、`FirstCollisionPoint3D`）
- `PlayerAttackManager` 已正确发布 3D 数据
- `DamageSystem` 的范围伤害已使用 3D 数据
- 但技能系统内部（触发器、效果）获取 `BallPhysics` 或 `StoppedEvent` 时，仍依赖 2D 投影

**影响范围**：
- 基于 `MovingEndTrigger` 的范围技能可能无法正确获取 3D 位置
- 需要精确 3D 位置的效果（如 `SpawnEffect`）可能位置错误

**解决方案概述**：
- 检查所有使用 `StoppedEvent` 的触发器/效果，确保优先使用 3D 字段
- 在 `SkillArgs` 中提供 `GetStoppedPosition3D()` 等便捷方法
- 对于仍需 2D 表达的场景，明确注释为 XZ 平面投影

---

### 🟡 问题 4：基于 Vector2 的工具方法需要重新对齐

**问题描述**：
- `WeakPointData.GetLocalPosition()`、`DropRangeConfig.GetRandomPosition()` 等方法仍使用 `Vector2`
- 这些方法在 3D 场景中的语义不明确（是 XY 平面还是 XZ 平面？）
- 写入 `Transform` 或事件数据时，可能出现坐标轴混淆（Y 被当作 Z）

**影响范围**：
- 位置计算可能错误
- 代码可读性差，容易引入新的 2D/3D 混淆

**解决方案概述**：
- 对于仍需 2D 表达的方法，明确注释为“XZ 平面坐标”
- 在写入 `Transform` 或事件时，显式转换：`new Vector3(offset.x, height, offset.y)`
- 考虑重构为返回 `Vector3`，或提供 `GetLocalPositionXZ()` 和 `GetLocalPosition3D()` 两个版本

---

### 🟡 问题 5：新事件数据的消费未统一

**问题描述**：
- `DamageTrigger` 和 `SkillManager.HandleDamageEvent()` 已能接收 `DamageEvent.HitPosition3D`
- 但下游效果（DoT、弱点判定等）尚未改动，导致 3D 信息在进入技能系统后被“压扁”
- 缺乏统一的 3D 数据访问接口

**影响范围**：
- 3D 信息在技能系统内部丢失
- 需要 3D 位置的效果无法正确工作

**解决方案概述**：
- 在 `SkillArgs` 中增加辅助方法：
  - `GetHitPositionXZ()`：返回 XZ 平面投影（用于逻辑计算）
  - `GetHitPosition3D()`：返回真实 3D 位置（用于特效/判定）
- 统一所有效果使用这些方法，而不是直接访问 `DamageEvent` 字段

---

## 三、升级优先级

### 高优先级（影响核心功能）

1. **弱点系统 3D 适配**（问题 1）
   - 影响：弱点判定和标记显示
   - 工作量：中等（需要修改判定逻辑、数据结构、UI 组件）

2. **伤害事件生成 3D 适配**（问题 2）
   - 影响：所有伤害特效和位置判定
   - 工作量：中等（需要修改多个效果类）

### 中优先级（影响扩展功能）

3. **StoppedEvent 3D 数据消费**（问题 3）
   - 影响：范围技能和位置相关效果
   - 工作量：较小（主要是检查和补充）

4. **工具方法重新对齐**（问题 4）
   - 影响：代码可维护性和未来扩展
   - 工作量：较小（主要是重构和注释）

5. **统一 3D 数据访问接口**（问题 5）
   - 影响：技能系统的整体一致性
   - 工作量：较小（主要是添加便捷方法）

---

## 四、实施建议

### 阶段 1：核心功能修复（高优先级）

1. 修复弱点系统：
   - 修改 `WeakPointManager.IsWeakPointHit()` 使用 XZ 平面判定
   - 修改 `WeakPointData.GetLocalPosition()` 明确为 XZ 平面或改为 `Vector3`
   - 修改 `WeakPointMarker` 支持 3D 位置和高度配置

2. 修复伤害事件生成：
   - 统一修改 `CollectorStrikeEffect`、`PoisonStatus`、`BurningStatus` 的 `DamageEvent` 构造
   - 确保所有 `HitPosition` 使用 XZ 投影，`HitPosition3D` 使用真实 3D 位置

### 阶段 2：数据传递优化（中优先级）

3. 完善 `SkillArgs` 接口：
   - 添加 `GetHitPosition3D()`、`GetStoppedPosition3D()` 等便捷方法
   - 统一所有效果使用这些方法

4. 检查并修复 `StoppedEvent` 消费：
   - 检查所有使用 `StoppedEvent` 的触发器/效果
   - 确保优先使用 3D 字段

### 阶段 3：代码质量提升（低优先级）

5. 重构工具方法：
   - 为所有 `Vector2` 方法添加 XZ 平面注释
   - 考虑重构为 `Vector3` 或提供双版本接口

---

## 五、验证标准

### 功能验证

1. **弱点系统**：
   - 在 3D 场景中，敌人弱点标记显示在正确位置（水平方向和高度）
   - 从不同角度攻击敌人，弱点判定正确（基于 XZ 平面方向）

2. **伤害特效**：
   - 所有伤害特效（DoT、技能伤害）显示在正确位置（3D 空间）
   - 伤害判定与视觉特效位置一致

3. **范围技能**：
   - 基于 `StoppedEvent` 的范围技能位置正确（3D 空间）
   - 掉落物位置正确（已通过 `GroundAlignAnchor` 验证）

### 代码质量验证

1. 所有 `Vector2` 使用处都有明确注释（XY 平面或 XZ 平面）
2. 所有 `DamageEvent` 构造处都填充了 `HitPosition3D`
3. 所有效果类都通过 `SkillArgs` 便捷方法访问 3D 数据

---

## 六、注意事项

1. **向后兼容**：
   - 保留 `Vector2` 字段用于向后兼容（逻辑计算可能仍需要 2D 投影）
   - 新增 `Vector3` 字段用于 3D 特效和判定

2. **性能考虑**：
   - `SkillArgs` 的便捷方法应使用缓存，避免重复计算
   - 3D 位置计算只在需要时进行（特效、判定），逻辑计算仍可使用 2D 投影

3. **测试场景**：
   - 需要在有高度差的 3D 场景中测试（敌人不在同一水平面）
   - 需要在坡面或非水平地面上测试（球体运动轨迹有高度变化）

---

## 七、相关文件清单

### 核心文件（需要修改）

- `Assets/Scripts/SkillSystem/WeakPointManager.cs`：弱点判定逻辑
- `Assets/Scripts/SkillSystem/WeakPointData.cs`：弱点数据结构
- `Assets/Scripts/SkillSystem/WeakPointMarker.cs`：弱点标记 UI
- `Assets/Scripts/SkillSystem/Effects/CollectorStrikeEffect.cs`：收集打击效果
- `Assets/Scripts/SkillSystem/TurnBasedStatus/Statuses/PoisonStatus.cs`：中毒状态
- `Assets/Scripts/SkillSystem/TurnBasedStatus/Statuses/BurningStatus.cs`：点燃状态
- `Assets/Scripts/SkillSystem/Core/SkillArgs.cs`：技能参数容器（添加便捷方法）

### 检查文件（可能需要修改）

- `Assets/Scripts/SkillSystem/Effects/SpawnEffect.cs`：生成效果（需要检查位置使用）
- `Assets/Scripts/SkillSystem/Configs/DropRangeConfig.cs`：掉落范围配置（已基本适配，需确认注释）

### 参考文件（已适配，可作为参考）

- `Assets/Scripts/EventSystem/DamageSystemEvents.cs`：事件数据结构（已包含 3D 字段）
- `Assets/Scripts/Core/Manager/DamageSystem.cs`：伤害系统（已使用 3D 数据）
- `Assets/Scripts/Player/PlayerAttackManager.cs`：玩家攻击管理（已发布 3D 数据）

