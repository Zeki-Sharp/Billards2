using UnityEngine;
using MoreMountains.Tools;

/// <summary>
/// 游戏流程控制器 - 只管理顶层阶段切换
/// 
/// 【核心职责】：
/// - 管理顶层游戏阶段：PlayerPhase, EnemyPhase
/// - 协调两个阶段控制器的切换
/// - 不涉及任何子阶段的具体逻辑
/// 
/// 【设计原则】：
/// - 只管理顶层阶段切换
/// - 通过事件系统与阶段控制器通信
/// - 保持架构的对称性和清晰性
/// </summary>
public class GameFlowController : MonoBehaviour
{
    public static GameFlowController Instance { get; private set; }
    
    
    [Header("调试")]
    [SerializeField] private bool showDebugInfo = true;
    
    // 当前状态
    private GameFlowState currentState = GameFlowState.PlayerPhase;
    
    // 阶段控制器引用
    private PlayerPhaseController playerPhaseController;
    private EnemyPhaseController enemyPhaseController;
    
    // 事件
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
    
    void Start()
    {
        // 延迟初始化，确保所有组件都已创建
        StartCoroutine(DelayedInitialization());
    }
    
    System.Collections.IEnumerator DelayedInitialization()
    {
        // 等待几帧，确保所有控制器都已创建和初始化
        yield return new WaitForSeconds(0.1f);
        
        InitializeControllers();
        SubscribeToEvents();
        
        // 启动游戏
        OnGameStart?.Invoke();
        
        // 自动启动玩家阶段
        SwitchToPlayerPhase();
    }
    
    void OnDestroy()
    {
        UnsubscribeFromEvents();
    }
    
    void InitializeControllers()
    {
        // 获取阶段控制器引用
        playerPhaseController = PlayerPhaseController.Instance;
        enemyPhaseController = EnemyPhaseController.Instance;
        
    }
    
    void SubscribeToEvents()
    {
        // 订阅PlayerPhaseController的完成事件
        if (playerPhaseController != null)
        {
            playerPhaseController.OnPlayerPhaseComplete += SwitchToEnemyPhase;
        }
        else
        {
            Debug.LogError("GameFlowController: PlayerPhaseController为null，无法订阅事件！");
        }
        
        // 订阅EnemyPhaseController的完成事件
        if (enemyPhaseController != null)
        {
            EnemyPhaseController.OnEnemyPhaseComplete += SwitchToPlayerPhase;
        }
        else
        {
            Debug.LogError("GameFlowController: EnemyPhaseController为null，无法订阅事件！");
        }
    }
    
    void UnsubscribeFromEvents()
    {
        // 取消订阅PlayerPhaseController的完成事件
        if (playerPhaseController != null)
        {
            playerPhaseController.OnPlayerPhaseComplete -= SwitchToEnemyPhase;
        }
        
        // 取消订阅EnemyPhaseController的完成事件
        if (enemyPhaseController != null)
        {
            EnemyPhaseController.OnEnemyPhaseComplete -= SwitchToPlayerPhase;
        }
    }
    
    #region 阶段切换
    
    
    /// <summary>
    /// 切换到玩家阶段
    /// </summary>
    void SwitchToPlayerPhase()
    {
        if (currentState == GameFlowState.PlayerPhase) return;
        
        // 先发布敌人阶段结束事件
        GameEventBus.PublishGameFlowStateChanged(GameFlowState.EnemyPhaseEnd);
        
        currentState = GameFlowState.PlayerPhase;
        
        if (showDebugInfo)
        {
            Debug.Log("GameFlowController: 敌人阶段完成，切换到玩家阶段");
        }
        
        // 发布游戏流程状态变化事件
        GameEventBus.PublishGameFlowStateChanged(GameFlowState.PlayerPhase);
        
        // 启动玩家阶段
        if (playerPhaseController != null)
        {
            playerPhaseController.StartPlayerPhase();
        }
        else
        {
            Debug.LogError("GameFlowController: PlayerPhaseController 为 null！");
        }
        
        OnStateChanged?.Invoke(currentState);
    }
    
    /// <summary>
    /// 切换到敌人阶段
    /// </summary>
    void SwitchToEnemyPhase()
    {
        if (currentState == GameFlowState.EnemyPhase) return;
        
        // 先发布玩家阶段结束事件
        GameEventBus.PublishGameFlowStateChanged(GameFlowState.PlayerPhaseEnd);
        
        currentState = GameFlowState.EnemyPhase;
        
        if (showDebugInfo)
        {
            Debug.Log("GameFlowController: 玩家阶段完成，切换到敌人阶段");
        }
        
        // 发布游戏流程状态变化事件
        GameEventBus.PublishGameFlowStateChanged(GameFlowState.EnemyPhase);
        
        // 启动敌人阶段
        if (enemyPhaseController != null)
        {
            enemyPhaseController.StartEnemyPhase();
        }
        
        OnStateChanged?.Invoke(currentState);
    }
    
    #endregion
    
    #region 公共属性
    
    public GameFlowState CurrentState => currentState;
    public bool IsPlayerPhase => currentState == GameFlowState.PlayerPhase;
    public bool IsEnemyPhase => currentState == GameFlowState.EnemyPhase;
    
    #endregion
}