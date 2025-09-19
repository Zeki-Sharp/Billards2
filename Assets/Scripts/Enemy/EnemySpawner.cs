using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 敌人生成器 - 管理每个回合的敌人生成
/// 
/// 【核心功能】：
/// - 配置每个回合要生成的敌人数据、数目和生成范围
/// - 在指定范围内随机生成敌人位置
/// - 支持多波次配置和循环生成
/// - 与 EnemyController 协调生成时机
/// </summary>
public class EnemySpawner : MonoBehaviour
{
    [Header("敌人生成设置")]
    [SerializeField] private Transform enemyParent; // 敌人父对象
    
    [Header("生成范围设置")]
    [SerializeField] private float minX = -10f;
    [SerializeField] private float maxX = 10f;
    [SerializeField] private float minY = -5f;
    [SerializeField] private float maxY = 5f;
    [SerializeField] private bool useCircularRange = false; // 是否使用圆形范围
    [SerializeField] private float circularRadius = 8f; // 圆形范围半径
    
    [Header("波次配置")]
    [SerializeField] private List<WaveConfig> waveConfigs = new List<WaveConfig>();
    [SerializeField] private bool loopWaves = true;
    [SerializeField] private int currentWaveIndex = 0;
    
    [Header("调试")]
    [SerializeField] private bool showDebugInfo = true;
    [SerializeField] private bool showSpawnRange = true;
    
    // 组件引用
    private EnemyController enemyController;
    
    void Start()
    {
        InitializeSpawner();
    }
    
    /// <summary>
    /// 初始化生成器
    /// </summary>
    void InitializeSpawner()
    {
        // 查找 EnemyController
        enemyController = FindFirstObjectByType<EnemyController>();
        if (enemyController == null)
        {
            Debug.LogError("EnemySpawner: 未找到 EnemyController！");
        }
        
        // 如果没有设置父对象，使用当前对象
        if (enemyParent == null)
        {
            enemyParent = transform;
        }
        
        if (showDebugInfo)
        {
            Debug.Log($"EnemySpawner: 初始化完成，生成范围: {GetSpawnRangeDescription()}");
        }
    }
    
    /// <summary>
    /// 计算当前回合要生成的敌人数量
    /// </summary>
    public int CalculateSpawnCount()
    {
        if (waveConfigs.Count == 0)
        {
            if (showDebugInfo)
            {
                Debug.LogWarning("EnemySpawner: 没有配置波次，返回0");
            }
            return 0;
        }
        
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
            Debug.Log($"EnemySpawner: 当前波次 {currentWave.waveName} 要生成 {totalCount} 个敌人");
        }
        
