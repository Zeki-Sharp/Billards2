# 回退到 Rigidbody 物理系统计划

> **目标**：将当前的自定义几何物理系统回退到 Unity 原生的 Rigidbody 物理系统，解决几何物理中难以修复的碰撞穿透问题。
> 
> **状态**：计划阶段，未执行
> 
> **预计工作量**：47-66 小时（约 6-8 个工作日）

---

## 一、背景与动机

### 1.1 当前问题

- 几何物理系统存在难以修复的碰撞穿透问题
- 球体在靠近墙壁时被撞击会穿透墙壁
- 球体之间可能出现重叠，导致后续碰撞检测失效
- 多次修复尝试均未彻底解决问题

### 1.2 回退原因

- Unity Rigidbody 物理系统经过充分测试，碰撞检测和分离机制成熟可靠
- 可以避免自定义物理系统带来的边缘情况问题
- 减少维护成本，利用 Unity 引擎的原生能力

### 1.3 风险评估

- **工作量较大**：需要重构核心物理系统及相关依赖
- **行为可能变化**：Rigidbody 物理的行为可能与几何物理有细微差异，需要重新调参
- **性能影响**：Rigidbody 物理可能带来额外的性能开销，需要优化

---

## 二、系统依赖分析

### 2.1 核心系统依赖

#### BallPhysics.cs（约 880 行）
- **当前状态**：完全依赖几何物理系统
- **需要重构**：约 80% 的代码需要修改或移除
- **关键方法**：
  - `SimulateGeometryStep()` - 需要移除
  - `HandleGeometryBallCollision()` - 改为使用 Unity 碰撞回调
  - `HandleGeometryWallCollision()` - 改为使用 Unity 碰撞回调
  - `PerformColliderCast()` - 需要移除
  - `SetVelocity()` / `GetVelocity()` - 改为读取 Rigidbody.velocity

#### 敌人移动系统（EnemyBehavior.cs）
- **当前依赖**：`UseActiveMoveGeometryConfig()`, `ApplyExternalGeometryVelocity()`
- **需要修改**：改为直接设置 `Rigidbody.velocity` 或使用 `AddForce()`
- **停止检测**：从 `IsMoving()` 改为检查 `Rigidbody.velocity.magnitude`

#### 玩家发射系统（PlayerBehavior.cs）
- **当前依赖**：`SetVelocity()` 方法
- **需要修改**：改为直接设置 `Rigidbody.velocity`
- **速度获取**：从几何物理改为读取 Rigidbody

### 2.2 事件系统依赖

#### GameEventBus 事件触发
- **OnBallStopped**：当前由几何物理在 `OnGeometryMovementStopped()` 中触发
- **OnBallCollision**：当前由几何物理在 `HandleGeometryBallCollision()` 中触发
- **CollisionEvent**：当前由几何物理手动构造

#### 需要修改
- 在 `FixedUpdate()` 中检测 `Rigidbody.velocity.magnitude < threshold` 触发停止事件
- 在 `OnCollisionEnter()` 中触发碰撞事件
- 从 Unity `Collision` 对象提取信息构造 `CollisionEvent`

### 2.3 轨迹预测系统

#### TrajectoryPredictor.cs
- **当前依赖**：`GeometryTrajectorySimulator`
- **需要重构**：
  - 方案 A：使用 Unity 的 `PhysicsScene.Simulate()` 进行轨迹预测
  - 方案 B：使用多场景物理模拟（参考 `MultiScene_Physics_Trajectory_Prediction_Plan.md`）

### 2.4 碰撞和伤害系统

#### DamageSystem.cs
- **当前依赖**：从几何物理的 `PublishGeometryCollisionEvent()` 获取碰撞事件
- **需要修改**：从 Unity `OnCollisionEnter()` 回调获取碰撞信息
- **速度计算**：从 `geometrySpeed` 改为 `Rigidbody.velocity.magnitude`

#### WallManager.cs / Wall.cs
- **当前依赖**：依赖几何物理发布的碰撞事件
- **需要修改**：使用 Unity `OnCollisionEnter()` 回调

### 2.5 配置和数据

