# 阶段4：技能系统多角色适配 - 详细实施计划

> **创建日期**：2024-11-03  
> **优先级**：⭐⭐⭐  
> **预计时间**：3-4天

---

## 📋 目标概述

将现有的**全局技能系统**改造为**角色专属技能系统**，实现：
- 每个技能归属于特定角色
- 技能触发器只响应归属角色的事件
- 技能效果只作用于归属角色
- 技能选择时预分配到具体角色

---

## 🏗️ 当前架构分析

### 现有技能系统结构
```
SkillManager (单例)
  ├── activeSkills: List<SkillConfig>           // 全局技能列表
  ├── skillInstances: Dictionary<string, SkillInstance>  // 技能实例
  └── AddSkill(SkillConfig)                      // 添加技能到全局

SkillInstance
  ├── triggers: List<BaseSkillTrigger>           // 触发器（无角色过滤）
  ├── effects: List<BaseSkillEffect>             // 效果（无角色指定）
  └── Execute()                                  // 执行效果

BaseSkillTrigger
  └── CheckCondition() → true/false              // 检查条件（无角色ID）

BaseSkillEffect
  └── Execute()                                  // 执行效果（无角色ID）
```

### 问题分析
1. ❌ **技能无归属**：所有技能全局生效，无法区分归属角色
2. ❌ **触发器无过滤**：所有角色的事件都会触发技能
3. ❌ **效果无目标**：无法指定效果作用于哪个角色

---

## 🎯 改造目标架构

### 目标技能系统结构
```
SkillManager (单例)
  ├── characterSkills: Dictionary<string characterID, List<SkillConfig>>  // 角色技能映射 ✅
  ├── skillOwnership: Dictionary<string skillInstanceID, string characterID>  // 技能归属映射 ✅
  ├── skillInstances: Dictionary<string, SkillInstance>  // 技能实例（保留）
  ├── AddSkillToCharacter(characterID, SkillConfig)     // 添加技能到角色 ✅
  └── GetCharacterSkills(characterID)                   // 查询角色技能 ✅

SkillInstance
  ├── ownerCharacterID: string                           // 归属角色ID ✅
  ├── triggers: List<BaseSkillTrigger>                   // 触发器（支持角色过滤）✅
  ├── effects: List<BaseSkillEffect>                     // 效果（支持角色指定）✅
  └── Execute(characterID)                               // 执行效果（传入角色ID）✅

BaseSkillTrigger
  ├── ownerCharacterID: string                           // 归属角色ID ✅
  └── CheckCondition(event) → 检查事件来源是否匹配      // 角色ID过滤 ✅

BaseSkillEffect
  ├── targetCharacterID: string                          // 目标角色ID ✅
  └── Execute(characterID)                               // 执行效果到指定角色 ✅
```

---

## 📝 实施步骤

### ✅ 步骤1：修改 SkillManager（技能归属管理）⏱️ 已完成

#### 1.1 添加角色技能映射数据结构 ✅
- ✅ `characterSkills`: Dictionary<string, List<SkillConfig>>
- ✅ `skillOwnership`: Dictionary<string, string>
- ✅ 添加 LINQ 引用

#### 1.2 添加角色技能管理方法 ✅
- ✅ `AddSkillToCharacter(characterID, skillConfig)`
- ✅ `GetCharacterSkills(characterID)`
- ✅ `GetSkillOwner(skillInstanceID)`
- ✅ `RemoveCharacterSkills(characterID)`
- ✅ `GetAllCharacterIDs()`

#### 1.3 修改现有 AddSkill 方法 ✅
- ✅ 标记为 `[Obsolete]`，建议使用新方法
- ✅ `ResetState()` 更新以清理角色技能映射
- ✅ `ShowSkillStatus()` 更新以显示角色归属

#### 验收标准 ✅
- [x] 可以添加技能到指定角色
- [x] 可以查询角色的所有技能
- [x] 技能归属映射正确存储

