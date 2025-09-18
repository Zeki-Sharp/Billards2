using UnityEngine;

/// <summary>
/// 游戏总管理器 - 负责游戏整体管理、胜负判断和生命周期
/// 从原有的阶段管理器重构为游戏总控制器
/// </summary>
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }
    
    [Header("游戏状态")]
    [SerializeField] private bool isGameActive = true;
    [SerializeField] private bool isGamePaused = false;
    [SerializeField] private bool isGameOver = false;
    
    [Header("游戏数据")]
    [SerializeField] private int score = 0;
    [SerializeField] private int playerHealth = 100;
    [SerializeField] private int currentWave = 1;
    [SerializeField] private int maxWaves = 10;
    
    [Header("胜负条件")]
    [SerializeField] private int winScore = 1000;
    [SerializeField] private int maxHealth = 100;
    
    [Header("调试")]
    [SerializeField] private bool showDebugInfo = true;
    
    // 组件引用（由GameInitializer设置）
    private GameFlowController gameFlowController;
    private TimeManager timeManager;
    private TransitionManager transitionManager;
    
    // 事件
    public System.Action<bool> OnGameStateChanged; // 游戏状态变化
    public System.Action<int> OnScoreChanged; // 分数变化
    public System.Action<int> OnHealthChanged; // 生命值变化
    public System.Action<int> OnWaveChanged; // 波次变化
    public System.Action OnGameOver; // 游戏结束
    public System.Action OnGameWin; // 游戏胜利
    
    void Awake()
    {
        // 单例模式：确保只有一个GameManager实例
        if (Instance == null)
        {
            Instance = this;
            // 设置全局物理参数
            Physics2D.gravity = Vector2.zero; // 禁用重力，台球不受重力影响
            Debug.Log("GameManager: 已禁用全局重力");
        }
        else
        {
            Debug.LogWarning("发现多个GameManager实例，销毁重复的实例");
            Destroy(gameObject);
            return;
        }
    }
    
    void Start()
    {
        // 确保物理设置在Start中再次应用（防止构建时被覆盖）
        Physics2D.gravity = Vector2.zero;
        Debug.Log($"GameManager: 构建版本重力设置确认 - {Physics2D.gravity}");
        
        // 游戏初始化由GameInitializer负责
        // 这里只做基本的游戏状态初始化
        InitializeGameState();
    }
    
    void Update()
    {
        // 游戏总管理器只负责胜负判断和状态检查
        if (isGameActive && !isGamePaused && !isGameOver)
        {
            CheckWinCondition();
            CheckLoseCondition();
        }
    }
    
    #region 游戏状态初始化
    
    void InitializeGameState()
    {
        // 初始化游戏状态
        isGameActive = true;
        isGamePaused = false;
        isGameOver = false;
        
        // 初始化游戏数据
        score = 0;
        playerHealth = maxHealth;
        currentWave = 1;
        
        // 触发初始化事件
        OnScoreChanged?.Invoke(score);
        OnHealthChanged?.Invoke(playerHealth);
        OnWaveChanged?.Invoke(currentWave);
        OnGameStateChanged?.Invoke(isGameActive);
        
        if (showDebugInfo)
        {
            Debug.Log("GameManager: 游戏状态初始化完成");
        }
    }
    
    #endregion
    
    #region 胜负判断
    
    void CheckWinCondition()
    {
        // 检查胜利条件：达到目标分数或完成所有波次
        if (score >= winScore || currentWave > maxWaves)
        {
            GameWin();
        }
    }
    
    void CheckLoseCondition()
    {
        // 检查失败条件：生命值归零
        if (playerHealth <= 0)
        {
            GameOver();
        }
    }
    
    void GameWin()
    {
        if (isGameOver) return;
        
        isGameOver = true;
        isGameActive = false;
        
        if (showDebugInfo)
        {
            Debug.Log($"GameManager: 游戏胜利！最终分数: {score}, 完成波次: {currentWave}");
        }
        
        OnGameWin?.Invoke();
        OnGameStateChanged?.Invoke(isGameActive);
    }
    
    void GameOver()
    {
        if (isGameOver) return;
        
        isGameOver = true;
        isGameActive = false;
        
        if (showDebugInfo)
        {
            Debug.Log($"GameManager: 游戏结束！最终分数: {score}, 完成波次: {currentWave}");
        }
        
        OnGameOver?.Invoke();
        OnGameStateChanged?.Invoke(isGameActive);
    }
    
    #endregion
    
    #region 游戏数据管理
    
    public void AddScore(int points)
    {
        if (isGameOver) return;
        
        score += points;
        OnScoreChanged?.Invoke(score);
        
        if (showDebugInfo)
        {
            Debug.Log($"GameManager: 获得分数 {points}, 总分: {score}");
        }
    }
    
    public void TakeDamage(int damage)
    {
        if (isGameOver) return;
        
        playerHealth = Mathf.Max(0, playerHealth - damage);
        OnHealthChanged?.Invoke(playerHealth);
        
        if (showDebugInfo)
        {
            Debug.Log($"GameManager: 受到伤害 {damage}, 剩余生命: {playerHealth}");
        }
    }
    
    public void Heal(int healAmount)
    {
        if (isGameOver) return;
        
        playerHealth = Mathf.Min(maxHealth, playerHealth + healAmount);
        OnHealthChanged?.Invoke(playerHealth);
        
        if (showDebugInfo)
        {
            Debug.Log($"GameManager: 恢复生命 {healAmount}, 当前生命: {playerHealth}");
        }
    }
    
    public void NextWave()
    {
        if (isGameOver) return;
        
        currentWave++;
        OnWaveChanged?.Invoke(currentWave);
        
        if (showDebugInfo)
        {
            Debug.Log($"GameManager: 进入下一波次 {currentWave}");
        }
    }
    
    #endregion
    
    #region 游戏控制
    
    public void PauseGame()
    {
        if (isGameOver) return;
        
        isGamePaused = true;
        Time.timeScale = 0f;
        
        if (showDebugInfo)
        {
            Debug.Log("GameManager: 游戏暂停");
        }
    }
    
    public void ResumeGame()
    {
        if (isGameOver) return;
        
        isGamePaused = false;
        Time.timeScale = 1f;
        
        if (showDebugInfo)
        {
            Debug.Log("GameManager: 游戏恢复");
        }
    }
    
    public void RestartGame()
    {
        // 重置游戏状态
        isGameOver = false;
        isGamePaused = false;
        isGameActive = true;
        Time.timeScale = 1f;
        
        // 重新初始化游戏数据
        InitializeGameState();
        
        if (showDebugInfo)
        {
            Debug.Log("GameManager: 游戏重启");
        }
    }
    
    #endregion
    
    #region 组件引用设置（由GameInitializer调用）
    
    public void SetGameFlowController(GameFlowController controller)
    {
        gameFlowController = controller;
    }
    
    
    public void SetTimeManager(TimeManager manager)
    {
        timeManager = manager;
    }
    
    public void SetTransitionManager(TransitionManager manager)
    {
        transitionManager = manager;
    }
    
    
    #endregion
    
    #region 公共属性
    
    public bool IsGameActive => isGameActive;
    public bool IsGamePaused => isGamePaused;
    public bool IsGameOver => isGameOver;
    public int Score => score;
    public int PlayerHealth => playerHealth;
    public int CurrentWave => currentWave;
    public int MaxWaves => maxWaves;
    
    #endregion
}
