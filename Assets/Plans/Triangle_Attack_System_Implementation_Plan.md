# 三角形范围攻击系统实现计划

> **创建日期**：2025年11月  
> **状态**：设计阶段  
> **优先级**：⭐⭐

---

## 📋 需求概述

### 功能描述
实现一个新的攻击角色，其攻击机制为：
1. 发射球体并移动
2. 记录轨迹关键点（起点、第一碰撞点、终点）
3. 球停止后，用三个点构成三角形范围
4. 对三角形区域内的敌人造成伤害
5. 显示三角形攻击特效

### 核心特性
- **瞄准阶段**：显示普通物理轨迹（与碰撞角色相同）
- **触发条件**：必须发生至少一次碰撞，否则不触发攻击
- **范围形状**：动态生成的三角形（非固定形状）
- **视觉表现**：三角形区域填充特效（类似现有圆形范围攻击）

---

## 🎯 设计目标

### 1. 复用现有架构
- ✅ 使用多 Profile 组合系统
- ✅ 复用 DamageSystem 的规则驱动架构
- ✅ 复用 EffectManager 的特效系统
- ✅ 不破坏现有碰撞/范围攻击功能

### 2. 配置驱动
- ✅ 通过 DamageRuleConfig 配置三角形攻击
- ✅ 支持与其他规则组合使用
- ✅ 易于创建变种角色

### 3. 最小侵入
- ✅ 不修改核心物理系统
- ✅ 扩展而非重写现有组件
- ✅ 保持向后兼容

---

## 🏗️ 系统架构设计

### 核心流程

```
发射阶段
  └─ 记录起点位置
  └─ 重置碰撞记录标记

移动阶段
  └─ 监听碰撞事件
  └─ 记录第一次碰撞点（仅一次）

停止阶段
  └─ 检查是否有碰撞记录
      ├─ 有碰撞 → 生成三角形 → 范围检测 → 造成伤害
      └─ 无碰撞 → 不触发攻击
```

### 组件职责划分

| 组件 | 职责 | 修改类型 |
|------|------|---------|
| **PlayerBehavior** | 记录轨迹关键点 | 扩展 |
| **DamageRuleConfig** | 配置三角形攻击规则 | 扩展 |
| **DamageSystem** | 执行三角形范围检测 | 扩展 |
| **PlayerAttackManager** | 显示三角形视觉特效 | 扩展 |
| **StoppedEvent** | 传递轨迹数据 | 扩展 |

---

## 🔧 核心组件改动

### 1. 轨迹记录系统（PlayerBehavior）

**改动点**：
- 添加字段：起点、第一碰撞点、碰撞标记
- 修改方法：`Launch()` - 记录起点并重置标记
- 修改方法：`OnCollisionEnter2D()` - 记录第一碰撞点
- 修改方法：`OnBallStoppedHandler()` - 传递轨迹数据

**设计理由**：
- PlayerBehavior 已经监听所有必要事件
- 不需要创建新组件
- 轨迹数据属于玩家行为的一部分

### 2. 规则配置扩展（DamageRuleConfig）

**新增字段**：
- `rangeShape`（枚举）：Circle / Triangle
- `requireTrajectory`（布尔）：是否需要轨迹记录

**向后兼容**：
- 默认值为 Circle（现有行为）
- 可选配置，不影响现有规则

### 3. 范围检测扩展（DamageSystem）

**改动点**：
- 扩展 `ProcessStoppedDamage()` 方法
- 新增 `ProcessTriangleDamage()` 方法
- 新增 `IsPointInTriangle()` 工具方法

**检测逻辑**：
1. 从 StoppedEvent 获取轨迹数据
2. 验证三角形有效性（三点不共线）
3. 使用几何算法检测敌人是否在三角形内
4. 对区域内敌人应用伤害规则

### 4. 事件系统扩展（StoppedEvent）

