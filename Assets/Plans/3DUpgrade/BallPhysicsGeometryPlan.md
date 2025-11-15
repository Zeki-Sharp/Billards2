# 球体物理几何化方案（BallPhysics Geometry-Based Design）

> 目标：用“几何反射 + 简单速度衰减”的方式重写球体运动与碰撞逻辑，弱化对通用刚体/摩擦/阻尼参数的依赖，使轨迹更可预测、更易调参，并与影子场景轨迹预测共享同一套模拟逻辑。

---

## 1. 设计目标与约束

- **可预期的反射角**：入射角 ≈ 反射角，撞墙/边界时轨迹符合直觉，不受复杂摩擦/迭代误差影响。
- **简单可控的减速**：使用少量参数（基础衰减率、碰撞损耗系数等）独立控制“滚动距离/停球位置”，避免多重阻尼叠加。
- **统一的模拟函数**：实时战斗与影子场景（轨迹预测）共用一套 `SimulateStep` 逻辑，保证“预测线 = 实际轨迹”。
- **支持多种碰撞对象**：
  - 球 ↔ 墙/边界（平面/线段类型）；
  - 球 ↔ 球（玩家球、敌人球等）；
  - 球 ↔ 特殊障碍（非球体、非墙，如机关、陷阱）。
- **与现有框架兼容**：保持 `BallPhysics` 的事件契约（Started/Stopped/Collision）、`DamageSystem` 所需的数据结构、黑板与技能系统的触发点。

---

## 2. 状态建模（数据层）

**核心状态（每个球体）**

- **位置**：`position`（Vector3），约束在 XZ 平面，Y 由场景高度/碰撞体决定。
- **方向**：`direction`（Vector3 单位向量，主要在 XZ 平面）。
- **速度标量**：`speed`（float，单位：单位/秒），不直接暴露刚体的 `velocity`，而是逻辑层自主管理。
- **半径/边界信息**：
  - `radius`（float，用于几何碰撞推算）。
  - 可选：不同球型/皮肤的“逻辑半径”和“渲染半径”分离。
- **物理参数**：
  - `bounceFactor`：碰撞后速度保留比例（0–1）。
  - `rollingDamping`：运动过程中的基础衰减率（线性或近似指数）。
  - `minSpeedThreshold`：低于该速度视为停止，触发 Stopped 事件。
- **运行时辅助状态**：
  - `isMoving`、`movementStartTime`（用于事件与统计）。
  - `lastCollisionNormal` / `lastReflectionDirection`（调试与三角形攻击等扩展）。

**与 Unity 物理组件的关系**

- 刚体/Collider 的职责：
  - 只提供**碰撞体形状与法线信息**（通过射线检测或重叠检测获取接触点与法线）。
  - 在需要时防止穿模（例如硬约束 Y 高度，或在极端情况回退到物理引擎）。
- 球的“运动学变量”（position/direction/speed）由 BallPhysics 逻辑层独立维护，不必与 `Rigidbody.velocity` 一一对应。

---

## 3. 时间步与模拟流程（SimulateStep 概念）

**核心思想**：用统一的 `SimulateStep(dt)` 函数推进球体状态，不区分“实时场景”与“影子场景”。

**单步流程概念**（不写具体代码）：

1. **输入**：当前 `position / direction / speed` 与时间步长 `dt`。
2. **若 speed 低于阈值**：
   - 将速度归零、触发 Stopped 流程（如果此前是 Moving 状态），返回。
3. **预测理想位移**（无碰撞）：
   - `delta = direction * speed * dt`；
   - `targetPos = position + delta`。
4. **碰撞检测阶段**：
   - 对所有可能发生碰撞的对象执行“沿运动方向的几何检测”，找到**最近的一次碰撞**（比例 `t`，0–1）：
     - 对墙/边界：线段/平面交点检测；也可以通过射线 + Unity Collider 提供的法线。
     - 对其他球：两圆（球）相交的解析解，求出最早的接触时间。
     - 对特殊障碍：射线/线段与 collider 的最近接触。
5. **若本步内无碰撞**：
   - `position = targetPos`。
   - 应用基础减速：`speed = ApplyRollingDamping(speed, dt)`。
   - 返回。
6. **若本步内发生一次或多次碰撞**：
   - 将位置推进到**第一次碰撞点**：
     - `position = position + direction * speed * dt * t`。
   - 根据碰撞类型调用不同的**响应策略**（见下一节）计算新的 `direction'` 与 `speed'`。
   - 剩余时间 `(1 - t) * dt` 可选择：
     - 再次调用 `SimulateStep(剩余时间)`（递归/迭代），允许一次时间步内处理多次碰撞；
     - 或将剩余时间累积到下一帧，控制每帧最多处理 N 次碰撞，避免极端耗时。

