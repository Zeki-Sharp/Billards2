# 玩家阶段与攻击系统重构计划

## 文档信息
- **创建日期**: 2025年10月
- **版本**: 1.0
- **状态**: 设计阶段
- **优先级**: 高
- **目标**: 梳理清晰 PlayerPhaseController 和 PlayerStateMachine 的职责边界
- **当前范围**: 仅重构状态机流程，暂不实现多种攻击方式

---

## 一、当前架构问题

### 1.1 职责重叠
- `PlayerStateMachine` 和 `PlayerPhaseController` 存在职责重叠
- 两者都管理 Charging、Moving 等状态，造成冗余
- `PlayerPhaseController` 监听 `PlayerStateMachine` 的状态变化来切换自己的子阶段，形成循环依赖

### 1.2 语义混乱
- `PlayerPhaseController.Normal` 的含义不清晰（是否允许移动？）
- `Launching` 阶段多余（发射和状态切换同时发生）
- 缺少专门处理攻击和Buff的阶段

### 1.3 架构定位错误
- `Transition` 实际是一个技能效果，不应该作为必然流程阶段
- Transition 放在 PhaseController 导致可选功能与必然流程混淆
- 与 EnemyPhaseController 的架构不对称（敌人都是必然流程）

---

## 二、设计目标

### 2.1 职责清晰
- **PlayerPhaseController**：负责回合级别的流程编排（开始、游玩、过渡、结束）
- **PlayerStateMachine**：负责操作级别的状态管理（等待输入、蓄力、移动、停止后处理）

### 2.2 消除冗余
- 删除两个状态机中重复的状态定义
- 明确单向依赖关系：PhaseController 委托 StateMachine，而非相互监听

### 2.3 架构对称
- 保持与 EnemyPhaseController 的设计一致性
- PhaseController 只做流程编排，不做具体执行
- 所有 Phase 都应该是必然流程，可选能力属于技能系统

### 2.4 技能系统集成
- Transition 作为技能效果，而非流程阶段
- MovingEnd 状态作为技能触发点
- 技能系统可以在 MovingEnd 执行各种"停球后效果"

---

## 三、重构方案

### 3.1 分层架构设计

```
第一层：GameFlowController（游戏总流程）
  ├─ PlayerPhase（玩家回合）
  └─ EnemyPhase（敌人回合）

第二层：PlayerPhaseController（回合流程编排）
  ├─ PhaseStart    - 回合开始，重置状态
  ├─ Playing       - 游玩中，委托给 PlayerStateMachine
  └─ PhaseEnd      - 回合结束，切换到敌人回合

第三层：PlayerStateMachine（操作状态管理）
  ├─ Idle          - 等待输入
  ├─ Charging      - 蓄力中
  ├─ Moving        - 移动中
  └─ MovingEnd     - 移动结束后（技能触发点）

第四层：技能系统（监听 MovingEnd 状态）
  ├─ TransitionSkill      - 时停+允许移动（可选技能）
  ├─ 其他停球后技能...   - Buff、治疗、召唤等
  └─ 技能参数可被配置和修改
```

### 3.2 关键改进点

#### 改进1：新增 MovingEnd 状态
- 球停止后的独立处理阶段，作为技能系统的触发点
- 技能系统监听 MovingEnd 状态，执行各种"停球后效果"
- Transition 技能在此阶段触发（时停+允许移动）
- 避免技能效果与状态切换的时序冲突

#### 改进2：PlayerPhaseController 简化
- 只保留 3 个回合级别的阶段（PhaseStart、Playing、PhaseEnd）
- 移除 Transition 阶段（改为技能效果）
- 不直接响应输入事件，委托给 PlayerStateMachine
- 与 EnemyPhaseController 保持架构对称（都是必然流程）

#### 改进3：Transition 作为技能
- Transition 从必然流程阶段改为可选技能效果
- 技能系统在 MovingEnd 状态检测并触发 Transition 技能
- Transition 参数可配置：持续时间、移动速度、时停强度等
- 支持技能升级、增强、禁用等灵活操作

#### 改进4：消除循环依赖
- PlayerPhaseController 调用 PlayerStateMachine，单向依赖
- PlayerStateMachine 通过事件通知 PlayerPhaseController 完成
- 不再相互监听状态变化

---

## 四、执行流程对比

### 4.1 旧流程（复杂）
```
PlayerStateMachine.Idle
  ↓ (蓄力开始)
PlayerStateMachine.Charging → PlayerPhaseController.Charging
  ↓ (发射)
PlayerStateMachine.Moving → PlayerPhaseController.Moving
  ↓ (球停止，时序不确定)
PlayerStateMachine.Idle → PlayerPhaseController.Transition

问题：两个状态机同步切换，职责混乱
```

### 4.2 新流程（清晰）
```
GameFlowController
  ↓
PlayerPhaseController.PhaseStart（重置）
  ↓
PlayerPhaseController.Playing（委托）
  ↓
PlayerStateMachine.Idle → Charging → Moving → MovingEnd
  ↓ (MovingEnd 触发技能系统)
技能系统检测：是否有 Transition 技能？
  ├─ 有 → 执行 Transition 效果（时停+移动）
  └─ 无 → 直接完成
  ↓ (完成通知)
PlayerPhaseController.PhaseEnd
  ↓
GameFlowController.SwitchToEnemyPhase()

优势：
- 单向依赖，流程清晰
- 必然流程与可选能力分离
- 技能系统灵活扩展
```

---

## 五、实施步骤

