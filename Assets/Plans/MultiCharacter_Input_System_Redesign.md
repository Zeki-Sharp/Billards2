# 多角色输入系统架构重新设计

> **文档目的**：专门解决多角色系统的输入检测问题
> 
> **范围**：只涉及输入检测、角色选择、蓄力控制
> 
> **不包含**：技能系统、UI详细设计、死亡判定（这些在 `MultiCharacter_Control_System_Plan.md` 中）

---

## 📋 问题诊断

### 当前架构的根本问题

**问题1：InputHandler 作为 Player 组件的耦合性**
- 每个球体都有自己的 `PlayerInputHandler`
- 禁用球体组件会导致无法检测输入
- 多个 InputHandler 同时响应导致所有球体都发射

**问题2：事件系统的广播问题**
- `GameEventBus.PublishChargingStarted()` 是全局广播
- 所有球体的 `ChargeSystem` 都会响应
- 无法区分事件的目标角色

**问题3：职责混乱**
- `BallSelectionManager` 需要依赖 `PlayerInputHandler` 检测点击
- `PlayerInputHandler` 又需要调用 `BallSelectionManager` 处理选择
- 循环依赖和职责不清

**问题4：WASD移动系统暂不需要**
- 当前多角色系统不需要移动输入
- 保留会增加复杂度

---

## 🎯 新架构设计原则

### 核心原则

1. **输入系统全局化**：场景中只有一个输入管理器，不受球体启用/禁用影响
2. **事件系统具体化**：事件携带角色ID，明确指定目标
3. **职责单一化**：每个管理器只负责一件事
4. **数据驱动**：通过数据传递而非组件引用通信
5. **移除冗余功能**：暂时不需要WASD移动，完全移除 `PlayerInputHandler`

---

## 🏗️ 新架构概览

```
┌─────────────────────────────────────────────────────────────┐
│                  全局输入与事件架构                           │
├─────────────────────────────────────────────────────────────┤
│  场景级输入管理（Scene-Level Input）                         │
│  └── GlobalInputManager (新增)                              │
│      - 场景单例，独立GameObject                              │
│      - 检测所有原始输入（左键/右键/滚轮）                     │
│      - 不依赖任何球体组件                                     │
│      - 射线检测判断点击目标                                   │
├─────────────────────────────────────────────────────────────┤
│  角色选择管理（Character Selection）                         │
│  └── CharacterSelectionController (新增)                    │
│      - 维护当前选中角色ID                                     │
│      - 响应 GlobalInputManager 的点击事件                    │
│      - 发布具体角色的选择/取消选择事件                         │
├─────────────────────────────────────────────────────────────┤
│  蓄力操作管理（Charge Control）                              │
│  └── ChargeController (新增)                                │
│      - 响应 GlobalInputManager 的滚轮事件                    │
│      - 直接调用特定角色的 ChargeSystem                       │
│      - 响应 GlobalInputManager 的发射事件                    │
├─────────────────────────────────────────────────────────────┤
│  新事件总线（Character-Specific Events）                     │
│  └── GameEventBus (扩展)                                     │
│      - OnCharacterSelected(characterID)                     │
│      - OnCharacterDeselected(characterID)                   │
│      - OnCharacterChargingStarted(characterID)              │
│      - OnCharacterChargingStopped(characterID, force)       │
│      - OnCharacterLaunched(characterID, direction, force)   │
├─────────────────────────────────────────────────────────────┤
│  球体组件（Ball Components - 简化为执行者）                  │
│  ├── PlayerInputHandler (✅ 完全移除)                       │
│  ├── ChargeSystem (改为被动响应)                            │
│  └── Player (保持不变)                                       │
│      - 不再主动检测输入                                       │
│      - 只响应来自控制器的指令                                 │
│      - 通过角色ID过滤事件                                     │
└─────────────────────────────────────────────────────────────┘
```

---

## 📦 核心模块设计

### 1. GlobalInputManager（全局输入管理器）

**定位**：场景中的独立GameObject，唯一的输入检测入口

