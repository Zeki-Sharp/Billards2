# 遗留问题清单

> 记录当前暂不处理、但未来需要清理的技术债务

## 📋 文档规范

### 问题记录格式
每个问题应包含以下内容，保持简洁（总字数 ≤ 300字）：

```markdown
### N. 问题标题（简短） ⭐⭐⭐

**问题**：1-2句话说明问题  
**方案**：列出 2-3 个可能的解决方案  
**影响**：影响范围 + 紧迫性评估  
**时机**：建议清理的时间点
```

### 优先级定义
- **⭐⭐⭐** - 高优先级：影响核心功能或开发效率，应尽快处理
- **⭐⭐** - 中优先级：影响代码质量或可维护性，可择时处理
- **⭐** - 低优先级：小问题或优化项，随时可清理

### 管理规则
1. **新增问题**：找到正确的优先级位置插入
2. **问题解决**：立即从文档删除，不保留历史记录
3. **优先级调整**：根据项目进展动态调整
4. **保持精简**：每个问题描述控制在 10 行以内

### ⚠️ 新增条目检查清单
添加新问题时，必须按以下步骤操作：
- [ ] 1. 评估优先级（⭐⭐⭐ / ⭐⭐ / ⭐）
- [ ] 2. 找到同优先级组的最后一个条目
- [ ] 3. 在其后插入新条目
- [ ] 4. 更新编号（确保 1, 2, 3... 连续）
- [ ] 5. 更新其他文档中的引用编号

---

## 🔧 待清理问题（按优先级排序）

### 1. 多态配置文件夹整理 ⭐⭐

**问题**：23个多态配置文件平铺在 `Configs/Polymorphic/`，层级过深且不易查找。  
**方案**：上移 `Polymorphic` 到 `SkillSystem/`，内部创建5个子文件夹按类型分类（Triggers、Conditions、Effects、ResetConditions、RemovalConditions）。  
**影响**：仅改变文件组织，不影响已配置数据。优先级中等。  
**时机**：Phase 3.3 完成后，随时可整理。

---

### 2. ConditionConfig 容器类优化 ⭐⭐

**问题**：`ConditionConfig` 实为多条件容器，但命名易误解为单一条件配置；架构不统一（只有 Condition 有容器）。  
**方案**：①改名为 `CompositeConditionConfig` ②改为单一条件删除容器 ③评估是否真正需要多条件组合。  
**影响**：命名混淆，但功能正常。优先级中等。  
**时机**：等后续出现多条件技能需求时回过头再来改

---

### 3. 配置层命名规范化 ⭐⭐

**问题**：配置类命名不符合 GC2 规范（应为 `PlayerClass` 而非 `PlayerData`）。  
**方案**：使用 Unity F2 重命名 `PlayerData` → `PlayerClass`、`EnemyData` → `EnemyClass`、`SkillConfig` → `SkillClass`。  
**影响**：120 处引用，Unity 自动更新。优先级低，现有命名可接受。  
**时机**：Phase 4 或更晚，不紧急。

---


---

### 4. 敌人架构组件查找优化 ⭐⭐

**问题**：EnemyBehavior 从根物体移到 enemyItem 后，多处代码使用 GetComponentInChildren/Parent 查找，造成性能开销和逻辑复杂度。  
**方案**：①Enemy.cs 已优化（enemyItem.GetComponent） ②AttackRange.cs 需通过 Enemy.enemyItem 间接查找 ③状态系统保持容错查找（支持灵活结构） ④WeakPointManager/DamageTextManager 等外部系统优化。  
**影响**：影响8个文件，其中 AttackRange 查找失败会导致攻击范围不转向。部分已修复（Enemy.cs、EnemyBehavior.cs），剩余项为性能优化。  
**时机**：逐步优化，AttackRange 问题优先级高，状态系统可保持现状作为容错。

---

### 5. PlayerStats 实时属性同步方案落地 ⭐⭐

**问题**：PlayerData → PlayerStats → DamageSystem 的数值链路存在覆盖和回退逻辑，技能执行顺序稍有差异就会失效。  
**方案**：参见《PlayerStats 实时属性同步改造计划》，按照计划统一初始化流程与读取口径。  
**影响**：影响 Player/PlayerStats/SkillManager/DamageSystem 等核心模块，需完整回归。  
**时机**：Phase 4 前完成，确保后续技能开发基于统一的实时属性体系。

---

### 6. 字符串引用更新 ⭐

**问题**：日志和注释中仍使用旧名称 `PlayerStatsManagerV2`（已改为 `PlayerStats`）。  
**方案**：全局搜索替换 `PlayerStatsManagerV2` → `PlayerStats`。  
**影响**：9 个文件约 44 处，仅影响可读性，不影响功能。  
**时机**：随时可清理，优先级最低。

---

### 7. 关卡场景名称硬编码 & SceneTransitionManager 未生效 ⭐⭐

**问题**：当前战斗关卡场景通过 `MapPlayerTracker` 采用硬编码方式按层级加载场景名（`Layer 0 → "Level1"` 等），而角色选择场景中的 `SceneTransitionManager.level1SceneName` 配置不会参与实际战斗场景跳转，导致文档/配置与真实流程不一致，也增加了 3D 版关卡（如 `Level1_3D`）接入难度。  
**方案**：① 将 `MapPlayerTracker` 的场景名称改为从统一配置（例如 LevelManager/ScriptableObject 或 `SceneTransitionManager`）读取，而不是字符串拼接 ② 或者在 `SceneTransitionManager` 中提供“从地图节点加载战斗场景”的统一入口，由地图系统调用 ③ 长期目标是消除所有 `"LevelX"` 字符串硬编码，只保留一个权威场景列表。  
**影响**：影响地图→战斗场景的跳转逻辑，以及后续新增/重命名战斗场景时的维护成本；对运行时功能无直接 bug，但容易产生“改了配置不生效”的混淆。  
**时机**：在 3D 关卡稳定后、开始整理关卡/地图系统时统一清理（建议在 3D 升级 Phase B～C 之间处理）。

---

## 📝 备注

- 所有遗留问题都不影响当前功能
- 问题解决后立即从此文档删除
- 定期评估优先级，避免技术债务积累

---

**最后更新**：2025年11月

