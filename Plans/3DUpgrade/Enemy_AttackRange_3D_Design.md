## 敌人攻击范围 3D 化设计方案（AttackRange）

### 1. 目标与约束

- **目标**
  - 保持与原有 2D 逻辑一致的攻击行为语义：**一回合只结算一次攻击**、由行为树驱动 Telegraphed → Attack → Cleanup。
  - 将攻击范围的展示与检测完全 3D 化，兼容当前基于几何物理的 3D 场景（XZ 平面）。
  - 为后续美术扩展提供干净的显示层（改贴图/材质即可，无需改逻辑）。

- **约束**
  - **不再依赖 2D 组件**：`Collider2D`、`Physics2D`。
  - **攻击检测仍采用“主动查询”**（Attack 阶段调用 `GetTargetsInRange`），避免 Trigger 频繁触发导致“一个回合多次伤害”。
  - AttackRange 继续只负责**展示 + 目标查询**，不直接做伤害与状态处理。

---

### 2. 现有 2D 方案回顾（基线）

- **结构**
  - `AttackRange` 作为敌人子物体，包含子物体 `Image`：
    - `Image` 上挂 `Collider2D`（多为 `PolygonCollider2D`）。
    - `Image` 上挂 `SpriteRenderer` 作为 2D 范围图形。

- **逻辑**
  - Telegraphed 阶段：
    - 行为树调用 `attackRange.ShowTelegraph(targetPos)`。
    - `AttackRange` 将自己激活、根据玩家方向旋转（围绕 Z 轴）。
  - Attack 阶段：
    - 行为树调用 `attackRange.ApplyTelegraphedDirection()` 固定方向。
    - 行为树调用 `attackRange.GetTargetsInRange()`：
      - 使用 `attackCollider.Overlap(contactFilter, list)` 主动查询所有重叠目标。
      - 过滤 `tag == Player` 的对象，返回目标列表。
    - 攻击行为脚本遍历这些目标，按规则发布 `CollisionEvent` 给 `DamageSystem`。
    - **只在 Attack 阶段调用一次 → 一回合只结算一次攻击**。
  - Cleanup 阶段：
    - 行为树调用 `attackRange.HideTelegraph()`，关闭可视和碰撞区域。

- **关键点**
  - 不使用 `OnTriggerEnter2D` 直接打伤害，而是**行为树在 Attack 阶段主动调用一次 Overlap**。
  - 攻击次数由行为树控制，不受物理引擎触发频率影响。

---

### 3. 3D 化设计原则

1. **保持“主动查询”模型不变**
   - 仍然在 Attack 阶段由攻击行为调用 `GetTargetsInRange()`。
   - 不用 `OnTriggerEnter(Collider)` 来结算攻击，避免 Trigger 重入导致多次伤害。

2. **显示与检测解耦但共用形状**
   - 3D 下使用 **一个 Quad + 3D Collider** 来承载攻击区域：
     - Quad 的 Mesh/材质负责显示。
     - 同一 GameObject 上的 `BoxCollider`/`SphereCollider`/`CapsuleCollider` 负责 3D 重叠检测。
   - 攻击范围的形状完全由预制体决定，脚本只做“拿 collider 当前的 bounds/参数，做 Overlap 查询”。

3. **统一坐标系：XZ 平面为逻辑平面**
   - 敌人、玩家都在 XZ 平面运动，Y 由 `BallPhysics.AlignToGround` 确定初始高度并锁定。
   - 攻击方向是 XZ 平面上的向量。
   - AttackRange 旋转只围绕 Y 轴（绕世界“向上”旋转），不再绕 Z 轴。

4. **可视觉替换**
   - 只要保持：
     - 子物体上有 `MeshRenderer` + 对应 `Collider`。
   - 就可以自由换材质、换 Mesh（例如改为扇形 Mesh），无需改逻辑。

---

### 4. 新的 AttackRange 3D 结构设计

#### 4.1 预制体结构（示例）

- `Enemy_Xxx (Prefab root)`
  - `AttackRange`（空节点，挂 `AttackRange` 脚本）  
    - `Visual`（原来的 `Image` 节点，3D 化）
      - `MeshFilter`（Quad 或自定义 Mesh）
      - `MeshRenderer`（半透明材质）
      - `BoxCollider` / `SphereCollider` / `CapsuleCollider`（`isTrigger = true`）

