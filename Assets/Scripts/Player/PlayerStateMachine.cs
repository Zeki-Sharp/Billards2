using UnityEngine;

/// <summary>
/// 玩家状态机 - 管理玩家的状态转换和逻辑
/// 
/// 【核心职责】：
/// - 管理玩家的三种状态：Idle（空闲）、Charging（蓄力）、Moving（运动）
/// - 处理状态间的转换逻辑和条件判断
/// - 协调PlayerCore和AimController的UI显示
/// - 通过直接引用与GameFlowController通信
/// 
/// 【状态定义】：
/// - Idle: 可以移动和开始蓄力
/// - Charging: 不能移动，显示瞄准线，更新蓄力进度
/// - Moving: 物理发射移动中，不能进行任何操作
/// 
/// 【设计原则】：
/// - 单一职责：只管理玩家状态，不处理具体业务逻辑
/// - 状态驱动：根据当前状态决定允许的操作
/// - 直接通信：通过引用直接通知GameFlowController
/// - 简单高效：避免复杂的事件系统
/// </summary>
public class PlayerStateMachine : MonoBehaviour
{
    /// <summary>
    /// 玩家状态枚举
    /// </summary>
    public enum PlayerState
    {
        Idle,        // 空闲状态：可以移动、可以开始蓄力
        Charging,    // 蓄力状态：不能移动、显示瞄准线、更新蓄力进度
        Moving       // 运动状态：物理发射移动中，不能进行任何操作
    }
    
    [Header("状态设置")]
    [SerializeField] private PlayerState initialState = PlayerState.Idle;
    [SerializeField] private bool showDebugInfo = true;
    
    // 当前状态
    private PlayerState currentState;
    
    // 组件引用
    private PlayerCore playerCore;
    private AimController aimController;
    private GameFlowController gameFlowController;
    private ChargeSystem chargeSystem;
    private TransitionManager transitionManager;
    
    // 事件
    public System.Action<PlayerState, PlayerState> OnStateChanged;
    
    void Start()
    {
        // 获取组件引用
        playerCore = GetComponent<PlayerCore>();
        aimController = FindFirstObjectByType<AimController>();
        gameFlowController = GameFlowController.Instance;
        chargeSystem = GetComponent<ChargeSystem>();
        transitionManager = FindFirstObjectByType<TransitionManager>();
        
        
        // 初始化状态
        currentState = initialState;
        EnterState(currentState);
        
        if (showDebugInfo)
        {
            Debug.Log($"PlayerStateMachine: 初始化完成，初始状态: {currentState}");
        }
    }
    
    void Update()
    {
        UpdateCurrentState();
    }
    
    #region 状态管理
    
    /// <summary>
    /// 切换到指定状态
    /// </summary>
    public void SwitchToState(PlayerState newState)
    {
        if (currentState == newState) return;
        
        PlayerState oldState = currentState;
        ExitState(oldState);
        currentState = newState;
        EnterState(newState);
        
        // 通知GameFlowController状态变化
        NotifyGameFlowStateChange(oldState, newState);
        
        OnStateChanged?.Invoke(newState, oldState);
        
        if (showDebugInfo)
        {
            Debug.Log($"PlayerStateMachine: 状态切换 {oldState} -> {newState}");
        }
    }
    
    /// <summary>
    /// 通知GameFlowController状态变化
    /// </summary>
    void NotifyGameFlowStateChange(PlayerState fromState, PlayerState toState)
    {
        if (gameFlowController == null) return;
        
        // 根据状态变化通知GameFlowController
        if (toState == PlayerState.Charging && fromState == PlayerState.Idle)
        {
            // 从空闲到蓄力：请求进入蓄力状态
            gameFlowController.RequestChargingState();
        }
        else if (toState == PlayerState.Moving && fromState == PlayerState.Charging)
        {
            // 从蓄力到移动：GameFlow不变，不需要通知
        }
        else if (toState == PlayerState.Idle && fromState == PlayerState.Moving)
        {
            Debug.Log("PlayerStateMachine: 从移动到空闲：请求进入过渡状态");
            // 从移动到空闲：请求进入过渡状态
            gameFlowController.RequestTransitionState();
        }
    }
    
    /// <summary>
    /// 更新当前状态逻辑
    /// </summary>
    void UpdateCurrentState()
    {
        switch (currentState)
        {
            case PlayerState.Idle:
                UpdateIdleState();
                break;
            case PlayerState.Charging:
                
                break;
            case PlayerState.Moving:
                
                break;
        }
    }
    
