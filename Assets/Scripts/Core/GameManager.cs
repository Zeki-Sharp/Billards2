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
    [SerializeField] private int currentWave = 1;
    [SerializeField] private int maxWaves = 10;
    
    [Header("胜负条件")]
    [SerializeField] private int winScore = 1000;
    
    [Header("调试")]
    [SerializeField] private bool showDebugInfo = true;
    
    // 暂停时记录被禁用的刚体
    private System.Collections.Generic.List<Rigidbody2D> pausedRigidbodies = new System.Collections.Generic.List<Rigidbody2D>();
    
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
        
        // 订阅事件
        SubscribeToEvents();
        
        // 游戏初始化由GameInitializer负责
        // 这里只做基本的游戏状态初始化
        InitializeGameState();
    }
    
    void OnDestroy()
    {
        // 取消订阅事件
        UnsubscribeFromEvents();
    }
    
    /// <summary>
    /// 订阅事件
    /// </summary>
    void SubscribeToEvents()
    {
        GameEventBus.OnSkillSelectionStarted += HandleSkillSelectionStarted;
        GameEventBus.OnSkillSelectionCompleted += HandleSkillSelectionCompleted;
        GameEventBus.OnGameCompleted += HandleGameCompleted;
    }
    
    /// <summary>
    /// 取消订阅事件
    /// </summary>
    void UnsubscribeFromEvents()
    {
        GameEventBus.OnSkillSelectionStarted -= HandleSkillSelectionStarted;
        GameEventBus.OnSkillSelectionCompleted -= HandleSkillSelectionCompleted;
        GameEventBus.OnGameCompleted -= HandleGameCompleted;
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
        currentWave = 1;
        
        // 触发初始化事件
        GameEventBus.PublishScoreChanged(score);
        GameEventBus.PublishWaveChanged(currentWave);
        GameEventBus.PublishGameStateChanged(isGameActive);
        
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
        // 检查失败条件：通过PlayerCore检查血量
        PlayerCore playerCore = FindFirstObjectByType<PlayerCore>();
        if (playerCore != null && playerCore.GetCurrentHealth() <= 0)
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
        
        GameEventBus.PublishGameWin();
        GameEventBus.PublishGameStateChanged(isGameActive);
    }
    
    void GameOver()
    {
        if (isGameOver) return;
        
        isGameOver = true;
        isGameActive = false;
        
        // 暂停游戏（显示失败界面时需要）
        PauseGame();
        
        if (showDebugInfo)
        {
            Debug.Log($"GameManager: 游戏结束！最终分数: {score}, 完成波次: {currentWave}");
        }
        
        GameEventBus.PublishGameOver();
        GameEventBus.PublishGameStateChanged(isGameActive);
    }
    
    #endregion
    
    #region 游戏数据管理
    
    public void AddScore(int points)
    {
        if (isGameOver) return;
        
        score += points;
        GameEventBus.PublishScoreChanged(score);
        
        if (showDebugInfo)
        {
            Debug.Log($"GameManager: 获得分数 {points}, 总分: {score}");
        }
    }
    
    /// <summary>
    /// 受到伤害 - 委托给PlayerCore处理
    /// </summary>
    public void TakeDamage(int damage)
    {
        if (isGameOver) return;
        
        PlayerCore playerCore = FindFirstObjectByType<PlayerCore>();
        if (playerCore != null)
        {
            playerCore.TakeDamage(damage);
        }
        
        if (showDebugInfo)
        {
            Debug.Log($"GameManager: 委托PlayerCore处理伤害 {damage}");
        }
    }
    
    /// <summary>
    /// 恢复生命 - 委托给PlayerCore处理
    /// </summary>
    public void Heal(int healAmount)
    {
        if (isGameOver) return;
        
        PlayerCore playerCore = FindFirstObjectByType<PlayerCore>();
        if (playerCore != null)
        {
            playerCore.Heal(healAmount);
        }
        
        if (showDebugInfo)
        {
            Debug.Log($"GameManager: 委托PlayerCore处理恢复 {healAmount}");
        }
    }
    
    public void NextWave()
    {
        if (isGameOver) return;
        
        currentWave++;
        GameEventBus.PublishWaveChanged(currentWave);
        
        if (showDebugInfo)
        {
            Debug.Log($"GameManager: 进入下一波次 {currentWave}");
        }
    }
    
    #endregion
    
    #region 游戏控制
    
    public void PauseGame()
    {
        isGamePaused = true;
        Time.timeScale = 0f;
        
        // 暂停所有 Rigidbody2D 的物理模拟
        PauseAllRigidbodies();
        
        // 停止所有敌人的协程和定时器
        StopAllEnemyBehaviors();
        
        if (showDebugInfo)
        {
            Debug.Log("GameManager: 游戏暂停");
        }
    }
    
    public void ResumeGame()
    {
        isGamePaused = false;
        Time.timeScale = 1f;
        
        // 恢复所有 Rigidbody2D 的物理模拟
        ResumeAllRigidbodies();
        
        if (showDebugInfo)
        {
            Debug.Log("GameManager: 游戏恢复");
        }
    }
    
    /// <summary>
    /// 暂停所有 Rigidbody2D 的物理模拟
    /// </summary>
    private void PauseAllRigidbodies()
    {
        pausedRigidbodies.Clear();
        
        Rigidbody2D[] allRigidbodies = FindObjectsByType<Rigidbody2D>(FindObjectsSortMode.None);
        foreach (var rb in allRigidbodies)
        {
            if (rb != null && rb.simulated)
            {
                // 禁用物理模拟（这是 Unity 推荐的暂停物理的方式）
                rb.simulated = false;
                // 记录被暂停的刚体，以便恢复时只恢复这些
                pausedRigidbodies.Add(rb);
            }
        }
        
        if (showDebugInfo)
        {
            Debug.Log($"GameManager: 已暂停 {pausedRigidbodies.Count} 个 Rigidbody2D 的物理模拟");
        }
    }
    
    /// <summary>
    /// 恢复所有 Rigidbody2D 的物理模拟
    /// </summary>
    private void ResumeAllRigidbodies()
    {
        // 只恢复之前被暂停的刚体
        foreach (var rb in pausedRigidbodies)
        {
            if (rb != null)
            {
                // 重新启用物理模拟
                rb.simulated = true;
            }
        }
        
        if (showDebugInfo)
        {
            Debug.Log($"GameManager: 已恢复 {pausedRigidbodies.Count} 个 Rigidbody2D 的物理模拟");
        }
        
        pausedRigidbodies.Clear();
    }
    
    /// <summary>
    /// 停止所有敌人的协程和定时器
    /// </summary>
    private void StopAllEnemyBehaviors()
    {
        // 停止所有敌人的协程
        Enemy[] allEnemies = FindObjectsByType<Enemy>(FindObjectsSortMode.None);
        int stoppedEnemyCount = 0;
        foreach (var enemy in allEnemies)
        {
            if (enemy != null)
            {
                enemy.StopAllCoroutines();
                stoppedEnemyCount++;
            }
        }
        
        // 停止EnemyController的Invoke定时器
        EnemyController enemyController = FindFirstObjectByType<EnemyController>();
        if (enemyController != null)
        {
            enemyController.CancelInvoke();
        }
        
        // 停止EnemyPhaseController的Invoke定时器
        EnemyPhaseController enemyPhaseController = FindFirstObjectByType<EnemyPhaseController>();
        if (enemyPhaseController != null)
        {
            enemyPhaseController.CancelInvoke();
        }
        
        if (showDebugInfo)
        {
            Debug.Log($"GameManager: 已停止 {stoppedEnemyCount} 个敌人的协程和定时器");
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
    
    #region 事件处理
    
    /// <summary>
    /// 处理技能选择开始事件
    /// </summary>
    void HandleSkillSelectionStarted(System.Collections.Generic.List<SkillConfig> skills)
    {
        PauseGame();
        
        if (showDebugInfo)
        {
            Debug.Log("GameManager: 技能选择开始，游戏已暂停");
        }
    }
    
    /// <summary>
    /// 处理技能选择完成事件
    /// </summary>
    void HandleSkillSelectionCompleted()
    {
        ResumeGame();
        
        if (showDebugInfo)
        {
            Debug.Log("GameManager: 技能选择完成，游戏已恢复");
        }
    }
    
    /// <summary>
    /// 处理游戏完成事件（所有关卡完成）
    /// </summary>
    void HandleGameCompleted()
    {
        // 暂停游戏（显示胜利界面时需要）
        PauseGame();
        
        if (showDebugInfo)
        {
            Debug.Log("GameManager: 游戏完成，游戏已暂停");
        }
    }
    
    #endregion
    
    #region 公共属性
    
    public bool IsGameActive => isGameActive;
    public bool IsGamePaused => isGamePaused;
    public bool IsGameOver => isGameOver;
    public int Score => score;
    public int CurrentWave => currentWave;
    public int MaxWaves => maxWaves;
    
    #endregion
}