### 第一阶段：重构 PlayerStateMachine
1. 添加 MovingEnd 状态枚举
2. 修改球停止事件处理，切换到 MovingEnd 状态
3. 添加 OnPlayingComplete 事件，用于通知 PlayerPhaseController
4. MovingEnd 状态暂时保持空实现（为未来扩展预留）

### 第二阶段：简化 PlayerPhaseController
1. 修改阶段枚举为 PhaseStart、Playing、PhaseEnd（移除 Transition）
2. 移除对 PlayerStateMachine.OnStateChanged 的监听
3. 改为直接调用 PlayerStateMachine.StartPlaying()
4. 订阅 PlayerStateMachine.OnPlayingComplete 事件
5. 移除 Transition 阶段相关逻辑（TransitionManager 调用等）

### 第三阶段：调整事件流
1. PlayerStateMachine 不再需要"通知GameFlowController"
2. 改为通知 PlayerPhaseController
3. 确保单向依赖关系

### 第四阶段：Transition 迁移（可选，本次暂不实施）
1. 保留 TransitionManager 功能，但改为技能效果触发
2. 技能系统在 MovingEnd 状态检测 Transition 技能
3. 如果有 Transition 技能，调用 TransitionManager
4. Transition 参数通过技能配置管理

### 第五阶段：测试与验证
1. 测试完整回合流程（不含 Transition）
2. 验证状态切换的正确性
3. 确认没有破坏现有功能
4. 验证 MovingEnd 状态作为扩展点的可行性

---

## 六、优势分析

### 6.1 架构优势
- **职责单一**：每个组件职责明确，易于理解和维护
- **层次清晰**：回合层、状态层、技能层三层分离
- **对称设计**：与 EnemyPhaseController 保持一致（都是必然流程）
- **必然与可选分离**：必然流程在 PhaseController，可选能力在技能系统

### 6.2 可维护性
- **消除冗余**：不再有重复的状态定义
- **单向依赖**：依赖关系清晰，便于追踪
- **易于测试**：各层可独立测试

### 6.3 可扩展性（技能系统集成）
- **Transition 技能化**：从必然流程变为可选技能，支持配置和升级
- **MovingEnd 触发点**：统一的"停球后效果"触发点
- **参数可配置**：Transition 持续时间、移动速度、时停强度等可自由调整
- **多技能支持**：未来可以有多个"停球后技能"同时生效
- **便于平衡**：技能化后可以独立调整，不影响核心流程

---

## 七、风险评估

### 7.1 技术风险
- **中等风险**：需要重构两个核心组件
- **缓解措施**：保持现有接口兼容，分阶段实施

### 7.2 兼容性风险
- **低风险**：主要修改内部实现，外部接口基本保持
- **缓解措施**：MovingEnd 状态暂时空实现，不影响现有流程

### 7.3 测试风险
- **低风险**：流程逻辑简单，易于验证
- **缓解措施**：按阶段测试，确保每步都正确

---

## 八、后续扩展方向（第二阶段）

本次重构为以下功能预留了扩展点：

### 8.1 Transition 技能实现
- 将 TransitionManager 改为技能效果触发
- 技能配置：持续时间、移动速度、时停强度
- 支持技能升级：延长时间、增加速度等
- 技能可以被禁用、替换为其他"停球后技能"

### 8.2 多种"停球后技能"
- Transition（时停+移动）
- 攻击技能（范围伤害、召唤等）
- Buff技能（回血、护盾等）
- 控制技能（减速敌人、定身等）

### 8.3 技能组合与升级
- 支持多个"停球后技能"同时生效
- 技能参数可被其他技能/装备增强
- 技能冲突处理（互斥、覆盖、叠加等）

### 8.4 多职业系统
- 职业选择界面
- 职业配置数据
- 职业专属技能组合

---

## 九、参考资料

### 9.1 相关文档
- `Skill_System_Architecture_Plan.md` - 技能系统架构
- `Stat_Modifier_System_Architecture_Plan.md` - 属性修改系统

### 9.2 相关组件
- `EnemyPhaseController` - 敌人阶段控制器（参考架构）
- `PlayerCore` - 玩家核心组件
- `GameEventBus` - 事件系统

---

## 附录：术语表

- **Phase（阶段）**：回合级别的必然流程节点，由 PlayerPhaseController 管理
  - PhaseStart：回合开始
  - Playing：游玩中（委托给 PlayerStateMachine）
  - PhaseEnd：回合结束
  - 注意：所有 Phase 都是必然发生的，可选功能属于技能系统

- **State（状态）**：操作级别的状态节点，由 PlayerStateMachine 管理
  - Idle：等待输入
  - Charging：蓄力中
  - Moving：移动中
  - MovingEnd：移动结束后（技能触发点）

- **Transition（过渡）**：
  - 旧定义：PlayerPhaseController 的一个必然阶段
  - 新定义：可选技能效果，在 MovingEnd 状态触发
  - 效果：时停+允许移动一段时间
  - 参数可配置：持续时间、移动速度、时停强度等

- **停球后技能**：在 MovingEnd 状态触发的技能类型
  - 包括：Transition、范围攻击、Buff效果、控制效果等
  - 特点：球停止后才生效，给玩家战术选择

- **委托模式**：PlayerPhaseController 不直接处理玩家操作，而是调用 PlayerStateMachine 来执行

- **单向依赖**：PhaseController → StateMachine，StateMachine 通过事件通知 PhaseController，避免循环依赖

- **必然与可选分离**：
  - 必然流程：所有玩家都会经历的阶段（Phase）
  - 可选能力：取决于技能配置的功能（Skill）

