/// <summary>
/// Manager 执行顺序常量 - 参考 Game Creator 2 的 ApplicationManager 设计
/// 
/// 【设计目标】：
/// - 明确各层 Manager 的初始化顺序
/// - 消除延迟初始化协程
/// - 提供清晰的依赖关系
/// - 使用 Unity 的 DefaultExecutionOrder 特性保证执行顺序
/// 
/// 【使用方式】：
/// [DefaultExecutionOrder(ManagerExecutionOrder.CORE)]
/// public class GameManager : SingletonManager&lt;GameManager&gt; { }
/// 
/// 【执行顺序说明】：
/// Unity 按 DefaultExecutionOrder 的数值从小到大执行 Awake() 方法
/// - 负数：早于默认执行
/// - 0：默认执行顺序
/// - 正数：晚于默认执行
/// 
/// 【分层架构】：
/// CORE (-100)       → 基础层，最先执行（物理设置、全局配置）
/// SYSTEM (-50)      → 系统层（技能、效果、属性等核心系统）
/// LEVEL (-30)       → 关卡层（关卡管理、进度管理）
/// CONTROLLER (0)    → 控制层（游戏流程、阶段控制）
/// UTILITY (10)      → 工具层（时间管理、伤害文本等）
/// UI (50)           → UI层（UI控制器，依赖所有系统）
/// COMPONENT (100)   → 组件层（非Manager，但需要依赖Manager）
/// 
/// 【依赖规则】：
/// - 每层只能依赖比自己执行顺序更早的层
/// - 同层之间不应有强依赖关系
/// - 如需跨层依赖，使用事件或在 Start() 中初始化
/// 
/// 【参考】：
/// Game Creator 2 - ApplicationManager.cs
/// </summary>
public static class ManagerExecutionOrder
{
    #region 执行顺序常量定义
    
    /// <summary>
    /// 基础层 (-100)
    /// 
    /// 【用途】：全局基础设置，最先执行
    /// 【包含】：
    /// - GameManager: 物理设置、游戏状态初始化
    /// 
    /// 【特点】：
    /// - 无依赖其他 Manager
    /// - 提供最基础的游戏环境设置
    /// </summary>
    public const int CORE = -100;
    
    /// <summary>
    /// 系统层 (-50)
    /// 
    /// 【用途】：核心游戏系统，依赖 CORE 层
    /// 【包含】：
    /// - SkillManager: 技能系统管理
    /// - EffectManager: 效果系统管理
    /// - WeakPointManager: 弱点系统管理
    /// - TurnPenaltyManager: 回合惩罚管理
    /// - DamageProcessor: 伤害计算（如需迁移）
    /// 
    /// 【特点】：
    /// - 只依赖 CORE 层
    /// - 提供核心游戏逻辑
    /// - 系统之间应尽量解耦
    /// </summary>
    public const int SYSTEM = -50;
    
    /// <summary>
    /// 关卡层 (-30)
    /// 
    /// 【用途】：关卡和进度管理，依赖 SYSTEM 层
    /// 【包含】：
    /// - LevelManager: 关卡流程管理
    /// - SkillSelectionManager: 技能选择管理
    /// - SceneTransitionManager: 场景切换管理
    /// 
    /// 【特点】：
    /// - 依赖 SYSTEM 层的核心系统
    /// - 管理关卡流程和进度
    /// - 处理场景切换逻辑
    /// </summary>
    public const int LEVEL = -30;
    
    /// <summary>
    /// 控制层 (0) - 默认执行顺序
    /// 
    /// 【用途】：游戏流程控制，依赖 SYSTEM 和 LEVEL 层
    /// 【包含】：
    /// - GameFlowController: 顶层游戏流程控制
    /// - PlayerPhaseController: 玩家回合控制
    /// - EnemyPhaseController: 敌人回合控制
    /// 
    /// 【特点】：
    /// - 依赖所有系统和关卡层
    /// - 管理游戏流程切换
    /// - 协调各个阶段的执行
    /// </summary>
    public const int CONTROLLER = 0;
    
    /// <summary>
    /// 工具层 (10)
    /// 
    /// 【用途】：辅助工具，依赖 CONTROLLER 层
    /// 【包含】：
    /// - TimeManager: 时间管理（依赖 GameFlowController）
    /// - DamageTextManager: 伤害文本显示
    /// - TrajectorySimulationManager: 轨迹模拟（如需迁移）
    /// 
    /// 【特点】：
    /// - 依赖 CONTROLLER 层
    /// - 提供辅助功能
    /// - 响应游戏流程变化
    /// </summary>
    public const int UTILITY = 10;
    
    /// <summary>
    /// UI 层 (50)
    /// 
    /// 【用途】：UI 控制，依赖所有 Manager 层
    /// 【包含】：
    /// - UIController: UI 总控制器
    /// 
    /// 【特点】：
    /// - 最晚执行（除 COMPONENT 外）
    /// - 可以安全访问所有 Manager
    /// - 处理 UI 显示和交互
    /// </summary>
    public const int UI = 50;
    
    /// <summary>
    /// 组件层 (100) - 非 Manager
    /// 
    /// 【用途】：游戏组件，依赖所有 Manager
    /// 【包含】：
    /// - PlayerStateMachine: 玩家状态机
    /// - Player: 玩家角色
    /// - Enemy: 敌人
    /// - ChargeSystem: 蓄力系统
    /// 
    /// 【特点】：
    /// - 最晚执行
    /// - 所有 Manager 都已初始化
    /// - 可以安全访问所有 Manager
    /// 
    /// 【注意】：
    /// - 这些通常不是 SingletonManager
    /// - 但需要依赖 Manager，所以执行顺序最晚
    /// </summary>
    public const int COMPONENT = 100;
    
