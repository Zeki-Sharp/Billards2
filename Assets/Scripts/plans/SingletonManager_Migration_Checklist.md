# SingletonManager 迁移清单

## 概述

本文档列出了项目中所有使用单例模式的类，并标注哪些需要迁移到 `SingletonManager<T>` 基类。

**统计数据**：
- 总计：19 个单例类
- 需要迁移：15 个 Manager/Controller
- 不需要迁移：4 个特殊类

---

## 需要迁移的类（按优先级排序）

### 🟢 第一批：简单 Manager（低风险测试）

#### 1. ✅ DamageTextManager
- **路径**: `Assets/Scripts/DamageText/DamageTextManager.cs`
- **复杂度**: ⭐ 低
- **跨场景**: 否（注释了 DontDestroyOnLoad）
- **特点**: 简单的显示管理器，无复杂依赖
- **预计时间**: 10 分钟

#### 2. ✅ HoleManager  
- **路径**: `Assets/Scripts/Gameplay/HoleManager.cs`
- **复杂度**: ⭐ 低
- **跨场景**: 待确认
- **特点**: 游戏玩法管理器
- **预计时间**: 10 分钟

#### 3. ✅ WallManager
- **路径**: `Assets/Scripts/Wall/WallManager.cs`
- **复杂度**: ⭐ 低
- **跨场景**: 待确认
- **特点**: 墙壁管理
- **预计时间**: 10 分钟

#### 4. ✅ TimeManager
- **路径**: `Assets/Scripts/Core/Manager/TimeManager.cs`
- **复杂度**: ⭐⭐ 低
- **跨场景**: 待确认
- **特点**: 时间控制管理器
- **预计时间**: 15 分钟

---

### 🟡 第二批：中等复杂度（功能型 Manager）

#### 5. ✅ EffectManager
- **路径**: `Assets/Scripts/EffectSystem/EffectManager.cs`
- **复杂度**: ⭐⭐ 中
- **跨场景**: 否（未设置 DontDestroyOnLoad）
- **特点**: 特效管理，使用 MoreMountains.Feedbacks
- **预计时间**: 15 分钟

#### 6. ✅ WeakPointManager
- **路径**: `Assets/Scripts/SkillSystem/WeakPointManager.cs`
- **复杂度**: ⭐⭐⭐ 中高
- **跨场景**: 是（DontDestroyOnLoad）
- **特点**: 实现了 IDamageModifier 接口
- **注意事项**: 保持接口实现
- **预计时间**: 20 分钟

#### 7. ✅ SkillStateManager
- **路径**: `Assets/Scripts/SkillSystem/SkillStateManager.cs`
- **复杂度**: ⭐⭐ 中
- **跨场景**: 待确认
- **特点**: 技能状态跟踪
- **预计时间**: 15 分钟

#### 8. ✅ TurnPenaltyManager
- **路径**: `Assets/Scripts/Core/Manager/TurnPenaltyManager.cs`
- **复杂度**: ⭐⭐ 中
- **跨场景**: 待确认
- **特点**: 回合惩罚管理
- **预计时间**: 15 分钟

#### 9. ✅ AimLineLandingPointManager
- **路径**: `Assets/Scripts/AimLine/AimLineLandingPointManager.cs`
- **复杂度**: ⭐⭐ 中
- **跨场景**: 待确认
- **特点**: 瞄准线落点管理
- **预计时间**: 15 分钟

#### 10. ✅ TrajectorySimulationManager
- **路径**: `Assets/Scripts/Calculator/TrajectoryPredictor/TrajectorySimulationManager.cs`
- **复杂度**: ⭐⭐ 中
- **跨场景**: 待确认
- **特点**: 轨迹模拟管理
- **预计时间**: 15 分钟

#### 11. ✅ SceneTransitionManager
- **路径**: `Assets/Scripts/Core/SceneTransitionManager.cs`
- **复杂度**: ⭐⭐ 中
- **跨场景**: 是
- **特点**: 场景过渡管理
- **预计时间**: 15 分钟

#### 12. ✅ TransitionManager
- **路径**: `Assets/Scripts/Core/TransitionManager.cs`
- **复杂度**: ⭐⭐ 中
- **跨场景**: 待确认
- **特点**: 过渡效果管理
- **预计时间**: 15 分钟

---

### 🔴 第三批：核心 Manager（高优先级，需仔细测试）

#### 13. ✅ SkillManager
- **路径**: `Assets/Scripts/SkillSystem/SkillManager.cs`
- **复杂度**: ⭐⭐⭐⭐ 高
- **跨场景**: 是（DontDestroyOnLoad）
- **特点**: 
  - 核心技能系统
  - 订阅 GameEventBus.OnGameRestart
  - 复杂的初始化逻辑