#### BallData.cs
- **保留并映射**：基础物理参数映射到 Unity 物理系统
  - `mass` → `Rigidbody.mass`
  - `bounceDamping` → `PhysicsMaterial.bounciness`
  - `friction` → `PhysicsMaterial.dynamicFriction` 和 `staticFriction`
  - `linearDamping` → `Rigidbody.drag`（线性阻尼）
  - `maxSpeed` → 用于速度限制检查（运行时限制 `Rigidbody.velocity`）
  - `stopThreshold` → 用于停止检测阈值
- **移除**：几何物理参数（`geometryHighSpeedPhaseDuration`, `geometryHighPhaseDamping` 等）
- **参数映射策略**：
  - 在 `InitializePhysics()` 中读取 `BallData` 参数并应用到 `Rigidbody` 和 `PhysicsMaterial`
  - 保持 `BallData` 作为配置源，不直接依赖 Rigidbody 的 Inspector 值
  - 支持运行时动态调整（通过修改 `BallData` 或直接修改物理组件）

#### EnemyLevelConfig.cs
- **移除**：`moveBallData` 相关配置（如果不再需要）
- **或保留**：改为配置 Rigidbody 参数

---

## 三、实施步骤

### 阶段 1：核心物理系统重构（15-20 小时）

#### 1.1 BallPhysics 核心重构
- 移除几何物理相关字段和方法
- 修改 `InitializePhysics()` 配置 Rigidbody（`isKinematic = false`）
- 移除 `SimulateGeometryStep()` 和 `FixedUpdate()` 中的几何模拟逻辑
- 实现 `OnCollisionEnter()` 回调处理碰撞
- 修改 `SetVelocity()` / `GetVelocity()` 使用 Rigidbody
- 实现 `FixedUpdate()` 中检测速度变化触发停止事件

#### 1.2 物理参数配置
- **从 BallData 映射参数到 Unity 物理系统**：
  - `ballData.mass` → `Rigidbody.mass`
  - `ballData.linearDamping` → `Rigidbody.drag`
  - `ballData.bounceDamping` → `PhysicsMaterial.bounciness`
  - `ballData.friction` → `PhysicsMaterial.dynamicFriction` 和 `staticFriction`
  - `ballData.maxSpeed` → 用于运行时速度限制（在 `FixedUpdate` 中检查）
  - `ballData.stopThreshold` → 用于停止检测阈值
- **创建和配置 PhysicsMaterial**：
  - 在 `InitializePhysics()` 中创建或获取 `PhysicsMaterial`
  - 从 `BallData` 读取参数并应用到材质
  - 将材质赋值给 `Collider.material`
- **重力配置决策**：
  - 如果选择使用重力：设置 `useGravity = true`，不锁定 Y 轴，确保地面有碰撞器
  - 如果选择不使用重力：设置 `useGravity = false`，锁定 Y 轴位移 `FreezePositionY`
- 配置全局重力强度（如需要）：`Physics.gravity`（默认 -9.81，可根据游戏需求调整）
- **保持参数配置的灵活性**：
  - `BallData` 仍然是配置源，便于在 Inspector 中调整
  - 支持运行时动态修改参数（修改 `BallData` 后重新应用，或直接修改物理组件）
  - 可以添加参数验证和范围限制

#### 1.3 碰撞检测配置
- 配置 `LayerMask` 用于碰撞检测
- 确保碰撞矩阵正确设置
- **地面/桌面碰撞器检查**：
  - 如果使用重力：确保地面/桌面有碰撞器（MeshCollider 或 BoxCollider）
  - 检查碰撞器是否正确配置，防止球穿透或下落
  - 测试球是否能正确"贴"在桌面上
- 测试碰撞检测是否正常工作

### 阶段 2：移动和发射系统重构（5-8 小时）

#### 2.1 敌人移动系统
- 修改 `EnemyBehavior.MoveToTarget()` 协程
- 移除 `UseActiveMoveGeometryConfig()` 调用
- 改为直接设置 `Rigidbody.velocity` 或使用 `AddForce()`
- 修改停止检测逻辑

#### 2.2 玩家发射系统
- 修改 `PlayerBehavior.Launch()` 方法
- 改为直接设置 `Rigidbody.velocity`
- 修改速度获取方法

#### 2.3 其他依赖系统
- 修改 `PlayerStateMachine`, `PlayerTurnManager` 等
- 更新 `DeathManager` 的 `ResetBallState()` 方法

### 阶段 3：事件和轨迹预测重构（12-18 小时）

