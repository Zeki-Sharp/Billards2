# ScriptableObject资源管理重构计划

## 目标
将所有静态配置的ScriptableObject从Resources文件夹迁移到独立的资源管理结构，优化资源加载方式，提升项目性能和可维护性。

## 当前问题

### 1. Resources文件夹滥用
- **静态配置混在其中**：Skills、Characters、Enemies、Items等配置都在Resources/Data/
- **无法优化打包**：所有Resources内容强制打包，无法剔除未使用资源
- **性能损耗**：`Resources.LoadAll`运行时扫描，加载慢
- **引用追踪困难**：Unity无法分析Resources资源的依赖关系

### 2. 动态加载滥用
- **SkillSelectionManager**：使用`Resources.LoadAll<SkillConfig>("")`加载所有技能
- **SkillConfig编辑器**：使用`Resources.Load/LoadAll`获取角色数据
- **无必要的动态性**：这些配置在设计时已确定，不需要运行时加载

### 3. 缺少集中管理
- 技能配置分散在Resources中，无统一视图
- 难以查看和管理所有可用技能
- 配置变更需要运行游戏才能发现问题

## 重构思路

### 1. 资源分类原则

**保留在Resources的资源：**
- 真正需要动态路径加载的资源（UI弹窗预制体）
- 运行时可选内容
- 第三方插件要求的资源

**迁移到Scriptable Objects的资源：**
- 所有游戏配置数据（Skills、Characters、Enemies、Items等）
- 设计时确定的静态配置
- 需要在Inspector中直接引用的资源

### 2. 创建集中式数据库

**核心数据库架构：**

**SkillDatabase（技能数据库）：**
- 维护所有技能配置的引用列表
- 提供查询和筛选接口
- 支持按角色、标签过滤
- 提供编辑器工具（自动发现、验证、清理）

**CharacterSelectionData（角色数据库）：**
- 已存在，实现了数据库模式
- 维护可选角色列表
- 需要从Resources迁移到Databases文件夹
- 改为直接引用而非Resources.Load

**其他数据库（可选扩展）：**
- `EnemyDatabase` - 敌人配置管理
- `ItemDatabase` - 道具配置管理
- `LevelDatabase` - 关卡配置管理

**统一优势：**
- Inspector可视化管理
- 编译时引用检查
- 无运行时扫描开销
- 支持AssetBundle打包
- 架构统一，易于维护

### 3. 引用方式改造

**旧方式（动态加载）：**
- `Resources.LoadAll<SkillConfig>("")`
- 字符串路径，运行时查找
- 无编译时检查

**新方式（直接引用）：**
- `[SerializeField] private SkillDatabase skillDatabase;`
- Inspector拖拽赋值
- 编译时类型安全

### 4. 文件夹结构调整

**Resources文件夹（精简后）：**
```
Resources/
├── UI/Popups/              # 动态加载的UI
├── GameSettings.asset      # 全局单例配置（可选）
└── DOTweenSettings.asset   # 第三方插件
```

**Scriptable Objects文件夹（新结构）：**
```
Scriptable Objects/
├── Databases/                          # 数据库资源（集中管理层）
│   ├── MainSkillDatabase.asset         # 技能数据库
│   ├── CharacterSelectionData.asset    # 角色数据库（从Resources迁移）
│   ├── EnemyDatabase.asset             # 敌人数据库（可选）
│   ├── ItemDatabase.asset              # 道具数据库（可选）
│   └── LevelDatabase.asset             # 关卡数据库（可选）
├── Characters/             # 角色配置
├── Combat/                 # 战斗配置
│   ├── Enemy/
│   └── Physics/
├── Skills/                 # 技能配置
├── Items/                  # 道具配置
├── Levels/                 # 关卡配置
├── UI/                     # UI配置
└── MapSystem/              # 地图系统（已有）
```

## 实施步骤

### 阶段1：创建基础架构
1. 创建SkillDatabase脚本
2. 添加技能列表管理功能
3. 添加查询和筛选接口
4. 添加编辑器工具方法（自动发现、验证、清理）

### 阶段2：创建数据库资源
1. 在Scriptable Objects/Databases/文件夹创建MainSkillDatabase资源
2. 使用"自动发现"功能从Resources临时加载并填充现有技能引用（过渡步骤）
3. 验证所有技能配置完整性
4. 检查并清理无效引用

**注意：** "自动发现"只是为了快速构建数据库引用列表，之后技能移出Resources时，引用会自动更新到新位置

