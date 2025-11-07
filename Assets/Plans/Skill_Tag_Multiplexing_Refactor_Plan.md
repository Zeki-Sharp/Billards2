# 技能多标签与角色专属副本改造计划

## 🎯 目标
- 支持在技能配置中为同一技能选择多个角色标签（含“通用”全选语义）。
- 技能池以“技能 × 角色”副本的形式存在，简化选项分配和 UI 表达。
- 保持现有技能逻辑（触发器、效果、升级）不变，仅调整数据结构与生成流程。

## 🔍 现状概览
- `SkillInfo.tag` 为单一字符串，通过 Odin 下拉选择 `default/common/角色名`。
- `SkillDatabase.GetSkillsForCharacter` 基于 `skill.skillTag == characterName` 或 `common` 返回同一份 `SkillConfig` 引用。
- `SkillSelectionManager` 在生成选项时，采用 `SkillSelectionOption` 记录目标角色，但底层仍共享 `SkillConfig`，导致 UI 和逻辑需额外区分。
- `SkillButtonPrefab` 通过索引再向 `SkillSelectionManager` 查询 `SkillSelectionOption`，存在“同一技能被多个角色共享时索引匹配不到”的隐患。

## 🧩 设计思路
1. **数据层改造**：将 `SkillInfo.tag` 扩展为 `List<string> allowedTags`，在 Inspector 提供“全选/清空”按钮批量维护标签。保留旧字段用于迁移，隐藏或标记为弃用。
2. **技能库扩展**：在 `SkillDatabase` 中生成 `SkillVariant { SkillConfig baseConfig; string tag; }` 列表/映射。可选缓存结构：`Dictionary<string, List<SkillVariant>>`（key 为角色 tag）。
3. **技能池生成**：`SkillSelectionManager` 以 `SkillVariant` 为基础创建 `SkillSelectionOption`，确保选项与角色绑定唯一；`currentSelection` 切换为保存 variant 或在 option 中携带即可。
4. **事件与 UI**：`GameEventBus.PublishSkillSelectionStarted` 增补选项索引或改为发布 DTO；`SkillSelectionUI` 直接使用 `SkillSelectionOption` 显示技能名、角色信息，按钮点击传递索引，避免再次索引查找差错。
5. **全选/默认行为**：不再依赖 `common` 字符串；策划点击“全选”按钮后 `allowedTags` 自动包含全部角色标签。`default`（如仍需保留）可视需求映射为“非绑定技能”或保留原意。
6. **兼容性处理**：通过 `OnValidate` 将旧 tag 写入 `allowedTags`；手动检查并重新分配标签即可，无需额外迁移脚本。

## 🛠️ 实施步骤

### Phase 1 — 数据结构与编辑器（优先级：高）
1. 在 `SkillInfo` 中新增 `List<string> allowedTags`，Odin 属性：多选、唯一、排序。
2. 在 Inspector 添加“全选所有角色标签”“清空标签”按钮；点击时自动填充/清理 `allowedTags`，去重排序。
3. 将原 `tag` 字段标记为 `[Obsolete]` 并隐藏 Inspector；`OnValidate()` 发现旧字段有值而新列表为空时迁移数据并清空旧字段。
4. `GetEffectiveTags()` 直接返回 `allowedTags` 副本；不再处理 `common`。

### Phase 2 — 技能数据库（优先级：高）
1. 定义 `SkillVariant` 结构，包含 `SkillConfig baseConfig`, `string tag`,（可选）`string displayName`。
2. 在 `SkillDatabase` 内生成/缓存 variant 列表：
   - 预先收集角色 tag（例如从 `PlayerData` 或 `CharacterSelectionData`）；
   - 遍历所有技能，调用 `GetEffectiveTags()`，为每个 tag 创建 variant。
3. 更新查询接口：
   - `GetSkillsForCharacter(string characterID)` 返回 `List<SkillVariant>`；
   - 提供 `GetVariants()` / `GetVariantsByTag()` 等辅助方法。

### Phase 3 — 技能选择流程（优先级：中）
1. 调整 `SkillSelectionOption` 结构：新增字段 `SkillVariant variant` 或 `SkillConfig baseConfig` + `string variantTag`; `skillConfig` 字段保留指向 `baseConfig` 以复用原逻辑。
2. 修改 `GenerateRandomSkillSelection()`：使用 variant 列表生成 `SkillSelectionOption`，`currentSelection` 改存选项或 variant。
3. 更新 `GameEventBus.PublishSkillSelectionStarted` 与 `SkillSelectionUI`：
   - 可增加新的事件参数（例如 `List<SkillSelectionOption>`），或在 `SkillSelectionManager` 内保留映射表以供 UI 查索引；
   - UI 在生成按钮时直接使用 `SkillSelectionOption`，展示“技能名 + 角色/等级”，并按索引调用 `OnSkillSelected`。
4. `OnSkillSelected` 根据 `option.variantTag` 直接分配技能，避免再次搜索。

### Phase 4 — 测试与验证（优先级：中）
1. 验证“多标签”技能是否在各角色中生成独立选项（如 `撞击增伤_A`、`撞击增伤_B`）。
2. 验证 `common` 情况：所有存活角色均能获得副本，UI 显示与分配正确。
3. 检查技能升级：升级选项是否按角色正确定位，升级后的技能仍保持对应副本。
4. 确认跨存档或旧数据：旧技能是否正确迁移，多标签是否可保存/读取。

## ⚠️ 风险与对策
- **存档兼容性**：旧存档可能引用 `SkillConfig` + 单标签；需要测试或提供迁移脚本。
- **性能/缓存**：若技能数量较多，应缓存 variant，避免每次查询都重新展开。
- **UI 兼容性**：事件签名或 UI 组件改动需同步更新所有订阅者，避免漏改导致空引用。
- **技能名称冲突**：若在 UI 上拼接“技能名 + 角色名”，需确保显示和实际 ID 区分清晰，以防日志或调试混淆。

## ✅ 验收标准
- 技能配置支持多标签；“通用”角色标签自动覆盖全部角色。
- 技能池中同一技能可针对多个角色生成独立选项，且 UI/逻辑运作正常。
- 选中技能时能准确落到目标角色，升级流程不受影响。
- 旧技能数据成功迁移，多标签配置在运行时无错误日志。

