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
/// - PhaseStart: 回合开始，重置状态
/// - Playing: 游玩中，委托给 PlayerStateMachine（Idle → Charging → Moving → MovingEnd）
/// - PhaseEnd: 回合结束，清理并切换到敌人回合
/// 
/// 【设计原则】：
/// - 只做流程编排，不做具体执行
/// - 单向依赖：调用 PlayerStateMachine，不监听其状态变化
/// - 通过 OnPlayingComplete 事件接收完成通知
/// - 所有 Phase 都是必然流程，可选能力属于技能系统
/// </summary>
public class PlayerPhaseController : SingletonManager<PlayerPhaseController>
{
    
    /// <summary>
    /// 玩家阶段枚举（重构版 - 只保留必然流程）
    /// </summary>
    public enum PlayerPhase
    {
        PhaseStart,     // 回合开始：重置状态
        Playing,        // 游玩中：委托给 PlayerStateMachine
        PhaseEnd        // 回合结束：清理、切换到敌人回合
    }
    
    
    [Header("调试")]
    [SerializeField] private bool showDebugInfo = true;
    
    // 当前阶段
    private PlayerPhase currentPhase = PlayerPhase.PhaseStart;
    
    // 组件引用
    private PlayerStateMachine playerStateMachine;
    
    // 事件
    public System.Action<PlayerPhase> OnPhaseChanged;
    public System.Action OnPlayerPhaseComplete; // 整个玩家阶段完成事件
    
    // 公共属性
    public PlayerPhase CurrentPhase => currentPhase;
    
    #region SingletonManager 重写
    
    protected override bool PersistAcrossScenes => false;
    
    protected override void OnManagerCreated()
    {
        // PlayerPhaseController 初始化逻辑在延迟协程中
    }
    
    #endregion
    
    void Start()
    {
        // 延迟初始化，确保PlayerStateMachine已经被创建
        StartCoroutine(DelayedInitialization());
    }
    
    System.Collections.IEnumerator DelayedInitialization()
    {
        // 等待一帧，确保所有组件都已创建
        yield return null;
        
        InitializeController();
    }
    
    void InitializeController()
    {
        // 获取组件引用
        playerStateMachine = FindFirstObjectByType<PlayerStateMachine>();
        
        // 订阅 PlayerStateMachine 的 Playing 完成事件
        if (playerStateMachine != null)
        {
            playerStateMachine.OnPlayingComplete += OnPlayingComplete;
            
            if (showDebugInfo)
            {
                Debug.Log("PlayerPhaseController: 初始化完成，已订阅 OnPlayingComplete 事件");
            }
        }
        else
        {
            Debug.LogError("PlayerPhaseController: 未找到 PlayerStateMachine！");
        }
    }
    
    protected override void OnDestroy()
    {
        base.OnDestroy();
        
        if (playerStateMachine != null)
        {
            playerStateMachine.OnPlayingComplete -= OnPlayingComplete;
        }
    }
    
    /// <summary>
    /// 开始玩家阶段（由GameFlowController调用）
    /// </summary>
    public void StartPlayerPhase()
    {
        if (showDebugInfo)
        {
            Debug.Log("PlayerPhaseController: 开始玩家阶段");
        }
        
        // 确保PlayerStateMachine引用正确
        if (playerStateMachine == null)
        {
            playerStateMachine = FindFirstObjectByType<PlayerStateMachine>();
            if (playerStateMachine != null)
            {
                playerStateMachine.OnPlayingComplete += OnPlayingComplete;
            }
            else
            {
                Debug.LogError("PlayerPhaseController: 在StartPlayerPhase时仍未找到PlayerStateMachine！");
                return;
            }
        }
        
        // 从 PhaseStart 开始
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
        // 清理阶段特定逻辑
        if (showDebugInfo)
        {
            Debug.Log($"PlayerPhaseController: 退出阶段 {phase}");
        }
    }
    
    /// <summary>
    /// 执行 PhaseStart 阶段
    /// </summary>
    void ExecutePhaseStart()
    {
        if (showDebugInfo)
        {
            Debug.Log("PlayerPhaseController: 执行 PhaseStart - 重置状态");
        }
        
        // 重置玩家状态（如果需要）
        // 目前自动进入 Playing 阶段
        SwitchToPhase(PlayerPhase.Playing);
    }
    
    /// <summary>
    /// 执行 Playing 阶段（委托给 PlayerStateMachine）
    /// </summary>
    void ExecutePlaying()
    {
        if (showDebugInfo)
        {
            Debug.Log("PlayerPhaseController: 执行 Playing - 委托给 PlayerStateMachine");
        }
        
        // 调用 PlayerStateMachine 开始游玩
        if (playerStateMachine != null)
        {
            playerStateMachine.StartPlaying();
            
            // 发布 Playing 阶段开始事件（PlayerStateMachine 已准备好，状态为 Idle）
            GameEventBus.PublishPlayerPlayingPhaseStarted();
            
            if (showDebugInfo)
            {
                Debug.Log("PlayerPhaseController: 已发布 PlayerPlayingPhaseStarted 事件");
            }
        }
        else
        {
            Debug.LogError("PlayerPhaseController: PlayerStateMachine 为 null！");
        }
        
        // 等待 PlayerStateMachine.OnPlayingComplete 事件
    }
    
    /// <summary>
    /// Playing 阶段完成回调（由 PlayerStateMachine 通知）
    /// </summary>
    void OnPlayingComplete()
    {
        if (showDebugInfo)
        {
            Debug.Log("PlayerPhaseController: Playing 阶段完成，进入 PhaseEnd");
        }
        
        SwitchToPhase(PlayerPhase.PhaseEnd);
    }
    
    /// <summary>
    /// 执行 PhaseEnd 阶段
    /// </summary>
    void ExecutePhaseEnd()
    {
        if (showDebugInfo)
        {
            Debug.Log("PlayerPhaseController: 执行 PhaseEnd - 清理并切换到敌人回合");
        }
        
        // 清理临时状态
        
        // 通知 GameFlowController 玩家阶段完成
        OnPlayerPhaseComplete?.Invoke();
    }
}
