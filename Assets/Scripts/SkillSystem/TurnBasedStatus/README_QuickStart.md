# 回合制状态系统 - 快速开始指南

## ✅ **Phase 1 已完成**

### **已实现的文件：**
1. ✅ `Core/TurnBasedStatusData.cs` - ScriptableObject配置
2. ✅ `Core/TurnBasedStatusComponent.cs` - 抽象基类
3. ✅ `Statuses/BurningStatus.cs` - 点燃状态实现
4. ✅ `Effects/TurnBasedStatusEffect.cs` - 施加状态的IEffect
5. ✅ `Triggers/DamageTrigger.cs` - 伤害触发器（已存在）

---

## 🚀 **5分钟配置点燃技能**

### **Step 1: 创建点燃状态配置**

1. 在 Project 窗口右键
2. **Create > Game > Turn Based Status Data**
3. 命名为 `BurningStatusData`

**配置参数：**
```
基本信息:
├─ Status ID: "burning"
├─ Display Name: "点燃"
├─ Icon: 火焰图标（可选）
└─ Description: "持续造成火焰伤害"

回合配置:
├─ Base Duration In Turns: 2（持续2回合）
└─ Trigger Phase: EnemyPhaseEnd（敌人回合结束时触发）

伤害配置:
└─ Base Damage Per Turn: 5（每回合5伤害）

堆叠配置:
└─ Max Stacks: 0（无限堆叠，回合数累加）

视觉效果:
├─ VFX Prefab: 火焰粒子特效（可选）
└─ Effect Color: 橙红色
```

---

### **Step 2: 创建点燃技能**

1. 在 Project 窗口右键
2. **Create > Game > Skill Config**
3. 命名为 `Skill_Burning_RangeAttack`

**配置参数：**
```
基本信息:
├─ Skill ID: "burning_range_attack"
├─ Skill Name: "点燃"
├─ Description: "范围攻击会点燃敌人，持续造成伤害"
└─ Source Character Name: "范围攻击角色"

触发器配置 (Trigger Configuration):
└─ 点击下拉框，选择：DamageTriggerConfig
    └─ 展开配置：
        ├─ Trigger Types: [Stopped]  ← 只勾选 Stopped
        ├─ Target Tag: "Enemy"
        └─ Show Debug Log: ☑️（调试时勾选）

技能等级配置 > Level 1 > 效果配置 (Effect Configuration):
└─ 点击下拉框，选择：TurnBasedStatusEffectConfig
    └─ 展开配置：
        ├─ Status Data: 拖入 BurningStatusData ⭐
        └─ Show Debug Log: ☑️（调试时勾选）

重置条件配置 (Reset Condition):
└─ 点击下拉框，选择：OnPhaseEndedResetCondition
    └─ 每回合自动重置，允许技能再次触发
```

---

### **Step 3: 分配给角色**

1. 找到范围攻击角色的 `PlayerData`
2. 展开 `Default Skills`
3. **Add Element**，拖入 `Skill_Burning_RangeAttack`

---

## 🎮 **测试**

### **测试步骤：**
1. 运行游戏
2. 选择范围攻击角色
3. 使用范围攻击击中敌人（球停止时）
4. 观察 Console 日志：
   ```
   [DamageTrigger] ✅ 触发条件满足：Stopped类型伤害
   [TurnBasedStatusEffect] ✅ 对 Enemy 施加点燃：2回合
   [点燃] 🔥 Enemy 开始燃烧！
   ```
5. 等待敌人回合结束
6. 观察日志：
   ```
   [点燃] 🔥 Enemy 受到点燃伤害：5，剩余1回合
   ```
7. 再等一个敌人回合
8. 观察日志：
   ```
   [点燃] 🔥 Enemy 受到点燃伤害：5，剩余0回合
   [点燃] Enemy 火焰熄灭
   ```

---

## 🎯 **效果验证**

### **点燃叠加测试：**
1. 第一次范围攻击点燃敌人：2回合，5伤害
2. 再次范围攻击同一个敌人
3. 观察日志：
   ```
   [TurnBasedStatusEffect] ✅ 对 Enemy 叠加点燃：+2回合，总计4回合
   ```
4. 敌人会在接下来的4个回合持续受到伤害

---

## 🔧 **常见问题**

### **Q: 为什么撞击不触发点燃？**
A: DamageTrigger 的 `Trigger Types` 只勾选了 `Stopped`，撞击是 `Collision` 类型

### **Q: 如何让撞击也触发点燃？**
A: 在 DamageTrigger 中，`Trigger Types` 同时勾选 `Collision` 和 `Stopped`

### **Q: 如何修改点燃持续时间和伤害？**
A: 修改 `BurningStatusData` 的配置：
- Base Duration In Turns: 持续回合数
- Base Damage Per Turn: 每回合伤害

### **Q: 如何修改伤害触发时机？**
A: 修改 `BurningStatusData` 的 `Trigger Phase`：
- EnemyPhaseEnd: 敌人回合结束时
- PlayerPhaseEnd: 玩家回合结束时
- EnemyPhase: 敌人回合开始时

### **Q: 点燃状态会跨场景保留吗？**
A: 不会。敌人不跨场景，状态会随敌人销毁而自动清除

---

## 📋 **完整配置清单**

- [ ] 创建 BurningStatusData 配置
  - [ ] 设置 statusID, displayName
  - [ ] 配置 baseDurationInTurns, baseDamagePerTurn
  - [ ] 设置 triggerPhase = EnemyPhaseEnd
- [ ] 创建 Skill_Burning_RangeAttack
  - [ ] 添加 DamageTrigger，triggerTypes = [Stopped]
  - [ ] 添加 TurnBasedStatusEffect，拖入 BurningStatusData
  - [ ] 添加 OnPhaseEndedResetCondition
  - [ ] 设置 Source Character Name = "范围攻击角色"
- [ ] 将技能分配给范围攻击角色的 PlayerData
- [ ] 测试点燃效果
- [ ] 测试点燃叠加

---

## 🎉 **完成！**

配置完成后：
- ✅ 范围攻击角色在停止时会点燃敌人
- ✅ 撞击不触发点燃
- ✅ 点燃效果自动累加回合数
- ✅ 每个敌人回合结束时自动造成伤害
- ✅ 回合数耗尽后自动移除

**完全自动化，无需额外代码！** 🔥