约定：
- `AttackRange` 脚本默认查找子物体 `"Visual"`（或兼容 `"Image"` 名称，便于过渡）。
- 所有重叠检测基于 `Visual` 上的 3D Collider。

#### 4.2 显示层（MeshRenderer + Quad）

- 使用 Quad（或其他平面 Mesh）作为底板：
  - Mesh 本身面向 +Z 轴。
  - 将 GameObject 旋转到**贴合 XZ 平面**，例如：
    - Quad 预制体本身旋转成“平放在地面上”。
    - 或在 `AttackRange` 的初始化中做一次性旋转（推荐尽量 prefab 内完成）。
- 材质：
  - 使用透明或半透明材质（URP 标准 Shader 即可）。
  - 颜色/渐变由美术配置，不写死在脚本。

---

### 5. 3D 检测逻辑设计（替代 OverlapCollider）

#### 5.1 目标检测 API（保持接口风格）

- 继续保留：

```csharp
public List<GameObject> GetTargetsInRange();
```

- 内部实现改为 3D Overlap 系列 API，但对外语义不变：**返回当前攻击范围内所有满足条件的目标**。

#### 5.2 具体检测方案

- 在 `GetTargetsInRange()` 中：
  - 查找子物体 `Visual`。
  - 获取其 `Collider`（3D）：
    - `BoxCollider` → 使用 `Physics.OverlapBox(center, halfExtents, rotation, layerMask, QueryTriggerInteraction.Ignore)`
    - `SphereCollider` → 使用 `Physics.OverlapSphere(center, radius, layerMask, QueryTriggerInteraction.Ignore)`
    - `CapsuleCollider` → 使用 `Physics.OverlapCapsule(point1, point2, radius, layerMask, QueryTriggerInteraction.Ignore)`
  - `layerMask` 只包含 Player 所在层（与现有 Player Layer 配置保持一致）。
  - 过滤 `CompareTag("Player")` 的对象，构造目标列表。

- 仍然是**主动查询**：
  - Attack 阶段的攻击行为调用一次 `GetTargetsInRange()` → 一次性结算伤害。

#### 5.3 与 DamageSystem 的关系

- 攻击行为（如 `MeleeAttackBehavior` / `RangedAttackBehavior`）在 Attack 阶段：
  - 遍历 `targets`：
    - 对每个 Player：
      - 以 **攻击源对象（例如 AttackRange 或敌人本体）** 为 `Source` 构造 `CollisionEvent`。
      - 使用现有的 3D 版本：
        - `CollisionEvent.CreateFromTrigger(sourceGameObject, playerCollider)`
      - 通过 `GameEventBus.PublishCollision(evt)` 发送给 `DamageSystem`。
  - `DamageRuleConfig.requireSourceState`（如 `"CanAttack"`） 仍挂在敌人的 Blackboard 上。
  - `DamageSystem`：
    - 从 `evt.Source` 追溯到敌人实体（例如从 AttackRange parent）并获取 Blackboard。
    - 只在 `CanAttack == true` 时通过规则。

- **保证“一回合一次”的核心点**：
  - `CanAttack` 只在行为树的 Attack 阶段短时间内置为 `true`，之后立即恢复为 `false`。
  - `GetTargetsInRange()` 只在 Attack 阶段调用一次。
  - 不依赖 `OnTriggerEnter` 等连续触发事件。

---

### 6. 行为树 & 生命周期对齐（保持 2D 语义）

#### 6.1 Telegraph 阶段

- 调用：
  - `attackRange.ShowTelegraph(playerPos3D)`：
    - 激活 `AttackRange` GameObject（以及子物体 `Visual`）。
    - 将 `AttackRange` 定位到期望的世界坐标（近战：敌人脚下；远程：预判落点）。
    - 计算 XZ 方向向量：`dir = (playerPos3D - enemyPos3D).normalized`。
    - 设置 `telegraphedDirection`（内部仍然可以缓存为 `Vector2(x, z)`）。
    - 将 `AttackRange` 的旋转设置为绕 Y 轴朝向 `dir`。

