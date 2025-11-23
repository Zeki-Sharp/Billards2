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

### ✅ 问题 2：伤害事件生成仍默认 XY 投影（已修复）

**问题描述**：
- `CollectorStrikeEffect`、`PoisonStatus`、`BurningStatus` 等效果在构造 `DamageEvent` 时：
  - `HitPosition` 字段为 `Vector2`，当前写法会隐式使用 `position.y` 作为第二分量
  - `HitDirection` 为 `Vector2`，未正确映射到 XZ 平面
  - 未填充 `HitPosition3D` 字段（虽然已存在）

**修复状态**：✅ **已修复**
- `CollectorStrikeEffect`：已使用 XZ 投影和 `HitPosition3D`
- `PoisonStatus`：已使用 XZ 投影和 `HitPosition3D`
- `BurningStatus`：已使用 XZ 投影和 `HitPosition3D`
- `SkillArgs`：已添加 `GetHitPosition3D()` 和 `GetHitPositionXZ()` 便捷方法

---

### ✅ 问题 3：StoppedEvent 3D 数据在技能系统中的消费未落地（已修复）

**问题描述**：
- `StoppedEvent` 已包含 3D 位置数据（`StoppedPosition3D`、`LaunchPosition3D`、`FirstCollisionPoint3D`）
- `PlayerAttackManager` 已正确发布 3D 数据
- `DamageSystem` 的范围伤害已使用 3D 数据
- 但技能系统内部（触发器、效果）获取 `BallPhysics` 或 `StoppedEvent` 时，仍依赖 2D 投影

**修复状态**：✅ **已修复**
- `SkillArgs`：已添加 `GetStoppedPosition3D()`、`GetLaunchPosition3D()`、`GetFirstCollisionPoint3D()` 便捷方法
- `SkillManager`：已订阅 `OnStopped` 事件，优先使用 `StoppedEvent`
- `MovingEndTrigger`：已支持处理 `StoppedEvent`（同时保留 `BallPhysics` 后备方案）

---

### ✅ 问题 4：基于 Vector2 的工具方法需要重新对齐（部分修复）

**问题描述**：
- `WeakPointData.GetLocalPosition()`、`DropRangeConfig.GetRandomPosition()` 等方法仍使用 `Vector2`
- 这些方法在 3D 场景中的语义不明确（是 XY 平面还是 XZ 平面？）
- 写入 `Transform` 或事件数据时，可能出现坐标轴混淆（Y 被当作 Z）

**修复状态**：✅ **部分修复**
- `DropRangeConfig.GetRelativeRandomPosition()`：✅ 已正确映射到 XZ 平面（`x += randomCircle.x; z += randomCircle.y;`）
- `WeakPointData.GetLocalPosition()`：🔴 **待修复**（属于问题1，弱点系统相关）
- 其他工具方法：已添加注释说明 XZ 平面语义

---

### ✅ 问题 5：新事件数据的消费未统一（已修复）

**问题描述**：
- `DamageTrigger` 和 `SkillManager.HandleDamageEvent()` 已能接收 `DamageEvent.HitPosition3D`
- 但下游效果（DoT、弱点判定等）尚未改动，导致 3D 信息在进入技能系统后被“压扁”
- 缺乏统一的 3D 数据访问接口

**修复状态**：✅ **已修复**
- `SkillArgs`：已添加完整的 3D 位置便捷方法：
  - `GetHitPositionXZ()`：返回 XZ 平面投影（用于逻辑计算）
  - `GetHitPosition3D()`：返回真实 3D 位置（用于特效/判定）
  - `GetStoppedPosition3D()`：获取停止位置 3D 坐标
  - `GetLaunchPosition3D()`：获取发射位置 3D 坐标
  - `GetFirstCollisionPoint3D()`：获取第一碰撞点 3D 坐标
  - `GetDeathPosition3D()`：获取死亡位置 3D 坐标
- 所有方法都有详细的使用场景注释和推荐做法

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

---

## 八、2D 向后兼容和残留代码清单

> **说明**：本文档记录了所有保留的 2D 相关代码，这些代码主要用于向后兼容或作为后备方案。在未来的重构中，可以考虑逐步移除这些兼容层。

### 8.1 事件数据结构中的 Vector2 字段（向后兼容）

