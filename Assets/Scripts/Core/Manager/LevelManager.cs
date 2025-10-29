using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

/// <summary>
/// 关卡管理器 - 负责管理关卡流程、进度和完成检测
/// 
/// 【核心职责】：
/// - 管理关卡列表和当前关卡索引
/// - 检测关卡完成条件（敌人计数）
/// - 处理关卡切换逻辑
/// - 集成到现有的事件系统
/// 
/// 【执行顺序】：LEVEL 层 (-30)
/// 【依赖】：SYSTEM 层
/// </summary>
[DefaultExecutionOrder(ManagerExecutionOrder.LEVEL)]
public class LevelManager : SingletonManager<LevelManager>
{
    
    [Header("关卡配置")]
    [SerializeField] private string[] sceneNames;  // 场景名称数组（按关卡顺序）
    [SerializeField] private int currentLevelIndex = 0;  // 当前关卡索引
    
    [Header("关卡完成检测")]
    [SerializeField] private int totalEnemyCount;  // 关卡敌人总数
    [SerializeField] private int killedEnemyCount;  // 已击杀敌人数
    
    [Header("调试")]
    [SerializeField] private bool showDebugInfo = true;
    
    // 组件引用
    private WaveConfigProvider waveConfigProvider;
    private LevelConfig currentLevelConfig;  // 当前场景的关卡配置
    
    // 状态管理
    private bool isLevelActive = false;
    private bool isLevelCompleted = false;
    
    #region SingletonManager 重写
    
    protected override bool PersistAcrossScenes => true;
    protected override bool EnableDebugLog => showDebugInfo;
    
    protected override void OnManagerCreated()
    {
        GameEventBus.OnGameRestart += ResetState;
        GameEventBus.OnDeath += OnDeath;
        SceneManager.sceneLoaded += OnSceneLoaded;
        
        if (showDebugInfo)
        {
            Debug.Log("LevelManager: 单例创建成功（LEVEL 层），将跨场景保留");
        }
    }
    
    protected override void OnManagerDestroyed()
    {
        GameEventBus.OnGameRestart -= ResetState;
        GameEventBus.OnDeath -= OnDeath;
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }
    
    #endregion
    
    void Start()
    {
        waveConfigProvider = FindFirstObjectByType<WaveConfigProvider>();
        ResetEnemyCount();
        LoadCurrentSceneLevel();
        
        if (showDebugInfo)
        {
            Debug.Log("LevelManager: 初始化完成");
        }
    }
    
    /// <summary>
    /// 加载当前场景的关卡
    /// </summary>
    void LoadCurrentSceneLevel()
    {
        // 获取当前场景的关卡配置
        currentLevelConfig = GetCurrentSceneLevelConfig();
        
        if (currentLevelConfig == null)
        {
            Debug.LogError("LevelManager: 当前场景没有关卡配置！");
            return;
        }
        
        // 设置关卡配置到 WaveConfigProvider
        if (waveConfigProvider != null)
        {
            waveConfigProvider.SetLevelConfig(currentLevelConfig);
        }
        else
        {
            Debug.LogWarning("LevelManager: 未找到 WaveConfigProvider！");
        }
        
        // 统计敌人总数
        CountTotalEnemies();
        
        // 恢复玩家状态（在新关卡开始时）
        RestorePlayerState();
        
        // 重置关卡状态
        isLevelActive = true;
        isLevelCompleted = false;
        
        if (showDebugInfo)
        {
            Debug.Log($"LevelManager: 加载场景关卡 - {currentLevelConfig.levelName}");
        }
        
        // 发布关卡开始事件
        GameEventBus.PublishLevelStarted(currentLevelIndex, currentLevelConfig);
    }
    
