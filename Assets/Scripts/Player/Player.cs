using UnityEngine;

/// <summary>
/// 玩家总控制器 - 事件驱动的组件协调器
/// 
/// 【核心职责】：
/// - 作为Player系统的总协调器和管理器
/// - 自动初始化和配置所有Player子组件
/// - 提供统一的对外接口和组件访问
/// - 响应状态变化事件进行协调
/// 
/// 【管理组件】：
/// - PlayerCore: 核心业务逻辑（物理、蓄力、血量）
/// - PlayerStateMachine: 玩家状态管理（事件驱动）
/// - PlayerInputHandler: 输入处理（事件驱动）
/// - PlayerMovementController: 移动控制（事件驱动）
/// 
/// 【设计原则】：
/// - 事件驱动架构，松耦合通信
/// - 使用协调器模式，不直接处理业务逻辑
/// - 自动组件管理，减少手动配置
/// - 通过事件系统响应状态变化
/// </summary>
public class Player : MonoBehaviour
{
    [Header("数据设置")]
    public PlayerData playerData; // 玩家配置数据
    
    [Header("组件引用")]
    [SerializeField] private PlayerCore playerCore;
    [SerializeField] private PlayerStateMachine stateMachine;
    [SerializeField] private PlayerInputHandler inputHandler;
    [SerializeField] private PlayerMovementController movementController;
    
    [Header("调试")]
    [SerializeField] private bool showDebugInfo = true;
    
    void Start()
    {
        InitializePlayer();
    }
    
    /// <summary>
    /// 初始化玩家
    /// </summary>
    void InitializePlayer()
    {
        // 获取或添加组件
        playerCore = GetComponent<PlayerCore>();
        stateMachine = GetComponent<PlayerStateMachine>();
        inputHandler = GetComponent<PlayerInputHandler>();
        movementController = GetComponent<PlayerMovementController>();
        
        // 确保所有组件都存在
        if (playerCore == null)
        {
            playerCore = gameObject.AddComponent<PlayerCore>();
            Debug.LogWarning("Player: 自动添加PlayerCore组件");
        }
        
        // 设置数据到PlayerCore
        if (playerCore != null)
        {
            playerCore.playerData = playerData;
        }
        
        if (stateMachine == null)
        {
            stateMachine = gameObject.AddComponent<PlayerStateMachine>();
            Debug.LogWarning("Player: 自动添加PlayerStateMachine组件");
        }
        
        if (inputHandler == null)
        {
            inputHandler = gameObject.AddComponent<PlayerInputHandler>();
            Debug.LogWarning("Player: 自动添加PlayerInputHandler组件");
        }
        
        if (movementController == null)
        {
            movementController = gameObject.AddComponent<PlayerMovementController>();
            Debug.LogWarning("Player: 自动添加PlayerMovementController组件");
        }
        
        // 订阅状态变化事件
        if (stateMachine != null)
        {
            stateMachine.OnStateChanged += OnPlayerStateChanged;
        }
        
        if (showDebugInfo)
        {
            Debug.Log("Player: 初始化完成，所有组件已准备就绪");
        }
    }
    
    /// <summary>
    /// 玩家状态变化事件处理
    /// </summary>
    void OnPlayerStateChanged(PlayerStateMachine.PlayerState newState, PlayerStateMachine.PlayerState oldState)
    {
        if (showDebugInfo)
        {
            Debug.Log($"Player: 状态变化 {oldState} -> {newState}");
        }
        
        // 根据状态变化执行相应逻辑
        switch (newState)
        {
            case PlayerStateMachine.PlayerState.Idle:
                OnEnterIdleState();
                break;
            case PlayerStateMachine.PlayerState.Charging:
                OnEnterChargingState();
                break;
            case PlayerStateMachine.PlayerState.Moving:
                OnEnterMovingState();
                break;
        }
    }
    
    /// <summary>
    /// 进入空闲状态
    /// </summary>
    void OnEnterIdleState()
    {
        if (showDebugInfo)
        {
            Debug.Log("Player: 进入空闲状态 - 可以移动和蓄力");
        }
    }
    
    /// <summary>
    /// 进入蓄力状态
    /// </summary>
    void OnEnterChargingState()
    {
        if (showDebugInfo)
        {
            Debug.Log("Player: 进入蓄力状态 - 显示瞄准线，停止移动");
        }
        
    }
    
    /// <summary>
    /// 进入运动状态
    /// </summary>
    void OnEnterMovingState()
    {
        if (showDebugInfo)
        {
            Debug.Log("Player: 进入运动状态 - 球在物理移动中");
        }
    }
    
    #region 公共接口
    
    /// <summary>
    /// 获取玩家核心组件
    /// </summary>
    public PlayerCore GetPlayerCore()
    {
        return playerCore;
    }
    
    /// <summary>
    /// 获取状态机组件
    /// </summary>
    public PlayerStateMachine GetStateMachine()
    {
        return stateMachine;
    }
    
    /// <summary>
    /// 获取输入处理器组件
    /// </summary>
    public PlayerInputHandler GetInputHandler()
    {
        return inputHandler;
    }
    
    /// <summary>
    /// 获取移动控制器组件
    /// </summary>
    public PlayerMovementController GetMovementController()
    {
        return movementController;
    }
    
    /// <summary>
    /// 重置玩家状态
    /// </summary>
    public void ResetPlayer()
    {
        if (stateMachine != null)
        {
            // 重置状态机到空闲状态
            stateMachine.SwitchToState(PlayerStateMachine.PlayerState.Idle);
        }
        
        if (playerCore != null)
        {
            playerCore.ResetForNewTurn();
        }
        
        if (showDebugInfo)
        {
            Debug.Log("Player: 玩家状态已重置");
        }
    }
    
    #endregion
    
    #region 调试信息
    
    void OnGUI()
    {
        if (showDebugInfo && stateMachine != null)
        {
            // 显示当前状态
            GUILayout.BeginArea(new Rect(10, 10, 200, 100));
            GUILayout.Label($"Player State: {stateMachine.CurrentState}");
            
            if (playerCore != null)
            {
                GUILayout.Label($"Charging: {playerCore.ChargingProgress:F1}%");
                GUILayout.Label($"Speed: {playerCore.GetSpeed():F2}");
            }
            
            GUILayout.EndArea();
        }
    }
    
    #endregion
}
