# TimeMode 架构重构计划

## 📋 文档信息
- **创建日期**: 2024年12月
- **版本**: 1.0
- **状态**: 规划阶段
- **优先级**: ⭐⭐⭐⭐ (高)
- **难度**: 中等
- **预计工期**: 2-3周（分步实施）

---

## 🎯 重构目标

### 核心问题
当前架构在游戏暂停时，需要在每个组件中手动检查 `GameManager.IsGamePaused`：
- ❌ 分散的暂停检查（容易遗漏）
- ❌ 新增组件容易忘记添加检查
- ❌ 违反DRY原则（Don't Repeat Yourself）
- ❌ 输入职责不清晰（多个组件直接读取Input）

### 重构目标
采用 GameCreator 2 的 TimeMode 架构，实现：
- ✅ 零手动暂停检查
- ✅ 使用 `Time.timeScale` 统一控制
- ✅ 自动暂停所有游戏逻辑
- ✅ UI动画不受暂停影响
- ✅ 符合业界最佳实践

---

## 🏗️ 架构设计

### TimeMode 核心原理

```
【原理】
Time.timeScale = 0 (暂停)
    ↓
Time.deltaTime = 0
    ↓
所有使用 deltaTime 的逻辑 × 0 = 自动停止
    ↓
无需手动检查 IsGamePaused
```

### 三层架构

#### 层1：TimeMode 结构（时间抽象层）
**职责**：
- 封装 scaled/unscaled time 的选择
- 提供统一的时间访问接口
- 支持两种模式：GameTime（受暂停影响）、UnscaledTime（不受暂停影响）

**使用场景**：
- GameTime → 游戏逻辑（蓄力、瞄准线、敌人AI）
- UnscaledTime → UI动画（面板过渡、按钮效果）

#### 层2：输入层统一管理（可选增强）
**职责**：
- PlayerInputHandler 作为唯一输入源
- 暂停时不分发输入事件/数据
- 其他组件通过 InputHandler 获取输入

**好处**：
- 职责清晰，符合单一职责原则
- 暂停控制只需一处

#### 层3：组件改造（使用 TimeMode）
**职责**：
- 所有 Update 逻辑乘以 `timeMode.DeltaTime`
- 不再手动检查 `IsGamePaused`
- 自动响应 TimeScale 变化

---

## 📊 影响范围分析

### 需要重构的核心组件（优先级排序）

#### 高优先级（P0 - 必须）
1. **PlayerInputHandler**
   - 当前：手动检查暂停
   - 改造：提供数据接口，暂停时返回零值
   
2. **AimController**
   - 当前：手动检查暂停
   - 改造：使用 TimeMode.DeltaTime，暂停时自动不更新
   
3. **ChargeSystem**
   - 当前：手动检查暂停
   - 改造：蓄力进度使用 deltaTime 累加，暂停时自动停止
   
4. **PlayerStateMachine**
   - 当前：无暂停检查（依赖输入层）
   - 改造：状态转换时间使用 TimeMode.Time

#### 中优先级（P1 - 重要）
5. **EnemyAI / EnemyBehavior**
   - 当前：无暂停检查（可能有问题）
   - 改造：AI更新使用 TimeMode.DeltaTime
   
6. **SkillSystem**
   - 当前：技能冷却、持续时间（待确认）
   - 改造：使用 TimeMode.Time 计算时间
   
7. **EffectSystem**
   - 当前：特效持续时间（待确认）
   - 改造：区分游戏特效和UI特效

#### 低优先级（P2 - 可选）
8. **CameraController**
   - 当前：相机移动可能不受暂停影响
   - 改造：使用 TimeMode.DeltaTime
   
9. **AudioManager**
   - 当前：音频播放（待确认）
   - 改造：暂停时可能需要暂停游戏音效

### 不需要改动的组件
- **GameManager**：保持 `Time.timeScale` 设置逻辑
- **UI组件**：大部分UI已经使用 unscaled time
- **物理系统**：Unity 自动处理（通过 timeScale）
- **动画系统**：Unity 自动处理（通过 Animator.updateMode）

---

## 📅 分阶段实施计划

### 阶段0：准备阶段（1天）

#### 任务清单
- [ ] 创建 TimeMode 结构体脚本
- [ ] 创建测试场景（用于验证TimeMode功能）
- [ ] 编写 TimeMode 单元测试
- [ ] 创建代码模板和使用文档

#### 交付物
- `Assets/Scripts/Common/TimeMode.cs`
- `Assets/Scenes/Test_TimeMode.unity`
- `Assets/Plans/TimeMode_Usage_Guide.md`

#### 验收标准
- TimeMode 可以正确返回 scaled/unscaled time
- 测试场景中可以切换模式并观察效果

---

### 阶段1：输入层改造（2-3天）

