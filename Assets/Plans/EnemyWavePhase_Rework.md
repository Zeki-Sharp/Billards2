## 敌人双层状态机改造规划

### 1. 改造背景与现状分析
- 当前 `EnemyPhaseController` 的阶段序列固定为 `Attack → Move → Spawn → Telegraph`，所有敌人共享同一个四段循环。
- `EnemyManager.ExecutePhase` 在阶段内部负责执行敌人行为，同时在 `Telegraph` 阶段触发 `WaveSpawnTrigger.GenerateCurrentWave()` 生成新敌人。
- 新生成的敌人先进入 `telegraphingEnemies`，在下一个 `Spawn` 阶段才转入 `activeEnemies`，导致实际表现为“首回合不动、第二回合才预告、第三回合才行动”。
- 波次逻辑分散：`WaveSpawnTrigger` 负责根据配置生成敌人，但缺乏对“当前波次是否结束”的监控；敌人死亡和波次推进没有统一入口。

### 2. 目标与设计原则
- 将敌人运行拆分为两层状态机：
  - **波次状态机（Wave Layer）**：仅负责波次生成、结束判断、进入下一波。
  - **敌人状态机（Enemy Layer）**：仅包含 `Telegraph → Attack → Move` 循环，不再耦合生成逻辑。
- 保证首波和后续波次的行为一致：波次开始时生成敌人 → 立即执行一次预告 → 下一回合起进入常规攻防循环。
- 遵循最小改动原则：充分复用现有 `EnemyManager`、`EnemySpawner`、`WaveSpawnTrigger`，通过重新划分职责来实现行为一致性。

### 3. 波次层（Wave Layer）规划
**新增/改造组件建议**
- 引入 `EnemyWaveController`（可继承 `SingletonManager`，执行顺序高于 `EnemyPhaseController`）：
  - 持有对 `WaveSpawnTrigger`、`EnemyManager`、`EnemyPhaseController` 的引用。
  - 订阅敌人死亡事件或统一的 `EnemyManager.OnEnemyUnregistered` 回调，实时维护当前波次存活数量。
  - 在波次开始时调用 `WaveSpawnTrigger.GenerateCurrentWave()`，并向 `EnemyPhaseController` 发出“重置敌人回合”的指令（起始阶段为 `Telegraph`）。
  - 监听波次结束：当存活数为 0 且没有 pending 的 `telegraphingEnemies` 时，触发短暂过渡（例如等待当前阶段结束），再推进到下一波。
- `WaveSpawnTrigger` 改造：
  - 移除在 `Telegraph` 阶段被动触发的依赖，提供 `GenerateNextWave()` 主动接口；内部继续使用现有的 `WaveConfigProvider`、`EnemySpawner`。
  - 保留初始波次生成逻辑，但由 `EnemyWaveController` 显式调用，而不是 `Start()` 自动执行。

**事件流**
1. 游戏或关卡启动 → `EnemyWaveController` 调用 `GenerateNextWave()`。
2. `WaveSpawnTrigger` 使用配置生成敌人，返回生成列表或数量；`EnemyManager.RegisterTelegraphingEnemy` 记录所有新敌人。
3. `EnemyWaveController` 调用 `EnemyPhaseController.ResetAndStartFromTelegraph()`，驱动敌人执行首次预告。
4. 敌人进入 `Telegraph → Attack → Move` 循环，循环在 `EnemyPhaseController` 内部保持运行。
5. 每当敌人死亡，`EnemyWaveController` 更新当前波次计数；当计数为 0 时，触发下一波生成（若配置还有剩余）。
6. 若所有波次完成，向 `GameFlowController`/`GameSession` 发布“敌人波次结束”事件，进入后续游戏流程（胜利结算或等待玩家操作）。

**现有脚本职责映射（参考代码）**
- `EnemyManager`
  - `ExecutePhase`：根据 `EnemyPhase` 调度敌人行为，当前含生成逻辑。
  - `RegisterTelegraphingEnemy` / `TransferToActive`：维护敌人列表。
  - `OnEnemyMoveComplete`：负责移动阶段完成判定。