---

### ✅ 步骤2：修改 SkillInstance（添加归属信息）⏱️ 已完成

#### 2.1 添加归属字段 ✅
- ✅ `ownerCharacterID`: string
- ✅ 构造函数中初始化为 null

#### 2.2 添加 SetOwner 方法 ✅
- ✅ `SetOwner(characterID)` 方法
- ✅ 自动传递给 `currentLevelInstance.trigger.SetOwner()`
- ✅ 在 SkillManager.AddSkillToCharacter 中调用

#### 验收标准 ✅
- [x] SkillInstance 包含归属角色ID
- [x] 创建时正确设置归属
- [x] 归属信息传递给触发器

---

### ✅ 步骤3：修改触发器系统（添加角色ID过滤）⏱️ 已完成

#### 3.1 扩展 ITrigger 接口 ✅
- ✅ 添加 `SetOwner(string characterID)` 方法到接口

#### 3.2 创建 TriggerHelper 辅助类 ✅
- ✅ `GetCharacterID(GameObject)` - 从球体获取角色ID
- ✅ `IsOwner(GameObject, characterID)` - 检查归属
- ✅ `CheckEventSource(eventData, ownerCharacterID)` - 通用事件过滤

#### 3.3 修改所有具体触发器 ✅
- [x] `CollisionTrigger` - 添加角色过滤（L65-68）
- [x] `MovingEndTrigger` - 添加角色过滤（L55-58）
- [x] `KillTrigger` - 添加角色过滤（L52-56）
- [x] `AlwaysTrueTrigger` - 实现 SetOwner（不使用过滤）
- [x] `DataSourceTrigger` - 实现 SetOwner（待扩展 HealthStateData）

#### 验收标准 ✅
- [x] 所有触发器实现 SetOwner
- [x] 碰撞/击杀/停止触发器添加角色ID过滤
- [x] TriggerHelper 提供通用过滤逻辑
- [x] 编译通过无错误

---

### ✅ 步骤4：修改效果系统（添加角色ID应用）⏱️ 已完成

#### 4.1 扩展 IEffect 接口 ✅
- ✅ 添加 `SetTarget(string characterID)` 方法到接口

#### 4.2 改造所有具体效果 ✅
- [x] `StatModifierEffect` - 根据角色ID查找目标角色并应用修改器
- [x] `HealEffect` - 根据角色ID查找目标角色并执行治疗
- [x] `DropItemEffect` - 添加 SetTarget（不使用，掉落位置来自事件）
- [x] `WeakPointEffect` - 添加 SetTarget（不使用，作用于敌人）
- [x] `TransitionEffect` - 添加 SetTarget（不使用，全局场景切换）
- [x] `SpawnEffect` - 添加 SetTarget（不使用，占位符效果）

#### 4.3 SkillInstance 自动传递目标角色 ✅
- ✅ `SkillInstance.SetOwner()` 调用 `effect.SetTarget(characterID)`
- ✅ 在 `SkillManager.AddSkillToCharacter` 中自动传递

#### 示例改造（StatModifierEffect）
```csharp
private bool GetTargetPlayer()
{
    // ✅ 根据 targetCharacterID 从 TeamData 查找对应角色
    var teamData = GameSession.Instance?.GetTeamData();
    var character = teamData.characters.Find(c => c.characterID == targetCharacterID);
    if (character?.ballInstance != null)
    {
        targetPlayer = character.ballInstance.GetComponent<PlayerBehavior>();
        statsManager = targetPlayer.GetComponent<PlayerStats>();
    }
}
```

#### 验收标准 ✅
- [x] 所有效果实现 SetTarget
- [x] StatModifierEffect/HealEffect 根据角色ID应用效果
- [x] 编译通过无错误

---

### ✅ 步骤5：修改技能选择流程（SkillSelectionManager）⏱️ 已完成

