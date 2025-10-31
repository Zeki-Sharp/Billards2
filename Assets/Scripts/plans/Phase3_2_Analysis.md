# Phase 3.2 Class/Instance 分离 - 执行方案

> 敌人系统三层属性迁移

---

## 📊 当前架构对比

### 玩家系统（已完成）✅

```
PlayerData (SO) - 配置层
  └─ 基础属性、攻击模式等

PlayerBehavior (MB) - 行为层
  └─ 核心业务逻辑（碰撞、蓄力、发射）

PlayerStats (MB) - 属性管理层
  ├─ RuntimeStatsManager（Stats 层）
  ├─ RuntimeAttributes（Attributes 层）
  ├─ RuntimeStatusEffects（StatusEffects 层）
  └─ 跨场景数据持久化（GameSession）
```

---

### 敌人系统（待重构）⚠️

**当前架构**：
```
EnemyData (SO) - 配置层
  └─ 血量、伤害、移动类型、攻击类型

EnemyBehavior (MB) - 行为层 + 属性管理层（混在一起）
  ├─ 行为逻辑（移动、攻击）
  └─ 直接管理 currentHealth（没有使用三层属性系统）
```

**目标架构**：
```
EnemyData (SO) - 配置层（不变）
  └─ 血量、伤害、移动类型、攻击类型

EnemyBehavior (MB) - 行为层（精简）
  └─ 行为逻辑（移动、攻击）

EnemyStats (MB) - 属性管理层（新建）⭐
  ├─ RuntimeStatsManager（Stats 层）
  ├─ RuntimeAttributes（Attributes 层）
  ├─ RuntimeStatusEffects（StatusEffects 层）
  └─ 统一玩家/敌人架构
```

---

## 🎯 执行计划（选项 A - 最小改动）

### 阶段 1：创建 EnemyStats（0.5 天）

**文件**：`Assets/Scripts/Enemy/EnemyStats.cs`

**参考模板**：`Assets/Scripts/StatModifierSystem/PlayerStats.cs`

**核心功能**：
1. 集成三层属性系统
   - `RuntimeStatsManager` - 基础属性（伤害、速度等）
   - `RuntimeAttributes` - 动态资源（血量）
   - `RuntimeStatusEffects` - 状态效果

2. 提供公共接口
   - `CurrentHealth` / `MaxHealth` / `HealthRatio`
   - `SetHealth()` / `AddHealth()` / `SubtractHealth()`
   - `GetFinalStat(string statID)`
   - `AddStatusEffect()` / `RemoveStatusEffect()`

3. 简化版（敌人不需要）
   - ❌ 跨场景持久化（敌人重新生成即可）
   - ❌ GameSession 集成
   - ❌ 事件发布（敌人 UI 直接调用 UpdateHealth）

---

### 阶段 2：集成到 EnemyBehavior（0.5 天）

**修改文件**：`Assets/Scripts/Enemy/EnemyBehavior.cs`

**改动内容**：
1. 添加组件引用
   ```csharp
   private EnemyStats statsManager;
   ```

2. 初始化（Start 或 SetEnemyData）
   ```csharp
   statsManager = GetComponent<EnemyStats>();
   if (statsManager == null)
   {
       statsManager = gameObject.AddComponent<EnemyStats>();
   }
   statsManager.SetEnemyData(enemyData);
   statsManager.Initialize();
   ```

3. 替换 currentHealth
   ```csharp
   // 旧：private float currentHealth;
   // 新：statsManager.CurrentHealth
   ```

4. 修改 TakeDamage 方法
   ```csharp
   public void TakeDamage(float damage)
   {
       if (isDead) return;
       
       statsManager.SubtractHealth(damage);
       
       // 更新血条
       if (healthBar != null)
       {
           healthBar.UpdateHealth(statsManager.CurrentHealth, statsManager.MaxHealth);
       }
       
       // 检查死亡
       if (statsManager.CurrentHealth <= 0)
       {
           Die();
       }
   }
   ```

5. 修改 InitializeHealth 方法
   ```csharp
   private void InitializeHealth()
   {
       // 旧：currentHealth = enemyData.maxHealth;
       // 新：statsManager 内部自动初始化
       
       if (healthBar != null)
       {
           healthBar.UpdateHealth(statsManager.CurrentHealth, statsManager.MaxHealth);
       }
   }
   ```

---

### 阶段 3：集成到 EnemySpawner（可选）

**修改文件**：`Assets/Scripts/Enemy/EnemySpawner.cs`（如果需要）

**改动内容**：
- 生成敌人后，确保 `EnemyStats` 组件正确初始化
- 可能不需要修改（EnemyBehavior 已经处理）

---

### 阶段 4：测试验证（0.5 天）

**测试内容**：
1. ✅ 敌人生成正常
2. ✅ 敌人血量初始化正常
3. ✅ 敌人受伤血量减少
4. ✅ 敌人死亡逻辑正常
5. ✅ 敌人血条 UI 正常显示
6. ✅ 敌人状态效果可以添加（如果有）

---

### 阶段 5：清理旧代码（0.5 天）

**清理内容**：
1. 删除 `EnemyBehavior` 中的 `currentHealth` 字段
2. 删除相关的冗余代码
3. 更新注释文档

**总计时间**：约 2 天

---

## ⚠️ 注意事项

### 1. 敌人不需要跨场景持久化

**理由**：
- 敌人会在场景重新生成
- 不需要保存血量到 GameSession
- 简化 EnemyStats 实现

### 2. 敌人血条 UI 是被动更新

**当前方式**：
- `EnemyBehavior` 直接调用 `healthBar.UpdateHealth()`
- 不使用 GameEventBus

**保持方式**：
- 继续使用被动更新
- 与玩家系统（事件驱动）不同

### 3. 命名约定

**使用名字**：
- `EnemyStats`（而非 `EnemyStatsManager`）
- 与 `PlayerStats` 对称

---

## 📋 改动文件清单

| 文件 | 操作 | 说明 |
|------|------|------|
| `Assets/Scripts/Enemy/EnemyStats.cs` | 新建 | 参考 PlayerStats |
| `Assets/Scripts/Enemy/EnemyBehavior.cs` | 修改 | 集成 EnemyStats |
| `Assets/Scripts/Enemy/EnemySpawner.cs` | 可能修改 | 确保初始化 |

**总计**：约 2-3 个文件

---

## ✅ 验收标准

- ✅ EnemyStats 创建完成
- ✅ 三层属性系统集成
- ✅ EnemyBehavior 使用 EnemyStats
- ✅ 敌人血量管理正常
- ✅ 敌人血条 UI 正常
- ✅ 架构统一（玩家/敌人都用三层属性）
- ✅ 无编译错误
- ✅ 现有功能正常

---

## 🚀 开始执行

**确认后开始**：
1. 我会创建 `EnemyStats.cs`
2. 修改 `EnemyBehavior.cs`
3. 测试验证
4. 清理旧代码

**等待你的确认**：是否开始执行？🤔