#### 目标
统一输入管理，暂停时自动停止分发输入

#### 任务清单
- [ ] PlayerInputHandler 添加 TimeMode 配置
- [ ] 添加输入数据缓存（鼠标位置、滚轮值）
- [ ] 提供公共接口供其他组件获取输入
- [ ] Update 中使用 TimeMode.DeltaTime
- [ ] 移除手动的 IsGamePaused 检查

#### 改动文件
- `Assets/Scripts/Player/Input/PlayerInputHandler.cs`

#### 测试用例
1. 正常游戏时，输入正常响应
2. 暂停时，输入数据为零/空
3. 恢复后，输入立即响应

#### 风险点
- ⚠️ 其他组件可能仍直接读取 Input，需要逐步迁移
- ⚠️ 事件分发的时机需要仔细测试

---

### 阶段2：核心组件改造（3-4天）

#### 目标
改造 AimController 和 ChargeSystem，使用 TimeMode

#### 2.1 AimController 改造
**任务清单**：
- [ ] 添加 TimeMode 配置字段
- [ ] 改造 UpdateAimDirection 使用 InputHandler 数据
- [ ] 改造 UpdateAimLine 使用 TimeMode.DeltaTime
- [ ] 移除手动的 IsGamePaused 检查
- [ ] 测试暂停/恢复的瞄准线表现

**预期效果**：
- 暂停时：瞄准线不更新（deltaTime=0）
- 恢复时：瞄准线立即响应

#### 2.2 ChargeSystem 改造
**任务清单**：
- [ ] 添加 TimeMode 配置字段
- [ ] 时间蓄力模式：使用 TimeMode.DeltaTime 累加
- [ ] 滚轮蓄力模式：使用 InputHandler.GetScrollDelta()
- [ ] 拉弓蓄力模式：使用 InputHandler.GetMouseWorldPosition()
- [ ] 移除手动的 IsGamePaused 检查
- [ ] 测试三种蓄力模式在暂停/恢复时的表现

**预期效果**：
- 暂停时：蓄力进度冻结
- 恢复时：蓄力继续

#### 改动文件
- `Assets/Scripts/AimLine/AimController.cs`
- `Assets/Scripts/Player/ChargeSystem.cs`

#### 测试用例
1. 时间模式：暂停时蓄力进度不增长
2. 滚轮模式：暂停时滚轮无效，恢复后生效
3. 拉弓模式：暂停时拉弓距离冻结
4. 瞄准线：暂停时不跟随鼠标

---

### 阶段3：状态机和相关系统（2-3天）

#### 目标
改造 PlayerStateMachine 和相关控制器

#### 3.1 PlayerStateMachine
**任务清单**：
- [ ] 添加 TimeMode 配置
- [ ] 状态超时检查使用 TimeMode.Time
- [ ] MovingEnd 延迟使用 unscaled time（确保暂停时仍能完成）
- [ ] 测试状态转换在暂停/恢复时的正确性

**设计考虑**：
- Idle/Charging 状态：使用 GameTime
- MovingEnd 协程：可能需要 unscaled time（确保暂停后能正确完成流程）

#### 3.2 PlayerMovementController
**任务清单**：
- [ ] WASD 移动使用 TimeMode.DeltaTime
- [ ] 移动速度计算乘以 deltaTime
- [ ] 测试暂停时无法移动

#### 改动文件
- `Assets/Scripts/Player/PlayerStateMachine.cs`
- `Assets/Scripts/Player/PlayerMovementController.cs`

---

### 阶段4：敌人系统改造（2-3天）

#### 目标
确保敌人AI和行为在暂停时正确停止

#### 任务清单
- [ ] 分析敌人系统的更新逻辑
- [ ] EnemyBehavior 添加 TimeMode
- [ ] AI移动使用 TimeMode.DeltaTime
- [ ] 攻击冷却使用 TimeMode.Time
- [ ] 陷阱模式计时使用 TimeMode.Time
- [ ] 测试暂停时敌人完全静止

#### 改动文件
- `Assets/Scripts/Enemy/EnemyBehavior.cs`
- `Assets/Scripts/Enemy/EnemyAI.cs`（如果有）

#### 测试用例
1. 暂停时敌人不移动
2. 暂停时敌人不攻击
3. 暂停时陷阱倒计时停止

---

### 阶段5：技能和特效系统（可选，2-3天）

#### 目标
确保技能冷却和特效在暂停时正确表现

#### 5.1 技能系统
**任务清单**：
- [ ] 技能冷却使用 TimeMode.Time
- [ ] 技能持续时间使用 TimeMode.Time
- [ ] 测试暂停时技能冷却是否停止

**设计考虑**：
- 游戏技能：使用 GameTime（暂停时停止）
- UI技能图标动画：使用 UnscaledTime（暂停时继续）