> 约束：在实际实现时，需要限制“每帧最多迭代次数”和“全局最小 dt”，保证性能与稳定性。

---

## 4. 碰撞类型与响应策略

### 4.1 球 ↔ 墙 / 边界

- **法线来源**：
  - 简单边界：根据墙的朝向直接使用固定法线（例如 +X / -X / +Z / -Z）。
  - 复杂几何：通过射线与碰撞体交点取得 `hit.normal`，再投影到 XZ 平面。
- **响应规则**：
  - 新方向：`direction' = Reflect(direction, wallNormal)`（几何反射，保证入射角≈反射角）。
  - 新速度：`speed' = speed * bounceFactor_wall`（墙体可以有单独的损耗系数）。
  - 维护视觉/事件：记录 `lastCollisionNormal`、触发碰撞事件（用于特效/伤害等）。

### 4.2 球 ↔ 球（质量相同）

- **法线**：以两球中心向量为法线：`n = (p2 - p1).normalized`。
- **速度分解与交换**：
  - 将两球当前速度向量（`v1 = direction1 * speed1`，`v2 = direction2 * speed2`）分解为：
    - 沿 `n` 的分量（法线方向）与垂直于 `n` 的分量（切向）。
  - 在完全弹性且等质量的情形下：
    - 交换两者沿 `n` 方向的分量（切向不变），再重新组合得到新的 `v1' / v2'`。
  - 在非完全弹性情形下：
    - 引入一个 `ballCollisionBounceFactor` 控制法线分量的衰减。
- 最后更新：
  - `direction1' = v1'.normalized`，`speed1' = |v1'|`；
  - `direction2' = v2'.normalized`，`speed2' = |v2'|`。

### 4.3 球 ↔ 特殊障碍（非球体）

- **法线与命中信息**：依赖 Unity 碰撞体或自定义几何（例如圆柱柱体、坡道等）。
- **响应策略分类**：
  - 反射型：类似墙，使用几何反射 + 折损系数。
  - 吸收/减速型：只修改速度标量或方向（如泥潭、缓冲区）。
  - 触发器型：不改变运动（或轻微改变），但触发技能/机关事件。
- 对于复杂形状（例如“斜面 + 几何装饰”），可以约定一套**简化碰撞体**：只用看不到的简化 Collider 提供法线，确保几何反射稳定。

---

## 5. 速度衰减与停球策略

**基础衰减模型（仅描述概念，不限定数学形式）**

- 常用两种：
  - **线性衰减**：`speed = max(0, speed - k * dt)`，适用于“均匀阻力”的直觉。
  - **近似指数衰减**：`speed = speed * exp(-lambda * dt)`，视觉上更接近“快速减速后长尾滑行”。
- 不再依赖多个来源的阻尼（刚体 drag、动态阻尼曲线、时间阻尼叠加），而是统一归约到一两个参数。

**停球判定**

- 当 `speed < minSpeedThreshold`：
  - 将速度直接置零；
  - 触发 `BallStopped` 事件（包括位置、停球时间、最后轨迹等）。
- 停球后状态：
  - 关闭后续 `SimulateStep`，直到下一次发射/外力触发；
  - 为轨迹预测和技能系统提供明确的“终点”。

---

## 6. 影子场景与轨迹预测（Shadow Simulation）

**核心目标**：预测轨迹使用的逻辑 = 实际运行使用的逻辑。

### 6.1 预测流程（概念）

- 从当前玩家球状态缓存：
  - `position0 / direction0 / speed0`；
  - 当前场景的墙体/球体/障碍简化列表（可以是数据快照）。
- 在“影子模拟器”中循环调用 `SimulateStep(dtPredict)`：
  - 每一步把新的 `position` 记录到轨迹点列表；
  - 若发生碰撞，记录碰撞类型与事件（用于 UI 提示，例如预计首个撞击对象）。
  - 当速度降到阈值以下或迭代次数达到上限时停止。
- 将轨迹点列表用于：
  - `LineRenderer` 样条或逐点线段；
  - 影子球/小光点沿路径运动的可视化；
  - UI 提示（落点圈、预计命中目标高亮）。

### 6.2 与当前 Shadow Scene 的衔接