    #endregion
    
    #region 辅助常量 - 细粒度控制
    
    /// <summary>
    /// 在 CORE 层之前执行
    /// 用于极少数需要在所有系统之前初始化的情况
    /// </summary>
    public const int BEFORE_CORE = CORE - 10;
    
    /// <summary>
    /// 在 CORE 层之后、SYSTEM 层之前执行
    /// </summary>
    public const int AFTER_CORE = CORE + 10;
    
    /// <summary>
    /// 在 SYSTEM 层之前执行
    /// </summary>
    public const int BEFORE_SYSTEM = SYSTEM - 10;
    
    /// <summary>
    /// 在 SYSTEM 层之后、LEVEL 层之前执行
    /// </summary>
    public const int AFTER_SYSTEM = SYSTEM + 10;
    
    /// <summary>
    /// 在 LEVEL 层之前执行
    /// </summary>
    public const int BEFORE_LEVEL = LEVEL - 10;
    
    /// <summary>
    /// 在 LEVEL 层之后、CONTROLLER 层之前执行
    /// </summary>
    public const int AFTER_LEVEL = LEVEL + 10;
    
    /// <summary>
    /// 在 CONTROLLER 层之前执行
    /// </summary>
    public const int BEFORE_CONTROLLER = CONTROLLER - 10;
    
    /// <summary>
    /// 在 CONTROLLER 层之后、UTILITY 层之前执行
    /// </summary>
    public const int AFTER_CONTROLLER = CONTROLLER + 10;
    
    /// <summary>
    /// 在 UTILITY 层之前执行
    /// </summary>
    public const int BEFORE_UTILITY = UTILITY - 10;
    
    /// <summary>
    /// 在 UTILITY 层之后、UI 层之前执行
    /// </summary>
    public const int AFTER_UTILITY = UTILITY + 10;
    
    /// <summary>
    /// 在 UI 层之前执行
    /// </summary>
    public const int BEFORE_UI = UI - 10;
    
    /// <summary>
    /// 在 UI 层之后、COMPONENT 层之前执行
    /// </summary>
    public const int AFTER_UI = UI + 10;
    
    #endregion
    
    #region 辅助方法
    
    /// <summary>
    /// 获取在指定顺序之前的执行顺序
    /// </summary>
    /// <param name="order">基准执行顺序</param>
    /// <param name="offset">偏移量（默认 -1）</param>
    /// <returns>新的执行顺序</returns>
    public static int Before(int order, int offset = 1)
    {
        return order - offset;
    }
    
    /// <summary>
    /// 获取在指定顺序之后的执行顺序
    /// </summary>
    /// <param name="order">基准执行顺序</param>
    /// <param name="offset">偏移量（默认 1）</param>
    /// <returns>新的执行顺序</returns>
    public static int After(int order, int offset = 1)
    {
        return order + offset;
    }
    
    #endregion
    
    #region 调试工具
    
#if UNITY_EDITOR
    /// <summary>
    /// 获取执行顺序的层级名称
    /// 用于调试和日志输出
    /// </summary>
    public static string GetLayerName(int executionOrder)
    {
        if (executionOrder <= CORE && executionOrder > SYSTEM)
            return $"CORE ({CORE})";
        if (executionOrder <= SYSTEM && executionOrder > LEVEL)
            return $"SYSTEM ({SYSTEM})";
        if (executionOrder <= LEVEL && executionOrder > CONTROLLER)
            return $"LEVEL ({LEVEL})";
        if (executionOrder <= CONTROLLER && executionOrder > UTILITY)
            return $"CONTROLLER ({CONTROLLER})";
        if (executionOrder <= UTILITY && executionOrder > UI)
            return $"UTILITY ({UTILITY})";
        if (executionOrder <= UI && executionOrder > COMPONENT)
            return $"UI ({UI})";
        if (executionOrder <= COMPONENT)
            return $"COMPONENT ({COMPONENT})";
        
        return "UNKNOWN";
    }
    
    /// <summary>
    /// 打印执行顺序信息（编辑器专用）
    /// </summary>
    public static void LogExecutionOrderInfo()
    {
        UnityEngine.Debug.Log("=== Manager 执行顺序体系 ===");
        UnityEngine.Debug.Log($"CORE:       {CORE}  (GameManager)");
        UnityEngine.Debug.Log($"SYSTEM:     {SYSTEM}  (SkillManager, EffectManager, WeakPointManager, ...)");
        UnityEngine.Debug.Log($"LEVEL:      {LEVEL}  (LevelManager, SkillSelectionManager, ...)");
        UnityEngine.Debug.Log($"CONTROLLER: {CONTROLLER}  (GameFlowController, PlayerPhaseController, ...)");
        UnityEngine.Debug.Log($"UTILITY:    {UTILITY}  (TimeManager, DamageTextManager)");
        UnityEngine.Debug.Log($"UI:         {UI}  (UIController)");
        UnityEngine.Debug.Log($"COMPONENT:  {COMPONENT}  (PlayerStateMachine, Player, Enemy)");
        UnityEngine.Debug.Log("==============================");
    }
#endif
    
    #endregion
}