**新增字段**：
- `LaunchPosition`：发射起点
- `FirstCollisionPoint`：第一碰撞点（可选）
- `HasCollision`：是否发生碰撞

**向后兼容**：
- 新字段为可选
- 现有圆形攻击不使用这些字段

### 5. 视觉表现（PlayerAttackManager）

**改动点**：
- 根据规则的 `rangeShape` 选择不同特效
- 三角形特效：动态生成三角形Mesh/Sprite
- 复用现有的显示-淡出逻辑

**表现方式**：
- 简单方案：SpriteRenderer + 动态顶点（类似现有范围圈）
- 颜色：红色填充 → 淡出（与圆形攻击一致）

---

## 📦 配置示例

### DamageProfile 组合

```
📁 Common_PlayerRules.asset
  ├─ 撞墙受伤
  ├─ 被敌人碰撞受伤
  └─ 触发陷阱受伤

📁 Attack_Triangle.asset
  └─ 三角形范围攻击
      - triggerType: Stopped
      - rangeShape: Triangle
      - requireTrajectory: true
      - baseDamage: 15

📁 三角形角色.asset (PlayerData)
  └─ damageProfiles:
      - Common_PlayerRules
      - Attack_Triangle
```

### 规则特性

| 特性 | 碰撞攻击 | 圆形攻击 | 三角形攻击 |
|------|---------|---------|-----------|
| 触发类型 | Collision | Stopped | Stopped |
| 范围形状 | 无 | Circle | Triangle |
| 轨迹记录 | 否 | 否 | 是 |
| 碰撞要求 | 必须 | 否 | 必须 |

---

## 🚀 实施步骤

### Phase 1：基础架构（1-2小时）
- [ ] 扩展 `DamageRuleConfig`（添加 rangeShape 字段）
- [ ] 扩展 `StoppedEvent`（添加轨迹数据字段）
- [ ] 在 `PlayerBehavior` 添加轨迹记录逻辑

### Phase 2：伤害检测（2-3小时）
- [ ] 在 `DamageSystem` 实现三角形范围检测
- [ ] 实现点在三角形内的几何算法
- [ ] 测试各种三角形形状（锐角、钝角、退化）

### Phase 3：视觉表现（1-2小时）
- [ ] 创建动态形状特效预制体
  - 预制体名：`DynamicShapeEffect.prefab`
  - 添加 MeshFilter + MeshRenderer 组件
  - 添加 MMF_Player 组件
  - 配置 MMF 反馈链：
    - MMF_MaterialColor（红色 → 透明，0.5s）
    - MMF_Scale（可选的缩放效果）
    - MMF_Destroy（延迟 0.5s 销毁）
- [ ] 实现轻量 ShapeEffectController 脚本（~15行代码）
  - `SetTriangle(p1, p2, p3)` 方法：动态生成三角形 Mesh
  - `SetCircle(center, radius)` 方法：动态生成圆形 Mesh
  - 调用 `MMF_Player.PlayFeedbacks()` 播放动画
- [ ] 扩展 `PlayerAttackManager` 显示三角形
  - 实例化预制体
  - 调用 `SetTriangle()` 传入三个点坐标
  - MMF 自动处理动画和销毁

### Phase 4：配置和测试（1小时）
- [ ] 创建三角形攻击规则配置
- [ ] 创建三角形角色 PlayerData
- [ ] 多场景测试和边界情况验证

**总预计时间**：5-8 小时（使用 MMF 减少视觉实现时间）

---

## ⚠️ 技术挑战和解决方案

### 挑战1：三角形退化情况
**问题**：三点共线或距离过近时三角形无效  
**方案**：
- 计算三角形面积，低于阈值时不触发攻击
- 在 UI 上给玩家反馈（可选）

### 挑战2：性能优化
**问题**：三角形内点检测比圆形检测慢  
**方案**：
- 先用包围盒（AABB）粗筛选
- 再用精确的三角形算法细筛选
- 敌人数量不多时影响可忽略

