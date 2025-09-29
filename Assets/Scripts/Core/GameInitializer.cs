using UnityEngine;
using DeepSpaceLabs.SAM;

/// <summary>
/// 游戏初始化器 - 负责初始化游戏场景和组件
/// 
/// 【核心职责】：
/// - 初始化游戏场景
/// - 设置组件引用
/// - 启动游戏流程
/// 
/// 【新架构说明】：
/// - GameFlowController通过单例模式自动获取引用
/// - PlayerPhaseController和EnemyPhaseController自动初始化
/// - 不再需要手动设置复杂的组件引用
/// </summary>
public class GameInitializer : MonoBehaviour
{
    [Header("初始化设置")]
    [SerializeField] private bool autoInitializeOnStart = true;
    [SerializeField] private bool showDebugInfo = true;
    
    // 核心组件引用
    private GameFlowController gameFlowController;
    private GameManager gameManager;
    private TransitionManager transitionManager;
    private EnemyPhaseController enemyPhaseController;
    private PlayerPhaseController playerPhaseController;
    private EffectManager effectManager;
    
    void Start()
    {
        if (autoInitializeOnStart)
        {
            InitializeGame();
        }
    }
    
    /// <summary>
    /// 初始化游戏
    /// </summary>
    public void InitializeGame()
    {
        if (showDebugInfo)
        {
            Debug.Log("GameInitializer: 开始初始化游戏");
        }
        
        // 查找核心组件
        FindCoreComponents();
        
        // 设置组件引用
        SetupComponentReferences();
        
        // 准备游戏场景
        PrepareGameScene();
        
        if (showDebugInfo)
        {
            Debug.Log("GameInitializer: 游戏初始化完成");
        }
    }
    
    /// <summary>
    /// 查找核心组件
    /// </summary>
    void FindCoreComponents()
    {
        // 优先初始化EffectManager，因为其他组件在OnEnable时需要访问它
        effectManager = FindAnyObjectByType<EffectManager>();
        if (effectManager == null)
        {
            // 创建EffectManager
            GameObject effectManagerGO = new GameObject("EffectManager");
            effectManager = effectManagerGO.AddComponent<EffectManager>();
            if (showDebugInfo)
            {
                Debug.Log("GameInitializer: 已创建EffectManager");
            }
        }
        else
        {
            if (showDebugInfo)
            {
                Debug.Log("GameInitializer: 找到现有EffectManager");
            }
        }
        
        // 查找GameFlowController
        gameFlowController = FindAnyObjectByType<GameFlowController>();
        if (gameFlowController == null)
        {
            Debug.LogError("GameInitializer: 未找到GameFlowController！");
        }
        
        // 查找GameManager
        gameManager = FindAnyObjectByType<GameManager>();
        if (gameManager == null)
        {
            Debug.LogError("GameInitializer: 未找到GameManager！");
        }
        
        // 查找TransitionManager
        transitionManager = FindAnyObjectByType<TransitionManager>();
        if (transitionManager == null)
        {
            Debug.LogError("GameInitializer: 未找到TransitionManager！");
        }
        
        // 查找EnemyPhaseController
        enemyPhaseController = FindAnyObjectByType<EnemyPhaseController>();
        if (enemyPhaseController == null)
        {
            // 创建EnemyPhaseController
            GameObject enemyPhaseControllerGO = new GameObject("EnemyPhaseController");
            enemyPhaseController = enemyPhaseControllerGO.AddComponent<EnemyPhaseController>();
            if (showDebugInfo)
            {
                Debug.Log("GameInitializer: 已创建EnemyPhaseController");
            }
        }
        
        // 查找PlayerPhaseController
        playerPhaseController = FindAnyObjectByType<PlayerPhaseController>();
        if (playerPhaseController == null)
        {
            // 创建PlayerPhaseController
            GameObject playerPhaseControllerGO = new GameObject("PlayerPhaseController");
            playerPhaseController = playerPhaseControllerGO.AddComponent<PlayerPhaseController>();
            if (showDebugInfo)
            {
                Debug.Log("GameInitializer: 已创建PlayerPhaseController");
            }
        }
    }
    
    /// <summary>
    /// 设置组件引用
    /// </summary>
    void SetupComponentReferences()
    {
        // 设置GameManager的引用
        if (gameManager != null)
        {
            gameManager.SetGameFlowController(gameFlowController);
            gameManager.SetTransitionManager(transitionManager);
        }
        
    }
    
    /// <summary>
    /// 准备游戏场景
    /// </summary>
    void PrepareGameScene()
    {
        // 准备敌人生成系统
        if (enemyPhaseController != null)
        {
            if (showDebugInfo)
            {
                Debug.Log("GameInitializer: 敌人生成系统已准备就绪（手动控制）");
            }
        }
        
        // 启动游戏流程
        if (gameFlowController != null)
        {
            // 新架构中游戏启动时直接进入玩家阶段
            // GameFlowController会在Start()中自动启动玩家阶段
            if (showDebugInfo)
            {
                Debug.Log("GameInitializer: 启动游戏流程");
            }
        }
    }
    
    #region 公共方法
    
    /// <summary>
    /// 重新初始化游戏
    /// </summary>
    public void ReinitializeGame()
    {
        if (showDebugInfo)
        {
            Debug.Log("GameInitializer: 重新初始化游戏");
        }
        
        InitializeGame();
    }
    
    /// <summary>
    /// 设置自动初始化
    /// </summary>
    public void SetAutoInitialize(bool autoInit)
    {
        autoInitializeOnStart = autoInit;
    }
    
    #endregion
}