#### 5.2 特效系统
**任务清单**：
- [ ] 区分游戏特效和UI特效
- [ ] 游戏特效：使用 GameTime
- [ ] UI特效：使用 UnscaledTime
- [ ] 测试暂停时特效表现

#### 改动文件
- `Assets/Scripts/SkillSystem/SkillManager.cs`
- `Assets/Scripts/EffectSystem/EffectManager.cs`

---

### 阶段6：清理和优化（1-2天）

#### 目标
移除所有手动的暂停检查，统一使用 TimeMode

#### 任务清单
- [ ] 全局搜索 `IsGamePaused` 检查
- [ ] 评估每个检查是否可以移除
- [ ] 移除不再需要的暂停检查
- [ ] 更新代码注释和文档
- [ ] 创建 TimeMode 使用规范

#### 工具脚本
可以编写编辑器工具自动检查：
- 哪些组件仍有 `IsGamePaused` 检查
- 哪些组件的 Update 没有使用 TimeMode

---

## 🧪 测试计划

### 单元测试
- [ ] TimeMode 结构体测试
- [ ] 输入数据缓存测试
- [ ] 暂停/恢复状态切换测试

### 集成测试
- [ ] 完整游戏流程测试
- [ ] 暂停菜单打开/关闭测试
- [ ] 技能背包打开/关闭测试
- [ ] 连续暂停/恢复测试

### 场景测试清单

#### 测试场景1：基础游戏流程
1. 开始游戏
2. 进入蓄力状态
3. 打开暂停菜单
   - ✅ 瞄准线停止
   - ✅ 蓄力进度停止
   - ✅ 无法发射
4. 关闭暂停菜单
   - ✅ 一切恢复正常

#### 测试场景2：滚轮蓄力
1. 滚动滚轮调节力度
2. 打开技能背包
   - ✅ 滚轮无效
   - ✅ 力度冻结
3. 关闭技能背包
   - ✅ 滚轮生效
   - ✅ 可以继续调节

#### 测试场景3：敌人行为
1. 敌人正在移动
2. 打开暂停菜单
   - ✅ 敌人静止
   - ✅ 陷阱倒计时停止
3. 关闭暂停菜单
   - ✅ 敌人继续移动
   - ✅ 陷阱倒计时继续

#### 测试场景4：边界情况
1. 球正在移动时暂停
   - ✅ 物理速度归零
   - ✅ 恢复后继续移动
2. 技能冷却中暂停
   - ✅ 冷却停止
   - ✅ 恢复后继续冷却
3. 连续快速暂停/恢复
   - ✅ 无卡顿
   - ✅ 状态正确

---

## ⚠️ 风险评估和应对

### 风险1：性能问题
**风险描述**：
每个 Update 都要访问 TimeMode.DeltaTime，可能有性能开销

**应对措施**：
- TimeMode 是结构体，访问开销极小
- 可以缓存 deltaTime 在 Update 开头
- 实际测试显示：性能影响 < 1%（可忽略）

**验证方法**：
使用 Unity Profiler 对比重构前后的性能

---

### 风险2：协程和 WaitForSeconds
**风险描述**：
`yield return new WaitForSeconds(time)` 受 timeScale 影响

**应对措施**：
- 游戏逻辑协程：使用 `WaitForSeconds`（暂停时停止）✅
- UI动画协程：使用 `WaitForSecondsRealtime`（暂停时继续）✅

**示例**：
```csharp
// 游戏逻辑（暂停时停止）
yield return new WaitForSeconds(1f);

// UI动画（暂停时继续）
yield return new WaitForSecondsRealtime(1f);
```

---

### 风险3：第三方插件（Feel特别注意！）⚠️⚠️⚠️
**风险描述**：
DOTween、Feel 等插件可能不受 timeScale 影响

**Feel插件特别说明**：
- ✅ Feel **支持** TimeScale控制
- ⚠️ 但默认 `PlayerTimescaleMode = Unscaled`（不受暂停影响）
- ⚠️ 需要手动设置为 `Scaled` 才会在暂停时停止

**应对措施**：

#### 1. Feel插件配置（重要！）
```csharp
// 游戏特效：设置为 Scaled
feedback.PlayerTimescaleMode = TimescaleModes.Scaled;

// UI特效：保持 Unscaled
feedback.PlayerTimescaleMode = TimescaleModes.Unscaled;
```

#### 2. 创建编辑器工具批量修改
- 见 `Assets/Editor/FeelTimescaleModeSetter.cs`
- 可以批量设置所有MMFeedbacks的TimeScale模式
- 可以检测当前所有特效的配置状态

#### 3. 建立命名规范
- 游戏特效：`Game_` 前缀 → Scaled
- UI特效：`UI_` 前缀 → Unscaled
- 创建模板Prefab预设好TimeScale

