using UnityEngine;
using MoreMountains.Tools;

/// <summary>
/// 玩家阶段控制器 - 回合级别的流程编排器（重构版）
/// 
/// 【核心职责】：
/// - 管理玩家回合的三个必然流程阶段：PhaseStart、Playing、PhaseEnd
/// - 委托 PlayerStateMachine 处理操作级别的状态管理
/// - 与 GameFlowController 协调回合切换
/// - 与 EnemyPhaseController 保持架构对称（都是流程编排器）
/// 
/// 【阶段定义】：
/// - None: 未开始，初始状态（在 StartPlayerPhase 调用前）
/// - PhaseStart: 回合开始，重置状态
/// - Playing: 游玩中，委托给 PlayerStateMachine（Idle → Charging → Moving → MovingEnd）
/// - PhaseEnd: 回合结束，清理并切换到敌人回合
/// 
/// 【设计原则】：
/// - 只做流程编排，不做具体执行
/// - 单向依赖：调用 PlayerStateMachine，不监听其状态变化
/// - 通过 OnPlayingComplete 事件接收完成通知
/// - 所有 Phase 都是必然流程，可选能力属于技能系统
/// 
/// 【执行顺序】：CONTROLLER 层 (0)
/// 【依赖】：SYSTEM 层, PlayerStateMachine (COMPONENT 层)
/// </summary>
[DefaultExecutionOrder(ManagerExecutionOrder.CONTROLLER)]
public class PlayerPhaseController : SingletonManager<PlayerPhaseController>
{
    
    /// <summary>
    /// 玩家阶段枚举（重构版 - 只保留必然流程）
    /// </summary>
    public enum PlayerPhase
    {
        None,           // 未开始：初始状态
        PhaseStart,     // 回合开始：重置状态
        Playing,        // 游玩中：委托给 PlayerStateMachine
        PhaseEnd        // 回合结束：清理、切换到敌人回合
    }
    
    
    [Header("调试")]
    [SerializeField] private bool showDebugInfo = true;
    
    // 当前阶段
    private PlayerPhase currentPhase = PlayerPhase.None;
    
    // 事件
    public System.Action<PlayerPhase> OnPhaseChanged;
    public System.Action OnPlayerPhaseComplete; // 整个玩家阶段完成事件
    
    // 公共属性
    public PlayerPhase CurrentPhase => currentPhase;
    
    #region SingletonManager 重写
    
    protected override bool PersistAcrossScenes => false;
    
    protected override void OnManagerCreated()
    {
        // 单例创建完成
    }
    
    #endregion
    
    void Start()
    {
        // ⚠️ 多角色系统改造：不再监听 PlayerStateMachine.OnPlayingComplete（因为有多个球）
        // 改为监听 PlayerTurnManager.OnTurnComplete
        
        if (PlayerTurnManager.Instance != null)
        {
            PlayerTurnManager.OnTurnComplete += OnTurnComplete;
        }
        else
        {
            Debug.LogWarning("PlayerPhaseController: 未找到 PlayerTurnManager，回合管理将不可用");
        }
    }
    
    protected override void OnDestroy()
    {
        base.OnDestroy();
        
        // ⚠️ 多角色系统改造：取消订阅 PlayerTurnManager 事件
        if (PlayerTurnManager.Instance != null)
        {
            PlayerTurnManager.OnTurnComplete -= OnTurnComplete;
        }
    }
    
    /// <summary>
    /// 开始玩家阶段（由GameFlowController调用）
    /// </summary>
    public void StartPlayerPhase()
    {
        SwitchToPhase(PlayerPhase.PhaseStart);
    }
    
    /// <summary>
    /// 切换到指定阶段
    /// </summary>
    void SwitchToPhase(PlayerPhase newPhase)
    {
        if (currentPhase == newPhase) return;
        
        PlayerPhase oldPhase = currentPhase;
        
        // 退出旧阶段
        ExitPhase(oldPhase);
        
        // 进入新阶段
        currentPhase = newPhase;
        EnterPhase(newPhase);
        
        // 触发事件
        OnPhaseChanged?.Invoke(newPhase);
        
        if (showDebugInfo)
        {
            Debug.Log($"PlayerPhaseController: 阶段切换 {oldPhase} → {newPhase}");
        }
    }
    
    /// <summary>
    /// 进入阶段
    /// </summary>
    void EnterPhase(PlayerPhase phase)
    {
        switch (phase)
        {
            case PlayerPhase.None:
                // None 状态不需要处理，只是初始状态
                break;
                
            case PlayerPhase.PhaseStart:
                ExecutePhaseStart();
                break;
                
            case PlayerPhase.Playing:
                ExecutePlaying();
                break;
                
            case PlayerPhase.PhaseEnd:
                ExecutePhaseEnd();
                break;
        }
    }
    
    /// <summary>
    /// 退出阶段
    /// </summary>
    void ExitPhase(PlayerPhase phase)
    {
        // 清理阶段特定逻辑（如果需要）
    }
    
    /// <summary>
    /// 执行 PhaseStart 阶段
    /// </summary>
    void ExecutePhaseStart()
    {
        // 重置玩家状态（如果需要）
        SwitchToPhase(PlayerPhase.Playing);
    }
    
    /// <summary>
    /// 执行 Playing 阶段（委托给 PlayerTurnManager）
    /// </summary>
    void ExecutePlaying()
    {
        // ✅ 发布玩家回合进行中状态
        GameEventBus.PublishGameFlowStateChanged(GameFlowState.PlayerPhasePlaying);
        
        // ✅ 多角色系统改造：委托 PlayerTurnManager 启动回合
        if (PlayerTurnManager.Instance != null)
        {
            PlayerTurnManager.Instance.StartTurn();
            GameEventBus.PublishPlayerPlayingPhaseStarted();
            
            if (showDebugInfo)
            {
                Debug.Log("PlayerPhaseController: Playing 阶段开始，等待玩家发射");
            }
        }
        else
        {
            Debug.LogError("PlayerPhaseController: PlayerTurnManager 为 null！");
        }
    }
    
    /// <summary>
    /// 回合完成回调（由 PlayerTurnManager 通知）
    /// </summary>
    void OnTurnComplete()
    {
        if (showDebugInfo)
        {
            Debug.Log("PlayerPhaseController: 收到回合完成事件，切换到 PhaseEnd");
        }
        
        SwitchToPhase(PlayerPhase.PhaseEnd);
    }
    
    /// <summary>
    /// 执行 PhaseEnd 阶段
    /// </summary>
    void ExecutePhaseEnd()
    {
        // 清理临时状态（如果需要）
        OnPlayerPhaseComplete?.Invoke();
    }
}
