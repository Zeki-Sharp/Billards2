### 墙体撞击 3D 计算器设计（旋转 & 位移参数）

> 目标：在 3D 场景中，球撞击四面一体的 `Wall` 根物体时，**以 Wall 中心为参考，在 XZ 平面上产生合理的整体位移与旋转**，并将所有计算逻辑收敛到「计算器」中，`StaticHitReceiver` 只负责传参与触发。

---

### 一、参数设计目标（你当前的预期）

1. **旋转（Rotation）**
   - **方向（正/负）来源**：只看「撞击点相对于 Wall 中心的位置」，即 `center → hit` 的方向决定旋转的正负（例如：右上角撞击就偏向右上角那一侧去“抬起/下压”）。
   - **角度大小来源**：只看撞击速度大小 `hitSpeed`，速度越大，旋转角度越大。

2. **位移（Position Offset）**
   - **方向来源**：只看撞击方向 `hitDirection`（通常是球的速度方向 / 碰撞法线的反向），**不关心撞击点相对于中心的位置**；也就是说，撞击方向指哪，整块墙就朝哪平移。
   - **位移量来源**：同样只看撞击速度 `hitSpeed`，速度越大，位移越大。

3. **整体效果**
   - 四面墙视为一个整体的 `Wall` 根物体：
     - 所有偏移和旋转都在 `Wall` 的 **XZ 平面** 内完成（只绕 Y 轴旋转，不改 Y 位置）。
     - 例如：球撞右上角时，**整体向右上角平移**，并且 **整体朝右上方向产生一个合适的旋转**。

---

### 二、坐标系与数据流设计

#### 1. 统一的世界数据输入

从几何物理 / 事件系统视角，计算器所需的原始数据统一为「世界空间」：

- `hitPositionWorld : Vector3`  
  - 碰撞接触点（来自 `CollisionEvent.ContactPoint3D` / `DamageEvent.HitPosition3D`）。
- `hitDirectionWorld : Vector3`  
  - 撞击方向（推荐使用球的速度方向 `velocity.normalized`，或 `-ContactNormal`）。
- `hitSpeed : float`  
  - 撞击速度标量（球在接触瞬间的速度大小）。
- `wallRoot : Transform`  
  - 代表整块墙体的根物体，其坐标系定义「墙的本地 XZ 平面」。

> 要求：`StaticHitReceiver`、`EffectManager` 等**不再做任何坐标映射/符号修正**，只负责按上述字段把世界数据转交给计算器。

#### 2. 计算器内部坐标系

计算器内部自行完成：

1. **世界 → Wall 本地**：
   - 撞击点本地坐标：
     ```csharp
     Vector3 localHit = wallRoot.InverseTransformPoint(hitPositionWorld);
     Vector2 localHitXZ = new Vector2(localHit.x, localHit.z);
     ```
   - 撞击方向本地坐标：
     ```csharp
     Vector3 localDir3D = wallRoot.InverseTransformDirection(hitDirectionWorld).normalized;
     Vector2 localDirXZ = new Vector2(localDir3D.x, localDir3D.z).normalized;
     ```

2. **Wall 本地坐标语义**：
   - `localHitXZ`：以墙中心为原点，**仅用于确定旋转方向象限**（左/右/前/后/斜角）。
   - `localDirXZ`：在墙的本地平面内，**仅用于位移方向**。

---

### 三、3D 旋转计算器设计（示意）

> 类名示例：`WallHitRotationCalculator3D`（MonoBehaviour / ScriptableObject 均可）

**接口草案**：

```csharp
public float CalculateRotationAngle(
    Transform wallRoot,
    Vector3 hitPositionWorld,
    Vector3 hitDirectionWorld,
    float hitSpeed
);
```

**核心逻辑（与需求对应）**：

1. **本地化输入**：
   - 使用前述 `localHitXZ`、`localDirXZ`。

2. **方向（正负号）——看撞击点相对中心的位置**：
   - 使用 `localHitXZ` 决定旋转的“朝向象限”，例如：
     - `localHitXZ` 在右上象限 → 按某种规则认为是“顺时针”（或逆时针），一致地输出正负号。
   - 一种可行方式：
     - 以某个基准向量（例如 `(0,1)` 代表“上方”）为参考，用 `SignedAngle` 或 `cross` 的符号决定方向。

3. **角度大小——看速度**：
   - 只依赖 `hitSpeed`：
     ```csharp
     float t = Mathf.InverseLerp(minSpeed, maxSpeed, hitSpeed);
     float magnitude = Mathf.Lerp(minAngle, maxAngle, t);
     ```
   - 最终角度：
     ```csharp
     float angle = directionSign * magnitude;
     ```

4. **输出给 MMF**：
   - 返回值即为绕 `Wall` 本地 Y 轴的角度；由 `MMFPlayerParameterSetter.SetRotationEffect` 写入对应的 MMF 通道。

---

### 四、3D 位移计算器设计（示意）

> 类名示例：`WallHitPositionCalculator3D`

**接口草案**：

```csharp
public Vector3 CalculatePositionOffset(
    Transform wallRoot,
    Vector3 hitPositionWorld,
    Vector3 hitDirectionWorld,
    float hitSpeed
);
```

**核心逻辑（与需求对应）**：

1. **本地化撞击方向**：
   - 使用 `localDirXZ`，忽略 `localHitXZ`：
     ```csharp
     // localDirXZ 决定方向
     Vector2 moveDirLocal2D = localDirXZ.normalized;
     ```

