using UnityEngine;

/// <summary>
/// 游戏初始化器 - 负责游戏启动时的初始化工作
/// 查找组件、建立引用关系、订阅事件、准备游戏场景
/// </summary>
public class GameInitializer : MonoBehaviour
{
    [Header("初始化设置")]
    [SerializeField] private bool autoInitializeOnStart = true;
    [SerializeField] private bool showDebugInfo = true;
    
    // 需要初始化的组件
    private GameManager gameManager;
    private GameFlowController gameFlowController;
    private PlayerStateMachine playerStateMachine;
    private PlayerCore playerCore;
    private ChargeSystem chargeSystem;
    private TransitionManager transitionManager;
    private TimeManager timeManager;
    
    // 游戏对象引用
    private Player player;
    private Enemy[] enemies;
    //private HoleManager holeManager;
    private EnemySpawner enemySpawner;
    
    void Start()
    {
        if (autoInitializeOnStart)
        {
            InitializeGame();
        }
    }
    
    #region 游戏初始化
    
    public void InitializeGame()
    {
        if (showDebugInfo)
        {
            Debug.Log("GameInitializer: 开始游戏初始化");
        }
        
        // 1. 查找核心组件
        FindCoreComponents();
        
        // 2. 查找游戏对象
        FindGameObjects();
        
        // 3. 建立引用关系
        SetupReferences();
        
        // 4. 订阅事件
        SubscribeToEvents();
        
        // 5. 准备游戏场景
        PrepareGameScene();
        
        if (showDebugInfo)
        {
            Debug.Log("GameInitializer: 游戏初始化完成");
        }
    }
    
    #endregion
    
    #region 组件查找
    
    void FindCoreComponents()
    {
        // 查找GameManager
        gameManager = FindAnyObjectByType<GameManager>();
        if (gameManager == null)
        {
            Debug.LogError("GameInitializer: 未找到GameManager！");
            return;
        }
        
        // 查找GameFlowController
        gameFlowController = FindAnyObjectByType<GameFlowController>();
        if (gameFlowController == null)
        {
            Debug.LogError("GameInitializer: 未找到GameFlowController！");
            return;
        }
        
        // 查找其他核心组件（如果存在）
        chargeSystem = FindAnyObjectByType<ChargeSystem>();
        timeManager = FindAnyObjectByType<TimeManager>();
        transitionManager = FindAnyObjectByType<TransitionManager>();
        
        if (showDebugInfo)
        {
            Debug.Log("GameInitializer: 核心组件查找完成");
        }
    }
    
    void FindGameObjects()
    {
        // 查找玩家
        player = FindAnyObjectByType<Player>();
        if (player == null)
        {
            Debug.LogWarning("GameInitializer: 未找到Player！");
        }
        
        // 查找所有敌人
        enemies = FindObjectsByType<Enemy>(FindObjectsSortMode.None);
        if (enemies.Length == 0)
        {
            Debug.LogWarning("GameInitializer: 未找到任何敌人！");
        }
        
        // // 查找HoleManager
        // holeManager = FindAnyObjectByType<HoleManager>();
        // if (holeManager == null)
        // {
        //     Debug.LogWarning("GameInitializer: 未找到HoleManager！");
        // }
        
        // 查找EnemySpawner
        enemySpawner = FindAnyObjectByType<EnemySpawner>();
        if (enemySpawner == null)
        {
            Debug.LogWarning("GameInitializer: 未找到EnemySpawner！");
        }
        
        if (showDebugInfo)
        {
            Debug.Log($"GameInitializer: 游戏对象查找完成 - 白球: {player != null}, 敌人: {enemies.Length}");
        }
    }
    
    #endregion
    
    #region 引用设置
    