- 保留现有“影子场景”的架构，但其 BallPhysics 实现将改为**直接调用相同的几何 Simulate**：
  - 影子场景不需要真实刚体，只需要场景静态几何信息与 BallPhysics 的模拟接口。
  - `isSimulationMode` 逻辑从“调用 Rigidbody2D 物理”切换为“禁用实时 Update，只在控制器中手动调用 SimulateStep”。
- 确保：
  - 同一份 BallData/参数在主场景与影子场景中使用；  
  - 预测和实际的时间步、碰撞几何完全一致，以避免偏差累积。

---

## 7. 集成到现有系统的方式

- **BallPhysics 角色**：
  - 从“封装 2D/3D 刚体 + 复杂阻尼”的组件，转变为“持有几何状态 + 提供 SimulateStep + 发布事件”的逻辑核心。
- **GameEventBus 事件**：
  - 保持 `OnBallStarted`、`OnBallStopped`、`OnCollision` 等事件接口不变；
  - 事件 payload 内的速度/方向字段从 `Rigidbody` 读取改为从 BallPhysics 的几何状态读取。
- **DamageSystem / 技能系统**：
  - 继续使用 `CollisionEvent` / `StoppedEvent`，但其中的轨迹点、法线、速度由几何层提供；
  - 三角形攻击/范围攻击等算法可以直接基于“几何路径”而不是物理采样点。
- **配置与调参**：
  - 在 BallData 中新增/收敛参数：
    - `bounceFactor_wall` / `bounceFactor_ball`；
    - `rollingDamping` / `minSpeedThreshold`；
    - 可选：预测迭代步数、单步 dt 等。
  - 为调试提供一个简单 Inspector 面板，实时观察反射角、速度衰减曲线。

---

## 8. 风险与验证计划

- **风险点**
  - 从“刚体驱动”转向“自定义模拟”会改变所有与速度相关的行为（伤害、特效、AI 预测）。
  - 球 ↔ 球多次连锁碰撞在极端情况下可能需要更多子步，需注意性能与上限控制。
  - 与现有 3D 物理（敌人、道具、机关）交互时，需要明确哪些对象仍使用 Unity 物理、哪些只参与几何模拟。
- **验证策略**
  1. 单球 + 单面墙测试：验证反射角、距离衰减是否符合预期（对比几何计算与录屏）。
  2. 单球 + 多面矩形桌：连续多次弹墙，观察轨迹是否稳定、无角度漂移。
  3. 双球碰撞：验证动量交换是否符合“肉眼合理”的台球直觉。
  4. 与现有影子轨迹系统对比：确保预测线与真实运动重合在可接受误差范围内。
  5. 混合场景（球 + 敌人 + 道具）：确认事件、伤害、特效仍按预期触发。

---

## 9. 推进顺序建议（BallPhysics 子计划）

1. **Phase G0 – 几何原型**  
   - 在独立测试场景中实现/验证 `SimulateStep` 原型：单球 + 静态墙体，关闭所有旧阻尼/时间系统。
2. **Phase G1 – 球 ↔ 球 & 影子预测联调**  
   - 加入球 ↔ 球碰撞逻辑；  
   - 将影子场景改为调用几何模拟，完成“预测线 = 实际轨迹”的验证。
3. **Phase G2 – 与现有系统集成**  
   - 替换当前战斗场景中的 BallPhysics 驱动方式，统一走几何模拟；  
   - 同步更新 DamageSystem、技能与特效的使用方式。
4. **Phase G3 – 参数固化与清理**  
   - 删除不再使用的 2D Physics/复杂阻尼代码；  
   - 将最终可用参数整理进 `BallData` 和调参指南文档。

> 完成 G2 之后，即可认为“球体物理”从引擎依赖型改造为可控的几何驱动型，为后续更复杂的关卡与技能设计打下基础。

---

## 10. 实施步骤拆解（BallPhysics 几何重构）

> 目标：将上文方案落地到 `BallPhysics` 主脚本与相关 Prefab / SO；按最小可行迭代推进，避免一次性大改导致不可控风险。