2. **位移量——只看速度**：
   ```csharp
   float t = Mathf.InverseLerp(minSpeed, maxSpeed, hitSpeed);
   float distance = Mathf.Lerp(minOffset, maxOffset, t);
   Vector2 offsetLocal2D = moveDirLocal2D * distance;
   Vector3 offsetLocal3D = new Vector3(offsetLocal2D.x, 0f, offsetLocal2D.y);
   ```

3. **回到世界坐标**：
   ```csharp
   Vector3 offsetWorld = wallRoot.TransformDirection(offsetLocal3D);
   return offsetWorld;
   ```

4. **MMF 注入**：
   - `StaticHitReceiver` 或 `MMFPlayerParameterSetter` 将 `offsetWorld` 直接传给 PositionSpring/位移相关 MMF。

---

### 五、职责分层与对现有 Receiver 的调整

1. **StaticHitReceiver 的职责**（精简版）：
   - 订阅 `CollisionEvent` / `DamageEvent`。
   - 过滤：自己的 Target / Tag / 冷却 / 是否静态等。
   - 整理世界空间参数：
     - `hitPositionWorld`
     - `hitDirectionWorld`
     - `hitSpeed`
     - `wallRoot`（通常是自己或配置字段）
   - 将上述参数 **原样传给 3D 计算器**，并负责调用 `MMFPlayer.PlayFeedbacks()`。
   - 不再包含任何「左右墙特判」「XY→XZ 映射」等计算逻辑。

2. **计算器的职责**：
   - 是**唯一**的「撞击 → 旋转/位移参数」生成处：
     - 旋转方向：由 `localHitXZ` 决定。
     - 旋转大小：由 `hitSpeed` 决定。
     - 位移方向：由 `hitDirectionWorld`（经本地化）决定。
     - 位移大小：由 `hitSpeed` 决定。
   - 对外暴露纯函数式接口，方便后续在其它静态物体上重用。

---

### 六、执行步骤规划（落地顺序）

1. **新增 3D 计算器脚本（不动现有 Receiver）**
   - 在 `Assets/Scripts/Calculator/WallHit/` 下新增：
     - `WallHitRotationCalculator3D.cs`
     - `WallHitPositionCalculator3D.cs`
   - 按本计划的接口与逻辑实现：
     - 接收世界空间参数 + `wallRoot`；
     - 内部完成世界 → 本地 XZ 的转换；
     - 返回「旋转角度」与「位移世界向量」。

2. **在 StaticHitReceiver 中接入新 3D 计算器（仅做“传参”改造）**
   - 增加两个可序列化字段：
     - `WallHitRotationCalculator3D rotationCalculator3D;`
     - `WallHitPositionCalculator3D positionCalculator3D;`
   - 在碰撞事件处理中：
     - 收集 `hitPositionWorld / hitDirectionWorld / hitSpeed / wallRoot`；
     - 若 3D 计算器存在，则调用其 `CalculateRotationAngle` / `CalculatePositionOffset`；
     - 将结果通过 `MMFPlayerParameterSetter` 写入 MMF；
     - 去掉 Receiver 内部「左右墙符号修正」「XY→XZ 投影」等旧数学逻辑。
   - 保留现有 2D 计算器调用一段时间（可以通过开关字段区分 2D/3D），便于对比与回滚。

3. **墙体 Prefab 配置与场景验证**
   - 在 `Wall` 根物体上：
     - 挂载并配置 `WallHitRotationCalculator3D` / `WallHitPositionCalculator3D`；
     - 确认 `wallRoot` 指向同一根 Transform。
   - 在测试场景中验证四个典型位置：
     - 中央撞击（仅沿法线方向轻微位移/旋转）；
     - 右上角 / 左上角 / 右下角 / 左下角撞击（观察整体向对应象限平移，旋转趋势是否符合直觉）。

4. **调参与可视化**
   - 为 3D 计算器增加调试选项（可选）：
     - 在 Scene 视图中绘制：
       - 撞击点、本地向量 `localHitXZ`、`localDirXZ`；
       - 计算出的旋转轴向量与位移向量。
   - 根据视觉手感微调：
     - `minSpeed/maxSpeed`、`minAngle/maxAngle`；
     - `minOffset/maxOffset`、缓动曲线等。

5. **收尾：清理旧路径 & 更新文档**
   - 当 3D 计算器方案在主场景中稳定后：
     - 从 `StaticHitReceiver` 中彻底移除依赖旧 2D 计算器的逻辑；
     - 标记并逐步删除不再使用的 `WallHitRotationController` / `WallHitPositionController` 旧 2D 专用代码（或迁入 Legacy 区域）。
   - 更新相关文档：
     - 在 `Wall_Collision_Effects_3D_Plan.md` 中补充「最终采用 3D 计算器方案」的说明；
     - 对应在 `Legacy_Issues.md` 中勾掉已解决的墙体 2D/3D 混用问题条目。

---

### 七、开放问题（留待实现阶段决策）

1. 旋转方向的具体规则（例如「右上角撞击时是抬右上、压左下」的精确符号）可以在实现时通过若干枚举配置实现，以便调节视觉手感。  
2. `hitDirectionWorld` 使用球的速度还是法线的反向，需根据现有物理手感做一次对比试验。  
3. 是否需要对 `hitSpeed` 做缓冲（如平方根/指数曲线）以匹配视觉期望，也留给实现阶段在计算器内部微调。

