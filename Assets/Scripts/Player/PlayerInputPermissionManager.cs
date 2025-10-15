using UnityEngine;

/// <summary>
/// 玩家输入权限管理器 - 统一管理输入权限检查逻辑
/// 
/// 【核心职责】：
/// - 检查是否允许WASD移动（暂时禁用）
/// - 检查是否允许蓄力输入（只在Idle状态）
/// - 监听PlayerStateMachine状态变化
/// </summary>
public class PlayerInputPermissionManager : MonoBehaviour
{
    
    // 组件引用（与原来PlayerInputHandler相同的获取方式）
    private GameFlowController gameFlowController;
    private PlayerStateMachine playerStateMachine;
    
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
        playerStateMachine = FindFirstObjectByType<PlayerStateMachine>();
    }
    
    /// <summary>
    /// 检查是否允许处理任何输入（顶层游戏阶段权限）
    /// </summary>
    public bool CanProcessInputInCurrentPhase()
    {
        // 检查GameFlowController是否存在且当前为玩家阶段
        return gameFlowController != null && gameFlowController.IsPlayerPhase;
    }
    
    /// <summary>
    /// 检查是否允许WASD移动（只在Transition阶段允许）
    /// </summary>
    public bool CanMoveInCurrentSubPhase()
    {
        // // 检查PlayerPhaseController是否存在且当前为Transition子阶段
        // return playerPhaseController != null && 
        //        playerPhaseController.CurrentSubPhase == PlayerPhaseController.PlayerSubPhase.Transition;
        //暂时完全留空
        return false;
    }
    
    /// <summary>
    /// 检查是否允许蓄力输入（只在Idle状态允许）
    /// </summary>
    public bool CanChargeInCurrentSubPhase()
    {
        // 检查PlayerStateMachine是否存在且当前为Idle状态
        return playerStateMachine != null && 
               playerStateMachine.CurrentState == PlayerStateMachine.PlayerState.Idle;
    }
}
