# 多等级配置实施计划（分步执行）

> 每步完成后验证，再进入下一步

---

## 📋 实施步骤（共 6 步）

### 步骤 1：创建 EnemyLevelConfig ⏳

**文件**：`Assets/Scripts/Data/EnemyLevelConfig.cs`（新建）

**内容**：数值配置类
- level（等级编号）
- isActive（是否激活）
- maxHealth, damage, moveSpeed, attackCooldown, attackRange

**改动范围**：1 个新文件  
**影响范围**：无（独立文件）  
**验收标准**：编译通过

---

### 步骤 2：EnemyData 添加多等级配置字段

**文件**：`Assets/Scripts/Data/EnemyData.cs`

**改动**：
- 添加 `List<EnemyLevelConfig> enemyLevels`
- 添加 `AutoAssignLevelNumbers()` 按钮
- **保留旧字段**（maxHealth, damage 等）作为向后兼容

**改动范围**：1 个文件  
**影响范围**：无（只增加字段，不修改现有逻辑）  
**验收标准**：Inspector 显示等级列表，旧系统仍正常工作

---

### 步骤 3：EnemyData 添加等级查询方法

**文件**：`Assets/Scripts/Data/EnemyData.cs`

**改动**：
- 添加 `GetLevelConfig(int level)` 方法
- 添加 `GetMaxLevel()` 方法
- 添加 `GetAvailableLevels()` 方法

**改动范围**：1 个文件  
**影响范围**：无（只增加方法）  
**验收标准**：方法可以正常调用

---

### 步骤 4：EnemyStats 支持等级参数

**文件**：`Assets/Scripts/Enemy/EnemyStats.cs`

**改动**：
- `Initialize()` 改为 `Initialize(int level = 1)`
- `RegisterBaseStats()` 从 levelConfig 读取数值
- 如果 levelConfig 为 null，回退到旧字段

**改动范围**：1 个文件  
**影响范围**：小（向后兼容，默认 level=1）  
**验收标准**：旧系统仍正常工作

---

### 步骤 5：EnemySpawn 添加 level 字段

**文件**：`Assets/Scripts/Data/WaveConfig.cs`

**改动**：
- `EnemySpawn` 添加 `public int level = 1;` 字段
- 修改 `ToString()` 显示等级

**改动范围**：1 个文件  
**影响范围**：小（默认 level=1，向后兼容）  
**验收标准**：Inspector 显示等级字段

---

### 步骤 6：EnemySpawner 传递等级参数

**文件**：`Assets/Scripts/Enemy/EnemySpawner.cs`（或相关生成逻辑）

**改动**：
- 生成敌人时传递 `enemySpawn.level` 参数
- `enemy.SetEnemyData(data, level)` 或 `stats.Initialize(level)`

**改动范围**：1-2 个文件  
**影响范围**：中（需要修改生成逻辑）  
**验收标准**：可以生成指定等级的敌人

---

## ⚠️ 执行原则

1. **每步独立验证**：完成一步后验证编译和功能
2. **向后兼容**：每步都保证旧系统仍能正常工作
3. **最小改动**：每步只改最少的代码
4. **等待确认**：完成后等待用户确认再继续

---

## 🎯 当前状态

**当前步骤**：步骤 1  
**状态**：准备执行  
**下一步**：创建 EnemyLevelConfig.cs