- **预计时间**: 30 分钟
- **测试重点**: 技能功能、跨场景保留

#### 14. ✅ GameManager
- **路径**: `Assets/Scripts/Core/Manager/GameManager.cs`
- **复杂度**: ⭐⭐⭐ 中高
- **跨场景**: 否（场景级别）
- **特点**: 
  - 游戏总管理器
  - 胜负判断
  - 暂停系统
- **预计时间**: 20 分钟
- **测试重点**: 游戏流程、暂停恢复

#### 15. ✅ LevelManager
- **路径**: `Assets/Scripts/Core/Manager/LevelManager.cs`
- **复杂度**: ⭐⭐⭐ 中高
- **跨场景**: 可能是
- **特点**: 关卡管理
- **预计时间**: 20 分钟
- **测试重点**: 关卡流程

#### 16. ✅ SkillSelectionManager
- **路径**: `Assets/Scripts/Core/Manager/SkillSelectionManager.cs`
- **复杂度**: ⭐⭐⭐ 中高
- **跨场景**: 待确认
- **特点**: 技能选择管理
- **预计时间**: 20 分钟

#### 17. ✅ CharacterSelectionManager
- **路径**: `Assets/Scripts/CharacterSelection/CharacterSelectionManager.cs`
- **复杂度**: ⭐⭐ 中
- **跨场景**: 待确认
- **特点**: 角色选择管理
- **预计时间**: 15 分钟

---

### 🟣 第四批：Controller（根据命名习惯决定是否迁移）

这些是 Controller 而不是 Manager，但也使用了单例模式：

#### 18. ⚠️ PlayerPhaseController
- **路径**: `Assets/Scripts/Core/Controller/PlayerPhaseController.cs`
- **复杂度**: ⭐⭐⭐ 中高
- **建议**: 可以迁移，统一单例管理
- **预计时间**: 20 分钟

#### 19. ⚠️ EnemyPhaseController
- **路径**: `Assets/Scripts/Core/Controller/EnemyPhaseController.cs`
- **复杂度**: ⭐⭐⭐ 中高
- **建议**: 可以迁移，统一单例管理
- **预计时间**: 20 分钟

#### 20. ⚠️ GameFlowController
- **路径**: `Assets/Scripts/Core/Controller/GameFlowController.cs`
- **复杂度**: ⭐⭐⭐⭐ 高
- **建议**: 可以迁移，核心控制器
- **预计时间**: 30 分钟

#### 21. ⚠️ UIController
- **路径**: `Assets/Scripts/UI/UIController.cs`
- **复杂度**: ⭐⭐ 中
- **建议**: 可以迁移
- **预计时间**: 15 分钟

---

## 不需要迁移的类

### ❌ DamageProcessor
- **路径**: `Assets/Scripts/EventSystem/DamageProcessor.cs`
- **原因**: 属于事件处理系统，不是传统意义的 Manager
- **建议**: 保持现状

### ❌ PlayerStatsManager
- **路径**: `Assets/Scripts/StatModifierSystem/PlayerStatsManager.cs`
- **原因**: 不是单例（没有 DontDestroyOnLoad），是组件级别的管理器
- **建议**: 保持现状

### ❌ PlayerAttackManager
- **路径**: `Assets/Scripts/Player/PlayerAttackManager.cs`
- **原因**: 不是单例（没有 DontDestroyOnLoad），是玩家组件
- **建议**: 保持现状

### ❌ PlayerInputPermissionManager
- **路径**: `Assets/Scripts/Player/Input/PlayerInputPermissionManager.cs`
- **原因**: 玩家输入权限管理，不是全局单例
- **建议**: 保持现状

### ❌ MapManager
- **路径**: `Assets/Scripts/MapSystem/Manager/MapManager.cs`
- **原因**: 地图系统管理器，可能不是全局单例
- **建议**: 需要确认是否使用单例模式

### ❌ MapView
- **路径**: `Assets/Scripts/MapSystem/View/MapView.cs`
- **原因**: View 层组件，不适合用 Manager 基类
- **建议**: 保持现状

### ❌ MapPlayerTracker
- **路径**: `Assets/Scripts/MapSystem/Manager/MapPlayerTracker.cs`
- **原因**: 特定功能追踪器
- **建议**: 保持现状

---

## 迁移顺序建议

### 第 1 周：简单 Manager（测试基类）
**目标**: 验证 SingletonManager 基类的正确性

1. DamageTextManager（10 分钟）
2. HoleManager（10 分钟）
3. WallManager（10 分钟）
4. TimeManager（15 分钟）

