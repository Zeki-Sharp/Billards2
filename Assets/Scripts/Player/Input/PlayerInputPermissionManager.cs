using UnityEngine;

/// <summary>
/// 玩家输入权限管理器 - 统一管理输入权限检查逻辑（场景级单例）
/// 
/// 【核心职责】：
/// - 检查是否允许处理输入（是否在玩家回合）
/// - 提供统一的权限检查接口
/// - 为所有输入相关组件提供权限查询
/// 
/// 【架构定位】：
/// - 场景级单例管理器
/// - 被 GlobalInputManager、CharacterSelectionController 等调用
/// - 依赖 GameFlowController 判断当前游戏阶段
/// 
/// 【执行顺序】：CONTROLLER 层 (0)
/// 【依赖】：GameFlowController（同层）
/// </summary>
[DefaultExecutionOrder(ManagerExecutionOrder.CONTROLLER)]
public class PlayerInputPermissionManager : SingletonManager<PlayerInputPermissionManager>
{
    [Header("调试")]
    [SerializeField] private bool showDebugInfo = false;
    
    // 组件引用
    private GameFlowController gameFlowController;
    
    #region SingletonManager 重写
    
    protected override bool PersistAcrossScenes => false; // 场景级单例，不跨场景
    protected override bool EnableDebugLog => showDebugInfo;
    
    protected override void OnManagerCreated()
    {
        // 获取 GameFlowController 引用
        gameFlowController = GameFlowController.Instance;
        
        if (gameFlowController == null)
        {
            Debug.LogError("[PlayerInputPermissionManager] 未找到 GameFlowController！权限检查将失效");
        }
        else if (showDebugInfo)
        {
            Debug.Log("[PlayerInputPermissionManager] 已连接到 GameFlowController");
        }
    }
    
    protected override void OnManagerDestroyed()
    {
        // 清理引用
        gameFlowController = null;
    }
    
    #endregion
    
    #region 权限检查接口
    
    /// <summary>
    /// 检查是否允许处理任何输入（顶层游戏阶段权限）
    /// </summary>
    /// <returns>true = 在玩家回合，可以处理输入；false = 不在玩家回合，禁止输入</returns>
    public bool CanProcessInputInCurrentPhase()
    {
        // 检查GameFlowController是否存在
        if (gameFlowController == null)
        {
            Debug.LogError("[PlayerInputPermissionManager] GameFlowController 为 null！权限检查失效，默认拒绝输入");
            return false;
        }
        
        // 检查当前是否为玩家阶段
        bool canProcess = gameFlowController.IsPlayerPhase;
        
        if (!canProcess && showDebugInfo)
        {
            Debug.Log($"[PlayerInputPermissionManager] 不在玩家回合（当前: {gameFlowController.CurrentState}），拒绝输入");
        }
        
        return canProcess;
    }
    
    /// <summary>
    /// ✅ 检查是否允许选择角色（综合权限：阶段 + 发射次数）
    /// </summary>
    /// <returns>true = 可以选择角色；false = 禁止选择</returns>
    public bool CanSelectCharacter()
    {
        // 条件1：必须在玩家回合
        if (gameFlowController == null || !gameFlowController.IsPlayerPhase)
        {
            if (showDebugInfo)
            {
                string reason = gameFlowController == null ? 
                    "GameFlowController 为 null" : 
                    $"当前不在玩家回合（{gameFlowController.CurrentState}）";
                Debug.Log($"[PlayerInputPermissionManager] CanSelectCharacter = false: {reason}");
            }
            return false;
        }
        
        // 条件2：必须有剩余发射次数
        if (PlayerTurnManager.Instance == null)
        {
            Debug.LogError("[PlayerInputPermissionManager] PlayerTurnManager.Instance 为 null！");
            return false;
        }
        
        int remainingLaunches = PlayerTurnManager.Instance.RemainingLaunches;
        if (remainingLaunches <= 0)
        {
            // 发射次数为0时拒绝（频繁操作，不输出日志）
            return false;
        }
        
        // 所有条件通过
        return true;
    }
    
    /// <summary>
    /// 检查是否允许WASD移动（已废弃，多角色系统不使用WASD移动）
    /// </summary>
    [System.Obsolete("多角色系统已移除WASD移动功能")]
    public bool CanMoveInCurrentSubPhase()
    {
        return false;
    }
    
    /// <summary>
    /// 检查是否允许蓄力输入（已废弃，PlayerInputHandler 专用，现在不使用）
    /// </summary>
    [System.Obsolete("该方法为 PlayerInputHandler 专用，多角色系统已改用 GlobalInputManager")]
    public bool CanChargeInCurrentSubPhase()
    {
        return false;
    }
    
    #endregion
    
    #region 公共查询接口
    
    /// <summary>
    /// 获取当前游戏流程状态（用于调试）
    /// </summary>
    public string GetCurrentPhaseInfo()
    {
        if (gameFlowController == null)
            return "GameFlowController 未初始化";
        
        return $"当前阶段: {gameFlowController.CurrentState}, " +
               $"是玩家回合: {gameFlowController.IsPlayerPhase}, " +
               $"是敌人回合: {gameFlowController.IsEnemyPhase}";
    }
    
    #endregion
}