#### 3.1 事件系统重构
- 实现 `OnCollisionEnter()` 回调
- 从 Unity `Collision` 对象提取信息
- 构造并发布 `CollisionEvent`
- 实现 `FixedUpdate()` 中的停止检测
- 触发 `OnBallStopped` 事件

#### 3.2 轨迹预测系统
- **方案选择**：评估使用 `PhysicsScene.Simulate()` 还是多场景模拟
- 实现新的轨迹预测逻辑
- 确保预测准确性与实际行为一致
- 测试预测性能

#### 3.3 碰撞事件处理
- 修改 `DamageSystem` 的碰撞事件来源
- 修改 `WallManager` 的碰撞检测方式
- 更新所有依赖碰撞事件的系统

### 阶段 4：测试和调试（8-12 小时）

#### 4.1 功能测试
- 玩家发射和移动测试
- 敌人移动和碰撞测试
- 墙壁碰撞和反弹测试
- 球体间碰撞测试
- 轨迹预测准确性测试
- 伤害系统触发测试
- 事件系统触发测试

#### 4.2 性能测试
- 物理更新性能测试
- 碰撞检测性能测试
- 轨迹预测性能测试
- 优化物理更新频率（如需要）

#### 4.3 参数调优
- **通过 BallData 调优**（推荐方式）：
  - 调整 `BallData` 中的参数（`mass`, `bounceDamping`, `friction`, `linearDamping` 等）
  - 参数会自动应用到 `Rigidbody` 和 `PhysicsMaterial`
  - 便于在 Inspector 中统一管理
- **直接调整物理组件**（高级用法）：
  - 直接修改 `Rigidbody` 或 `PhysicsMaterial` 的值
  - 适用于需要运行时动态调整的场景
- **参数映射验证**：
  - 确保 `BallData` 参数正确映射到 Unity 物理系统
  - 验证参数范围和有效性
- **平衡游戏手感**：
  - 调整反弹系数（`bounceDamping` → `bounciness`）
  - 调整摩擦力（`friction`）
  - 调整阻尼（`linearDamping` → `drag`）
  - 调整质量（`mass`）影响碰撞响应

---

## 四、技术要点

### 4.1 Rigidbody 配置要点

- **isKinematic**：设置为 `false`，让物理引擎控制运动
- **useGravity**：
  - **方案 A（推荐）**：设置为 `true`，使用重力让球自然"贴"在桌面上
    - 优点：更真实的物理行为，球会自动贴合桌面，无需手动约束 Y 轴
    - 要求：确保地面/桌面有碰撞器（MeshCollider 或 BoxCollider），防止球下落
    - 注意：需要调整重力强度，确保球不会过度弹跳
  - **方案 B（保守）**：设置为 `false`，保持平面游戏逻辑
    - 优点：与当前几何物理行为一致，无需调整地面碰撞器
    - 缺点：需要手动约束 Y 轴，物理行为不如方案 A 自然
- **constraints**：
  - 如果使用重力（方案 A）：**不锁定 Y 轴**，让物理引擎自然处理
  - 如果不使用重力（方案 B）：锁定 Y 轴位移 `FreezePositionY`
- **collisionDetectionMode**：根据速度选择合适的检测模式
  - 低速：`Discrete`（默认，性能好）
  - 高速：`Continuous` 或 `ContinuousDynamic`（防止穿透）
- **interpolation**：考虑使用 `Interpolate` 提高平滑度

### 4.2 碰撞检测要点

- **LayerMask 配置**：确保 `geometryWallMask` 和 `geometryBallMask` 正确配置
- **碰撞矩阵**：确保碰撞矩阵允许必要的碰撞
- **碰撞器类型**：选择合适的碰撞器类型（Sphere, Capsule, Box）
- **物理材质**：为不同对象配置不同的 `PhysicsMaterial`

### 4.3 速度控制要点

- **直接设置 velocity**：适用于精确控制速度的场景
  - `Rigidbody.velocity = new Vector3(velocity.x, rb.velocity.y, velocity.z)`（保持 Y 轴速度）
  - 或 `Rigidbody.velocity = new Vector3(velocity.x, 0, velocity.z)`（如果锁定 Y 轴）
- **使用 AddForce**：适用于需要加速度的场景
  - `Rigidbody.AddForce(force, ForceMode.Impulse)` 或 `ForceMode.VelocityChange`