    void SetupReferences()
    {
        // 设置GameManager的组件引用
        if (gameManager != null)
        {
            gameManager.SetGameFlowController(gameFlowController);
            gameManager.SetTransitionManager(transitionManager);
        }
        
        // 设置GameFlowController的组件引用
        if (gameFlowController != null)
        {
            gameFlowController.SetPlayerStateMachine(playerStateMachine);
            gameFlowController.SetPlayerCore(playerCore);
            gameFlowController.SetTransitionManager(transitionManager);
            
            // 设置Player相关引用
            if (player != null)
            {
                PlayerStateMachine playerStateMachine = player.GetStateMachine();
                PlayerCore playerCore = player.GetPlayerCore();
                
                if (playerStateMachine != null)
                {
                    gameFlowController.SetPlayerStateMachine(playerStateMachine);
                }
                
                if (playerCore != null)
                {
                    gameFlowController.SetPlayerCore(playerCore);
                }
            }
        }
        
        // 设置ChargeSystem的组件引用
        if (chargeSystem != null)
        {
            
            // 设置Player相关引用
            if (player != null)
            {
                PlayerStateMachine playerStateMachine = player.GetStateMachine();
                PlayerCore playerCore = player.GetPlayerCore();
                AimController aimController = FindAnyObjectByType<AimController>();
                
                if (playerStateMachine != null)
                {
                    playerStateMachine.SetChargeSystem(chargeSystem);
                }
                
                if (playerCore != null)
                {
                    playerCore.SetChargeSystem(chargeSystem);
                }
                
                if (aimController != null)
                {
                    aimController.SetChargeSystem(chargeSystem);
                }
            }
        }
        
        if (showDebugInfo)
        {
            Debug.Log("GameInitializer: 组件引用设置完成");
        }
    }
    
    #endregion
    
    #region 事件订阅
    
    void SubscribeToEvents()
    {
        // 订阅玩家事件
        // if (player != null)
        // {
        //     // 订阅玩家停止事件
        //     PlayerCore playerCore = player.GetPlayerCore();
        //     if (playerCore != null)
        //     {
        //         playerCore.OnBallStopped += OnPlayerStopped;
        //     }
        //     if (showDebugInfo)
        //     {
        //         Debug.Log("GameInitializer: 订阅玩家事件");
        //     }
        // }
        
        
        // // 订阅HoleManager事件
        // if (holeManager != null)
        // {
        //     holeManager.OnPlayerInHole += OnPlayerInHole;
        //     if (showDebugInfo)
        //     {
        //         Debug.Log("GameInitializer: 订阅HoleManager事件");
        //     }
        // }
    }
    
    #endregion
    
    #region 游戏场景准备
    
    void PrepareGameScene()
    {
        // 启动敌人生成系统
        if (enemySpawner != null)
        {
            enemySpawner.StartSpawning();
            if (showDebugInfo)
            {
                Debug.Log("GameInitializer: 启动敌人生成系统");
            }
        }
        
        // 启动游戏流程
        if (gameFlowController != null)
        {
            gameFlowController.StartNormalState();
            if (showDebugInfo)
            {
                Debug.Log("GameInitializer: 启动游戏流程");
            }
        }
    }
    
    #endregion
    
    #region 事件处理
    
    // void OnPlayerStopped()
    // {
    //     if (showDebugInfo)
    //     {
    //         //Debug.Log("GameInitializer: 玩家停止事件");
    //     }
        
    //     // 将事件传递给GameFlowController处理
    //     if (gameFlowController != null)
    //     {
    //         gameFlowController.OnPlayerStopped();
    //     }
    // }
    
    void OnEnemyPhaseComplete()
    {
        if (showDebugInfo)
        {
            Debug.Log("GameInitializer: 敌人阶段完成事件");
        }
        
        // 将事件传递给GameFlowController处理
        if (gameFlowController != null)
        {
            gameFlowController.OnEnemyPhaseComplete();
        }
    }
    
    void OnPlayerInHole(Player player)
    {
        if (showDebugInfo)
        {
            Debug.Log("GameInitializer: 玩家进洞事件");
        }
        
        // 将事件传递给GameFlowController处理
        if (gameFlowController != null)
        {
            gameFlowController.OnPlayerInHole(player);
        }
    }
    
    #endregion
    
    #region 公共方法
    
    public void ReinitializeGame()
    {
        if (showDebugInfo)
        {
            Debug.Log("GameInitializer: 重新初始化游戏");
        }
        
        InitializeGame();
    }
    
    public void SetAutoInitialize(bool autoInit)
    {
        autoInitializeOnStart = autoInit;
    }
    
    #endregion
}