### 挑战3：视觉表现
**问题**：动态生成三角形Mesh可能复杂  
**方案**：使用 **脚本生成 Mesh + MMF Player 动画** 的混合方案
- **几何生成**（脚本，~15行）：
  - 用 Mesh 动态生成三角形顶点
  - 支持填充效果
  - 代码简洁，易维护
- **视觉动画**（MMF Player，无代码）：
  - MMF_MaterialColor：颜色淡出
  - MMF_Scale：缩放动画（可选）
  - MMF_Destroy：自动销毁
  - 美术可在 Inspector 中调整参数
- **优势**：
  - ✅ 代码量极少（15行 vs 传统方案 50行）
  - ✅ 视觉效果可配置（无需改代码）
  - ✅ 复用 MMF 生态（音效、震动等）

### 挑战4：轨迹数据生命周期
**问题**：轨迹数据何时清理  
**方案**：
- 每次发射时重置（在 Launch 方法中）
- 避免跨回合残留数据

---

## 🎨 视觉特效实现详解（MMF + 脚本混合方案）

### 为什么选择混合方案？

**MMF Player 的能力边界：**
- ❌ **无法动态生成几何形状**：三角形的三个点是运行时确定的，MMF 基于预设配置，无法根据动态坐标生成 Mesh
- ✅ **擅长视觉动画**：颜色淡出、缩放、粒子、音效、震动等

**结论：** 几何生成用脚本（必须），视觉动画用 MMF（最优）

### 预制体结构

```
DynamicShapeEffect.prefab
├─ MeshFilter           (动态 Mesh 容器)
├─ MeshRenderer         (材质渲染)
│   └─ Material: UnlitColor (支持颜色动画)
├─ MMF_Player          (视觉动画控制器)
│   ├─ MMF_MaterialColor (红色 → 透明，0.5s)
│   ├─ MMF_Scale (1.0 → 1.1，0.5s，可选)
│   └─ MMF_Destroy (延迟 0.5s 销毁)
└─ ShapeEffectController (轻量脚本)
    ├─ SerializeField: MMF_Player feedbackPlayer
    └─ 方法：SetTriangle() / SetCircle()
```

### ShapeEffectController 脚本（核心代码）

```csharp
// 仅需 15 行核心代码
public class ShapeEffectController : MonoBehaviour
{
    [SerializeField] private MMF_Player feedbackPlayer;
    
    public void SetTriangle(Vector3 p1, Vector3 p2, Vector3 p3)
    {
        // 生成三角形 Mesh（脚本必须做的事）
        Mesh mesh = new Mesh();
        mesh.vertices = new Vector3[] { p1, p2, p3 };
        mesh.triangles = new int[] { 0, 1, 2 };
        GetComponent<MeshFilter>().mesh = mesh;
        
        // 播放 MMF 动画（MMF 接管后续）
        feedbackPlayer?.PlayFeedbacks();
    }
}
```

### MMF Player 配置示例

在 Unity Inspector 中配置 MMF_Player：

| Feedback 类型 | 参数配置 | 效果 |
|--------------|---------|------|
| **MMF_MaterialColor** | Duration: 0.5s<br>From: (1, 0, 0, 0.5) 红色半透明<br>To: (1, 0, 0, 0) 完全透明 | 颜色淡出 |
| **MMF_Scale** | Duration: 0.5s<br>From: 1.0<br>To: 1.1<br>AnimationCurve: EaseOut | 轻微放大（可选） |
| **MMF_Destroy** | Delay: 0.5s | 动画结束后销毁 |

### 调用流程

```
PlayerAttackManager
  ↓
ShowTriangleEffect(p1, p2, p3)
  ↓
实例化 DynamicShapeEffect.prefab
  ↓
shapeController.SetTriangle(p1, p2, p3)
  ├─ 【脚本】生成三角形 Mesh
  └─ 【MMF】播放动画链
      ├─ 0.0s: 红色三角形出现
      ├─ 0.5s: 颜色淡出完成
      └─ 0.5s: 自动销毁
```