#### CollisionEvent
- **文件**：`Assets/Scripts/EventSystem/DamageSystemEvents.cs`
- **字段**：
  - `ContactPoint: Vector2` - 碰撞点（2D，向后兼容，实际存储 XZ 平面投影）
  - `ContactNormal: Vector2` - 碰撞法线（2D，实际存储 XZ 平面法线）
- **3D 对应字段**：`ContactPoint3D: Vector3?`
- **保留原因**：逻辑计算可能仍需要 2D 投影（XZ 平面）
- **使用场景**：在 3D 版本中，`ContactPoint` 存储的是 XZ 平面投影 `(x, z)`
- **注释位置**：第 43、111、123 行

#### StoppedEvent
- **文件**：`Assets/Scripts/EventSystem/DamageSystemEvents.cs`
- **字段**：
  - `StoppedPosition: Vector2` - 停止位置（2D 投影，兼容旧逻辑，实际存储 XZ 平面投影）
  - `LaunchPosition: Vector2?` - 发射起点（2D 投影，实际存储 XZ 平面投影）
  - `FirstCollisionPoint: Vector2?` - 第一碰撞点（2D 投影，实际存储 XZ 平面投影）
- **3D 对应字段**：
  - `StoppedPosition3D: Vector3?`
  - `LaunchPosition3D: Vector3?`
  - `FirstCollisionPoint3D: Vector3?`
- **保留原因**：向后兼容旧逻辑，同时提供 3D 数据
- **使用场景**：在 `CreateWithTrajectory3D` 中，自动将 3D 位置投影到 XZ 平面
- **注释位置**：第 143、150、151、212、214、215 行

#### DamageEvent
- **文件**：`Assets/Scripts/EventSystem/DamageSystemEvents.cs`
- **字段**：
  - `HitPosition: Vector2` - 击中位置（2D，向后兼容，用于旧逻辑，实际存储 XZ 平面投影）
  - `HitDirection: Vector2` - 击中方向（2D，向后兼容，实际存储 XZ 平面方向）
- **3D 对应字段**：`HitPosition3D: Vector3?`
- **保留原因**：向后兼容旧逻辑，同时提供 3D 数据
- **使用场景**：在技能效果中，`HitPosition` 存储的是 XZ 平面投影 `(x, z)`
- **注释位置**：第 244、249 行

### 8.2 技能系统中的 Vector2 使用（残留）

#### WeakPointData.GetLocalPosition()
- **文件**：`Assets/Scripts/SkillSystem/WeakPointData.cs`
- **方法签名**：`public Vector2 GetLocalPosition(float radius)`
- **问题**：返回 `Vector2`，语义不明确（是 XY 平面还是 XZ 平面？）
- **当前使用**：在 `WeakPointManager` 中用于计算弱点标记的局部位置
- **问题描述**：在 3D 场景中，`Vector2` 的 Y 分量被错误地当作垂直高度
- **待修复**：应改为返回 `Vector3`，或明确注释为 XZ 平面坐标
- **使用位置**：
  - `WeakPointManager.cs:264` - `Vector2 localPos = data.GetLocalPosition(radius);`
  - `WeakPointManager.cs:308` - `Vector2 newLocalPos = data.GetLocalPosition(radius);`
- **状态**：🔴 **待修复**（属于问题1，弱点系统相关）

#### WeakPointMarker.localOffset
- **文件**：`Assets/Scripts/SkillSystem/WeakPointMarker.cs`
- **字段**：`private Vector2 localOffset;`
- **问题**：使用 `Vector2` 存储局部偏移，无法正确表达 3D 空间中的位置
- **当前使用**：存储弱点标记相对于敌人的局部位置
- **待修复**：应改为 `Vector3`，支持配置高度偏移
- **使用位置**：
  - `WeakPointMarker.cs:14` - 字段定义
  - `WeakPointMarker.cs:40` - `Initialize(Transform enemy, Vector2 offset, int sector)`
  - `WeakPointMarker.cs:75` - `UpdatePosition(Vector2 newOffset, int newSector)`
- **状态**：🔴 **待修复**（属于问题1，弱点系统相关）

