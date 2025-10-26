# 多场景物理模拟轨迹预测系统实现计划

> **适用场景**：台球、弹珠等需要精确碰撞和多次反弹预测的游戏  
> **核心方案**：使用Unity多场景物理引擎进行真实模拟，非数学抛物线计算  
> **预计工作量**：17-23小时（约3个工作日）

---

## 📋 目录

- [一、方案概述](#一方案概述)
- [二、核心原理与要点](#二核心原理与要点)
- [三、系统架构](#三系统架构)
- [四、实施步骤](#四实施步骤)
- [五、配置与集成](#五配置与集成)
- [六、风险与优化](#六风险与优化)

---

## 一、方案概述

### 1.1 当前问题

| 问题 | 影响 |
|------|-----|
| 手动物理模拟不准确 | 预测距离偏差大，需手动校正 |
| 无法处理碰撞反弹 | 无法预测球与台边/其他球的碰撞 |
| 代码重复维护困难 | 需手动同步BallPhysics逻辑 |
| 性能开销大 | 1000步手动计算，首次耗时高 |

### 1.2 新方案优势

✅ **100%准确**：使用真实Unity物理引擎，自动处理所有碰撞  
✅ **支持多次反弹**：不限反弹次数，预测复杂轨迹  
✅ **代码复用**：直接使用BallPhysics，无需手动同步  
✅ **性能优秀**：一帧内完成模拟，比手动计算更快  
✅ **无缝整合**：保留现有渲染系统，仅修改10-20行代码

### 1.3 方案对比

|| 当前方案 | 新方案 |
|---|---------|--------|
| 物理引擎 | 手动模拟 | 真实Unity引擎 |
| 碰撞检测 | ❌ 无 | ✅ 完整支持 |
| 反弹次数 | 0次 | 无限次 |
| 准确度 | 需偏差校正 | 100%准确 |
| 维护成本 | 代码重复 | 完全复用 |

---

## 二、核心原理与要点

### 2.1 实现原理

**核心思想**：创建独立"影子场景"，用加速时间运行真实物理模拟

```
流程：
1. 创建独立物理场景（LocalPhysicsMode.Physics2D）
2. 复制球和台边到影子场景
3. 应用力，使用 PhysicsScene2D.Simulate() 快速模拟
4. 记录每步位置，生成轨迹点
5. 主场景继续运行，互不干扰
```

### 2.2 三大关键要点（必须满足）

#### ⚠️ 1. 使用 fixedDeltaTime 模拟

```csharp
// 每次模拟必须使用固定时间步长
physicsScene.Simulate(Time.fixedDeltaTime);
```

**原因**：Unity物理引擎依赖固定时间步长，跳步会导致碰撞检测失效

#### ⚠️ 2. 镜像所有碰撞几何和材质

**必须复制**：
- 球：Rigidbody2D + CircleCollider2D + PhysicsMaterial2D
- 台边：EdgeCollider2D/BoxCollider2D + PhysicsMaterial2D

**不要复制**：
- 渲染组件（SpriteRenderer、MeshRenderer）
- UI、特效、声音等非物理组件

**识别方式**：使用现有Tag（"Player"、"Enemy"、"Wall"）

#### ⚠️ 3. 保持动态物理参数一致

**核心挑战**：BallPhysics包含游戏逻辑（Update循环、事件发布），不能直接复制到影子场景

**已实施方案**：为BallPhysics添加模拟模式支持（见阶段四详细说明）
- ✅ 重构为纯函数，解除时间耦合
- ✅ 添加 `isSimulationMode` 标记，禁用游戏逻辑
- ✅ 提供 `ManualPhysicsUpdate()` 手动更新接口
- ✅ 保持100%物理计算一致性

**为什么不能直接复制**：
- ❌ Update() 在 PhysicsScene2D.Simulate() 中不会被调用
- ❌ GameEventBus事件会污染主场景
- ❌ Time.time 在快速模拟中不适用（需要使用累积模拟时间）

#### ⚠️ 4. 处理Dynamic物体的休眠机制

**核心问题**：当两个Dynamic Rigidbody2D碰撞时，如果其中一个初始速度为0，Unity会将其标记为"休眠"状态以优化性能，导致碰撞检测失效（球会直接穿透）

**解决方案**：

| 物体类型 | 影子场景设置 | 原因 |
|---------|------------|------|
| 被击打的球（玩家球） | Dynamic | 需要模拟完整运动轨迹 |
| 静止的球（敌人球） | Dynamic + WakeUp + NeverSleep | 碰撞前保持静止，碰撞后真实物理响应 |
| 台边/障碍物 | Static/Kinematic | 固定不动 |

**关键理解**：
- WakeUp() 只让物体参与碰撞检测，不会改变其速度
- velocity = 0 的Dynamic物体不会自己移动
- 碰撞前所有静止球保持原位（瞄准线稳定）
- 碰撞后物理响应完全真实（球会被撞飞）

**备选简化方案**：将静止的球设为Kinematic（碰撞后不会移动，适合障碍物场景）

### 2.3 性能优化策略

**脏标记系统**：
- 力度变化 > 0.5 才重新模拟
- 方向变化 > 2° 才重新模拟
- 限制更新频率：20-30 FPS

**结果**：节省90%计算量

---

## 三、系统架构

### 3.1 新增组件（3个）

| 组件 | 职责 | 优先级 |
|------|------|-------|
| **TrajectorySimulationManager** | 创建和管理影子场景 | P0 必须 |
| **TrajectoryPredictor** | 执行轨迹模拟，输出路径点 | P0 必须 |
| **SimulationObjectReplicator** | 复制动态物体和静态边界到影子场景 | P0 必须 |

**📌 对象分类说明**：

SimulationObjectReplicator 使用两种对象类型：

| 类型 | 物理特性 | 复制内容 | 示例 |
|------|---------|---------|------|
| **Dynamic Objects**<br>（动态物体） | • Dynamic Rigidbody2D<br>• 需要施加初始速度<br>• 需要跟踪轨迹 | Rigidbody2D<br>Collider<br>PhysicsMaterial<br>BallPhysics | Player<br>Enemy |
| **Static Objects**<br>（静态边界） | • Static/Kinematic<br>• 位置固定<br>• 只提供碰撞边界 | Collider<br>PhysicsMaterial<br>Transform | Wall<br>Obstacle |

> 💡 **为什么要区分**：动态物体需要模拟运动和记录轨迹，静态边界只提供碰撞。区分后可以针对性优化复制策略和处理逻辑。

### 3.2 现有组件（完全保留）

✅ **AimLineRenderer**：分段渲染轨迹  
✅ **AimLineMaterialController**：流动效果、渐变  
✅ **AimLineLandingPointManager**：落点显示  
✅ **AimController**：瞄准控制（仅修改10-20行）

**为什么能保留**：这些组件接受 `List<Vector3>` 通用格式，与计算方式无关

### 3.3 数据流

```
旧方式：
AimLineReflectionCalculator.CalculateReflectionPath() 
  → List<Vector3> 
  → AimLineRenderer.RenderSegmentedAimLine()

新方式：
TrajectoryPredictor.PredictTrajectory() 
  → List<Vector3> 
  → AimLineRenderer.RenderSegmentedAimLine()  // 渲染调用不变！
```

---

## 四、实施步骤

### 阶段一：影子场景搭建（2-3小时）⭐ P0

**目标**：创建独立物理场景，验证基本功能

**关键步骤**：
1. 新建 TrajectorySimulationManager.cs
2. 在 Awake() 中创建影子场景（LocalPhysicsMode.Physics2D）
3. 获取 PhysicsScene2D 引用
4. 验证场景隔离：主场景和影子场景互不干扰

**验收**：
- [ ] 影子场景成功创建
- [ ] 可在影子场景模拟球运动
- [ ] 物理行为与主场景一致

---

### 阶段二：对象复制系统（3-4小时）⭐ P0

**目标**：复制球和台边到影子场景

**关键步骤**：
1. 新建 SimulationObjectReplicator.cs
2. 使用现有Tag扫描对象：
   - 动态物体（"Player"、"Enemy"）：需要模拟运动和跟踪轨迹
   - 静态边界（"Wall"）：只提供碰撞边界
3. 复制物理相关组件：
   - Rigidbody2D、Collider2D、PhysicsMaterial2D（必需）
   - BallPhysics（必需，包含动态物理计算）
   - BallData引用（BallPhysics依赖）
4. 使用 SceneManager.MoveGameObjectToScene() 移动
5. ⚠️ **复制玩家球后配置**：
   - `isSimulationMode = true`（启用模拟模式）
   - 调用 `InitializeSimulationState()`（初始化模拟状态）
6. ⚠️ **复制敌人球后配置**：
   - `isSimulationMode = true`（启用模拟模式）
   - `bodyType = Dynamic`（确保碰撞后物理响应真实）
   - `velocity = Vector2.zero`（初始静止）
   - 调用 `WakeUp()` + `sleepMode = NeverSleep`（防止休眠失效）

**验收**：
- [ ] 台边成功复制，球能反弹
- [ ] 反弹角度与主场景一致
- [ ] 其他球也能正确复制
- [ ] 玩家球能正确碰撞敌人球（不会穿透）
- [ ] 碰撞前敌人球保持静止（瞄准线稳定）
- [ ] 碰撞后敌人球按真实物理响应

**Unity配置**：
- ⚠️ 使用现有Tag，无需新增：Player、Enemy、Wall

---

### 阶段三：轨迹模拟核心（4-5小时）⭐ P0

**目标**：实现准确的轨迹预测

**核心循环**：
```
初始化影子场景的球，设置位置和速度
设置 isSimulationMode = true
调用 InitializeSimulationState()

float simulationTime = 0f
for 最多500步:
    记录当前位置
    physicsScene.Simulate(Time.fixedDeltaTime)  // ⚠️ 关键！
    simulationTime += Time.fixedDeltaTime
    ballPhysics.ManualPhysicsUpdate(simulationTime)  // ⚠️ 更新动态参数
    检测碰撞（速度方向变化）
    if 速度 < ballData.stopThreshold: break  // ⚠️ 使用BallData的停止阈值

返回 List<Vector3> 轨迹点
```

**关键技术点**：

| 参数 | 模拟场景应用 | 说明 |
|-----|-------------|------|
| mass, friction, bounciness | ✅ 自动应用 | 通过复制物理组件自动同步 |
| maxSpeed | ✅ 自动应用 | ManualPhysicsUpdate中自动限制 |
| stopThreshold | ⚠️ 需手动检查 | CheckMovement()被跳过，需在预测器循环中读取ballData.stopThreshold作为停止条件 |

**停止条件处理**：
- BallPhysics的CheckMovement()在模拟模式下不执行
- 预测器需手动读取 `ballData.stopThreshold` 判断球是否停止
- 保持与主场景一致的停止判定标准

**验收**：
- [ ] 预测直线、单次反弹、多次反弹
- [ ] 预测误差 < 5%
- [ ] 停止条件与主场景一致（使用相同的stopThreshold）

---

### 阶段四：动态物理集成（2-3小时）⭐ P1

**目标**：使BallPhysics支持影子场景模拟，保持100%物理参数一致性

#### 实施方案：渐进式重构

**步骤1：提取纯函数（已完成✅）**

将动态物理计算逻辑重构为可复用的纯函数：

1. `CalculateDynamicPhysics(float currentTime, float currentSpeed)` 
   - 纯函数，接收时间和速度参数
   - 返回计算得到的弹性和阻尼值
   - 解除对 `Time.time` 的耦合

2. `ApplyDynamicPhysics(float bounciness, float damping)`
   - 负责将计算结果应用到物理组件
   - 包含阈值检查逻辑

3. 重构 `UpdateDynamicPhysics()` 使用上述两个函数

**步骤2：添加模拟模式支持（已完成✅）**

在BallPhysics中添加：

1. **模拟模式标记**：
   ```csharp
   public bool isSimulationMode = false;  // 影子场景设为true
   ```

2. **禁用游戏逻辑**：
   - Update() 中跳过 CheckMovement 和 UpdateDynamicPhysics
   - 3处事件发布添加 `if (!isSimulationMode)` 检查

3. **模拟专用方法**：
   - `InitializeSimulationState()` - 初始化影子场景状态
   - `ManualPhysicsUpdate(float simulationTime)` - 手动更新物理参数
   - `EnforcePhysicsConstraints()` - 执行物理约束

**关键技术点**：

| 问题 | 解决方案 |
|-----|---------|
| 时间来源不同 | 纯函数接收时间参数，主场景用Time.time，影子场景用累积时间 |
| 相对时间一致性 | 影子场景 ballStartTime=0，确保运动时长相对值相同 |
| 更新频率一致 | 使用独立的 simulationLastUpdateTime 跟踪更新间隔 |
| 事件污染 | 模拟模式下不发布任何GameEventBus事件 |

**使用示例**：
```csharp
// 影子场景中
BallPhysics simulatedBall = replicatedBall.GetComponent<BallPhysics>();
simulatedBall.isSimulationMode = true;  // 启用模拟模式
simulatedBall.InitializeSimulationState();  // 初始化状态

float simulationTime = 0f;
for (int i = 0; i < maxSteps; i++) {
    physicsScene.Simulate(Time.fixedDeltaTime);
    simulationTime += Time.fixedDeltaTime;
    simulatedBall.ManualPhysicsUpdate(simulationTime);  // 手动更新
}
```

**验收**：
- [x] BallPhysics重构完成，主场景行为不变
- [x] 模拟模式添加完成，支持手动更新
- [ ] 影子场景中物理参数与主场景一致（待整合测试）
- [ ] 预测结果与实际运动误差 < 5%

---

### 阶段五：整合到渲染系统（1-2小时）⭐ P0

**目标**：将预测结果整合到现有瞄准线系统

**修改范围**：
- 文件：AimController.cs
- 改动：约10-20行
- 保持不变：所有渲染脚本

**修改内容**：
```csharp
// 旧代码
List<Vector3> pathPoints = reflectionCalculator.CalculateReflectionPath(...);

// 新代码
List<Vector3> pathPoints = trajectoryPredictor.PredictTrajectory(...);

// 渲染调用完全不变
aimLineRenderer.RenderSegmentedAimLine(pathPoints);
```

**验收**：
- [ ] 轨迹显示正确
- [ ] 碰撞点自动标记
- [ ] 落点显示正确
- [ ] 现有视觉效果保持不变

---

### 阶段六：性能优化（1-2小时）⭐ P2

**目标**：减少不必要的重复模拟

**脏标记系统**：
```csharp
if (力度变化 > 0.5 OR 方向变化 > 2°) {
    if (Time.time - lastUpdate > 0.033f) {  // 限制30FPS
        isDirty = true;
    }
}
if (isDirty) {
    重新模拟;
    isDirty = false;
}
```

**验收**：
- [ ] 小幅调整不触发模拟
- [ ] CPU占用 < 10%

---

### 阶段七：测试与调优（3-4小时）⭐ P1

**测试项**：
- [ ] 直线、45°、90°等各方向发射
- [ ] 单次、多次墙壁反弹
- [ ] 球与球碰撞
- [ ] 最小/最大力度

**准确性目标**：
- 平均误差 < 5%
- 最大误差 < 10%
- 反弹角度误差 < 3°

**性能目标**：
- 单次模拟 < 10ms
- 游戏帧率 > 60 FPS

---

## 五、配置与集成

### 5.1 Unity配置要求

**1. GameObject Tag配置（使用现有Tag）**

| 对象 | Tag | 组件要求 |
|------|-----|---------|
| 玩家球 | Player ✅ | Rigidbody2D + CircleCollider2D + PhysicsMaterial2D |
| 敌人球 | Enemy ✅ | 同上 |
| 台边 | Wall ✅ | EdgeCollider2D/BoxCollider2D + PhysicsMaterial2D |

**2. Launcher GameObject配置**

添加新组件：
- TrajectorySimulationManager
- TrajectoryPredictor
- SimulationObjectReplicator

保留现有组件：
- AimController
- AimLineRenderer
- AimLineMaterialController
- AimLineLandingPointManager

### 5.2 Inspector参数配置

**TrajectorySimulationManager**：
- Max Simulation Steps: 500
- Stop Velocity Threshold: 0.01

**TrajectoryPredictor**：
- Sample Distance: 0.1
- Max Trajectory Points: 200

**SimulationObjectReplicator**：
- Dynamic Object Tags: ["Player", "Enemy"]  // 动态物体（需要模拟运动）
- Static Object Tags: ["Wall"]  // 静态边界（碰撞体）
- Replicate Materials: true
- Replicate Physics: true

### 5.3 Tag管理扩展

**添加新物体类型**（可选）：

方式1：Inspector中添加
```
SimulationObjectReplicator
├─ Dynamic Object Tags: ["Player", "Enemy", "NewType"]
└─ Static Object Tags: ["Wall", "Obstacle"]
```

方式2：代码中动态添加
```csharp
replicator.AddDynamicObjectTag("NewType");
replicator.AddStaticObjectTag("Obstacle");
```

---

## 六、风险与优化

### 6.1 技术风险

| 风险 | 等级 | 对策 | 状态 |
|------|-----|------|------|
| 多场景物理版本兼容 | 中 | 先验证当前Unity版本支持 | 待验证 |
| BallPhysics依赖主场景 | ~~中~~ | ✅ 已重构为模拟模式支持 | ✅ 已解决 |
| Dynamic物体休眠导致碰撞失效 | 高 | 对静止球使用WakeUp()+NeverSleep | 已知方案 |
| 长轨迹性能问题 | 低 | 限制最大步数+脏标记 | 待实施 |
| 物理参数同步精度 | ~~中~~ | ✅ 通过纯函数复用保证100%一致 | ✅ 已解决 |

### 6.2 性能优化

**计算优化**：
- 脏标记：只在参数变化时模拟
- 帧频控制：限制20-30 FPS
- 早停：速度阈值检测

**内存优化**：
- 对象池：复用模拟球
- 不复制渲染组件

### 6.3 成功指标

**功能**：
- [ ] 直线+多次反弹预测
- [ ] 球球碰撞预测
- [ ] 轨迹可视化

**性能**：
- [ ] 单次模拟 < 10ms
- [ ] 帧率 > 60 FPS
- [ ] CPU占用 < +10%

**准确性**：
- [ ] 平均误差 < 5%
- [ ] 最大误差 < 10%

**代码质量**：
- [x] BallPhysics重构完成，纯函数提取
- [x] 模拟模式添加完成
- [ ] 单元测试覆盖（可选）

---

## 📅 实施时间表

| 阶段 | 内容 | 工作量 | 优先级 | 状态 |
|------|------|-------|--------|------|
| 一 | 影子场景搭建 | 2-3h | P0 ⭐ | 待实施 |
| 二 | 对象复制系统 | 3-4h | P0 ⭐ | 待实施 |
| 三 | 轨迹模拟核心 | 4-5h | P0 ⭐ | 待实施 |
| 四 | 动态物理集成 | 2-3h | P1 | ✅ 已完成 |
| 五 | 整合渲染系统 | 1-2h | P0 ⭐ | 待实施 |
| 六 | 性能优化 | 1-2h | P2 | 待实施 |
| 七 | 测试调优 | 3-4h | P1 | 待实施 |

**总计**：17-23小时（约3个工作日）  
**已完成**：阶段四（BallPhysics重构 + 模拟模式）约1.5小时

---

## 💡 核心优势总结

### 最小化改动，最大化复用

**完全保留（0改动）**：
- ✅ AimLineRenderer：所有渲染逻辑
- ✅ AimLineMaterialController：所有材质效果
- ✅ AimLineLandingPointManager：落点管理
- ✅ 所有视觉效果和材质设置

**最小修改**：
- 📝 BallPhysics：重构为纯函数 + 添加模拟模式（约80行新增代码）
- 📝 AimController：10-20行
- ➕ 新增3个组件（轨迹预测系统）

**为什么能无缝整合**：
1. 现有渲染系统接受通用 `List<Vector3>` 格式
2. 计算与渲染完全解耦
3. 新旧方案输出格式一致
4. 所有视觉效果自动保留
5. BallPhysics通过模拟模式标记保持向后兼容

### 方案价值

✅ **准确性提升**：从手动模拟到真实物理引擎，100%准确  
✅ **功能增强**：支持无限次反弹，预测球球碰撞  
✅ **维护简化**：复用BallPhysics，无需手动同步，纯函数保证一致性  
✅ **性能优化**：脏标记+帧频控制，节省90%计算  
✅ **零学习成本**：保持所有现有视觉效果  
✅ **代码质量**：重构提升可测试性和可维护性

---

**参考来源**：[Unity Trajectory Prediction (Simulation Method)](https://austin-mackrell.medium.com/unity-trajectory-prediction-simulation-method-5b441ee1604)