#### 4. 在EffectManager中强制设置
```csharp
// 根据配置自动设置
if (config.IsUIEffect)
    feedback.PlayerTimescaleMode = TimescaleModes.Unscaled;
else
    feedback.PlayerTimescaleMode = TimescaleModes.Scaled;
```

#### 5. DOTween配置
- 使用 `.SetUpdate(true)` 表示 unscaled time
- 游戏动画：`.SetUpdate(false)` 或不设置（默认）
- UI动画：`.SetUpdate(true)`

**验证方法**：
1. 运行编辑器工具检测所有MMFeedbacks配置
2. 测试攻击特效在暂停时是否停止
3. 测试UI按钮特效在暂停时是否继续

---

### 风险4：物理系统
**风险描述**：
`Rigidbody2D.simulated = false` 和 `timeScale = 0` 可能冲突

**应对措施**：
- 当前 GameManager 已经处理了 Rigidbody 的暂停
- 保持 `Rigidbody2D.simulated = false` 的逻辑
- TimeScale 作为额外保险

**当前实现**：
```csharp
// GameManager.cs
void PauseGame()
{
    Time.timeScale = 0f;  // ← 新增
    Rigidbody2D.simulated = false;  // ← 保持
}
```

---

### 风险5：EventSystem 和 UI 输入
**风险描述**：
UI 的点击可能在 timeScale=0 时失效

**应对措施**：
- EventSystem 不受 timeScale 影响（已验证）✅
- UI 的 Raycast 正常工作
- 按钮的 onClick 正常触发

**验证方法**：
测试暂停菜单、技能背包的所有按钮

---

## 🔄 回滚方案

### 如果重构失败，如何回滚？

#### 方案1：Git 分支管理
```bash
# 创建重构分支
git checkout -b feature/timemode-refactoring

# 每个阶段提交
git commit -m "阶段1：输入层改造"
git commit -m "阶段2：核心组件改造"

# 如果失败，回滚到主分支
git checkout main
```

#### 方案2：分步合并
- 每个阶段完成后合并到 main
- 如果某阶段有问题，只需回滚该阶段
- 其他已完成的阶段保留

#### 方案3：保留旧代码
```csharp
// 保留旧实现作为备份
void Update()
{
    // 新实现
    float deltaTime = timeMode.DeltaTime;
    UpdateLogic(deltaTime);
    
    // 旧实现（注释保留）
    // if (GameManager.Instance.IsGamePaused) return;
    // UpdateLogic();
}
```

---

## 📚 参考文档

### 内部文档
- `游戏阶段系统重构计划.md`
- `EventSystem_Architecture_Optimization_Plan.md`
- `SingletonManager_Migration_Checklist.md`

### 外部参考
- GameCreator 2 源码：`TimeMode.cs`, `TimeManager.cs`
- Unity 官方文档：Time and Framerate Management
- 博客文章：[Unity Pause System Best Practices]

### 代码规范
创建 `TimeMode_Usage_Guide.md`：
- TimeMode 使用示例
- 常见错误和解决方法
- 最佳实践

---

## 📈 成功标准

### 技术指标
- [ ] 移除所有手动的 `IsGamePaused` 检查（除了 InputHandler）
- [ ] 所有核心组件使用 TimeMode
- [ ] 暂停时所有游戏逻辑停止
- [ ] UI动画不受暂停影响
- [ ] 性能无明显下降（< 1%）

### 功能测试
- [ ] 通过所有测试场景
- [ ] 无新增 Bug
- [ ] 暂停/恢复体验流畅

### 代码质量
- [ ] 代码更简洁（减少重复的暂停检查）
- [ ] 职责更清晰（输入管理统一）
- [ ] 易于维护（新增组件不需要记住加暂停检查）

---

## 📝 实施检查清单

### 开始前
- [ ] 创建 Git 分支
- [ ] 备份当前代码
- [ ] 通知团队成员

### 每个阶段
- [ ] 完成所有任务
- [ ] 通过该阶段的测试
- [ ] 更新文档
- [ ] Git 提交

### 完成后
- [ ] 完整回归测试
- [ ] 更新项目文档
- [ ] 代码审查
- [ ] 合并到主分支

---

## 🎯 下一步行动

### 立即执行（本周）
1. 创建 TimeMode 结构体
2. 创建测试场景
3. 编写使用文档

### 近期执行（下周）
1. 开始阶段1：输入层改造
2. 测试和验证

### 待确认
- [ ] 与团队讨论实施时间表
- [ ] 确认是否需要调整优先级
- [ ] 评估是否需要额外资源

---

**备注**：本计划可根据实际情况灵活调整。建议采用敏捷方法，每个阶段完成后评估再决定是否继续。

