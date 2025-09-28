using UnityEngine;

/// <summary>
/// 玩家状态机 - 事件驱动的状态管理系统
/// 
/// 【核心职责】：
/// - 管理玩家的三种状态：Idle（空闲）、Charging（蓄力）、Moving（运动）
/// - 响应蓄力事件进行状态转换
/// - 通过事件系统与其他组件通信
/// - 协调游戏流程状态变化
/// 
/// 【状态定义】：
/// - Idle: 可以移动和开始蓄力
/// - Charging: 蓄力中，显示瞄准线
/// - Moving: 物理发射移动中，不能进行任何操作
/// 
/// 【设计原则】：
/// - 事件驱动架构，松耦合通信
/// - 单一职责：只管理状态转换
/// - 通过GameEventBus响应蓄力事件
/// - 可独立测试和扩展
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
    private ChargeSystem chargeSystem;
    
    // 事件
    public System.Action<PlayerState, PlayerState> OnStateChanged;
    
    void Start()
    {
        // 获取组件引用
        playerCore = GetComponent<PlayerCore>();
        chargeSystem = GetComponent<ChargeSystem>();
        
        // 订阅蓄力事件
        GameEventBus.OnChargingStarted += OnChargingStarted;
        GameEventBus.OnChargingStopped += OnChargingStopped;
        
        // 初始化状态
        currentState = initialState;
        EnterState(currentState);
        
        if (showDebugInfo)
        {
            Debug.Log($"PlayerStateMachine: 初始化完成，初始状态: {currentState}");
        }
    }
    
    void OnDestroy()
    {
        // 取消订阅蓄力事件
        GameEventBus.OnChargingStarted -= OnChargingStarted;
        GameEventBus.OnChargingStopped -= OnChargingStopped;
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
        // 现在由PlayerPhaseController来管理状态切换
        // 这里只触发事件，让PlayerPhaseController监听
        if (showDebugInfo)
        {
            Debug.Log($"PlayerStateMachine: 状态变化 {fromState} -> {toState}，通知PlayerPhaseController");
        }
        
        // 触发状态变化事件，PlayerPhaseController会监听这个事件
        OnStateChanged?.Invoke(toState, fromState);
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
                // 清理蓄力状态 - 现在由事件驱动，不需要直接调用
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
                // 进入蓄力状态 - 现在由事件驱动，不需要直接调用
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
        // 检查是否在物理移动（排除WASD移动）
        if (playerCore != null && playerCore.IsPhysicsMoving() && !playerCore.IsMoving())
        {
            SwitchToState(PlayerState.Moving);
        }
    }
    
    
    
    #endregion
    
    #region 外部接口
    
    /// <summary>
    /// 蓄力开始事件处理
    /// </summary>
    void OnChargingStarted()
    {
        if (currentState == PlayerState.Idle)
        {
            SwitchToState(PlayerState.Charging);
        }
    }
    
    /// <summary>
    /// 蓄力停止事件处理
    /// </summary>
    void OnChargingStopped()
    {
        if (currentState == PlayerState.Charging)
        {
            // 获取充能进度
            float chargingPower = chargeSystem != null ? chargeSystem.GetChargingPower() : 0f;
            
            
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
            // 发布蓄力重置事件
            GameEventBus.PublishChargingReset();
            
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