#### 6.2 Attack 阶段

- 调用顺序（保持 2D 思路）：
  1. `attackRange.ApplyTelegraphedDirection()`：
     - 确保旋转与缓存方向一致（防止 Telegraphed 阶段后位置变化导致方向漂移）。
  2. 行为脚本在敌人 Blackboard 上设置：
     - `CanAttack = true`。
  3. 调用 `GetTargetsInRange()`：
     - 使用 3D Collider + `Physics.OverlapXXX` 获取重叠玩家。
  4. 对每个目标发布 `CollisionEvent` → `DamageSystem` 结算伤害。
  5. 立即将 `CanAttack = false`，并根据需求隐藏或延迟隐藏 AttackRange：
     - 近战：立即隐藏。
     - 远程：可以允许一小段时间的残留特效，但不再参与伤害判定。

#### 6.3 Cleanup 阶段

- 行为树进入 Move / Idle 等非攻击阶段时调用：
  - `attackRange.HideTelegraph()`：
    - 关闭 AttackRange GameObject（可见+Collider 一起关闭）。
  - 确保 `CanAttack` 已经被清理为 `false`。

---

### 7. 视觉表现建议（与逻辑关联）

- **Quad 形状**：
  - 基础：圆形/矩形纹理，贴在 Quad 上。
  - 扩展：可以替换为扇形或自定义 Mesh，逻辑层只关心 Collider 尺寸。

- **颜色与动画**：
  - Telegraph 阶段：
    - 使用更透明/柔和的材质，或加入渐变动画（Shader 或 `MaterialPropertyBlock`）。
  - Attack 瞬间：
    - 可以加一个快速亮起/缩放的动画，由 `MMFeedbacks` 或 Timeline 驱动。

- **与几何物理的关系**：
  - AttackRange 自身不参与几何移动，不需要 `BallPhysics`。
  - 只需跟随敌人或设定的投射点，位置由行为树控制。

---

### 8. 后续实现步骤（供下一阶段使用）

> 以下是后续可以按步骤实施的改动计划，此文档阶段**只做设计，不改代码**。

- **步骤 1：Prefab 调整**
  - 在各类 `AttackRange` 预制体中：
    - 将 `Image` 节点改为 `Visual`（或保留名字但内部改为 Quad）。
    - 添加 `MeshFilter` + `MeshRenderer`（Quad Mesh + 半透明材质）。
    - 在 `Visual` 上添加合适的 `Collider`（`BoxCollider` 或 `CapsuleCollider`），设置为 `isTrigger = true`。

- **步骤 2：AttackRange.cs 3D 化**
  - 替换 `Collider2D` 相关逻辑为 3D `Collider` + `Physics.OverlapXXX`。
  - 将方向计算从 XY 平面改为 XZ 平面，但保留 `Vector2` 接口（使用 `(x, z)` 映射）。
  - 保持 `ShowTelegraph / HideTelegraph / ApplyTelegraphedDirection / GetTargetsInRange` 的整体接口和调用顺序不变。

- **步骤 3：攻击行为脚本适配 3D**
  - 在 `MeleeAttackBehavior`、`RangedAttackBehavior`、`ThornAttackBehavior` 中：
    - 保持“一次 Attack 调用一次 GetTargetsInRange”的模式不变。
    - 使用 3D Collider 调用 3D 版本 `CollisionEvent.CreateFromTrigger`。
    - 确认 `CanAttack` 的设置/清理时机正确，防止多次伤害。

- **步骤 4：联调与可视化调优**
  - 打开 `DamageSystem` 调试日志，确认每次攻击只产生一次碰撞事件。
  - 调整 Quad 尺寸与 Collider 尺寸，使视觉范围与实际判定一致。
  - 根据需要微调材质与特效。

---

本设计保证：
- 攻击行为的**生命周期和次数语义与 2D 版本一致**。
- 检测逻辑从 2D `OverlapCollider` 平滑迁移到 3D `Physics.OverlapXXX`。
- 显示层从 `SpriteRenderer` 迁移到 `MeshRenderer + Quad`，与当前 3D 场景和几何物理体系自然对齐。