### 方案优势对比

| 方案 | 代码量 | 可配置性 | 扩展性 | 推荐度 |
|------|--------|---------|--------|--------|
| **纯脚本实现** | ~50行 | 低（需改代码） | ⭐⭐ | ⭐⭐ |
| **脚本+MMF（本方案）** | ~15行 | 高（Inspector配置） | ⭐⭐⭐ | ⭐⭐⭐⭐ |
| **纯MMF** | 0行 | ❌ 不可行 | ❌ | ❌ |

### 未来扩展性

使用此混合方案，未来可以轻松扩展：

1. **添加音效**：在 MMF_Player 中添加 `MMF_Sound`
2. **添加震动**：添加 `MMF_Haptics`（移动端）
3. **添加粒子**：添加 `MMF_ParticlesPlay`（边缘粒子效果）
4. **添加闪光**：添加 `MMF_Flash`（攻击闪烁）
5. **不同形状**：复用相同预制体，只需扩展 `SetRectangle()` 等方法

**所有这些扩展都不需要改动脚本代码！**

---

## 🔄 向后兼容性

### 不受影响的功能
- ✅ 碰撞攻击角色
- ✅ 圆形范围攻击角色
- ✅ 现有 DamageProfile 配置
- ✅ EffectManager 特效系统
- ✅ SkillManager 被动技能系统

### 兼容性保证
- 所有新增字段都有默认值
- 使用枚举区分攻击类型
- 现有代码路径不改变逻辑

---

## 📊 优势分析

### 相比其他方案的优势

| 方案 | 优势 | 劣势 |
|------|------|------|
| **方案A：扩展 DamageProfile（本方案）** | 最小改动，完全复用现有架构 | 需要扩展多个组件 |
| 方案B：创建专门的三角形攻击系统 | 完全独立，不影响现有 | 代码重复，维护成本高 |
| 方案C：用临时 if-else 判断角色类型 | 最快实现 | 反模式，难扩展 |

### 扩展性
- ✅ 未来可以轻松添加矩形、扇形等形状
- ✅ 支持组合多种攻击规则
- ✅ 易于创建变种角色（如"小三角形+低伤害"）

---

## 🎨 Unity 配置清单

### 需要创建的资源
- [ ] `Attack_Triangle.asset` - 三角形攻击规则
- [ ] `三角形角色.asset` - 角色配置
- [ ] `TriangleAreaEffect.prefab` - 三角形特效预制体

### Inspector 配置
- [ ] 在 Player 预制体上添加三角形特效预制体引用
- [ ] 配置三角形特效的颜色、透明度、持续时间

---

## 📝 后续优化方向

### 可选优化（低优先级）
1. **瞄准预测**：在瞄准时显示预测的三角形区域
2. **轨迹可视化**：用虚线连接三个关键点
3. **面积缩放**：根据伤害值动态调整三角形大小
4. **连击系统**：连续三角形攻击增加伤害

### 未来扩展可能
- 四边形攻击（四个关键点）
- 扇形攻击（中心+角度范围）
- 自定义多边形攻击

---

## ✅ 验收标准

### 功能验收
- [ ] 发射后记录起点和碰撞点
- [ ] 球停止后正确生成三角形
- [ ] 三角形区域内的敌人受到伤害
- [ ] 无碰撞时不触发攻击
- [ ] 显示三角形特效且正确淡出

### 性能验收
- [ ] 帧率不低于现有范围攻击
- [ ] 无内存泄漏
- [ ] 无明显卡顿

### 兼容性验收
- [ ] 现有角色功能不受影响
- [ ] 可以与其他规则正常组合
- [ ] 配置文件加载正常

---

**文档版本**：v1.0  
**最后更新**：2025年11月

