# Phase A 任务清单（准备与基线）

> 说明：Phase A 聚焦在准备工作与基线搭建，确保 3D 升级所需的文档、分支、依赖清单和改写方案已落地。场景复制与 Editor 配置由用户在 Unity 中执行，此处仅给出需要在项目内完成的可操作项。

---

## A1. 文档与目录整理
1. **迁移历史资料**  
   - 将旧版 `Vector2UsageAudit`, `3DUpgradePlan` 等文档搬迁至 `Plans/3DUpgrade/`，确认引用路径有效。  
   - 由 `UpgradeMasterPlan.md` 维护统一索引，新文档创建后及时更新。
2. **文档格式统一**  
   - 每篇子文档首段补充“目的/阶段”说明，保持命名规则：`PhaseX_*.md`、`SystemName_Plan.md`。

## A2. 代码分支与版本基线
1. **Git 分支**  
   - 创建/切换 `feature/3d-upgrade` 分支，同步最新主干。  
   - 建立 `PhaseA` 标签或里程碑，记录启动时间与范围。
2. **版本记录**  
   - 在 `Plans/3DUpgrade/Changelog.md`（待建）记录 Phase A 的关键提交与完成项。

## A3. 依赖清单补完
1. **Vector2 / Physics2D**  
   - 复查 `Vector2UsageAudit.md`，补充遗漏脚本；新增 `Physics2DUsageAudit.md`，列出所有 `Rigidbody2D/Collider2D/Physics2D.*` 的脚本与 Prefab。  
   - 标注每个脚本所属系统及迁移优先级（P0/P1/P2）。
2. **SpriteRenderer / Animator(2D)**  
   - 编写 `SpriteBasedAssets.md`，列出所有 2D 角色/敌人/特效 Prefab 与关联脚本，为后续资源替换做准备。

## A4. 3D 改写策略
1. **系统排序**  
   - 确定 Movement → Player → Physics → Damage → Spawn/Map 的改写顺序，并在 Dependency Audit 中标注。
2. **接口影响评估**  
   - 梳理行为树、GameEventBus、ScriptableObject 在切换到 `Vector3`/3D 物理时需要调整的字段与序列化方案。
3. **回滚/验证计划**  
   - 为每个系统定义验证标准与回滚方案（例如保留 2D 分支快照），避免临时桥接。

## A5. 场景与资源基线
1. **场景备份**  
   - （由用户在 Unity 中完成）复制主场景为 `MainScene_3D`。  
   - 在文档中记录需要替换的资源列表与依赖脚本。
2. **Prefab 标记**  
   - 为需要 3D 化的关键 Prefab 添加标签（例如 `Needs3DUpgrade`），编写清单以便跟踪。

## A6. 排期与协作
1. **任务登记**  
   - 将以上子任务拆分到项目管理工具，指派负责人与预计完成日期。  
   - 每个任务完成后更新 `PhaseA_TaskList.md` 状态（可在末尾追加表格）。
2. **评审安排**  
   - 约定 Phase A 评审会议，内容包括依赖清单完整度、3D 改写顺序、分支流程。

---

## 当前进度（初始化）
| 子任务 | 负责人 | 目标时间 | 状态 |
| --- | --- | --- | --- |
| 文档迁移 & 索引搭建 | 待定 |  | Not Started |
| Git 分支 & Changelog 建立 | 待定 |  | Not Started |
| Physics2D Usage Audit | 待定 |  | Not Started |
| SpriteBasedAssets 清单 | 待定 |  | Not Started |
| 3D 改写顺序与方案 | 待定 |  | Not Started |
| Prefab 标记 & 资源列表 | 待定 |  | Not Started |

> 完成项请在表格中更新负责人/时间/状态，并将细节同步到 Changelog。