    /// <summary>
    /// 退出状态
    /// </summary>
    void ExitState(PlayerState state)
    {
        switch (state)
        {
            case PlayerState.Idle:
                // 清理空闲状态
                break;
            case PlayerState.Charging:
                // 清理蓄力状态
                if (chargeSystem != null)
                {
                    chargeSystem.StopCharging();
                }
                break;
            case PlayerState.Moving:
                // 清理运动状态
                break;
        }
    }
    
    /// <summary>
    /// 进入状态
    /// </summary>
    void EnterState(PlayerState state)
    {
        switch (state)
        {
            case PlayerState.Idle:
                // 进入空闲状态
                break;
            case PlayerState.Charging:
                // 进入蓄力状态
                if (chargeSystem != null)
                {
                    chargeSystem.StartCharging();
                }
                
                // 显示蓄力UI
                if (aimController != null)
                {
                    aimController.ShowChargingUI();
                }
                break;
            case PlayerState.Moving:
                // 进入运动状态
                // 状态变化会通过事件通知GameFlowController
                break;
        }
    }
    
    #endregion
    
    #region 状态逻辑
    
    /// <summary>
    /// 更新空闲状态
    /// </summary>
    void UpdateIdleState()
    {
        // 检查是否在物理移动（排除WASD微调移动）
        if (playerCore != null && playerCore.IsPhysicsMoving() && !playerCore.IsMicroMoving())
        {
            SwitchToState(PlayerState.Moving);
        }
    }
    
    
    
    #endregion
    
    #region 外部接口
    
    /// <summary>
    /// 开始蓄力（由输入系统调用）
    /// </summary>
    public void StartCharging()
    {
        if (currentState == PlayerState.Idle)
        {
            // 如果正在WASD移动，先停止移动
            PlayerMovementController movementController = GetComponent<PlayerMovementController>();
            if (movementController != null)
            {
                movementController.StopWASDMovement();
            }
            
            SwitchToState(PlayerState.Charging);
        }
    }
    
    /// <summary>
    /// 发射蓄力（由输入系统调用）
    /// </summary>
    public void LaunchCharged()
    {
        if (currentState == PlayerState.Charging)
        {
            // 获取充能进度
            float chargingPower = chargeSystem != null ? chargeSystem.GetChargingPower() : 0f;
            
            // 设置transition时长
            if (transitionManager != null)
            {
                transitionManager.SetTransitionDurationFromCharging(chargingPower);
            }
            
            // 隐藏蓄力UI
            if (aimController != null)
            {
                aimController.HideChargingUI();
            }
            
            // 停止蓄力（但不重置，因为要发射）
            if (chargeSystem != null)
            {
                chargeSystem.StopCharging();
            }
            
            // 发射
            if (playerCore != null)
            {
                playerCore.LaunchCharged();
            }
            
            // 切换到运动状态
            SwitchToState(PlayerState.Moving);
            
            if (showDebugInfo)
            {
                Debug.Log($"PlayerStateMachine: 发射完成，充能进度: {chargingPower:F2}");
            }
        }
    }
    
    /// <summary>
    /// 球停止运动（由PlayerCore调用）
    /// </summary>
    public void OnBallStopped()
    {
        if (currentState == PlayerState.Moving)
        {
            // 重置蓄力系统
            if (chargeSystem != null)
            {
                chargeSystem.ResetCharging();
            }
            
            // 隐藏蓄力UI
            if (aimController != null)
            {
                aimController.HideChargingUI();
            }
            
            SwitchToState(PlayerState.Idle);
        }
    }
    
    #endregion
    
    
    #region 公共属性
    
    /// <summary>
    /// 当前玩家状态
    /// </summary>
    public PlayerState CurrentState => currentState;
    
    /// <summary>
    /// 是否正在蓄力
    /// </summary>
    public bool IsCharging => currentState == PlayerState.Charging;
    
    /// <summary>
    /// 是否正在移动
    /// </summary>
    public bool IsMoving => currentState == PlayerState.Moving;
    
    /// <summary>
    /// 是否空闲
    /// </summary>
    public bool IsIdle => currentState == PlayerState.Idle;
    
    #endregion
    
    #region 事件处理
    
    
    #endregion
    
    #region 组件设置
    
    /// <summary>
    /// 设置蓄力系统引用
    /// </summary>
    public void SetChargeSystem(ChargeSystem system)
    {
        chargeSystem = system;
        
        
        Debug.Log($"PlayerStateMachine: 设置蓄力系统引用为 {system.name}");
    }
    
    #endregion
}