    /// <summary>
    /// 获取当前场景的关卡配置
    /// </summary>
    /// <returns>当前场景的关卡配置</returns>
    LevelConfig GetCurrentSceneLevelConfig()
    {
        // 从 WaveConfigProvider 获取当前关卡配置
        if (waveConfigProvider != null)
        {
            var config = waveConfigProvider.GetCurrentLevelConfig();
            if (config != null)
            {
                return config;
            }
        }
        
        Debug.LogError("LevelManager: WaveConfigProvider 中没有关卡配置！请确保每个场景的 WaveConfigProvider 都配置了 LevelConfig。");
        return null;
    }
    
    /// <summary>
    /// 统计关卡中的敌人总数
    /// </summary>
    void CountTotalEnemies()
    {
        totalEnemyCount = 0;
        killedEnemyCount = 0;
        
        if (currentLevelConfig == null)
            return;
        
        // 统计初始敌人（需要考虑每个EnemySpawn的count）
        if (currentLevelConfig.generateInitialEnemies)
        {
            int initialEnemyCount = 0;
            foreach (var enemySpawn in currentLevelConfig.initialEnemies)
            {
                if (enemySpawn != null && enemySpawn.enemyData != null)
                {
                    initialEnemyCount += enemySpawn.count;
                }
            }
            totalEnemyCount += initialEnemyCount;
            
            if (showDebugInfo)
            {
                Debug.Log($"LevelManager: 初始敌人数量: {initialEnemyCount}");
            }
        }
        
        // 统计波次敌人（需要考虑每个EnemySpawn的count）
        int waveEnemyCount = 0;
        int waveIndex = 0;
        foreach (var wave in currentLevelConfig.waves)
        {
            if (wave != null)
            {
                int currentWaveEnemyCount = 0;
                foreach (var enemySpawn in wave.enemySpawns)
                {
                    if (enemySpawn != null && enemySpawn.enemyData != null)
                    {
                        currentWaveEnemyCount += enemySpawn.count;
                    }
                }
                waveEnemyCount += currentWaveEnemyCount;
                
                if (showDebugInfo)
                {
                    Debug.Log($"LevelManager: 波次 {waveIndex + 1} 敌人数量: {currentWaveEnemyCount}");
                }
            }
            waveIndex++;
        }
        totalEnemyCount += waveEnemyCount;
        
        if (showDebugInfo)
        {
            Debug.Log($"LevelManager: 波次敌人总数量: {waveEnemyCount}");
        }
        
        if (showDebugInfo)
        {
            Debug.Log($"LevelManager: 关卡敌人总数: {totalEnemyCount} (初始敌人 + 波次敌人)");
        }
    }
    
    /// <summary>
    /// 重置敌人计数
    /// </summary>
    void ResetEnemyCount()
    {
        totalEnemyCount = 0;
        killedEnemyCount = 0;
    }
    
    /// <summary>
    /// 重置关卡管理器状态（游戏重启时调用）
    /// </summary>
    public void ResetState()
    {
        // 重置关卡索引到第一关
        currentLevelIndex = 0;
        
        // 复用现有的重置敌人计数方法
        ResetEnemyCount();
        
        // 重置关卡状态标志
        isLevelActive = false;
        isLevelCompleted = false;
        
        // 清空关卡配置引用（会在下次场景加载时重新获取）
        currentLevelConfig = null;
        
        if (showDebugInfo)
        {
            Debug.Log("LevelManager: 重置完成 - 关卡索引归零，敌人计数清空");
        }
    }
    
    /// <summary>
    /// 死亡事件处理
    /// </summary>
    /// <param name="deathData">死亡数据</param>
    void OnDeath(DeathData deathData)
    {
        if (!isLevelActive || isLevelCompleted)
            return;
        
        // 检查是否是敌人死亡
        if (deathData.DeadObjectTag == "Enemy" || deathData.DeathType == "EnemyDeath")
        {
            killedEnemyCount++;
            
            // 同步更新总击杀数到GameRuntimeData
            GameRuntimeData.AddEnemyKill();
            
            if (showDebugInfo)
            {
                Debug.Log($"LevelManager: 敌人死亡 {killedEnemyCount}/{totalEnemyCount} - {deathData.DeadObject?.name}");
            }
            
            // 检查关卡完成条件
            if (killedEnemyCount >= totalEnemyCount)
            {
                CompleteCurrentLevel();
            }
        }
    }
    