- `EnemyPhaseController`
  - `enemyPhaseSequence`：定义四阶段循环。
  - `StartEnemyPhase` / `ExecuteNextEnemyPhase`：推进阶段并广播事件。
- `WaveSpawnTrigger`
  - `GenerateInitialEnemies` / `GenerateCurrentWave`：读取配置并生成敌人。
  - `AdvanceToNextWave`：推进 `WaveConfigProvider` 内部索引。
- `WaveConfigProvider`
  - `GetSpawnData` / `ShouldSpawn` / `GetSpawnCount`：提供当前波次数据。
  - `AdvanceToNextWave`：切换波次配置。

### 4. 敌人层（Enemy Layer）规划
- 调整 `EnemyPhaseController` 的阶段序列为 `Telegraph → Attack → Move`：
  - 提供新的入口方法 `StartEnemyPhaseFromTelegraph()`，在波次生成后立即调用。
  - 每个阶段完成后仍通过 `EnemyManager.OnPhaseCanSwitch` 推进下一阶段。
- `EnemyManager` 改造重点：
  - 移除阶段切换中的生成逻辑（`ExecuteTelegraphPhase` 不再调用 `GenerateCurrentWave`）。
  - 保留 `telegraphingEnemies`/`activeEnemies` 两个列表，但需要调整转换时机：
    - 新生成的敌人加入 `telegraphingEnemies` 后，`Telegraph` 阶段执行 `StartPhase(EnemyPhase.Telegraph)`。
    - `Telegraph` 结束即可将 `telegraphingEnemies` 转移到 `activeEnemies`，不再等待额外的 `Spawn` 阶段。
  - 改造移动计数逻辑，确保空敌人列表时照常推进。
- `Enemy` / `EnemyBehavior`：
  - 确认 `StartPhase(EnemyPhase.Telegraph)` 在敌人第一次出现时也能正确展示预告。
  - 添加必要的状态标记 `hasTelegraphed`（如现有逻辑需要）以决定攻击阶段是否执行。

**改造后接口草图**
- `EnemyWaveController`
  - `InitializeWaveLoop()`：在战斗开始或关卡载入后调用，订阅必要事件。
  - `StartNextWave()`：
    1. 通过 `WaveSpawnTrigger.GenerateNextWave(out List<Enemy> spawned)` 生成敌人。
    2. 将生成数量缓存到 `pendingEnemies`，等待 `EnemyManager` 注册完成。
    3. 调用 `EnemyPhaseController.StartEnemyPhaseFromTelegraph()`。
  - `HandleEnemyRegistered(Enemy enemy)` / `HandleEnemyDead(Enemy enemy)`：更新活跃计数，判定波次结束。
  - `CompleteAllWaves()`：向 `GameFlowController` 发布总战斗结束事件。
- `EnemyPhaseController`
  - `StartEnemyPhaseFromTelegraph()`：重置阶段索引为 `Telegraph` 并立即执行。
  - `ForceRestartLoop()`：供波次结束或异常恢复时重置状态。
- `WaveSpawnTrigger`
  - `GenerateNextWave(out int enemyCount)`：封装原 `GenerateCurrentWave`，返回实际生成数量。
  - `HasMoreWaves()`：使用 `WaveConfigProvider.ShouldSpawn()` 判断是否还有波次。

### 5. 配置与数据流适配
- `WaveConfigProvider`：保持现有接口不变，由 `EnemyWaveController` 调用；需要新增查询接口，返回“当前波次的敌人总数”供波次控制器预先记录。
- `GameFlowState` / 事件系统：
  - 波次开始与结束需要发布新的事件，供 UI、音效、剧情响应。
  - 如果战斗流程中存在“玩家回合 ↔ 敌人回合”控制，需要评估波次生成与玩家回合切换之间的节奏，避免生成时阻塞主流程。

### 6. 逐步实施计划
1. **梳理与文档化**：详细列出当前敌人阶段、事件与数据结构（完成本规划后可作为参考）。
2. **引入波次控制层**：
   - 创建 `EnemyWaveController`，在 `Awake/Start` 中绑定 `WaveSpawnTrigger`、`EnemyManager`、`EnemyPhaseController`。
   - 订阅 `EnemyManager` 的注册/注销事件（若缺失需补充 `OnEnemyRegistered`、`OnEnemyUnregistered`）。
   - 为后续步骤实现基础统计（当前波次敌人数、剩余波次）。