        return totalCount;
    }
    
    /// <summary>
    /// 生成敌人
    /// </summary>
    public void GenerateEnemies()
    {
        WaveConfig currentWave = GetCurrentWave();
        if (currentWave == null)
        {
            if (showDebugInfo)
            {
                Debug.LogWarning("EnemySpawner: 没有可用的波次配置");
            }
            return;
        }
        
        if (showDebugInfo)
        {
            Debug.Log($"EnemySpawner: 开始生成波次 {currentWave.waveName} 的敌人");
        }
        
        // 生成每种类型的敌人
        foreach (var enemySpawn in currentWave.enemySpawns)
        {
            for (int i = 0; i < enemySpawn.count; i++)
            {
                GenerateSingleEnemy(enemySpawn);
            }
        }
        
        // 进入下一个波次
        AdvanceToNextWave();
    }
    
    /// <summary>
    /// 生成单个敌人
    /// </summary>
    void GenerateSingleEnemy(EnemySpawn enemySpawn)
    {
        if (enemySpawn.enemyData == null || enemySpawn.enemyData.enemyContainerPrefab == null)
        {
            Debug.LogError("EnemySpawner: 敌人数据或预制体为空！");
            return;
        }
        
        // 计算生成位置
        Vector3 spawnPosition = CalculateSpawnPosition(enemySpawn);
        
        // 实例化敌人
        GameObject enemyInstance = Instantiate(enemySpawn.enemyData.enemyContainerPrefab, spawnPosition, Quaternion.identity, enemyParent);
        
        // 设置敌人数据
        Enemy enemy = enemyInstance.GetComponent<Enemy>();
        if (enemy != null)
        {
            enemy.SetEnemyData(enemySpawn.enemyData);
            
            // 注册到预告列表（新生成的敌人先进入预告阶段）
            if (enemyController != null)
            {
                enemyController.RegisterTelegraphingEnemy(enemy);
            }
        }
        
        if (showDebugInfo)
        {
            Debug.Log($"EnemySpawner: 生成敌人 {enemySpawn.enemyData.enemyName} 在位置 {spawnPosition}");
        }
    }
    
    /// <summary>
    /// 计算生成位置
    /// </summary>
    Vector3 CalculateSpawnPosition(EnemySpawn enemySpawn)
    {
        if (!enemySpawn.useRandomPosition)
        {
            return enemySpawn.customPosition;
        }
        
        Vector3 position;
        
        if (useCircularRange)
        {
            // 圆形范围生成
            float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
            float distance = Random.Range(0f, circularRadius);
            position = new Vector3(
                Mathf.Cos(angle) * distance,
                Mathf.Sin(angle) * distance,
                0f
            );
        }
        else
        {
            // 矩形范围生成
            position = new Vector3(
                Random.Range(minX, maxX),
                Random.Range(minY, maxY),
                0f
            );
        }
        
        return position;
    }
    
    /// <summary>
    /// 获取当前波次
    /// </summary>
    WaveConfig GetCurrentWave()
    {
        if (waveConfigs.Count == 0)
        {
            return null;
        }
        
        if (currentWaveIndex >= waveConfigs.Count)
        {
            if (loopWaves)
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
    /// 进入下一个波次
    /// </summary>
    void AdvanceToNextWave()
    {
        currentWaveIndex++;
        
        if (currentWaveIndex >= waveConfigs.Count && loopWaves)
        {
            currentWaveIndex = 0;
            if (showDebugInfo)
            {
                Debug.Log("EnemySpawner: 波次循环，回到第一个波次");
            }
        }
    }
    
    /// <summary>
    /// 获取生成范围描述
    /// </summary>
    string GetSpawnRangeDescription()
    {
        if (useCircularRange)
        {
            return $"圆形范围，半径: {circularRadius}";
        }
        else
        {
            return $"矩形范围: X({minX}~{maxX}), Y({minY}~{maxY})";
        }
    }
    
    /// <summary>
    /// 重置波次索引
    /// </summary>
    public void ResetWaveIndex()
    {
        currentWaveIndex = 0;
        if (showDebugInfo)
        {
            Debug.Log("EnemySpawner: 重置波次索引");
        }
    }
    
    /// <summary>
    /// 设置生成范围（矩形）
    /// </summary>
    public void SetSpawnRange(float minX, float maxX, float minY, float maxY)
    {
        this.minX = minX;
        this.maxX = maxX;
        this.minY = minY;
        this.maxY = maxY;
        this.useCircularRange = false;
        
        if (showDebugInfo)
        {
            Debug.Log($"EnemySpawner: 设置矩形生成范围 X({minX}~{maxX}), Y({minY}~{maxY})");
        }
    }
    
    /// <summary>
    /// 设置生成范围（圆形）
    /// </summary>
    public void SetSpawnRange(float radius)
    {
        this.circularRadius = radius;
        this.useCircularRange = true;
        
        if (showDebugInfo)
        {
            Debug.Log($"EnemySpawner: 设置圆形生成范围，半径: {radius}");
        }
    }
    
    // 调试绘制生成范围
    void OnDrawGizmosSelected()
    {
        if (!showSpawnRange) return;
        
        Gizmos.color = Color.green;
        
        if (useCircularRange)
        {
            // 绘制圆形范围
            Gizmos.DrawWireSphere(transform.position, circularRadius);
        }
        else
        {
            // 绘制矩形范围
            Vector3 center = new Vector3((minX + maxX) / 2f, (minY + maxY) / 2f, 0f);
            Vector3 size = new Vector3(maxX - minX, maxY - minY, 0f);
            Gizmos.DrawWireCube(center, size);
        }
    }
}
