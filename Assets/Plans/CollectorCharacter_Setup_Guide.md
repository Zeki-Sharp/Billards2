# 收集者角色配置指南

> **创建时间**：2025年11月  
> **状态**：待配置  
> **预计时间**：15-20分钟

---

## ⚠️ **重要：统一使用 characterID**

**本项目已统一使用 `characterID` 作为角色标识符，而非角色名称。**

**好处：**
- ✅ 支持角色重命名，不影响技能、道具等引用
- ✅ 支持多语言本地化
- ✅ 代码更清晰，逻辑更稳定

**配置要求：**
- 所有 `PlayerData` 必须配置 `characterID` 字段（如 `character_1`, `character_2`）
- 技能 Tag 使用 `characterID`（如 `character_1`）
- 道具拾取限制使用 `characterID`

---

## 📋 **配置清单**

- [ ] **步骤1**：创建收集者专属掉落物 ItemConfig
- [ ] **步骤2**：创建掉落物补充技能（被动）
- [ ] **步骤3**：创建收集打击技能（主动）
- [ ] **步骤4**：创建收集者角色 PlayerData
- [ ] **步骤5**：测试验证功能

---

## 🔧 **步骤1：创建收集者专属掉落物**

### **路径**
`Assets/Data/Items/` → 右键 → Create → Game → Item Config

### **文件名**
`Item_CollectorGem`

### **配置参数**

| 字段 | 值 | 说明 |
|-----|-----|-----|
| **基本信息** | | |
| 道具名称 | `收集者宝石` | 显示名称 |
| 道具描述 | `只有收集者能拾取的特殊宝石` | 描述文本 |
| 道具图标 | （拖拽 Sprite） | UI 图标 |
| **效果配置** | | |
| 关联技能 | `留空或配置治疗技能` | 拾取后的效果（可选） |
| 是否为一次性效果 | `true` | 拾取后立即消失 |
| **目标配置** | | |
| 目标类型 | `Picker` | 效果作用于拾取者 |
| **拾取限制** | | |
| ⚠️ **拾取限制** | `SpecificCharacter` | ⚠️ **必须设置为特定角色** |
| ⚠️ **限制角色名** | `character_1` | ⚠️ **填写收集者的角色ID** |
| **掉落配置** | | |
| 道具预制体 | （拖拽预制体） | 场景中的掉落物模型 |

### **⚠️ 关键配置**
```
拾取限制 = SpecificCharacter
限制角色 = 撞击角色 (character_1)  ← 下拉选择，存储 characterID
```

**如何配置 characterID？**
1. 打开你的 `PlayerData` SO（如收集者角色）
2. 在 **"玩家特有信息"** 中找到 **"角色唯一ID"** 字段
3. 填写 `character_1`（或 `character_2`, `character_3` 等）
4. 保存

**ID 命名规范：**
- 格式：`character_数字`
- 示例：`character_1`, `character_2`, `character_3`
- 必须唯一，不能重复

---

## 🔧 **步骤2：创建掉落物补充技能（被动）**

### **路径**
`Assets/Data/Skills/` → 右键 → Create → Game → Skill Config

### **文件名**
`Skill_Collector_Replenish`

### **配置参数**

#### **基本信息**
| 字段 | 值 |
|-----|-----|
| 技能名称 | `掉落物补充` |
| 技能描述 | `每回合开始时，确保场上有3个收集者宝石` |
| 技能类型 | `Passive` |

#### **⚠️ Trigger 配置**
1. 点击 `Trigger Config` 下拉菜单
2. 选择 **`PhaseStateTriggerConfig`**
3. 配置：

| 字段 | 值 | 说明 |
|-----|-----|-----|
| 触发的游戏阶段 | `PlayerPhaseStart` | ⚠️ **玩家回合开始时** |
| 显示调试日志 | `true` | 方便调试 |

#### **⚠️ Effect 配置**
1. 点击 `Effect Config` 下拉菜单
2. 选择 **`DropItemReplenishEffectConfig`**
3. 配置：

| 字段 | 值 | 说明 |
|-----|-----|-----|
| **掉落物配置** | | |
| 目标掉落物数量 | `3` | 保持3个 |
| ⚠️ **掉落物配置** | `Item_CollectorGem` | ⚠️ **拖拽步骤1创建的 ItemConfig** |
| 生成区域 - 最小坐标 | `(-8, -4)` | 左下角 |
| 生成区域 - 最大坐标 | `(8, 4)` | 右上角 |
| 显示调试日志 | `true` | 方便调试 |