**职责**：
- 检测所有原始输入（鼠标点击、滚轮）
- 进行射线检测判断点击目标
- 发布原始输入事件（点击、滚轮、右键）
- 处理UI遮挡检测

**检测的输入**：
- 左键点击：用于选择角色或发射
- 右键点击：用于取消选择
- 滚轮滚动：用于调节蓄力

**不负责**：
- 不管理当前选中状态
- 不处理蓄力逻辑
- 不判断输入意图（交给其他控制器）

**优势**：
- 永远不会被禁用
- 单一职责：只负责输入检测
- 不依赖球体组件

---

### 2. CharacterSelectionController（角色选择控制器）

**定位**：管理当前选中的角色状态

**职责**：
- 维护当前选中的角色ID
- 响应 GlobalInputManager 的点击球体事件
- 判断是否允许切换选择（例如蓄力>0时不允许）
- 发布具体角色的选择/取消选择事件

**不负责**：
- 不检测输入
- 不处理蓄力

**数据流**：
```
GlobalInputManager 检测点击
    ↓ 发布：OnBallClicked(ballGameObject)
CharacterSelectionController 响应
    ↓ 判断：是否可以选择/切换
    ↓ 发布：OnCharacterSelected(characterID)
各系统响应（UI、蓄力、相机等）
```

---

### 3. ChargeController（蓄力控制器）

**定位**：协调蓄力操作流程

**职责**：
- 监听角色选择事件，启动对应角色的蓄力
- 响应 GlobalInputManager 的滚轮输入
- 将滚轮输入传递给当前选中角色的 ChargeSystem
- 响应 GlobalInputManager 的发射事件
- 触发当前选中角色的发射

**不负责**：
- 不检测输入
- 不维护选中状态

**数据流**：
```
CharacterSelectionController 发布 OnCharacterSelected(characterID)
    ↓
ChargeController 响应
    ↓ 查找角色的 ChargeSystem
    ↓ 调用 StartCharging()
    ↓ 发布：OnCharacterChargingStarted(characterID)
    
GlobalInputManager 发布 OnScrollInput(delta)
    ↓
ChargeController 响应
    ↓ 调用当前角色 ChargeSystem.AdjustForce(delta)
    
GlobalInputManager 发布 OnLaunchInput()
    ↓
ChargeController 响应
    ↓ 调用当前角色 ChargeSystem.Launch()
    ↓ 发布：OnCharacterLaunched(characterID, direction, force)
```

---

### 4. GameEventBus 扩展（带角色ID的事件）

**新增事件类型**：

**输入相关**：
- `OnCharacterSelected(characterID)` - 角色被选中
- `OnCharacterDeselected(characterID)` - 角色被取消选中
- `OnCharacterChargingStarted(characterID)` - 特定角色开始蓄力
- `OnCharacterChargingStopped(characterID, force)` - 特定角色停止蓄力
- `OnCharacterLaunched(characterID, direction, force)` - 特定角色发射

**战斗相关**：
- `OnCharacterDamaged(characterID, damage, sourceID)` - 特定角色受伤
- `OnCharacterHealed(characterID, amount)` - 特定角色治疗
- `OnCharacterDied(characterID)` - 特定角色死亡

**技能相关**：
- `OnCharacterSkillAdded(characterID, skillID)` - 给特定角色添加技能
- `OnCharacterSkillActivated(characterID, skillID)` - 特定角色的技能触发
- `OnCharacterSkillRemoved(characterID, skillID)` - 移除特定角色的技能

**设计优势**：
- 事件接收者可以过滤自己关心的角色ID
- UI可以显示具体哪个角色的状态变化
- 技能系统可以正确判断触发条件
- 不会有全局广播导致的误触发

---

### 5. 球体组件简化（从主动检测改为被动响应）

**ChargeSystem 改造**：
- 移除对 `GameEventBus.OnChargingStarted` 的订阅
- 提供公共方法供 ChargeController 调用
- 内部只负责力度计算和状态管理
- 不再关心输入检测