    /// <summary>
    /// 完成当前关卡
    /// </summary>
    void CompleteCurrentLevel()
    {
        if (isLevelCompleted)
            return;
        
        isLevelCompleted = true;
        isLevelActive = false;
        
        if (showDebugInfo)
        {
            Debug.Log($"LevelManager: 关卡 {currentLevelIndex + 1} 完成！");
        }
        
        // 保存玩家状态（在发布事件前）
        SavePlayerState();
        
        // 发布关卡完成事件
        GameEventBus.PublishLevelCompleted(currentLevelIndex, currentLevelConfig);
    }
    
    /// <summary>
    /// 加载下一关卡场景
    /// </summary>
    void LoadNextLevelScene()
    {
        currentLevelIndex++;
        
        if (sceneNames == null || currentLevelIndex >= sceneNames.Length)
        {
            if (showDebugInfo)
            {
                Debug.Log("LevelManager: 所有关卡已完成！");
            }
            GameCompleted();
            return;
        }
        
        // 获取下一个场景名称
        string nextSceneName = sceneNames[currentLevelIndex];
        if (string.IsNullOrEmpty(nextSceneName))
        {
            Debug.LogError($"LevelManager: 关卡 {currentLevelIndex + 1} 的场景名称为空！");
            return;
        }
        
        if (showDebugInfo)
        {
            Debug.Log($"LevelManager: 加载下一关卡场景 - {nextSceneName}");
        }
        
        // 直接使用 SceneManager 加载下一个场景
        UnityEngine.SceneManagement.SceneManager.LoadScene(nextSceneName);
    }
    
    /// <summary>
    /// 加载下一关卡（由外部调用，如SkillSelectionManager）
    /// </summary>
    public void LoadNextLevel()
    {
        LoadNextLevelScene();
    }
    
    /// <summary>
    /// 游戏完成处理
    /// </summary>
    void GameCompleted()
    {
        if (showDebugInfo)
        {
            Debug.Log("LevelManager: 所有关卡完成！游戏通关！");
        }
        
        // 发布游戏完成事件
        // UIController会监听此事件并显示VictoryPanel
        // VictoryPanel的Restart按钮会处理返回角色选择的逻辑
        GameEventBus.PublishGameCompleted();
    }
    
    /// <summary>
    /// 返回主菜单
    /// </summary>
    void ReturnToMainMenu()
    {
        // 直接使用 SceneManager 返回主菜单
        // 假设主菜单场景名为 "MainMenu"
        UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenu");
    }
    
    #region 场景加载事件处理
    
    /// <summary>
    /// 场景加载完成事件处理
    /// </summary>
    /// <param name="scene">加载的场景</param>
    /// <param name="mode">加载模式</param>
    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (showDebugInfo)
        {
            Debug.Log($"LevelManager: 场景加载完成 - {scene.name}");
        }
        
        // 从GameRuntimeData同步当前地图层级到currentLevelIndex
        // MapPlayerTracker在加载战斗场景时会设置GameRuntimeData的当前层级
        if (GameRuntimeData.IsFromMapSystem())
        {
            int mapLayer = GameRuntimeData.GetCurrentMapLayer();
            if (currentLevelIndex != mapLayer)
            {
                if (showDebugInfo)
                {
                    Debug.Log($"LevelManager: 同步地图层级 - 从 {currentLevelIndex} 更新到 {mapLayer}");
                }
                currentLevelIndex = mapLayer;
            }
        }
        
        // 重新查找新场景中的WaveConfigProvider
        waveConfigProvider = FindFirstObjectByType<WaveConfigProvider>();
        
