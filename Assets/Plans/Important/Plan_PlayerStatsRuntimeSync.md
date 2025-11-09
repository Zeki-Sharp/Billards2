# PlayerStats 实时属性同步改造计划

## 1. 背景与目标
- 当前技能系统会在关卡加载后立即执行属性修改，但 `PlayerStats.Initialize()` 会重新注册基础值，导致实时修改被覆盖（如攻击范围技能）。
- 目标是建立“基础配置 → 实时属性”的单向同步流程，所有数值读取均来自实时层，避免回退到 `PlayerData`。

## 2. 范围
- 玩家 `PlayerStats` 初始化流程与 GameSession 数据恢复。
- 触及的主要系统：`Player`, `PlayerStats`, `DamageSystem`, `PlayerAttackManager`, 技能效果（StatModifierEffect）。
- 敌人或其他实体暂不纳入本次改造。

## 3. 现状问题
1. `PlayerStats.Initialize()` 在技能执行后重新写入基础值，覆盖修改器结果。
2. `DamageSystem`、`PlayerAttackManager` 等仍带有回退逻辑（读取 `PlayerData`），逻辑冗余。
3. 玩家重新生成（复活、场景切换）时，需要在正确时机重新执行被动技能。

## 4. 改造方案
### 4.1 初始化序列调整
1. `Player` 启动时按顺序：`InitializeComponents` → `DistributePlayerData` → `SetupComponentReferences` → `PlayerStats.Initialize()`。
2. `PlayerStats.Initialize()` 内部完成：
   - 把 `PlayerData` 中的基础值写入 `RuntimeStatsManager`。
   - （可选）从 `GameSession` 恢复实时修改器快照。
3. 初始化完成后再调用 `SkillManager.NotifyCharacterSpawned()`，确保技能作用在最新的 `PlayerStats` 上。

### 4.2 数值读取统一
1. 梳理所有读取 `PlayerData.attackPower/areaRadius` 的位置，改为调用 `PlayerStats`。
2. 对外暴露只读接口（如 `PlayerStats.GetFinalStat()`），禁止直接访问 `PlayerData`。
3. `DamageSystem`、`PlayerAttackManager` 保留“缺少 PlayerStats 时使用基础值”的兜底，但增加日志以便排查。

### 4.3 技能触发策略
1. `SkillManager.TryExecuteImmediateSkill()` 在执行前调用 `effect.SetCanExecute(true)`，确保被动技能可再次触发。
2. `StatModifierEffect` 在检测到目标切换时，先移除旧句柄，再绑定新的 `PlayerStats`。
3. 如果未来增加敌人属性系统，复用同一流程。

## 5. 工作计划
| 阶段 | 任务 | 备注 |
| --- | --- | --- |
| Phase 1 | 整理初始化顺序，确认 `PlayerStats` 写入时机 | 重点验证多角色场景 |
| Phase 2 | 梳理所有属性读取点，统一走 `PlayerStats` | 引入临时日志辅助验证 |
| Phase 3 | 调整被动技能执行策略，补充自动重触发逻辑 | 包括场景切换 / 复活场景 |
| Phase 4 | 回归测试：技能、范围伤害、战斗表现 | 保证现有数值无回退 |
| Phase 5 | 文档与技术债更新，移除临时日志 | 维护知识库 |

## 6. 风险与缓解
| 风险 | 影响 | 缓解措施 |
| --- | --- | --- |
| 初始化顺序出错 | 角色组件拿到未就绪的 `PlayerStats` | 逐层增加断言与调试日志 |
| 敌人/其他系统未同步 | 旧逻辑依赖 `PlayerData` 仍产生回退 | 保留兜底逻辑并记录警告 |
| 技能复用流程改变 | 未重新执行导致技能失效 | 为 `NotifyCharacterSpawned` 编写单元测试/集成测试 |

## 7. 验证清单
- [ ] 技能选择后，日志显示 `FinalAreaRadius` 等属性值正确更新。
- [ ] 进入关卡后，`DamageSystem` 使用新值进行范围判定。
- [ ] 角色复活/场景切换后被动技能仍然生效。
- [ ] 回退日志（读取 `PlayerData`）仅在确实缺少 `PlayerStats` 时出现。

## 8. 输出与文档
- 调整完成后更新 `Legacy_Issues.md` 状态，并在此计划文档记录最终结论。
- 对外文档（如 README/开发者指南）补充实时属性流程说明。