**PlayerInputHandler 处理方案**：

✅ **采用方案A：完全移除**
- 当前不需要 WASD 移动功能
- 所有输入统一由 GlobalInputManager 处理
- 简化 Player Prefab 结构
- 未来如需移动功能，可在 GlobalInputManager 中添加

---

## 🔄 交互流程

### 完整操作流程

#### 1. 选择角色流程

```
用户点击球体
    ↓
GlobalInputManager 检测点击 + 射线检测
    ↓ 发布：OnBallClicked(ballObject)
    ↓
CharacterSelectionController 响应
    ↓ 从 ballObject 获取 characterID
    ↓ 检查是否可以选择（蓄力为0？角色存活？）
    ↓ 取消上一个选中角色（如果有）
    ↓ 发布：OnCharacterDeselected(oldCharacterID)
    ↓ 设置新选中角色
    ↓ 发布：OnCharacterSelected(newCharacterID)
    ↓
多个系统响应：
    - ChargeController：启动蓄力
    - UI系统：显示选中标识
    - 相机系统：聚焦角色
    - AudioManager：播放选中音效
```

#### 2. 蓄力与发射流程

```
角色已选中，用户滚动滚轮
    ↓
GlobalInputManager 检测滚轮
    ↓ 发布：OnScrollInput(delta)
    ↓
ChargeController 响应
    ↓ 获取当前选中角色的 ChargeSystem
    ↓ 调用 chargeSystem.AdjustForce(delta)
    ↓ ChargeSystem 更新力度
    ↓ 发布：OnChargeForceChanged(characterID, newForce)
    ↓
UI响应：更新力度条

---

用户点击发射
    ↓
GlobalInputManager 检测点击
    ↓ 判断意图：蓄力>0？→ 发射
    ↓ 发布：OnLaunchInput()
    ↓
ChargeController 响应
    ↓ 获取当前选中角色的 ChargeSystem
    ↓ 调用 chargeSystem.Launch()
    ↓ ChargeSystem 执行发射
    ↓ 发布：OnCharacterLaunched(characterID, direction, force)
    ↓
多个系统响应：
    - PlayerMovementController：应用力
    - UI系统：隐藏力度条
    - AudioManager：播放发射音效
    - 取消选中：CharacterSelectionController 清空选中
```

#### 3. 右键取消流程

```
用户右键点击
    ↓
GlobalInputManager 检测右键
    ↓ 发布：OnCancelInput()
    ↓
CharacterSelectionController 响应
    ↓ 重置蓄力（如果有）
    ↓ 发布：OnCharacterDeselected(characterID)
    ↓
多个系统响应：
    - ChargeController：停止蓄力
    - UI系统：隐藏选中标识
```

---

## 🔗 与其他系统的接口

> **注意**：技能系统、UI系统、死亡判定的详细改造请参考 `MultiCharacter_Control_System_Plan.md`

### 对外提供的事件

**供UI系统订阅**：
- `OnCharacterSelected(characterID)` - 用于显示选中高亮
- `OnCharacterDeselected(characterID)` - 用于隐藏选中标识
- `OnCharacterChargingStarted(characterID)` - 用于显示蓄力UI
- `OnCharacterLaunched(characterID, direction, force)` - 用于隐藏蓄力UI

**供技能系统订阅**：
- `OnCharacterLaunched(characterID, ...)` - 用于触发"发射后"类技能

**供战斗系统订阅**：
- `OnCharacterSelected(characterID)` - 用于相机聚焦、音效等

### 需要响应的事件

**来自战斗系统**：
- `OnCharacterDied(characterID)` - 如果是当前选中角色，自动取消选择

---

## ⚠️ 关键设计决策

### 1. 为什么 InputManager 要全局化？

**问题**：每个球体都有 InputHandler 会导致多个响应
**解决**：场景唯一的输入管理器，不受球体启用/禁用影响

### 2. 为什么事件需要携带角色ID？

**问题**：全局事件无法区分目标，所有角色都响应
**解决**：带ID事件让接收者自己过滤，实现精确控制