#### 5.1 扩展 SkillSelectionOption 数据结构 ✅
- ✅ 添加 `targetCharacterID` - 目标角色ID
- ✅ 添加 `characterName` - 角色名称（UI显示）
- ✅ 添加 `positionIndex` - 角色位置（1/2/3号位）

#### 5.2 改造技能选项生成逻辑 ✅
- ✅ 从 TeamData 获取存活角色列表
- ✅ 随机打乱角色顺序
- ✅ 为每个技能选项预分配目标角色
- ✅ 日志显示角色归属信息

#### 5.3 改造技能应用逻辑 ✅
- ✅ `OnSkillSelected` 查找包含角色信息的 SkillSelectionOption
- ✅ `AddOrUpgradeSkillToCharacter` 调用 `SkillManager.AddSkillToCharacter`
- ✅ 使用角色特定的技能实例ID（`character_1_SkillName`）

#### 5.4 改造已有技能查询 ✅
- ✅ `GetPlayerExistingSkills` 从所有角色收集技能并去重

#### 验收标准 ✅
- [x] 技能选项生成时预分配角色
- [x] 技能应用到预分配的角色
- [x] 日志显示正确的角色归属信息

---

### ✅ 步骤6：修改技能选择UI（显示角色标识）⏱️ 已完成

#### 6.1 修改技能名称显示 ✅
- ✅ `GetSkillNameWithLevel` 添加角色归属前缀
- ✅ 格式：`[1号位 撞击角色] 碰撞增伤 lv.1`

#### 6.2 实现效果 ✅
```
显示格式示例：
- 技能1: [1号位 撞击角色] 碰撞增伤 lv.1
- 技能2: [2号位 范围攻击角色] 范围扩大
- 技能3: [3号位 三角形攻击角色] 治疗术
```

#### 验收标准 ✅
- [x] 技能名称显示归属角色信息
- [x] 包含位置编号和角色名称
- [x] 与等级标识正确组合
- [ ] 死亡角色的选项置灰（可选优化）

---

### 步骤7：适配 GameEventBus 事件（补充角色ID）⏱️ 1小时

#### 7.1 检查现有事件
```csharp
// 已完成的事件
✅ OnCharacterSelected
✅ OnCharacterLaunched
✅ OnCharacterCompleted
✅ OnCharacterDamaged
✅ OnCharacterDied
✅ OnCharacterSkillAdded
✅ OnCharacterSkillActivated

// 需要补充的事件（如有）
```

#### 7.2 确保所有相关事件包含角色ID
- 检查碰撞事件
- 检查停止事件
- 检查阶段事件

#### 验收标准
- [ ] 所有技能相关事件包含角色ID
- [ ] 事件发布和订阅正确

---

### 步骤8：集成测试（所有技能）⏱️ 3-4小时

#### 8.1 创建技能测试清单
```
基础技能测试：
- [ ] 碰撞增伤技能（CollisionTrigger + AddModifierEffect）
- [ ] 范围增强技能（StoppedTrigger + AddModifierEffect）
- [ ] 治疗技能（PhaseStartTrigger + HealEffect）
- [ ] ... 列出所有现有技能

多角色场景测试：
- [ ] 角色A有技能1，角色B没有，只有A触发
- [ ] 角色A和B都有同一技能，各自独立触发
- [ ] 角色A死亡后，技能不再触发
- [ ] 技能选择后正确分配到预设角色
```

#### 8.2 测试流程
1. 选择3个角色
2. 完成关卡，触发技能选择
3. 验证技能选项显示角色归属
4. 选择技能
5. 验证技能添加到正确角色
6. 下一关卡测试技能触发
7. 验证只有归属角色触发技能

#### 验收标准
- [ ] 所有现有技能功能正常
- [ ] 技能归属正确
- [ ] 技能触发和效果正确应用
- [ ] 无崩溃和严重Bug

---

## ⚠️ 风险与对策