| 序号 | 步骤 | 关键内容 | 输出/验证 |
| --- | --- | --- | --- |
| **S1** | **状态接入** | 在 `BallPhysics` 中添加几何状态字段（`direction/speed/isMoving/elapsedTime/sphereRadius` 等）及分段衰减参数、LayerMask、`knockbackScale`。旧 2D/动阻尼字段标记为 Legacy。 | 编译通过；Inspector 可配置与原型一致的参数。 |
| **S2** | **初始化重写** | `InitializePhysics` 只保留 3D 分支：确保 `Rigidbody`（设为 kinematic、锁 Y） + `SphereCollider`，推断 `sphereRadius`；设置初始方向/速度。 | 玩家 3D Prefab 中 `BallPhysics` Inspector 与原型参数一致；进入场景无报错。 |
| **S3** | **SimulateStep 接入** | 将原型 `SimulateStep/HandleWallCollision/HandleBallCollision/ApplyDamping` 移植到 `BallPhysics`，`FixedUpdate` 改为调用几何模 拟；保留事件触发（Start/Stop/Collision）。 | 在干净测试场景中，正式玩家球的反射角、分段衰减、球↔球效果与原型一致。 |
| **S4** | **玩家链路验证** | 在 `Level1_3D` 中用真实玩家发射流程测试：输入、蓄力、发射、BallPhysics 几何运动、事件（Stopped/Collision）是否正常；影子轨迹暂时关闭。 | 玩家回合能正常结束；日志中无旧物理相关警告；QA 验收通过。 |
| **S5** | **玩家参数收敛** | 把所有几何参数迁入 `Player_Physics`（SO），`BallPhysics` 仅保留场景级配置（Layer、半径、调试）。确保所有玩家 prefab/variant 都引用统一 SO。 | Inspector 中不再出现旧物理字段；玩家 SO 参数可一键调整；Prefab 进入场景无覆盖告警。 |
| **S6** | **敌人参数接通** | 为敌人球的配置（SO 或 Prefab）添加 `geometryKnockbackScale`、分段阻尼等参数；验证轻/重敌人在新逻辑下的被击表现。 | 至少两类敌人（轻/重）在几何逻辑下表现不同；与 Damage/AI 事件兼容。 |
| **S7** | **影子场景 & 瞄准线统一** | 抽象 `GeometrySimulator`，`BallPhysics` 的 `SimulateGeometryStep`、影子场景(`SimulationObjectReplicator`)、蓄力瞄准线/轨迹预测(`TrajectoryPredictor`、`ChargeSystem`)全走同一套几何模拟；废弃旧的 2D/动态阻尼路径。 | 影子预测线与实际轨迹一致；蓄力 UI 显示的预测碰撞/停点与真实结果匹配；`isSimulationMode` 仅使用几何模拟。 |
| **S8** | **后续优化** | 根据需要接入弹簧床/加速道具等特殊交互（通过设置 `direction/speed/elapsedTime`）；整理文档、调参指南。 | 特殊交互验证通过；`BallPhysicsGeometryPlan` 标记完成时间。 |

> 注：S1~S6 完成后，旧 2D 路径与动态阻尼系统即可整体移除；所有球体将共享统一的几何物理底层，方便后续内容迭代。

---

## 11. 当前状态 & 待办

- **已完成**：S1 ~ S5，S7（部分）。  
  - S1~S5：玩家实战链路已切换几何物理，参数统一收敛到 `BallData`（SO）。  
  - S7（部分）：已完成几何模拟器抽象和瞄准线改造。
    - ✅ 创建 `GeometryTrajectorySimulator` 和 `GeometrySimulationConfig`，实现纯几何轨迹模拟
    - ✅ 改造 `TrajectoryPredictor` 使用几何模拟，移除 `PhysicsScene2D` 依赖
    - ✅ 改造 `AimController` 使用 `Vector3`，修复 3D 鼠标坐标转换
    - ✅ `BallPhysics.CreateGeometryConfig()` 提供配置接口
- **进行中**：S6 暂缓（敌人参数），S7 剩余工作（影子场景清理）。  
- **后续重点**：
  1. **影子场景清理**：移除 `SimulationObjectReplicator`、`BallPhysics.isSimulationMode` 的旧 2D 物理逻辑；删除 `InitializeLegacy2DForSimulation` 等临时兼容代码。  
  2. **测试与回归**：完成上述清理后，统一做一次 Level1_3D + 预测 UI 的整体验证，确认"预测 = 实战"。

> **S7 进展说明**：  
> - 已实现无影子场景的轨迹预测方案：`GeometryTrajectorySimulator` 直接使用与 `BallPhysics` 相同的几何算法进行预测，保证"预测 = 实战"。  
> - `AimController` 和 `AimLineRenderer` 已支持 `Vector3` 路径点和碰撞点，瞄准线系统完全适配 3D。  
> - 下一步只需清理影子场景相关代码，即可完成 S7。


