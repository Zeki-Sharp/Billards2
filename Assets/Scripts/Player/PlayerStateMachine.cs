using UnityEngine;

/// <summary>
/// 玩家状态机 - 操作级别的状态管理系统
/// 
/// 【核心职责】：
/// - 管理玩家的四种操作状态：Idle、Charging、Moving、MovingEnd
/// - 响应蓄力事件和物理事件进行状态转换
/// - 通过事件系统与其他组件通信
/// - 通过 OnPlayingComplete 事件通知 PlayerPhaseController
/// 
/// 【状态定义】：
/// - Idle: 等待输入，可以开始蓄力
/// - Charging: 蓄力中，显示瞄准线
/// - Moving: 物理发射移动中
/// - MovingEnd: 球停止后的处理阶段，技能触发点（新增）
/// 
/// 【设计原则】：
/// - 事件驱动架构，松耦合通信
/// - 单一职责：只管理操作状态转换
/// - 通过GameEventBus响应输入和物理事件
/// - 为技能系统提供触发点（MovingEnd状态）
/// 
/// 【执行顺序】：BEFORE_CONTROLLER (-10)，早于 Controller 层
/// 【依赖】：同 GameObject 组件 (PlayerCore, ChargeSystem)
/// </summary>
[DefaultExecutionOrder(ManagerExecutionOrder.BEFORE_CONTROLLER)]
public class PlayerStateMachine : MonoBehaviour
{
    /// <summary>
    /// 玩家状态枚举
    /// </summary>
    public enum PlayerState
    {
        Idle,        // 空闲状态：等待输入
        Charging,    // 蓄力状态：不能移动、显示瞄准线、更新蓄力进度
        Moving,      // 运动状态：物理发射移动中，不能进行任何操作
        MovingEnd    // 移动结束状态：球停止后的处理阶段，技能触发点
    }
    
    [Header("状态设置")]
    [SerializeField] private PlayerState initialState = PlayerState.Idle;
    [SerializeField] private bool showDebugInfo = true;
    [SerializeField] [Tooltip("Moving状态超时时间（秒），防止力度过小时卡住")] 
    private float movingStateTimeout = 0.2f;
    
    // 当前状态
    private PlayerState currentState;
    
    // 组件引用
    private PlayerBehavior playerBehavior;
    private ChargeSystem chargeSystem;
    
    // Moving 状态相关
    private float movingStateEnterTime = 0f; // 进入 Moving 状态的时间
    
    // 事件
    public System.Action<PlayerState, PlayerState> OnStateChanged;
    public System.Action OnPlayingComplete; // Playing阶段完成事件（通知PlayerPhaseController）
    
    void Start()
    {
        // 获取组件引用
        playerBehavior = GetComponent<PlayerBehavior>();
        chargeSystem = GetComponent<ChargeSystem>();
        
        // 订阅蓄力事件
        GameEventBus.OnChargingStarted += OnChargingStarted;
        GameEventBus.OnChargingStopped += OnChargingStopped;
        
        // 订阅物理事件
        GameEventBus.OnBallStopped += OnBallStoppedHandler;
        
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
        
        // 取消订阅物理事件
        GameEventBus.OnBallStopped -= OnBallStoppedHandler;
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
        
        // 状态变化通过 OnStateChanged 事件通知监听者
        
        OnStateChanged?.Invoke(newState, oldState);
        
        if (showDebugInfo)
        {
            Debug.Log($"PlayerStateMachine: 状态切换 {oldState} -> {newState}");
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
                UpdateMovingState();
                break;
            case PlayerState.MovingEnd:
                // MovingEnd 状态由协程处理，不需要在 Update 中更新
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
            case PlayerState.MovingEnd:
                // 清理 MovingEnd 状态
                break;
        }
    }
    
    /// <summary>
    /// 进入状态
    /// </summary>
    void EnterState(PlayerState state)
    {
        // ✅ 新伤害系统：更新 Blackboard 攻击状态
        UpdateAttackState(state);
        
        switch (state)
        {
            case PlayerState.Idle:
                // 进入空闲状态
                break;
            case PlayerState.Charging:
                // 进入蓄力状态 - 现在由事件驱动，不需要直接调用
                break;
            case PlayerState.Moving:
                // 进入运动状态，记录进入时间
                movingStateEnterTime = Time.time;
                break;
            case PlayerState.MovingEnd:
                // 进入 MovingEnd 状态
                // 启动协程处理 MovingEnd 阶段
                StartCoroutine(ExecuteMovingEndPhase());
                break;
        }
    }
    