#### **Condition 配置**
- 留空（无需条件，每次都触发）

---

## 🔧 **步骤3：创建收集打击技能（主动）**

### **路径**
`Assets/Data/Skills/` → 右键 → Create → Game → Skill Config

### **文件名**
`Skill_Collector_Strike`

### **配置参数**

#### **基本信息**
| 字段 | 值 |
|-----|-----|
| 技能名称 | `收集打击` |
| 技能描述 | `回合结束时，根据拾取数量对最近敌人造成伤害` |
| 技能类型 | `Active` |

#### **⚠️ Trigger 配置**
1. 点击 `Trigger Config` 下拉菜单
2. 选择 **`PhaseStateTriggerConfig`**
3. 配置：

| 字段 | 值 | 说明 |
|-----|-----|-----|
| 触发的游戏阶段 | `PlayerPhaseEnd` | ⚠️ **玩家回合结束时** |
| 显示调试日志 | `true` | 方便调试 |

#### **⚠️ Effect 配置**
1. 点击 `Effect Config` 下拉菜单
2. 选择 **`CollectorStrikeEffectConfig`**
3. 配置：

| 字段 | 值 | 说明 |
|-----|-----|-----|
| **伤害配置** | | |
| 每个掉落物的伤害 | `10` | 初始伤害（可根据技能等级调整） |
| 显示调试日志 | `true` | 方便调试 |

**💡 技能升级示例：**
- Lv1: `10` 伤害/个
- Lv2: `15` 伤害/个
- Lv3: `20` 伤害/个

#### **Condition 配置**
- 留空（即使拾取数量为0也会触发，内部会检查）

---

## 🔧 **步骤4：创建收集者角色 PlayerData**

### **路径**
`Assets/Data/Players/` → 右键 → Create → Game → Player Data

### **文件名**
`Player_Collector`

### **配置参数**

#### **显示信息**
| 字段 | 值 | 说明 |
|-----|-----|-----|
| 名称 | `收集者` | 显示名称，可以随时改 |
| 角色唯一ID | `character_1` | ⚠️ **必须填写，不能改** |
| 图标 | （拖拽 Sprite） | 角色头像 |
| 描述 | `通过收集掉落物积累伤害并爆发` | 角色介绍 |

#### **玩家基本信息**
| 字段 | 值 |
|-----|-----|
| 玩家预制体 | （拖拽球体预制体） |

#### **物理数据**
| 字段 | 值 |
|-----|-----|
| 球体数据 | （拖拽 BallData SO） |

#### **战斗配置**
| 字段 | 值 | 说明 |
|-----|-----|-----|
| 基础最大血量 | `100` | 可调整 |
| 攻击力 | `10` | 基础攻击力 |
| 攻击范围 | `2` | 范围攻击半径 |

#### **⚠️ 技能配置**
| 字段 | 值 | 说明 |
|-----|-----|-----|
| ⚠️ **初始技能** | | **添加2个技能** |
| 技能1 | `Skill_Collector_Replenish` | ⚠️ 掉落物补充（被动） |
| 技能2 | `Skill_Collector_Strike` | ⚠️ 收集打击（主动） |

---

## 🔧 **步骤5：测试验证功能**

### **测试前准备**
1. 确保 `DropItemTracker` 脚本已添加到场景中的某个 Manager 对象上
2. 确保 `GameEventBus` 正常工作

### **测试场景**
1. 在角色选择界面选择 **收集者角色**
2. 进入战斗场景

### **测试步骤**

#### **测试1：掉落物补充（被动）**

**预期行为：**
1. ✅ 玩家回合开始时，自动生成3个"收集者宝石"
2. ✅ 如果上回合吃了1个，本回合只生成1个（补充到3个）
3. ✅ 如果上回合吃了3个，本回合生成3个
4. ✅ 其他角色无法拾取这些宝石

**验证方法：**
- 观察 Console 日志：`[DropItemReplenishEffect] 需要补充 X 个掉落物`
- 观察场景中的掉落物数量

#### **测试2：收集打击（主动）**

**预期行为：**
1. ✅ 玩家回合结束时，自动触发
2. ✅ 如果本回合拾取了2个宝石，对最近敌人造成 `2 × 10 = 20` 点伤害
3. ✅ 如果本回合未拾取任何宝石，不造成伤害
4. ✅ 伤害数字显示在敌人头顶