- **速度限制**：
  - 使用 `BallData.maxSpeed` 作为限制值
  - 在 `FixedUpdate()` 中检查 `Rigidbody.velocity.magnitude`
  - 如果超过限制，将速度归一化并乘以 `maxSpeed`
- **停止检测**：
  - 使用 `BallData.stopThreshold` 作为阈值
  - 在 `FixedUpdate()` 中检查 `Rigidbody.velocity.magnitude < stopThreshold`
  - 触发停止事件并可能将速度置零

### 4.4 轨迹预测要点

- **PhysicsScene.Simulate()**：使用独立物理场景进行预测
- **多场景模拟**：创建独立的物理场景，复制对象进行模拟
- **性能优化**：限制模拟步数，使用合适的时间步长
- **准确性验证**：确保预测轨迹与实际轨迹一致

---

## 五、风险与挑战

### 5.1 技术风险

- **碰撞检测差异**：Unity 物理的碰撞检测可能与几何物理有差异
- **速度控制精度**：Rigidbody 的速度控制可能不如几何物理精确
- **轨迹预测复杂性**：实现准确的轨迹预测可能需要复杂的多场景模拟
- **性能开销**：Rigidbody 物理可能带来额外的性能开销
- **重力相关风险**：
  - 如果使用重力：需要确保地面碰撞器配置正确，防止球下落或穿透
  - 重力可能导致球在碰撞时产生轻微的垂直弹跳，需要调整物理材质参数
  - 重力强度需要根据游戏需求调整，过强可能导致球过度弹跳

### 5.2 兼容性风险

- **现有配置**：可能需要重新配置所有物理参数
- **预制体**：可能需要更新所有预制体的物理组件配置
- **场景设置**：可能需要调整场景中的物理设置

### 5.3 游戏性风险

- **手感变化**：物理行为的变化可能影响游戏手感
- **平衡调整**：可能需要重新平衡游戏机制
- **玩家反馈**：玩家可能注意到物理行为的变化

---

## 六、回退策略

### 6.1 分支管理

- 创建新分支 `feature/rigidbody-rollback`
- 保留几何物理代码，通过条件编译或开关控制
- 便于回滚和对比测试

### 6.2 渐进式迁移

- 先实现核心功能，确保基本功能正常
- 逐步迁移依赖系统
- 每个阶段进行充分测试

### 6.3 回滚方案

- 保留几何物理代码作为备份
- 通过配置开关切换物理系统
- 如果回退失败，可以快速回滚到几何物理

---

## 七、成功标准

### 7.1 功能标准

- ✅ 所有现有功能正常工作
- ✅ 碰撞检测准确可靠
- ✅ 没有穿透问题
- ✅ 轨迹预测准确

### 7.2 性能标准

- ✅ 物理更新性能可接受
- ✅ 碰撞检测性能可接受
- ✅ 轨迹预测性能可接受

### 7.3 质量标准

- ✅ 代码质量符合项目标准
- ✅ 没有明显的 Bug
- ✅ 游戏手感良好
- ✅ 玩家体验不受影响

---

## 八、后续优化

### 8.1 性能优化

- 优化物理更新频率
- 优化碰撞检测范围
- 使用对象池减少物理对象创建

### 8.2 功能增强

- 添加物理材质系统
- 支持更复杂的碰撞形状
- 增强轨迹预测功能

### 8.3 代码优化

- 重构物理相关代码
- 提取公共逻辑
- 优化代码结构

---

## 九、时间表

| 阶段 | 任务 | 预计时间 | 状态 |
|------|------|---------|------|
| 阶段 1 | 核心物理系统重构 | 15-20 小时 | 未开始 |
| 阶段 2 | 移动和发射系统重构 | 5-8 小时 | 未开始 |
| 阶段 3 | 事件和轨迹预测重构 | 12-18 小时 | 未开始 |
| 阶段 4 | 测试和调试 | 8-12 小时 | 未开始 |
| **总计** | | **47-66 小时** | **未开始** |

---

## 十、参考资料

- Unity Rigidbody 文档
- Unity Physics 文档
- Unity Collision Detection 文档
- `MultiScene_Physics_Trajectory_Prediction_Plan.md`（多场景物理模拟方案）
- `BallPhysicsGeometryPlan.md`（当前几何物理系统设计文档）

---

**文档版本**：1.0  
**创建日期**：2024  
**最后更新**：2024  
**状态**：计划阶段

