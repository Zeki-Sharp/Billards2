using UnityEngine;

/// <summary>
/// 游戏总管理器 - 负责游戏整体管理、胜负判断和生命周期
/// 从原有的阶段管理器重构为游戏总控制器
/// 
/// 【执行顺序】：CORE 层 (-100)，最先执行
/// 【依赖】：无依赖
/// 【初始化】：OnManagerCreated 中初始化游戏状态
/// </summary>
[DefaultExecutionOrder(ManagerExecutionOrder.CORE)]
public class GameManager : SingletonManager<GameManager>
{
    
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
    
    // 暂停请求计数器（用于处理多个面板同时暂停的情况）
    private int pauseRequestCount = 0;
    
    #region SingletonManager 重写
    
    protected override bool PersistAcrossScenes => false;
    protected override bool EnableDebugLog => showDebugInfo;
    
    protected override void Awake()
    {
        base.Awake(); // 必须先调用基类
        
        //设置全局物理参数
        Physics2D.gravity = Vector2.zero;
        
        if (EnableDebugLog)
        {
            Debug.Log("GameManager: 已禁用全局重力");
        }
    }
    
    protected override void OnManagerCreated()
    {
        // 初始化游戏状态
        InitializeGameState();
        
        if (EnableDebugLog)
        {
            Debug.Log("GameManager: 初始化完成（CORE 层）");
        }
    }
    
    #endregion
    
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
        PlayerBehavior playerCore = FindFirstObjectByType<PlayerBehavior>();
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
    
    
    #region 游戏控制
    
    public void PauseGame()
    {
        pauseRequestCount++;
        
        if (showDebugInfo)
        {
            Debug.Log($"GameManager: 暂停请求 +1，当前计数: {pauseRequestCount}");
        }
        
        // 只有第一次暂停请求时才真正暂停游戏
        if (pauseRequestCount == 1)
        {
            isGamePaused = true;
            Time.timeScale = 0f;
            
            // 暂停所有 Rigidbody2D 的物理模拟
            PauseAllRigidbodies();
            
            if (showDebugInfo)
            {
                Debug.Log("GameManager: 游戏已暂停（真实暂停）");
            }
        }
    }
    
    public void ResumeGame()
    {
        if (pauseRequestCount > 0)
        {
            pauseRequestCount--;
            
            if (showDebugInfo)
            {
                Debug.Log($"GameManager: 恢复请求 -1，当前计数: {pauseRequestCount}");
            }
        }
        else
        {
            if (showDebugInfo)
            {
                Debug.LogWarning("GameManager: 恢复请求被忽略，计数已经是0（可能多次调用了ResumeGame）");
            }
            return;
        }
        
        // 只有计数归零时才真正恢复游戏
        if (pauseRequestCount == 0)
        {
            isGamePaused = false;
            Time.timeScale = 1f;
            
            // 恢复所有 Rigidbody2D 的物理模拟
            ResumeAllRigidbodies();
            
            if (showDebugInfo)
            {
                Debug.Log("GameManager: 游戏已恢复（真实恢复）");
            }
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
        
        // 停止 EnemyManager 的Invoke定时器
        EnemyManager enemyManager = EnemyManager.Instance;
        if (enemyManager != null)
        {
            enemyManager.CancelInvoke();
        }
        
        // 停止EnemyPhaseController的Invoke定时器
        EnemyPhaseController enemyPhaseController = EnemyPhaseController.Instance;
        if (enemyPhaseController != null)
        {
            enemyPhaseController.CancelInvoke();
        }
        
        if (showDebugInfo)
        {
            Debug.Log($"GameManager: 已停止 {stoppedEnemyCount} 个敌人的协程和定时器");
        }
    }
    
    #endregion
    
    #region 公共属性
    
    public bool IsGameActive => isGameActive;
    public bool IsGamePaused => isGamePaused;
    public bool IsGameOver => isGameOver;
    
    #endregion
    
    #region 调试方法
    
    [ContextMenu("显示暂停状态")]
    void ShowPauseStatus()
    {
        Debug.Log($"GameManager 暂停状态:\n" +
                  $"暂停请求计数: {pauseRequestCount}\n" +
                  $"isGamePaused: {isGamePaused}\n" +
                  $"Time.timeScale: {Time.timeScale}\n" +
                  $"暂停的刚体数: {pausedRigidbodies.Count}");
    }
    
    [ContextMenu("强制重置暂停状态")]
    void ForceResetPauseState()
    {
        Debug.LogWarning("GameManager: 强制重置暂停状态！");
        pauseRequestCount = 0;
        isGamePaused = false;
        Time.timeScale = 1f;
        ResumeAllRigidbodies();
    }
    
    #endregion
}
