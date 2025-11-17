### 墙壁碰撞特效 3D 升级规划（草案）

> 目标：在保持当前几何物理（`BallPhysics`）为唯一真相的前提下，让**球撞墙 / 撞敌人 / 撞其它物体**都统一走一套「几何碰撞事件 → 伤害系统 → 特效系统」的链路，实现：
> - 撞击位置的 3D 圆形爆炸（全局 Hit 特效）
> - 墙体位移 / 旋转特效
> - 敌人 / 玩家被击特效

---

### 一、现状梳理（2D → 3D）

#### 1. 物理层

- **2D 时代**
  - Unity 物理 + `Rigidbody2D` 推动球体运动。
  - 撞墙由 `OnCollisionEnter2D` 提供 `Collision2D`（contacts/normal）。
  - 墙壁特效入口：`WallManager.OnWallHit(Collision2D, wallTransform)`。

- **3D 现状**
  - 球体运动完全交给 `BallPhysics` 的几何模拟（`PerformColliderCast + HandleGeometryWallCollision`），使用 kinematic `Rigidbody`。
  - 真正知道「撞到了谁、在哪儿、法线是什么」的唯一位置是：
    - `BallPhysics.HandleGeometryWallCollision(RaycastHit hitInfo, float remainingDt)`
    - `BallPhysics.HandleGeometryBallCollision(...)`
  - 原 2D 的 `OnCollisionEnter2D` 路径基本失效，墙体不再自然收到碰撞事件。

#### 2. 事件与特效层

- **新伤害系统事件结构**
  - `CollisionEvent`：几何碰撞统一数据。
    - 已扩展出 `ContactPoint3D` 用于 3D 接触点。
  - `DamageSystem.ProcessDamage(rule, CollisionEvent)`：
    - 生成 `AttackData`，并优先使用 `evt.ContactPoint3D` 作为 `AttackData.Position`。
  - `EffectManager`：
    - 根据 `AttackData` 播放：
      - 攻击者特效 `Hit`
      - 全局特效 `GlobalHitAttack`
      - 目标特效 `BeHit`

- **墙壁特效（2D 遗留）**
  - `WallManager.OnWallHit(Collision2D, wallTransform)`：
    - 计算撞击点、法线、速度。
    - 使用 `WallHitRotationController` / `WallHitPositionController` 计算墙体摇晃参数。
    - 手动组装 `AttackData`（包含墙体专用字段），调用 `EffectManager` 播三类特效。
  - 问题：目前 3D 下这条链路无法被触发。

---

### 二、总体设计思路（3D 统一方案）

核心思想：**把所有几何碰撞都收敛到 `BallPhysics → CollisionEvent`，再由 DamageSystem + EffectManager + 各个「订阅者」按需处理。**

#### 1. BallPhysics：只负责「发布几何碰撞事件」

- 碰撞检测：
  - 球体 vs 球体：`HandleGeometryBallCollision`
  - 球体 vs 墙体 / 其它：`HandleGeometryWallCollision`
- 在发生碰撞时：
  - 统一调用 `PublishGeometryCollisionEvent(GameObject target, Vector3 contactPoint, Vector3 normal)`。
  - `CollisionEvent` 填充：
    - `Source`：本球体
    - `Target`：命中的 `target`
    - `ContactPoint3D`：`contactPoint`
    - `ContactPoint`：`(x, z)` 投影（兼容 2D 逻辑）
    - `ContactNormal`：`(x, z)` 平面法线
    - `Velocity`：当前几何速度
  - 不关心 Target 是墙还是敌人，也不关心具体播什么特效。

#### 2. DamageSystem：负责「CollisionEvent → AttackData」

- 对所有 `CollisionEvent`，统一生成 `AttackData`：
  - `Position`：优先用 `ContactPoint3D`，否则用 `ContactPoint` 投影。
  - `Direction`：由 `ContactNormal` 决定。
  - `Attacker` / `Target` / `Damage`：由规则配置。
- 发布 `DamageEvent`：
  - `HitPosition3D`：保留 3D 接触点，方便订阅者使用。

#### 3. EffectManager & 订阅者：按 Target 类型分发特效

- **EffectManager（通用层）**
  - 只根据 `AttackData` 做通用处理：
    - 播放攻击者特效 `Hit`。
    - 对玩家发起的 `Hit`，在 `AttackData.Position` 播放全局圆形 `GlobalHitAttack`。
    - 对目标播放 `BeHit`（敌人 / 墙 / 其它）。

- **WallManager（墙体特效专用层）**
  - 订阅 `DamageEvent` 或 `CollisionEvent`：
    - 如果 `Target` / `Source` 上带有 `WallHitRotationController` / `WallHitPositionController`，认为是墙体相关事件。
    - 使用 `HitPosition3D` / `HitDirection` / `VelocityAtHit`：
      - 调用两个 Controller 计算：
        - `WallHitRotationAngle`
        - `WallHitPositionOffset`
      - 回填到一个新的 `AttackData` 或直接调用墙体特效的 `MMF_PlayerParameterSetter.SetWallHitParameters`。
    - 可以选择：
      - 复用 `EffectManager.PlayAttackEffect` 播 `Hit/GlobalHitAttack/BeHit`；
      - 或只在墙体侧添加额外的抖动/位移，不改变全局特效逻辑。

#### 4. 敌人 / 玩家特效