#### WeakPointManager.IsWeakPointHit()
- **文件**：`Assets/Scripts/SkillSystem/WeakPointManager.cs`
- **问题代码**：`Vector2 toHit = ((Vector2)(hitPosition - enemyPos)).normalized;`
- **问题描述**：将 3D 位置强制转换为 `Vector2`，隐式丢弃 Z 轴
- **当前行为**：使用 `Vector2` 计算角度，导致方向判定错误
- **待修复**：应改为 `new Vector2(delta.x, delta.z)`，基于 XZ 平面计算
- **使用位置**：`WeakPointManager.cs:469`
- **状态**：🔴 **待修复**（属于问题1，弱点系统相关）

#### DropRangeConfig.GetRelativeRandomPosition()
- **文件**：`Assets/Scripts/SkillSystem/Configs/DropRangeConfig.cs`
- **问题代码**：`Vector2 randomCircle = Random.insideUnitCircle * dropRadius;`
- **当前行为**：使用 `Random.insideUnitCircle` 生成 2D 随机点，然后映射到 XZ 平面
- **状态**：✅ **已适配** - 代码已正确映射到 XZ 平面（`x += randomCircle.x; z += randomCircle.y;`）
- **保留原因**：`Random.insideUnitCircle` 是 Unity 提供的便捷方法，内部使用 `Vector2` 是合理的
- **使用位置**：`DropRangeConfig.cs:58`

### 8.3 伤害事件构造中的 Vector2（向后兼容）

#### CollectorStrikeEffect
- **文件**：`Assets/Scripts/SkillSystem/Effects/CollectorStrikeEffect.cs`
- **代码**：
  ```csharp
  HitPosition = new Vector2(enemyPos3D.x, enemyPos3D.z),  // XZ平面投影（向后兼容）
  HitDirection = new Vector2(direction3D.x, direction3D.z),  // XZ平面方向
  ```
- **状态**：✅ **已修复** - 正确使用 XZ 平面投影和 `HitPosition3D`
- **注释**：第 224、226 行

#### PoisonStatus
- **文件**：`Assets/Scripts/SkillSystem/TurnBasedStatus/Statuses/PoisonStatus.cs`
- **代码**：
  ```csharp
  HitPosition = new Vector2(pos3D.x, pos3D.z),  // XZ平面投影（向后兼容）
  HitDirection = Vector2.zero,  // 持续伤害无方向
  ```
- **状态**：✅ **已修复** - 正确使用 XZ 平面投影和 `HitPosition3D`
- **注释**：第 54、56 行

#### BurningStatus
- **文件**：`Assets/Scripts/SkillSystem/TurnBasedStatus/Statuses/BurningStatus.cs`
- **代码**：
  ```csharp
  HitPosition = new Vector2(pos3D.x, pos3D.z),  // XZ平面投影（向后兼容）
  HitDirection = Vector2.zero,  // 持续伤害无方向
  ```
- **状态**：✅ **已修复** - 正确使用 XZ 平面投影和 `HitPosition3D`
- **注释**：第 47、49 行

### 8.4 触发器中的向后兼容

#### MovingEndTrigger
- **文件**：`Assets/Scripts/SkillSystem/Triggers/MovingEndTrigger.cs`
- **状态**：✅ **已清理** - 已移除 `BallPhysics` 后备方案，现在只使用 `StoppedEvent`
- **清理内容**：
  - 移除了 `CheckEvent` 中的 `BallPhysics` 后备检查
  - 移除了 `OnBallStopped` 事件订阅（该方法为空，无实际作用）
  - 更新了 `SkillManager.IsEventRelevantForSkill` 中的检查逻辑
- **清理原因**：`OnStopped` 事件已稳定，`SkillManager` 只使用 `StoppedEvent`，不再需要 `BallPhysics` 后备
- **清理时间**：2024年（3D升级阶段）

#### SkillManager.HandleBallStoppedEvent()
- **文件**：`Assets/Scripts/SkillSystem/SkillManager.cs`
- **状态**：✅ **已清理** - 已移除，现在只使用 `HandleStoppedEvent(StoppedEvent)`
- **清理原因**：`OnStopped` 事件已稳定，不再需要 `OnBallStopped` 后备方案
- **清理时间**：2024年（3D升级阶段）

### 8.5 SkillArgs 中的方法

