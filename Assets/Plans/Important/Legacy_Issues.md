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

### 8. 几何碰撞偶发穿墙（球体穿过墙体） ⭐⭐⭐

**问题**：在当前几何物理 + 3D 特效流程下，偶尔会出现球体在高速或特定角度下“突然穿过墙体”的情况（未被几何碰撞捕获或多次反弹后偏移出有效范围），属于影响体验的恶性 bug。  
**方案**：① 系统性梳理 `BallPhysics` 中几何投射/反射逻辑，检查剩余时间步长、浮点容差和多次碰撞累积误差 ② 为墙体增加安全冗余（扩展碰撞 AABB 或在反弹后进行一次“回投影”校正）③ 加入防穿墙断言/日志，收集复现数据。  
**影响**：直接影响核心玩法的可靠性，玩家会感知到“球突然消失/穿出边界”，优先级高。  
**时机**：在 3D 碰撞事件和特效链路稳定后尽快处理（3D 升级 Phase B 收尾前）。  

---

### 9. 静态物体几何“材质”缺失，行为完全由球体 BallData 决定 ⭐⭐

**问题**：当前几何模拟中，静态物体（墙/障碍物）没有自己的几何“材质”配置，所有反弹/阻尼行为完全由 BallData 决定，无法表达“不同墙面/地面行为不同”的需求。  
**方案**：① 设计 GeometrySurfaceData（bounce/friction 等），通过 StaticHitReceiver 或单独组件挂在静态物体上 ② BallPhysics 在 HandleGeometryWallCollision 中读取 surfaceData 作为 BallData 的乘数 ③ 后续可扩展到伤害规则/音效/特效选择。  
**影响**：目前功能上可以接受，但会限制 3D 场景中“冰面/弹簧墙/软垫”等丰富行为设计，属于中优先级扩展点。  
**时机**：墙体基础特效和静态受击链路稳定后再做（3D 升级 Phase C 或之后）。  

---

### 10. 墙体受击系统存在临时参数与废弃 2D 路径未收敛 ⭐⭐

**问题**：WallManager/WallCollisionDetector/EffectManager 里仍保留多套 2D 方案和临时字段（如 wallBeHitPlayer、OnWallHit 2D 重载、旧的 BeHit 注册逻辑），与新引入的 StaticHitReceiver 共存，容易造成理解混乱和误用。  
**方案**：① 明确静态受击的唯一入口（推荐 StaticHitReceiver + OnCollisionEvent）② 将 WallManager 撞墙逻辑瘦身为“防抖+统计/调试” ③ 标记并移除不再使用的 2D 路径与临时参数，更新文档。  
**影响**：主要影响维护成本和新同事理解成本，短期功能正常但长期会积累技术债。  
**时机**：在 StaticHitReceiver 应用到主要静态物体后统一清理（3D 升级 Phase B 收尾阶段）。  

---

### 11. 敌人与敌人 / 角色之间碰撞的受击规则不一致 ⭐⭐

**问题**：DamageSystem 中敌人-敌人、玩家-玩家、玩家-敌人之间的碰撞规则不统一，部分走旧的 2D Trigger/Collider 流程，部分走新 CollisionEvent 流程，导致某些组合下受击/特效触发逻辑“看起来正常但语义不清晰”。  
**方案**：① 全面梳理 DamageProfile 和规则表，明确每种组合（Player→Enemy、Enemy→Player、Enemy↔Enemy 等）的 Source/Target 定义 ② 统一改为基于 CollisionEvent 的规则驱动，废弃旧 OnCollision/OnTrigger 入口 ③ 补充碰撞单元测试或最小验证场景。  
**影响**：可能在以后扩展敌人种类或加入友军单位时暴露出边缘 bug，属于逻辑一致性和可扩展性问题。  
**时机**：3D 物理事件链路确认稳定后，集中一轮整理 DamageSystem 规则（建议在 3D 升级 Phase C 之前处理）。  

---

## 📝 备注

- 所有遗留问题都不影响当前功能
- 问题解决后立即从此文档删除
- 定期评估优先级，避免技术债务积累

---

**最后更新**：2025年11月

