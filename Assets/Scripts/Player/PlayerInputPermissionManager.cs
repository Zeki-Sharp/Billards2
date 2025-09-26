using UnityEngine;

/// <summary>
/// 玩家输入权限管理器 - 统一管理输入权限检查逻辑
/// 
/// 【核心职责】：
/// - 检查是否允许WASD移动（只在Transition阶段）
/// - 检查是否允许蓄力输入（只在Normal阶段）
/// - 使用与原来PlayerInputHandler相同的初始化逻辑
/// </summary>
public class PlayerInputPermissionManager : MonoBehaviour
{
    [Header("调试")]
    [SerializeField] private bool showDebugInfo = true;
    
    // 组件引用（与原来PlayerInputHandler相同的获取方式）
    private GameFlowController gameFlowController;
    private PlayerPhaseController playerPhaseController;
    
    void Start()
    {
        // 使用与原来PlayerInputHandler相同的初始化逻辑
        InitializeManager();
    }
    
    /// <summary>
    /// 初始化权限管理器
    /// </summary>
    void InitializeManager()
    {
        // 获取组件引用（与原来PlayerInputHandler相同的方式）
        gameFlowController = GameFlowController.Instance;
        playerPhaseController = PlayerPhaseController.Instance;
        
        if (showDebugInfo)
        {
            Debug.Log("PlayerInputPermissionManager: 初始化完成");
        }
    }
    
    /// <summary>
    /// 检查是否允许WASD移动（只在Transition阶段允许）
    /// </summary>
    public bool CanMoveInCurrentSubPhase()
    {
        // 检查玩家子阶段（顶层阶段权限已在HandleInput()中检查）
        if (playerPhaseController == null)
        {
            if (showDebugInfo)
            {
                Debug.LogWarning("PlayerInputPermissionManager: PlayerPhaseController实例为空！");
            }
            return false;
        }
        
        // 只在Transition子阶段允许WASD移动
        bool canMove = playerPhaseController.CurrentSubPhase == PlayerPhaseController.PlayerSubPhase.Transition;
        
        if (showDebugInfo && !canMove)
        {
            Debug.Log($"PlayerInputPermissionManager: 当前子阶段 {playerPhaseController.CurrentSubPhase} 不允许WASD移动");
        }
        
        return canMove;
    }
    
    /// <summary>
    /// 检查是否允许蓄力输入（只在Normal阶段允许）
    /// </summary>
    public bool CanChargeInCurrentSubPhase()
    {
        // 检查玩家子阶段（顶层阶段权限已在HandleInput()中检查）
        if (playerPhaseController == null)
        {
            if (showDebugInfo)
            {
                Debug.LogWarning("PlayerInputPermissionManager: PlayerPhaseController实例为空！");
            }
            return false;
        }
        
        // 只在Normal子阶段允许蓄力输入
        bool canCharge = playerPhaseController.CurrentSubPhase == PlayerPhaseController.PlayerSubPhase.Normal;
        
        if (showDebugInfo && !canCharge)
        {
            Debug.Log($"PlayerInputPermissionManager: 当前子阶段 {playerPhaseController.CurrentSubPhase} 不允许蓄力输入");
        }
        
        return canCharge;
    }
}
