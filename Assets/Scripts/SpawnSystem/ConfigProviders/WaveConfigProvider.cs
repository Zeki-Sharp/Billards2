using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 波次配置提供者 - 管理关卡波次配置
/// 从EnemySpawner中提取波次管理逻辑，实现配置与生成分离
/// </summary>
public class WaveConfigProvider : MonoBehaviour, SpawnConfigProvider<EnemySpawn>
{
    [Header("关卡配置")]
    [SerializeField] private LevelConfig levelConfig;
    
    
    [Header("波次状态")]
    [SerializeField] private int currentWaveIndex = 0;
    
    [Header("调试")]
    [SerializeField] private bool showDebugInfo = true;
    
    // 缓存的波次配置列表
    private List<WaveConfig> waveConfigs = new List<WaveConfig>();
    
    /// <summary>
    /// 初始化配置提供者
    /// </summary>
    public void Initialize()
    {
        // 从LevelConfig加载波次配置
        LoadWaveConfigs();
        
        if (showDebugInfo)
        {
            Debug.Log($"[WaveConfigProvider] 初始化完成，加载了 {waveConfigs.Count} 个波次配置");
        }
    }
    
    /// <summary>
    /// 获取生成数据列表 - 返回当前波次的敌人配置
    /// </summary>
    /// <returns>当前波次的敌人生成配置列表</returns>
    public List<EnemySpawn> GetSpawnData()
    {
        WaveConfig currentWave = GetCurrentWave();
        if (currentWave == null)
        {
            return new List<EnemySpawn>();
        }
        
        return currentWave.enemySpawns;
    }
    
    /// <summary>
    /// 判断是否应该生成
    /// </summary>
    /// <returns>是否有可用的波次配置</returns>
    public bool ShouldSpawn()
    {
        return GetCurrentWave() != null;
    }
    
    /// <summary>
    /// 获取生成数量
    /// </summary>
    /// <returns>当前波次要生成的敌人总数</returns>
    public int GetSpawnCount()
    {
        WaveConfig currentWave = GetCurrentWave();
        if (currentWave == null)
        {
            return 0;
        }
        
        int totalCount = 0;
        foreach (var enemySpawn in currentWave.enemySpawns)
        {
            totalCount += enemySpawn.count;
        }
        
        if (showDebugInfo)
        {
            Debug.Log($"[WaveConfigProvider] 当前波次要生成 {totalCount} 个敌人");
        }
        
        return totalCount;
    }
    
    /// <summary>
    /// 重置配置提供者状态
    /// </summary>
    public void Reset()
    {
        currentWaveIndex = 0;
        if (showDebugInfo)
        {
            Debug.Log("[WaveConfigProvider] 重置波次索引");
        }
    }
    
    /// <summary>
    /// 获取当前波次配置
    /// </summary>
    /// <returns>当前波次配置</returns>
    public WaveConfig GetCurrentWave()
    {
        if (waveConfigs.Count == 0)
        {
            return null;
        }
        
        if (currentWaveIndex >= waveConfigs.Count)
        {
            if (levelConfig != null && levelConfig.loopWaves)
            {
                currentWaveIndex = 0;
            }
            else
            {
                return null; // 不循环且已超出范围
            }
        }
        
        return waveConfigs[currentWaveIndex];
    }
    
    /// <summary>
    /// 推进到下一个波次
    /// </summary>
    public void AdvanceToNextWave()
    {
        currentWaveIndex++;
        
        if (currentWaveIndex >= waveConfigs.Count && levelConfig != null && levelConfig.loopWaves)
        {
            currentWaveIndex = 0;
            if (showDebugInfo)
            {
                Debug.Log("[WaveConfigProvider] 波次循环，回到第一个波次");
            }
        }
        
        if (showDebugInfo)
        {
            Debug.Log($"[WaveConfigProvider] 推进到波次索引: {currentWaveIndex}");
        }
    }
    
    
    /// <summary>
    /// 获取初始敌人生成配置列表
    /// </summary>
    /// <returns>初始敌人生成配置列表</returns>
    public List<EnemySpawn> GetInitialSpawnData()
    {
        return levelConfig?.initialEnemies ?? new List<EnemySpawn>();
    }

    /// <summary>
    /// 是否应该生成初始敌人
    /// </summary>
    /// <returns>是否生成初始敌人</returns>
    public bool ShouldGenerateInitialEnemies()
    {
        return levelConfig?.generateInitialEnemies ?? false;
    }
    
    /// <summary>
    /// 获取当前波次索引
    /// </summary>
    /// <returns>当前波次索引</returns>
    public int GetCurrentWaveIndex()
    {
        return currentWaveIndex;
    }
    
    /// <summary>
    /// 获取总波次数
    /// </summary>
    /// <returns>总波次数</returns>
    public int GetTotalWaveCount()
    {
        return waveConfigs.Count;
    }
    
    /// <summary>
    /// 从LevelConfig加载波次配置
    /// </summary>
    private void LoadWaveConfigs()
    {
        waveConfigs.Clear();
        
        if (levelConfig != null)
        {
            waveConfigs.AddRange(levelConfig.waves);
        }
        else
        {
            Debug.LogWarning("[WaveConfigProvider] LevelConfig未设置，将使用空的波次配置");
        }
    }
    
    /// <summary>
    /// 设置关卡配置
    /// </summary>
    /// <param name="config">关卡配置</param>
    public void SetLevelConfig(LevelConfig config)
    {
        levelConfig = config;
        LoadWaveConfigs();
        
        if (showDebugInfo)
        {
            Debug.Log($"[WaveConfigProvider] 设置关卡配置: {config?.name}");
        }
    }
    
    /// <summary>
    /// 获取初始敌人生成范围配置
    /// </summary>
    /// <returns>初始敌人生成范围配置</returns>
    public SpawnRangeConfig GetInitialEnemySpawnRange()
    {
        if (levelConfig != null)
        {
            return levelConfig.initialEnemySpawnRange;
        }
        
        Debug.LogWarning("[WaveConfigProvider] LevelConfig为空，返回默认范围配置");
        return new SpawnRangeConfig();
    }
    
    /// <summary>
    /// 获取波次敌人生成范围配置
    /// </summary>
    /// <returns>波次敌人生成范围配置</returns>
    public SpawnRangeConfig GetWaveEnemySpawnRange()
    {
        if (levelConfig != null)
        {
            return levelConfig.waveEnemySpawnRange;
        }
        
        Debug.LogWarning("[WaveConfigProvider] LevelConfig为空，返回默认范围配置");
        return new SpawnRangeConfig();
    }
    
    /// <summary>
    /// 获取当前关卡配置
    /// </summary>
    /// <returns>当前关卡配置</returns>
    public LevelConfig GetCurrentLevelConfig()
    {
        return levelConfig;
    }
    
}