#### GetHitPositionXZ()
- **文件**：`Assets/Scripts/SkillSystem/Core/SkillArgs.cs`
- **方法签名**：`public Vector2 GetHitPositionXZ()`
- **用途**：返回 XZ 平面投影，用于逻辑计算（范围判定、距离计算等）
- **保留原因**：某些逻辑计算可能仍需要 2D 投影
- **推荐**：需要 3D 位置时使用 `GetHitPosition3D()` 替代
- **注释位置**：第 234-241 行

#### GetStoppedPosition3D()
- **文件**：`Assets/Scripts/SkillSystem/Core/SkillArgs.cs`
- **状态**：✅ **已清理** - 已移除 `BallPhysics` 后备方案，现在只使用 `StoppedEvent`
- **清理内容**：移除了 `TryGetEventData<BallPhysics>` 的后备检查
- **清理原因**：`SkillManager` 和 `MovingEndTrigger` 已只使用 `StoppedEvent`，不再需要 `BallPhysics` 后备
- **清理时间**：2024年（3D升级阶段）

### 8.6 其他遗留代码

#### CollisionEvent.CreateFromTrigger (2D版本)
- **文件**：`Assets/Scripts/EventSystem/DamageSystemEvents.cs`
- **方法**：`CreateFromTrigger(GameObject source, Collider2D targetCollider)`
- **状态**：保留用于 2D 场景的向后兼容
- **注释**：`// 从 Trigger 碰撞创建碰撞事件（2D版本，保留向后兼容）`
- **保留原因**：支持旧的 2D 碰撞检测代码（如 `PlayerBehavior.OnTriggerEnter2D`）
- **使用位置**：第 79 行

#### CollisionEvent.Create (2D版本)
- **文件**：`Assets/Scripts/EventSystem/DamageSystemEvents.cs`
- **方法**：`Create(GameObject source, Collision2D collision)`
- **状态**：保留用于 2D 场景的向后兼容
- **保留原因**：支持旧的 2D 物理碰撞代码
- **使用位置**：第 54 行

#### PlayerBehavior.OnTriggerEnter2D
- **文件**：`Assets/Scripts/Player/Core/PlayerBehavior.cs`
- **方法**：`void OnTriggerEnter2D(Collider2D other)`
- **状态**：保留用于 2D 碰撞检测
- **保留原因**：玩家碰撞检测仍使用 2D 触发器
- **使用位置**：第 403 行

### 8.7 总结

#### 已修复的 2D 残留
- ✅ 伤害事件构造（`CollectorStrikeEffect`、`PoisonStatus`、`BurningStatus`）
- ✅ `DropRangeConfig` 的位置计算（已正确映射到 XZ 平面）
- ✅ `SkillArgs` 3D 位置便捷方法（已添加完整接口）

#### 已清理的向后兼容代码
- ✅ `SkillManager.HandleBallStoppedEvent()` - 已移除，现在只使用 `HandleStoppedEvent(StoppedEvent)`
- ✅ `MovingEndTrigger` 的 `BallPhysics` 后备方案 - 已移除，现在只使用 `StoppedEvent`
- ✅ `SkillArgs.GetStoppedPosition3D()` 中的 `BallPhysics` 后备方案 - 已移除，现在只使用 `StoppedEvent`

#### 待修复的 2D 残留（弱点系统相关，暂不处理）
- 🔴 `WeakPointData.GetLocalPosition()` - 应改为 `Vector3` 或明确注释为 XZ 平面
- 🔴 `WeakPointMarker.localOffset` - 应改为 `Vector3`
- 🔴 `WeakPointManager.IsWeakPointHit()` - 应使用 XZ 平面计算方向

#### 保留的向后兼容代码
- ✅ 事件数据结构中的 `Vector2` 字段（用于逻辑计算，XZ 平面投影）
- ✅ `CollisionEvent` 的 2D 创建方法
- ✅ `SkillArgs.GetHitPositionXZ()` 方法（用于逻辑计算）
- ✅ `PlayerBehavior.OnTriggerEnter2D`（2D 碰撞检测）

#### 建议
1. **短期**：为所有 `Vector2` 使用处添加明确注释（XY 平面或 XZ 平面）
2. **中期**：修复弱点系统的 2D 残留问题（问题 1）
3. **长期**：考虑逐步移除不必要的向后兼容代码，统一使用 3D 数据