- 对敌人 / 玩家来说，不需要特殊通道：
  - 继续通过 `DamageSystem` → `AttackData` → `EffectManager` 的通用路径。
  - 撞到敌人时：
    - 会播 `Hit`（攻击者），`BeHit`（敌人）。
    - 若满足规则，也会播 `GlobalHitAttack`。
  - 与墙体唯一区别在于：是否额外订阅 `DamageEvent` 做墙体摇晃。

---

### 三、具体执行步骤规划（不含代码细节）

#### 步骤 1：梳理并确认现有事件链路

1. 确认 `BallPhysics.PublishGeometryCollisionEvent` 的调用点和触发场景：
   - 球 vs 墙
   - 球 vs 敌人
   - 球 vs 玩家（如果有）
2. 确认 `GameEventBus.PublishCollision(evt)` 的订阅者列表：
   - `DamageSystem.OnCollisionEvent`。
   - 是否有其它旧系统仍在直接订阅。

#### 步骤 2：统一 CollisionEvent 的 3D 数据约定

1. 明确定义：
   - `ContactPoint3D`：**唯一的 3D 接触点真相**。
   - `ContactPoint`（Vector2）：始终表示在 XZ 平面的投影（向后兼容）。
2. 在所有 `Create` / `CreateFromTrigger` / `PublishGeometryCollisionEvent` 中统一填充：
   - 保证任何维度的碰撞（2D/3D/几何）最终都会有一致的 3D/2D 数据组合。

> 回答问题 1：  
> **是的，ContactPoint 应该以 `ContactPoint3D` 为主。**  
> 现有设计是：`ContactPoint3D` 存 3D 真值，`ContactPoint` 存 XZ 投影，兼容旧逻辑。  
> 未来所有需要空间位置的地方都应该优先使用 `ContactPoint3D`。

#### 步骤 3：在 DamageSystem 中标准化 AttackData 生成

1. 为所有由碰撞触发的规则（包括墙、敌人、陷阱等）统一生成 `AttackData`：
   - `Position` 从 `evt.ContactPoint3D` / `ContactPoint` 统一转换为 3D。
   - `Direction`、`HitSpeed` 等统一来源于 `CollisionEvent`。
2. 确认当前所有 AttackData 使用者：
   - `EffectManager`（Hit / GlobalHitAttack / BeHit）。
   - 旧的 PlayerCore / Enemy 行为（若仍有）。

> 回答问题 2：  
> **AttackData 目前不是多余层，而是「表现层兼容结构」。**  
> - `CollisionEvent`：偏逻辑/物理（Source/Target/法线/速度/时间），与旧系统解耦。  
> - `AttackData`：为所有既有的特效、数值处理、技能系统提供统一入口（类型、位置、攻击者/目标标签等）。  
> 在逐步迁移过程中，它仍然是一个有价值的适配层，并且方便以后在不动物理/规则的前提下调整表现。

#### 步骤 4：墙体特效 3D 化接入

1. 在 `WallManager` 中新增对 `DamageEvent`（或 `CollisionEvent`）的订阅入口：
   - 根据 `Target` / `Source` 是否有墙体组件决定是否处理。
   - 将 `HitPosition3D` / `HitDirection` / `VelocityAtHit` 映射到墙体坐标系。
2. 使用现有 `WallHitRotationController` / `WallHitPositionController`：
   - 输入：3D 碰撞点 + 法线 + 速度。
   - 输出：墙体旋转角度 / 位置偏移。
3. 将结果注入到墙体专用的 MMF 参数：
   - 使用 `MMFPlayerParameterSetter.SetWallHitParameters` 或类似帮助类。
   - 保持现有 MMF 资源不变，仅替换数据来源。

#### 步骤 5：验证与调优

1. 单独验证场景：
   - 玩家撞水平墙 / 垂直墙。
   - 玩家撞敌人 / 敌人撞墙。
2. 检查：
   - GlobalHitAttack 是否总是出现在真实 3D 接触点。
   - 墙体摇晃方向是否与法线、屏幕方位一致。
   - 多次快速撞击时，防抖逻辑是否仍然生效。

---

### 四、关于 ContactPoint 与 AttackData 的评估结论

1. **ContactPoint / ContactPoint3D**
   - 未来所有需要空间信息的逻辑（特效 / 弱点判定 / 位移等）都应该**优先使用 `ContactPoint3D`**。
   - `ContactPoint`（Vector2）建议保留为：
     - XZ 平面逻辑（如原有 2D 角度判断、投射算法）使用。
     - 兼容尚未迁移的 2D 代码。
   - 长期目标可以是：
     - 在逻辑层完全转向 `ContactPoint3D`，`ContactPoint` 只在「2D 可视化/调试」中使用。

2. **AttackData 是否多余？**
   - 目前来看，**AttackData 仍然是必要的「表现层统一入口」**：
     - 大量现有代码和特效资源以 AttackData 为中心组织（AttackType、AttackerTag、TargetTag、Position、Direction 等）。
     - 即使未来完全用 `CollisionEvent` 驱动物理与数值，表现层仍需一个稳定的数据结构，用于驱动：
       - MMF 参数
       - UI 反馈
       - 技能系统中的「攻击类型分支」
   - 更合理的定位是：
     - `CollisionEvent`：低层逻辑/物理事件。
     - `DamageEvent`：数值结算结果。
     - `AttackData`：**提供给「表现层与部分高层逻辑」的统一攻击上下文**，它从前两者派生，但不与物理实现强耦合。

综上，短期内不建议删除 AttackData，而是继续把它作为所有攻击/撞击特效的统一数据入口；待整个 3D 升级完成、所有使用方都迁移到新的事件系统后，再评估是否需要进一步简化结构。