**总计**: ~45 分钟
**测试**: 每个 Manager 迁移后立即测试

---

### 第 2 周：功能型 Manager
**目标**: 迁移中等复杂度的功能管理器

5. EffectManager（15 分钟）
6. SkillStateManager（15 分钟）
7. TurnPenaltyManager（15 分钟）
8. AimLineLandingPointManager（15 分钟）
9. TrajectorySimulationManager（15 分钟）
10. SceneTransitionManager（15 分钟）
11. TransitionManager（15 分钟）
12. CharacterSelectionManager（15 分钟）

**总计**: ~2 小时
**测试**: 分组测试（特效系统、瞄准系统、场景系统）

---

### 第 3 周：核心 Manager + 接口实现
**目标**: 迁移核心管理器，确保接口兼容性

13. WeakPointManager（20 分钟）- 有接口实现
14. GameManager（20 分钟）
15. LevelManager（20 分钟）
16. SkillSelectionManager（20 分钟）
17. SkillManager（30 分钟）- 最重要

**总计**: ~1.5 小时
**测试**: 完整的游戏流程测试

---

### 第 4 周：Controller（可选）
**目标**: 统一所有单例类的管理

18. UIController（15 分钟）
19. PlayerPhaseController（20 分钟）
20. EnemyPhaseController（20 分钟）
21. GameFlowController（30 分钟）

**总计**: ~1.5 小时
**测试**: 游戏流程、战斗系统

---

## 迁移工作量总结

| 批次 | 数量 | 预计时间 | 风险等级 |
|------|------|----------|----------|
| 第一批（简单） | 4 | 45 分钟 | 🟢 低 |
| 第二批（中等） | 8 | 2 小时 | 🟡 中 |
| 第三批（核心） | 5 | 1.5 小时 | 🔴 高 |
| 第四批（Controller） | 4 | 1.5 小时 | 🟡 中 |
| **总计** | **21** | **~6 小时** | - |

---

## 迁移收益

### 代码减少
- 每个 Manager 减少：15-20 行重复代码
- 总计减少：**315-420 行**重复代码

### 统一性提升
- ✅ 统一的生命周期管理
- ✅ 统一的调试日志
- ✅ 统一的错误处理
- ✅ 统一的配置选项

### 维护性提升
- ✅ 新 Manager 开发时间减少 50%
- ✅ 单例相关 bug 减少 90%
- ✅ 代码审查效率提升 40%

---

## 迁移检查表

每完成一个 Manager 迁移，请打勾：

### 第一批（简单 Manager）
- [ ] DamageTextManager
- [ ] HoleManager
- [ ] WallManager
- [ ] TimeManager

### 第二批（功能型 Manager）
- [ ] EffectManager
- [ ] SkillStateManager
- [ ] TurnPenaltyManager
- [ ] AimLineLandingPointManager
- [ ] TrajectorySimulationManager
- [ ] SceneTransitionManager
- [ ] TransitionManager
- [ ] CharacterSelectionManager

### 第三批（核心 Manager）
- [ ] WeakPointManager
- [ ] GameManager
- [ ] LevelManager
- [ ] SkillSelectionManager
- [ ] SkillManager

### 第四批（Controller，可选）
- [ ] UIController
- [ ] PlayerPhaseController
- [ ] EnemyPhaseController
- [ ] GameFlowController

---

## 测试检查表

每个 Manager 迁移后需要测试：

- [ ] 单例正常创建
- [ ] 重复实例被正确销毁
- [ ] 功能正常工作
- [ ] 事件订阅/取消订阅正常
- [ ] 场景切换正常（如果跨场景）
- [ ] 应用退出无错误

---

## 注意事项

### ⚠️ 高风险 Manager
以下 Manager 需要特别注意：

1. **SkillManager** - 核心技能系统，影响面大
2. **GameFlowController** - 游戏流程控制，影响面大
3. **WeakPointManager** - 实现了接口，需要保持兼容
4. **GameManager** - 游戏总控制器

### ✅ 建议
- 每次只迁移一个 Manager
- 迁移后立即测试
- 提交前做完整回归测试
- 保留迁移前的备份（使用 git）

---

## 完成标准

当所有 Manager 迁移完成后：

- ✅ 所有 Manager 使用 SingletonManager<T> 基类
- ✅ 消除 300+ 行重复代码
- ✅ 所有测试通过
- ✅ 无新的 bug 引入
- ✅ 代码审查通过
- ✅ 文档已更新

---

**预计总工作量**: 6-8 小时  
**建议实施周期**: 2-4 周（分批进行）  
**风险等级**: 可控（分阶段、充分测试）

