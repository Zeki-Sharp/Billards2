using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 波次列表生成策略 - 专门为WaveConfigProvider设计
/// 
/// 【核心功能】：
/// - 从WaveConfigProvider获取当前波次的敌人列表
/// - 将EnemySpawn列表转换为EnemyData列表
/// - 支持初始敌人和波次敌人的生成
/// 
/// 【适用场景】：
/// - 敌人波次生成
/// - 预设敌人配置的批量生成
/// </summary>
[System.Serializable]
public class WaveListSpawnStrategy : ISpawnStrategy<EnemyData>
{
    [Header("波次配置")]
    [Tooltip("波次配置提供者")]
    public WaveConfigProvider configProvider;
    
    [Tooltip("是否生成初始敌人")]
    public bool generateInitialEnemies = true;
    
    [Tooltip("是否生成波次敌人")]
    public bool generateWaveEnemies = true;
    
    [Header("调试")]
    [Tooltip("是否启用调试日志")]
    public bool enableDebugLog = false;
    
    // 内部状态
    private bool hasGeneratedInitialEnemies = false;
    private List<EnemyData> currentSpawnList = new List<EnemyData>();
    
    /// <summary>
    /// 获取要生成的对象列表
    /// </summary>
    /// <returns>生成对象的EnemyData列表</returns>
    public List<EnemyData> GetSpawnList()
    {
        currentSpawnList.Clear();
        
        // 生成初始敌人
        if (generateInitialEnemies && !hasGeneratedInitialEnemies)
        {
            GenerateInitialEnemiesList();
            hasGeneratedInitialEnemies = true;
        }
        // 生成波次敌人
        else if (generateWaveEnemies)
        {
            GenerateWaveEnemiesList();
        }
        
        if (enableDebugLog)
        {
            Debug.Log($"[WaveListSpawnStrategy] 返回生成列表，数量: {currentSpawnList.Count}");
        }
        
        return new List<EnemyData>(currentSpawnList);
    }
    
    /// <summary>
    /// 获取生成数量
    /// </summary>
    /// <returns>本次生成的对象总数</returns>
    public int GetSpawnCount()
    {
        return currentSpawnList.Count;
    }
    
    /// <summary>
    /// 验证策略配置是否有效
    /// </summary>
    /// <returns>配置是否有效</returns>
    public bool ValidateConfig()
    {
        if (configProvider == null)
        {
            Debug.LogError("[WaveListSpawnStrategy] configProvider 未设置！");
            return false;
        }
        
        // 检查 WaveConfigProvider 是否已初始化
        if (configProvider is WaveConfigProvider waveProvider)
        {
            if (enableDebugLog)
            {
                Debug.Log($"[WaveListSpawnStrategy] WaveConfigProvider 状态检查:");
                Debug.Log($"- 当前波次索引: {waveProvider.GetCurrentWaveIndex()}");
                Debug.Log($"- 总波次数: {waveProvider.GetTotalWaveCount()}");
                Debug.Log($"- 是否应该生成: {waveProvider.ShouldSpawn()}");
                Debug.Log($"- 是否生成初始敌人: {waveProvider.ShouldGenerateInitialEnemies()}");
            }
        }
        
        return true;
    }
    
    /// <summary>
    /// 生成初始敌人列表
    /// </summary>
    private void GenerateInitialEnemiesList()
    {
        if (configProvider == null)
        {
            if (enableDebugLog)
            {
                Debug.LogError("[WaveListSpawnStrategy] configProvider 为空，无法生成初始敌人");
            }
            return;
        }
        
        if (!configProvider.ShouldGenerateInitialEnemies())
        {
            if (enableDebugLog)
            {
                Debug.Log("[WaveListSpawnStrategy] 配置为不生成初始敌人");
            }
            return;
        }
        
        List<EnemySpawn> initialEnemies = configProvider.GetInitialSpawnData();
        
        if (enableDebugLog)
        {
            Debug.Log($"[WaveListSpawnStrategy] 获取到初始敌人配置，数量: {initialEnemies.Count}");
        }
        
        ConvertEnemySpawnsToEnemyData(initialEnemies);
        
        if (enableDebugLog)
        {
            Debug.Log($"[WaveListSpawnStrategy] 生成初始敌人列表，数量: {currentSpawnList.Count}");
        }
    }
    
    /// <summary>
    /// 生成波次敌人列表
    /// </summary>
    private void GenerateWaveEnemiesList()
    {
        if (configProvider == null || !configProvider.ShouldSpawn())
        {
            if (enableDebugLog)
            {
                Debug.Log("[WaveListSpawnStrategy] 没有更多波次需要生成");
            }
            return;
        }
        
        List<EnemySpawn> currentWaveEnemies = configProvider.GetSpawnData();
        ConvertEnemySpawnsToEnemyData(currentWaveEnemies);
        
        // 推进到下一波次
        configProvider.AdvanceToNextWave();
        
        if (enableDebugLog)
        {
            Debug.Log($"[WaveListSpawnStrategy] 生成波次敌人列表，数量: {currentSpawnList.Count}");
        }
    }
    
    /// <summary>
    /// 将EnemySpawn列表转换为EnemyData列表
    /// </summary>
    /// <param name="enemySpawns">敌人生成配置列表</param>
    private void ConvertEnemySpawnsToEnemyData(List<EnemySpawn> enemySpawns)
    {
        foreach (var enemySpawn in enemySpawns)
        {
            for (int i = 0; i < enemySpawn.count; i++)
            {
                currentSpawnList.Add(enemySpawn.enemyData);
            }
        }
    }
    
    /// <summary>
    /// 重置策略状态
    /// </summary>
    public void ResetState()
    {
        hasGeneratedInitialEnemies = false;
        currentSpawnList.Clear();
        
        if (enableDebugLog)
        {
            Debug.Log("[WaveListSpawnStrategy] 状态已重置");
        }
    }
    
    /// <summary>
    /// 设置生成模式
    /// </summary>
    /// <param name="initial">是否生成初始敌人</param>
    /// <param name="wave">是否生成波次敌人</param>
    public void SetGenerationMode(bool initial, bool wave)
    {
        generateInitialEnemies = initial;
        generateWaveEnemies = wave;
        
        if (enableDebugLog)
        {
            Debug.Log($"[WaveListSpawnStrategy] 设置生成模式 - 初始: {initial}, 波次: {wave}");
        }
    }
}