        if (showDebugInfo)
        {
            if (waveConfigProvider != null)
            {
                Debug.Log($"LevelManager: 找到新场景的WaveConfigProvider - {waveConfigProvider.gameObject.name}");
            }
            else
            {
                Debug.LogError("LevelManager: 新场景中未找到WaveConfigProvider！");
            }
        }
        
        // 每次场景加载完成后都重新加载当前场景的关卡
        // 这样确保玩家状态恢复在新场景中正确执行
        LoadCurrentSceneLevel();
    }
    
    #endregion
    
    #region 公共方法
    
    /// <summary>
    /// 获取当前关卡索引
    /// </summary>
    /// <returns>当前关卡索引</returns>
    public int GetCurrentLevelIndex()
    {
        return currentLevelIndex;
    }
    
    /// <summary>
    /// 获取当前关卡配置
    /// </summary>
    /// <returns>当前关卡配置</returns>
    public LevelConfig GetCurrentLevelConfig()
    {
        return currentLevelConfig;
    }
    
    /// <summary>
    /// 获取关卡总数
    /// </summary>
    /// <returns>关卡总数</returns>
    public int GetTotalLevelCount()
    {
        return sceneNames != null ? sceneNames.Length : 0;
    }
    
    /// <summary>
    /// 获取关卡完成进度
    /// </summary>
    /// <returns>关卡完成进度 (0-1)</returns>
    public float GetLevelProgress()
    {
        if (totalEnemyCount == 0)
            return 0f;
        
        return (float)killedEnemyCount / totalEnemyCount;
    }
    
    /// <summary>
    /// 检查是否有下一关卡
    /// </summary>
    /// <returns>是否有下一关卡</returns>
    public bool HasNextLevel()
    {
        return currentLevelIndex + 1 < (sceneNames != null ? sceneNames.Length : 0);
    }
    
    /// <summary>
    /// 检查当前关卡是否完成
    /// </summary>
    /// <returns>当前关卡是否完成</returns>
    public bool IsCurrentLevelCompleted()
    {
        return isLevelCompleted;
    }
    
    #endregion
    
    #region 调试方法
    
    [ContextMenu("显示关卡信息")]
    void ShowLevelInfo()
    {
        Debug.Log($"LevelManager 调试信息:\n" +
                  $"当前关卡索引: {currentLevelIndex}\n" +
                  $"关卡总数: {GetTotalLevelCount()}\n" +
                  $"敌人总数: {totalEnemyCount}\n" +
                  $"已击杀敌人: {killedEnemyCount}\n" +
                  $"关卡进度: {GetLevelProgress():P2}\n" +
                  $"关卡是否完成: {isLevelCompleted}\n" +
                  $"是否有下一关卡: {HasNextLevel()}");
    }
    
    [ContextMenu("强制完成当前关卡")]
    void ForceCompleteCurrentLevel()
    {
        if (!isLevelCompleted)
        {
            CompleteCurrentLevel();
        }
    }
    
    [ContextMenu("跳转到下一关卡")]
    void SkipToNextLevel()
    {
        LoadNextLevel();
    }
    
    #endregion
    
    #region 玩家状态管理
    
    /// <summary>
    /// 保存玩家状态（在关卡完成时调用）
    /// </summary>
    void SavePlayerState()
    {
        // ✅ 数据已经在 GameRuntimeData 中，不需要额外保存
        if (showDebugInfo)
        {
            Debug.Log("LevelManager: 数据已保存在 GameRuntimeData 中");
        }
    }
    
    /// <summary>
    /// 恢复玩家状态（在新关卡开始时调用）
    /// </summary>
    void RestorePlayerState()
    {
        // ✅ 数据已经在 GameRuntimeData 中，不需要额外恢复
        if (showDebugInfo)
        {
            Debug.Log("LevelManager: 数据已从 GameRuntimeData 中获取");
        }
    }
    
    #endregion
}