### 高风险项
1. **技能数量多，改造工作量大**
   - 对策：分批改造，优先改造常用技能
   - 对策：建立测试清单，逐个验证

2. **可能遗漏某些触发器或效果**
   - 对策：全局搜索所有 BaseSkillTrigger 和 BaseSkillEffect 的子类
   - 对策：代码审查

3. **向后兼容性**
   - 对策：保留旧的 AddSkill 方法（可选）
   - 对策：或明确标记为过时，强制使用新方法

### 中风险项
4. **技能选择UI改造可能影响用户体验**
   - 对策：迭代设计，收集反馈
   - 对策：保持界面简洁清晰

5. **CharacterInstance 生命周期管理**
   - 对策：确保角色死亡时正确清理技能
   - 对策：场景切换时正确保留技能数据

---

## 📊 时间估算

| 步骤 | 任务 | 预估时间 | 优先级 |
|------|------|----------|--------|
| 步骤1 | SkillManager 改造 | 1-2小时 | ⭐⭐⭐ |
| 步骤2 | SkillInstance 改造 | 30分钟 | ⭐⭐⭐ |
| 步骤3 | Trigger 改造 | 2-3小时 | ⭐⭐⭐ |
| 步骤4 | Effect 改造 | 2-3小时 | ⭐⭐⭐ |
| 步骤5 | 选择流程改造 | 2小时 | ⭐⭐ |
| 步骤6 | UI 改造 | 1-2小时 | ⭐⭐ |
| 步骤7 | 事件系统补充 | 1小时 | ⭐ |
| 步骤8 | 集成测试 | 3-4小时 | ⭐⭐⭐ |
| **总计** | | **13-18小时** | |

**预计完成时间**：2-3个工作日

---

## ✅ 验收标准

### 功能完整性
- [ ] 技能可以归属到指定角色
- [ ] 技能触发器正确过滤角色ID
- [ ] 技能效果正确应用到目标角色
- [ ] 技能选择时预分配角色
- [ ] 技能选择UI显示角色归属
- [ ] 所有现有技能功能正常

### 代码质量
- [ ] 所有改动有注释说明
- [ ] 日志信息完整
- [ ] 无编译错误和警告
- [ ] 代码通过审查

### 稳定性
- [ ] 无崩溃
- [ ] 无严重Bug
- [ ] 多场景切换正常
- [ ] 长时间运行稳定

---

## 📝 实施建议

### 分批实施策略
1. **第一批（核心基础）**：步骤1-2，建立归属映射基础
2. **第二批（触发器改造）**：步骤3，改造常用触发器
3. **第三批（效果改造）**：步骤4，改造常用效果
4. **第四批（选择流程）**：步骤5-6，完善选择流程
5. **第五批（测试验证）**：步骤7-8，全面测试

### 增量测试
- 每完成一批改造，立即测试
- 保留测试场景和日志
- 发现问题及时修复

---

**文档版本**：1.2  
**创建日期**：2024-11-03  
**最后更新**：2024-11-03  
**状态**：接近完成（步骤1-7已完成，待集成测试）  
**下一步**：步骤8 集成测试 或 实战验证

---

## 📊 当前进度总结

| 步骤 | 状态 | 完成度 |
|------|------|--------|
| **步骤1：SkillManager** | ✅ 完成 | 100% |
| **步骤2：SkillInstance** | ✅ 完成 | 100% |
| **步骤3：触发器系统** | ✅ 完成 | 100% |
| **步骤4：效果系统** | ✅ 完成 | 100% |
| **步骤5：选择流程** | ✅ 完成 | 100% |
| **步骤6：UI改造** | ✅ 完成 | 100% |
| **步骤7：事件系统** | ✅ 已完成（早期） | 100% |
| **步骤8：集成测试** | ⏳ 待实施 | 0% |

**总体进度：约 88%（核心实现已完成，待集成测试）**