### 阶段3：重构SkillSelectionManager
1. 添加SkillDatabase字段
2. 修改InitializeSkillSelectionManager逻辑
3. 创建LoadSkillsFromDatabase方法替代DiscoverAllSkills
4. 移除Resources.LoadAll调用
5. 在场景中配置数据库引用

### 阶段4：处理角色数据库和编辑器代码
1. 将CharacterSelectionData.asset移到Scriptable Objects/Databases/
2. 修改SkillConfig.cs的GetAvailableSkillTags方法
3. 使用AssetDatabase替代Resources.Load
4. 改为查找项目中的CharacterSelectionData和PlayerData
5. 更新所有使用CharacterSelectionData的地方改为直接引用
6. 仅编辑器代码使用AssetDatabase

### 阶段5：迁移ScriptableObject资源
1. 创建Scriptable Objects子文件夹结构
2. 从Resources/Data/迁移所有配置资源：
   - Player → Scriptable Objects/Characters/
   - Enemy → Scriptable Objects/Combat/Enemy/
   - Skill → Scriptable Objects/Skills/
   - Item → Scriptable Objects/Items/
   - Spawn → Scriptable Objects/Levels/
   - 物理参数 → Scriptable Objects/Combat/Physics/
   - UI参数 → Scriptable Objects/UI/
3. 验证所有引用自动更新
4. 检查场景和预制体引用完整性

### 阶段6：清理Resources文件夹
1. 确认所有配置资源已迁移
2. 删除空的Resources/Data/子文件夹
3. 保留必要的动态加载资源（UI/Popups、DOTweenSettings等）
4. 验证UIController等使用Resources.Load的功能正常

### 阶段7：测试验证
1. 测试技能选择系统功能
2. 测试角色选择和切换
3. 测试关卡配置加载
4. 测试UI动态加载功能
5. 验证打包后资源正常

## 配置约定

### 命名规范
- 数据库资源：`MainSkillDatabase.asset`
- 技能配置：保持原有命名（如：`击杀提升伤害.asset`）
- 文件夹命名：英文PascalCase（如：`Characters/`、`Combat/`）

### 引用管理
- 所有Manager使用Inspector直接引用数据库
- 数据库资源放在明确路径：`Scriptable Objects/Databases/`
- 避免使用Resources.Load，除非确实需要动态加载

### 编辑器工具
- 数据库提供右键菜单功能：
  - "自动发现并添加所有技能"
  - "清理无效技能"
  - "验证数据库完整性"
- 支持编辑器脚本批量操作

## 验收标准

### 功能完整性
- [ ] 技能选择系统正常工作
- [ ] 角色选择和技能标签正常
- [ ] 关卡配置正确加载
- [ ] UI动态加载功能保持正常
- [ ] 所有ScriptableObject引用完整

### 性能优化
- [ ] 无运行时Resources.LoadAll调用（UI除外）
- [ ] 启动和关卡加载速度提升
- [ ] 内存占用优化

### 可维护性
- [ ] Inspector可视化管理所有技能
- [ ] 新增技能只需创建资源+添加到数据库
- [ ] 资源引用丢失时Editor立即显示Missing
- [ ] 文档和注释更新完整

### 文件组织
- [ ] Resources文件夹仅保留必要资源
- [ ] Scriptable Objects结构清晰
- [ ] 资源分类合理，易于查找

## 注意事项

### 迁移风险
- Unity会自动更新.meta文件，引用通常不会丢失
- 建议迁移前备份或提交Git
- 每个阶段完成后立即测试

### 兼容性
- UIController保持使用Resources.Load（正确用法）
- 第三方插件要求的Resources资源不要移动
- 保持向后兼容，逐步迁移

### 未来扩展
- 考虑使用Addressables系统（更现代的资源管理）
- 为其他系统创建类似的数据库：
  - `EnemyDatabase` - 统一管理所有敌人配置
  - `ItemDatabase` - 统一管理所有道具配置
  - `LevelDatabase` - 统一管理所有关卡配置
- 支持多个数据库切换（不同难度、不同角色集等）
- 考虑创建`GameConfig`单例统一管理所有数据库引用

## 相关文档
- Unity官方：Resources最佳实践
- Unity官方：ScriptableObject使用指南
- 项目内：SkillSystem架构文档

## 预期收益
- ⚡ 启动速度提升
- 💾 内存管理优化
- 🔍 资源管理可视化
- 🛡️ 编译时错误检查
- 📦 支持AssetBundle分包
- 🔧 维护效率提升