    /// <summary>
    /// 更新攻击状态（新伤害系统）
    /// </summary>
    void UpdateAttackState(PlayerState state)
    {
        if (playerBehavior == null) return;
        
        var blackboard = playerBehavior.GetBlackboard();
        
        if (state == PlayerState.Moving)
        {
            blackboard.Set("CanAttack", true);
            if (showDebugInfo)
            {
                Debug.Log("[PlayerStateMachine] ✅ 设置 CanAttack = true (Moving 状态)");
            }
        }
        else
        {
            blackboard.Set("CanAttack", false);
            if (showDebugInfo)
            {
                Debug.Log($"[PlayerStateMachine] ❌ 设置 CanAttack = false ({state} 状态)");
            }
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
        if (playerBehavior != null && playerBehavior.IsPhysicsMoving() && !playerBehavior.IsMoving())
        {
            SwitchToState(PlayerState.Moving);
        }
    }
    
    /// <summary>
    /// 更新运动状态
    /// </summary>
    void UpdateMovingState()
    {
        // 检查超时：如果进入 Moving 状态后一段时间，球仍未真正开始移动
        // 说明力度太小，球"原地停止"，应该直接完成流程
        float timeInMovingState = Time.time - movingStateEnterTime;
        
        if (timeInMovingState >= movingStateTimeout)
        {
            // 检查球是否真的在移动
            bool isBallActuallyMoving = playerBehavior != null && playerBehavior.IsPhysicsMoving();
            
            if (!isBallActuallyMoving)
            {
                // 球在超时时间内没有真正开始移动，认为是"原地停止"
                if (showDebugInfo)
                {
                    Debug.Log($"PlayerStateMachine: Moving状态超时 ({timeInMovingState:F2}s)，球未真正移动，视为原地停止，切换到MovingEnd");
                }
                
                // 发布蓄力重置事件
                GameEventBus.PublishChargingReset();
                
                // 直接切换到 MovingEnd 状态
                SwitchToState(PlayerState.MovingEnd);
            }
        }
    }
    
    /// <summary>
    /// 执行 MovingEnd 阶段
    /// </summary>
    System.Collections.IEnumerator ExecuteMovingEndPhase()
    {
        if (showDebugInfo)
        {
            Debug.Log("PlayerStateMachine: 进入 MovingEnd 阶段");
        }
        
        // 触发球停止攻击（攻击系统触发点）
        PlayerBehavior playerBehavior = FindFirstObjectByType<PlayerBehavior>();
        if (playerBehavior != null)
        {
            playerBehavior.HandleBallStoppedAttack();
            if (showDebugInfo)
            {
                Debug.Log("PlayerStateMachine: MovingEnd 阶段 - 触发球停止攻击");
            }
        }
        
        
        // 等待一小段时间（0.1秒，可配置）
        yield return new WaitForSeconds(0.1f);
        
        // 关闭时停特效
        TimeStopEffect timeStopEffect = FindFirstObjectByType<TimeStopEffect>();
        if (timeStopEffect != null)
        {
            timeStopEffect.SetIntensityImmediate(0f);
            if (showDebugInfo)
            {
                Debug.Log("PlayerStateMachine: MovingEnd 阶段 - 关闭时停特效");
            }
        }
        
        if (showDebugInfo)
        {
            Debug.Log("PlayerStateMachine: MovingEnd 阶段完成，通知 PlayerPhaseController");
        }
        
        // 通知 PlayerPhaseController Playing 阶段完成
        OnPlayingComplete?.Invoke();
    }
    
    #endregion
    
    #region 外部接口
    
    /// <summary>
    /// 开始 Playing 阶段（由 PlayerPhaseController 调用）
    /// </summary>
    public void StartPlaying()
    {
        // 确保从 Idle 状态开始
        if (currentState != PlayerState.Idle)
        {
            SwitchToState(PlayerState.Idle);
        }
        
        if (showDebugInfo)
        {
            Debug.Log("PlayerStateMachine: 开始 Playing 阶段");
        }
    }
    
    /// <summary>
    /// 蓄力开始事件处理
    /// </summary>
    void OnChargingStarted()
    {
        if (showDebugInfo)
        {
            Debug.Log($"PlayerStateMachine.OnChargingStarted: 收到蓄力开始事件，当前状态={currentState}");
        }
        
        if (currentState == PlayerState.Idle)
        {
            SwitchToState(PlayerState.Charging);
        }
        else
        {
            if (showDebugInfo)
            {
                Debug.LogWarning($"PlayerStateMachine.OnChargingStarted: 无法开始蓄力，当前状态不是Idle（当前={currentState}）");
            }
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
            if (playerBehavior != null)
            {
                playerBehavior.LaunchCharged();
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
    /// 球停止运动事件处理（直接订阅GameEventBus）
    /// </summary>
    private void OnBallStoppedHandler(BallPhysics ball)
    {
        // 检查是否是自己的球
        if (playerBehavior == null || !playerBehavior.IsMyBall(ball))
        {
            return;
        }
        
        if (currentState == PlayerState.Moving)
        {
            // 发布蓄力重置事件
            GameEventBus.PublishChargingReset();
            
            // 切换到 MovingEnd 状态（球停止后的处理阶段）
            SwitchToState(PlayerState.MovingEnd);
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