**验证方法：**
- 观察 Console 日志：`[CollectorStrikeEffect] 收集打击生效！拾取数量=X，总伤害=Y`
- 观察敌人血量变化
- 观察伤害数字显示

#### **测试3：拾取追踪**

**预期行为：**
1. ✅ 只有收集者角色能拾取"收集者宝石"
2. ✅ 拾取后 `DropItemTracker` 记录数量
3. ✅ 回合结束后计数清零

**验证方法：**
- 观察 Console 日志：`[DropItemTracker] character_1 拾取 收集者宝石，本回合: X`
- 尝试用其他角色拾取（应该失败）

---

## 🐛 **常见问题排查**

### **问题1：掉落物没有生成**

**可能原因：**
- ❌ `ItemConfig` 没有正确配置 `pickupRestriction`
- ❌ `restrictedCharacterName` 填错了角色ID
- ❌ 技能的 Trigger 没有选择 `PlayerPhaseStart`

**解决方法：**
1. 检查 `Item_CollectorGem` 的 `拾取限制` 是否为 `SpecificCharacter`
2. 检查 `限制角色名` 是否为 `character_1`（与 PlayerData 的 characterID 一致）
3. 检查技能的 Trigger 是否为 `PlayerPhaseStart`

---

### **问题2：收集打击没有造成伤害**

**可能原因：**
- ❌ 技能的 Trigger 没有选择 `PlayerPhaseEnd`
- ❌ `DropItemTracker` 没有记录拾取数量
- ❌ 场上没有敌人

**解决方法：**
1. 检查 Console 是否有 `[DropItemTracker]` 的拾取日志
2. 检查技能的 Trigger 是否为 `PlayerPhaseEnd`
3. 确保场上有敌人

---

### **问题3：其他角色也能拾取"收集者宝石"**

**可能原因：**
- ❌ `ItemConfig` 的 `拾取限制` 没有设置为 `SpecificCharacter`

**解决方法：**
1. 重新检查 `Item_CollectorGem` 的配置
2. 确保 `拾取限制` = `SpecificCharacter`
3. 确保 `限制角色名` 填写正确

---

### **问题4：点燃伤害不触发**

**可能原因：**
- ❌ `BurningStatusData` SO 的 `triggerPhase` 字段还是旧的枚举值

**解决方法：**
1. 在 Unity 中找到 `BurningStatusData` SO 文件
2. 选中后在 Inspector 中找到 **"触发阶段 (Trigger Phase)"**
3. 改为 **`EnemyPhaseEnd`**（敌人回合结束时）
4. 保存（Ctrl+S）

---

## 📊 **预期效果演示**

### **完整流程：**

```
【回合1 - 玩家回合开始】
→ 掉落物补充触发：生成3个收集者宝石

【回合1 - 玩家操作】
→ 收集者角色移动并拾取了2个宝石
→ DropItemTracker 记录：character_1 拾取了2个

【回合1 - 玩家回合结束】
→ 收集打击触发：2 × 10 = 20 点伤害
→ 对最近的敌人造成20点伤害
→ DropItemTracker 清空计数

【回合2 - 玩家回合开始】
→ 掉落物补充触发：场上还剩1个，生成2个（补充到3个）

→ (循环)
```

---

## ✅ **配置完成检查清单**

- [ ] `Item_CollectorGem` 创建并正确配置
  - [ ] `拾取限制` = `SpecificCharacter`
  - [ ] `限制角色名` = 收集者的 characterID
- [ ] `Skill_Collector_Replenish` 创建并正确配置
  - [ ] Trigger = `PhaseStateTrigger [PlayerPhaseStart]`
  - [ ] Effect = `DropItemReplenishEffectConfig`
- [ ] `Skill_Collector_Strike` 创建并正确配置
  - [ ] Trigger = `PhaseStateTrigger [PlayerPhaseEnd]`
  - [ ] Effect = `CollectorStrikeEffectConfig`
- [ ] `Player_Collector` 创建并正确配置
  - [ ] 初始技能包含上述两个技能
- [ ] 所有测试通过
  - [ ] 掉落物补充正常
  - [ ] 收集打击正常
  - [ ] 拾取追踪正常

---

## 🎉 **完成！**

配置完成后，收集者角色应该能：
- ✅ 每回合开始自动补充收集者宝石到3个
- ✅ 只有收集者能拾取这些宝石
- ✅ 回合结束时根据拾取数量造成伤害
- ✅ 完全自动化，无需额外代码

**祝你游戏愉快！** 🔥