3. **抽离生成逻辑**：
   - 将 `EnemyManager.ExecuteTelegraphPhase` 中触发生成的部分删除或改为仅广播事件。
   - 在 `EnemyWaveController.StartNextWave()` 中调用 `WaveSpawnTrigger.GenerateNextWave()`；确认 `EnemySpawner` 会自动把敌人加入 `telegraphingEnemies`。
4. **调整敌人阶段顺序**：
   - 修改 `EnemyPhaseController` 的 `enemyPhaseSequence` 为 `Telegraph → Attack → Move`，并实现 `StartEnemyPhaseFromTelegraph()`。
   - 在 `EnemyManager.ExecuteTelegraphPhase` 中，结束时调用 `TransferToActive`，确保下一阶段就是已激活敌人。
5. **统一首回合行为**：
   - 在 `Enemy` 中增加 `hasTelegraphed` 标记（或复用现有状态），攻击阶段根据该标记跳过/执行。
   - 验证首次 `Telegraph` 带来的 UI 效果是否正确显示。
6. **波次结束判定**：
   - 当 `EnemyWaveController` 发现当前波次存活敌人数为 0，若 `WaveSpawnTrigger.HasMoreWaves()` 返回 true，则等待当前阶段结束再执行 `StartNextWave()`。
   - 若没有下一波，向 `GameFlowController` 抛出 `OnEnemyWavesCleared` 事件，进入胜利流程或后续系统。
7. **清理与优化**：
   - 删除或弃用 `EnemyPhase.Spawn` 枚举值及相关分支，更新所有引用。
   - 检查 `Inspector` 序列化字段（例如阶段间隔、调试开关）是否仍适用。
   - 同步更新脚本文档注释、调试日志内容。
8. **集成测试**：
   - **流程测试**：单波次、双/多波次、无波次（空关卡）。
   - **状态测试**：敌人被控制/跳过行动、敌人在 `Telegraph` 后立即被击杀、移动阶段未完成等异常场景。
   - **性能测试**：大量敌人同时生成、快速连续波次切换。

### 7. 风险与待确认事项
- `EnemyManager` 与 `Enemy` 之间的事件耦合较多（如移动完成回调）；改造时需留意避免出现阶段死锁。
- `WaveSpawnTrigger` 目前在 `Start()` 中调用初始生成；改造后要保证不出现重复生成或遗漏初始波次。
- 若有BOSS或特殊敌人需要自定义阶段，需评估新结构是否能扩展多态行为。
- UI 与音效可能依赖原有阶段事件（例如 `OnPhaseStart` 日志），修改顺序后要同步更新相关监听。

### 8. 产出物与后续文档
- 本规划供后续编写详细任务拆解（可在 `TodoList.md` 更新具体实施步骤）。
- 实施过程中需要同步更新 `EnemyPhaseController`、`EnemyManager`、`WaveSpawnTrigger`、`EnemySpawner`、`GameFlowController` 等脚本的文档注释，确保维护人员理解新的双层状态机结构。
- 在实现完成后补充两份配套文档：
  - 《EnemyWaveController 使用说明》：说明序列化字段、事件、与外部系统的交互方式。
  - 《战斗流程回放脚本》：记录用于自动化验证的脚本或控制台命令，确保波次循环可回归测试。

### 9. 测试与验收清单
- **功能完整性**：波次生成、阶段切换、敌人攻击/移动/预告三阶段在任意波次表现一致。
- **状态恢复**：在战斗暂停/继续、关卡重开后，`EnemyWaveController` 能正确重置并重新开始循环。
- **事件正确性**：调试输出与事件总线（`GameEventBus`）的广播内容与预期一致，可被 UI、音效等系统消费。
- **异常处理**：敌人生成失败、波次配置为空、EnemySpawner 找不到数据等异常场景能输出明确日志并不中断流程。

