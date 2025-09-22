using UnityEngine;
using MoreMountains.Tools;

/// <summary>
/// 游戏流程控制器 - 管理Normal、Charging、Transition三状态
/// 
/// 【核心职责】：
/// - 管理游戏全局流程状态（Normal/Charging/Transition）
/// - 协调时停系统、过渡系统、敌人系统等
/// - 通过直接引用与Player系统通信
/// 
/// 【设计原则】：
/// - 不直接检测玩家输入（由PlayerInputHandler处理）
/// - 通过直接引用与PlayerStateMachine通信
/// - 专注于游戏流程逻辑，不处理具体的玩家行为
/// - 简单高效：避免复杂的事件系统
/// </summary>
public class GameFlowController : MonoBehaviour
{
    public static GameFlowController Instance { get; private set; }
    
    /// <summary>
    /// 游戏流程状态枚举
    /// </summary>
    public enum GameFlowState
    {
        Normal,         // 正常游戏状态：玩家移动躲避，敌人移动+射击
        Charging,       // 蓄力时停状态：完全时停，玩家瞄准蓄力
        Transition,     // 过渡状态：玩家可移动，敌人和子弹仍时停，白球运动
        EnemyPhase      // 敌人阶段：玩家控制完全禁用，敌人正常行动
    }
    

    
    // 当前状态
    private GameFlowState currentState = GameFlowState.Normal;
    
    // 组件引用（由GameInitializer设置）
    private TransitionManager transitionManager;
    private PlayerStateMachine playerStateMachine;
    private PlayerCore playerCore;
    private EnemyPhaseController enemyPhaseController;

    
    // 事件（使用MM架构）
    public System.Action<GameFlowState> OnStateChanged;
    public System.Action OnGameStart;
    
    void Awake()
    {
        // 单例模式
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Debug.LogWarning("发现多个GameFlowController实例，销毁重复的实例");
            Destroy(gameObject);
            return;
        }
    }
    
    void OnDestroy()
    {
        // 取消事件订阅
        if (enemyPhaseController != null)
        {
            EnemyPhaseController.OnEnemyPhaseComplete -= OnEnemyPhaseComplete;
        }
    }
    
    void Update()
    {
    
    }
    
    
    #region 状态切换
    
    public void SwitchToNormalState()
    {
        if (currentState == GameFlowState.Normal) return;
        
        GameFlowState oldState = currentState;
        currentState = GameFlowState.Normal;
        
        // 触发状态变化事件
        OnStateChanged?.Invoke(currentState);
        
    }
    
    public void SwitchToChargingState()
    {
        if (currentState == GameFlowState.Charging) return;
        
        GameFlowState oldState = currentState;
        currentState = GameFlowState.Charging;
        
        // 触发状态变化事件
        OnStateChanged?.Invoke(currentState);
        
    }
    
    public void SwitchToTransitionState()
    {
        if (currentState == GameFlowState.Transition) return;
        
        GameFlowState oldState = currentState;
        currentState = GameFlowState.Transition;
        
        // 触发时停出场特效
        EffectEvent.Trigger("Timestop Out Effect", Vector3.zero);
        
        // 开始过渡
        if (transitionManager != null)
        {
            transitionManager.StartTransition();
        }
        
        // 触发状态变化事件
        OnStateChanged?.Invoke(currentState);
        
    }
    
    public void SwitchToEnemyPhase()
    {
        if (currentState == GameFlowState.EnemyPhase) return;
        
        GameFlowState oldState = currentState;
        currentState = GameFlowState.EnemyPhase;
        
        
        // 启动敌人阶段控制器
        if (enemyPhaseController != null)
        {
            enemyPhaseController.StartEnemyPhase();
        }
        else
        {
            Debug.LogWarning("GameFlowController: EnemyPhaseController 未设置！");
        }
        
        // 触发状态变化事件
        OnStateChanged?.Invoke(currentState);
        
    }
    
    #endregion
    
    #region 直接通信方法
    
    /// <summary>
    /// 请求进入蓄力状态（由PlayerStateMachine调用）
    /// </summary>
    public void RequestChargingState()
    {
        
        // 直接切换，因为PlayerStateMachine已经验证了条件
        SwitchToChargingState();
    }
    
    /// <summary>
    /// 请求进入过渡状态（由PlayerStateMachine调用）
    /// </summary>
    public void RequestTransitionState()
    {
        // 直接切换，因为PlayerStateMachine已经验证了条件
        SwitchToTransitionState();
    }
    
    #endregion
    
    #region 游戏逻辑
    
    public void StartNormalState()
    {
        SwitchToNormalState();
        OnGameStart?.Invoke();
    }
    
    
    #endregion
    
    #region 事件处理

    public void OnEnemyPhaseComplete()
    {
        // 敌人阶段完成，回到正常状态
        SwitchToNormalState();
    }
    
    #endregion
    
    #region 组件引用设置
    
    
    public void SetTransitionManager(TransitionManager manager)
    {
        transitionManager = manager;
    }
    
    
    #endregion
    
    #region 辅助方法
    
    string GetPreviousStateName()
    {
        switch (currentState)
        {
            case GameFlowState.Normal: return "Normal";
            case GameFlowState.Charging: return "Charging";
            case GameFlowState.Transition: return "Transition";
            case GameFlowState.EnemyPhase: return "EnemyPhase";
            default: return "Unknown";
        }
    }
    
    #endregion
    
    #region 公共属性
    
    public GameFlowState CurrentState => currentState;
    public bool IsNormalState => currentState == GameFlowState.Normal;
    public bool IsChargingState => currentState == GameFlowState.Charging;
    public bool IsTransitionState => currentState == GameFlowState.Transition;
    public bool IsEnemyPhase => currentState == GameFlowState.EnemyPhase;
    
    #endregion
    
    #region 组件引用设置
    
    public void SetPlayerStateMachine(PlayerStateMachine stateMachine)
    {
        playerStateMachine = stateMachine;
    }
    
    public void SetPlayerCore(PlayerCore core)
    {
        playerCore = core;
    }
    
    public void SetEnemyPhaseController(EnemyPhaseController controller)
    {
        // 取消之前的订阅
        if (enemyPhaseController != null)
        {
            EnemyPhaseController.OnEnemyPhaseComplete -= OnEnemyPhaseComplete;
        }
        
        enemyPhaseController = controller;
        
        // 订阅新的事件
        if (enemyPhaseController != null)
        {
            EnemyPhaseController.OnEnemyPhaseComplete += OnEnemyPhaseComplete;
        }
    }
    
    #endregion
}