### 3. 为什么要分离选择/蓄力/发射管理器？

**问题**：单一Manager职责过重，难以维护
**解决**：单一职责原则，每个Manager只做一件事

### 4. ChargeSystem 为什么要改为被动？

**问题**：多个ChargeSystem订阅全局事件会同时触发
**解决**：改为提供方法，由Controller主动调用特定角色的ChargeSystem

---

## 📐 Unity场景配置

### 新增GameObject结构

```
═══════════════════════════════════════
🎮 GameSystems (场景根对象)
───────────────────────────────────────
  └─ GlobalInputManager (新增)
      - 独立GameObject
      - 不依赖任何球体
      - 不会被禁用/销毁
      
  └─ CharacterSelectionController (新增)
      - 场景单例
      - 管理选中状态
      
  └─ ChargeController (新增)
      - 场景单例
      - 协调蓄力流程
      
═══════════════════════════════════════
```

### Player Prefab 简化

```
Player Prefab
  ├── PlayerInputHandler (✅ 完全移除)
  ├── ChargeSystem (改为被动响应)
  ├── BallPhysics (不变)
  ├── Player (不变)
  └── ... (其他组件不变)
```

---

## 🚀 实施步骤

### 步骤1：扩展事件系统
- 在 `GameEventBus` 中添加带角色ID的新事件
- 保持与现有事件的兼容性
- 添加事件数据类（如果需要）

### 步骤2：创建 GlobalInputManager
- 创建独立的场景GameObject
- 实现输入检测（左键、右键、滚轮）
- 实现射线检测
- 发布原始输入事件

### 步骤3：创建 CharacterSelectionController
- 维护当前选中的角色ID
- 响应 GlobalInputManager 的点击事件
- 判断选择/切换逻辑
- 发布角色选择/取消选择事件

### 步骤4：创建 ChargeController
- 响应角色选择事件，启动对应角色的蓄力
- 响应滚轮输入，调节当前角色的蓄力
- 响应发射输入，触发当前角色的发射
- 发布蓄力和发射事件

### 步骤5：改造 ChargeSystem
- 移除对全局事件的订阅
- 改为提供公共方法
- 保持内部逻辑不变

### 步骤6：移除 PlayerInputHandler
- 从 Player Prefab 中删除组件
- 清理相关引用
- 测试新输入流程

### 步骤7：集成测试
- 测试选择、蓄力、发射流程
- 测试切换角色
- 测试右键取消
- 确保没有多球体同时发射的问题

---

## ✅ 设计验证

### 核心问题验证

| 问题 | 旧架构 | 新架构 |
|------|--------|--------|
| 所有球体都发射 | ❌ 所有InputHandler响应 | ✅ 只有选中角色发射 |
| 禁用球体后无法点击 | ❌ InputHandler被禁用 | ✅ GlobalInputManager独立存在 |
| 技能触发错误角色 | ❌ 全局事件无法区分 | ✅ 事件带角色ID过滤 |
| UI更新不知道是哪个角色 | ❌ 无法区分 | ✅ 事件明确指定角色 |
| 职责不清晰 | ❌ Manager互相依赖 | ✅ 单一职责，清晰流程 |

---

## 📝 总结

### 核心改变

1. **输入检测从分散到集中**：GlobalInputManager 场景单例
2. **事件从广播到定向**：所有事件携带角色ID
3. **组件从主动到被动**：球体组件只响应指令，不检测输入
4. **职责从混乱到清晰**：每个Manager单一职责

### 架构优势

- ✅ 解决多球体输入冲突
- ✅ 支持精确的角色控制
- ✅ 技能系统可正确过滤角色
- ✅ UI可显示具体角色状态
- ✅ 易于扩展和维护
- ✅ 符合单一职责原则

### 与现有系统兼容性

- 保留现有的 GameEventBus 结构，只扩展新事件
- 球体组件只需简化，不需要完全重写
- 现有的移动、物理、碰撞逻辑完全不变
- UI系统只需订阅新事件，不影响旧UI

