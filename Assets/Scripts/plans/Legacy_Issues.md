# 遗留问题清单

> 记录当前暂不处理、但未来需要清理的技术债务

---

## 🔧 待清理问题（按优先级排序）

### 1. ConditionConfig 容器类问题 ⭐⭐

**问题描述**：
- `ConditionConfig.cs` 是一个复合条件容器，支持多条件 AND/OR 组合
- 命名错误：实际是 `CompositeConditionConfig`，但叫 `ConditionConfig` 容易误解
- 架构不统一：为什么只有 Condition 需要容器，而 Trigger/Effect 不需要？
- 可能过度设计：多条件组合功能是否真的需要？

**当前使用**：
- `SkillLevelConfig.conditionConfig` 字段使用此类
- 包含 `logicType`（AND/OR）和 `List<ConditionBase> conditions`

**潜在解决方案**：
1. **方案A**：改名为 `CompositeConditionConfig`，作为一个多态 Condition 类型
2. **方案B**：直接改为单一条件 `ConditionBase conditionConfig`，删除容器
3. **方案C**：统一设计，为所有系统添加复合配置容器（过度设计）

**清理时机**：
- Phase 3.3 多态化完成后评估
- 如果从未使用多条件组合，删除容器改为单一条件
- 如果确实需要，改名并规范化

**命名冲突问题**：
- 无法使用 `CompositeCondition` 命名（与运行时类 `CompositeCondition : ICondition` 冲突）
- 需要保留 `Config` 后缀：`CompositeConditionConfig`

---

### 2. 向后兼容桥接属性清理 ⭐⭐⭐

**问题描述**：
- Phase 3.1 完成后，保留了大量向后兼容属性
- 这些属性是临时桥接，未来应删除

**涉及文件**：
- `PlayerData.cs` - playerName/playerIcon/characterDescription
- `EnemyData.cs` - enemyName/enemyIcon
- `SkillConfig.cs` - skillName/description/skillTag

**影响范围**：
- 约 150+ 处代码调用旧属性
- 需要全局替换为 `.info.name` 访问方式

**清理时机**：
- Phase 4 或后续统一清理
- 当前不影响功能，优先级中等

**清理步骤**：
1. 全局搜索 `playerData.playerName`，替换为 `playerData.info.name`
2. 全局搜索 `enemyData.enemyName`，替换为 `enemyData.info.name`
3. 全局搜索 `skillConfig.skillName`，替换为 `skillConfig.info.name`
4. 删除向后兼容属性（3 个文件）
5. 测试验证

---

### 2. 配置层命名规范化 ⭐⭐

**问题描述**：
- 当前配置层命名不符合 GC2 规范
- GC2 建议：配置用 `XxxClass`，运行时用 `XxxStats`/`XxxBehavior`

**当前命名 vs GC2 规范**：
| 当前命名 | GC2 规范 | 文件类型 | 影响范围 |
|---------|---------|---------|---------|
| PlayerData | PlayerClass | ScriptableObject | ~50 处 |
| EnemyData | EnemyClass | ScriptableObject | ~30 处 |
| SkillConfig | SkillClass | ScriptableObject | ~40 处 |

**已完成的改名**：
- ✅ PlayerStatsManagerV2 → PlayerStats
- ✅ PlayerCore → PlayerBehavior

**清理时机**：
- Phase 3.2 完成后
- Phase 4 或更后面
- 优先级低，现有命名也可接受

**改名方法**：
- 使用 Unity Rename 工具（F2）
- Unity 会自动更新所有引用
- 安全且快速

**改名步骤**：
1. 关闭 Unity，提交 Git
2. 打开 Unity
3. Project 窗口 → PlayerData.cs → F2 → PlayerClass
4. Project 窗口 → EnemyData.cs → F2 → EnemyClass
5. Project 窗口 → SkillConfig.cs → F2 → SkillClass
6. 等待 Unity 编译
7. 测试游戏
8. 提交 Git

---

### 3. 字符串引用更新（可选）⭐

**问题描述**：
- PlayerStatsManagerV2 → PlayerStats 改名后
- 代码中的字符串引用（日志、注释）仍使用旧名

**涉及文件**（9 个）：
1. `PlayerStats.cs` - Debug 日志（约 12 处）
2. `PlayerBehavior.cs` - 错误提示（1 处）
3. `PlayerAttackManager.cs` - 注释和日志（5 处）
4. `StatModifierEffect.cs` - 注释和日志（2 处）
5. `AttributeRatioFloat.cs` - 日志（1 处）
6. `StatBasedFloat.cs` - 日志（1 处）
7. `README_ThreeLayerSystem.md` - 示例代码（1 处）
8. `Modifier_Migration_Guide.md` - 示例代码（2 处）
9. `Phase2_1_Completion_Summary.md` - 文档说明（约 20 处）

**清理时机**：
- 随时可以清理，不影响功能
- 优先级最低
- 可以在日常开发中顺便修改

**清理方法**：
- 全局搜索 `PlayerStatsManagerV2`
- 替换为 `PlayerStats`
- 或逐个文件手动修改

---

## 📊 优先级说明

| 优先级 | 说明 | 建议清理时机 |
|--------|------|-------------|
| ⭐⭐⭐ | 高优先级 | Phase 4 统一清理 |
| ⭐⭐ | 中优先级 | Phase 3.2 完成后考虑 |
| ⭐ | 低优先级 | 日常开发中顺便清理 |

---

## ✅ 清理进度

- [ ] 向后兼容桥接属性清理（150+ 处）
- [ ] 配置层命名规范化（3 个文件）
- [ ] 字符串引用更新（9 个文件）

---

## 📝 备注

- 所有遗留问题都不影响当前功能
- 优先完成核心功能（Phase 3.2 - 敌人系统重构）
- 技术债务应定期清理，避免积累过多

---

**创建日期**：2024年12月  
**最后更新**：2024年12月  
**维护者**：项目开发团队

