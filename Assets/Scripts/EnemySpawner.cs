using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class EnemySpawner : MonoBehaviour
{
    [Header("敌人生成设置")]
    public Transform enemyParent; // 敌人父物体（可选）
    
    [Header("生成范围设置")]
    public float minX = -10f; // 生成范围左边界
    public float maxX = 10f;  // 生成范围右边界
    public float minY = -5f;  // 生成范围下边界
    public float maxY = 5f;   // 生成范围上边界
    
    [Header("波次配置")]
    public List<WaveConfig> waveConfigs = new List<WaveConfig>(); // 波次配置列表
    public float globalWaveInterval = 10f; // 全局波次间隔（秒）
    public int maxWaves = 10; // 最大波次数（0表示无限）
    
    [Header("生成控制")]
    public bool autoStart = true; // 是否自动开始生成
    public bool loopWaves = true; // 是否循环波次
    
    [Header("测试设置")]
    public KeyCode spawnKey = KeyCode.Space; // 手动触发下一波
    public KeyCode toggleKey = KeyCode.T; // 切换生成开关
    
    private List<Enemy> spawnedEnemies = new List<Enemy>(); // 已生成的敌人列表
    private Player targetPlayer;
    
    // 生成状态
    private int currentWaveIndex = 0; // 当前波次索引
    private float lastWaveTime = 0f; // 上次生成波次的时间
    private bool isSpawning = false; // 是否正在生成
    private int totalWavesSpawned = 0; // 已生成的波次总数
    private bool isCurrentWaveActive = false; // 当前波次是否正在生成中
    
    void Start()
    {
        targetPlayer = FindAnyObjectByType<Player>();
        
        if (waveConfigs == null || waveConfigs.Count == 0)
        {
            Debug.LogError("波次配置列表未设置或为空！");
        }
        
        if (autoStart)
        {
            StartSpawning();
        }
        
        Debug.Log($"EnemySpawner初始化完成，生成范围: X({minX}~{maxX}), Y({minY}~{maxY}), 波次间隔: {globalWaveInterval}秒");
    }
    
    void Update()
    {
        // 自动生成波次
        if (isSpawning && !isCurrentWaveActive)
        {
            // 使用ScaledTime，自动处理时间缩放
            if (ScaledTime.time - lastWaveTime >= globalWaveInterval)
            {
                SpawnNextWave();
            }
        }
        
        // 手动控制
        if (Input.GetKeyDown(spawnKey))
        {
            SpawnNextWave();
        }
        
        if (Input.GetKeyDown(toggleKey))
        {
            ToggleSpawning();
        }
    }
    
    /// <summary>
    /// 开始生成
    /// </summary>
    public void StartSpawning()
    {
        isSpawning = true;
        lastWaveTime = ScaledTime.time;
        Debug.Log("敌人生成已开始");
    }
    
    /// <summary>
    /// 停止生成
    /// </summary>
    public void StopSpawning()
    {
        isSpawning = false;
        Debug.Log("敌人生成已停止");
    }
    
    /// <summary>
    /// 切换生成状态
    /// </summary>
    public void ToggleSpawning()
    {
        if (isSpawning)
        {
            StopSpawning();
        }
        else
        {
            StartSpawning();
        }
    }
    
    /// <summary>
    /// 生成下一波敌人
    /// </summary>
    public void SpawnNextWave()
    {
        // 检查是否达到最大波次限制
        if (maxWaves > 0 && totalWavesSpawned >= maxWaves)
        {
            Debug.Log("已达到最大波次限制，停止生成");
            StopSpawning();
            return;
        }
        
        // 检查波次配置是否有效
        if (currentWaveIndex >= waveConfigs.Count)
        {
            if (loopWaves)
            {
                currentWaveIndex = 0; // 循环到第一个波次
            }
            else
            {
                Debug.Log("所有波次配置已完成");
                StopSpawning();
                return;
            }
        }
        
        WaveConfig currentWave = waveConfigs[currentWaveIndex];
        if (currentWave == null || currentWave.enemySpawns.Count == 0)
        {
            Debug.LogWarning($"第{currentWaveIndex + 1}波配置无效，跳过");
            currentWaveIndex++;
            return;
        }
        
        // 开始生成当前波次
        StartCoroutine(SpawnWave(currentWave));
        
        // 更新状态
        lastWaveTime = ScaledTime.time;
        totalWavesSpawned++;
        currentWaveIndex++;
        
        Debug.Log($"开始生成第{totalWavesSpawned}波: {currentWave.waveName}");
    }
    
    /// <summary>
    /// 生成指定波次的敌人
    /// </summary>
    IEnumerator SpawnWave(WaveConfig waveConfig)
    {
        isCurrentWaveActive = true;
        
        // 等待波次延迟
        if (waveConfig.waveDelay > 0)
        {
            yield return ScaledTime.WaitForSeconds(waveConfig.waveDelay);
        }
        
        // 检查是否需要等待上一波敌人全部死亡
        if (waveConfig.waitForPreviousWave)
        {
            yield return new WaitUntil(() => GetAliveEnemyCount() == 0);
        }
        
        // 生成这一波的所有敌人
        foreach (var enemySpawn in waveConfig.enemySpawns)
        {
            if (enemySpawn.enemyData == null) continue;
            
            // 等待敌人生成延迟
            if (enemySpawn.spawnDelay > 0)
            {
                yield return ScaledTime.WaitForSeconds(enemySpawn.spawnDelay);
            }
            
            // 生成指定数量的敌人
            for (int i = 0; i < enemySpawn.count; i++)
            {
                SpawnEnemyFromData(enemySpawn.enemyData, enemySpawn.useRandomPosition, enemySpawn.customPosition);
                
                // 等待生成间隔
                if (enemySpawn.spawnInterval > 0 && i < enemySpawn.count - 1)
                {
                    yield return ScaledTime.WaitForSeconds(enemySpawn.spawnInterval);
                }
            }
        }
        
        isCurrentWaveActive = false;
        Debug.Log($"第{totalWavesSpawned}波生成完成，当前敌人总数: {spawnedEnemies.Count}");
    }
    
    /// <summary>
    /// 获取存活的敌人数量
    /// </summary>
    int GetAliveEnemyCount()
    {
        int aliveCount = 0;
        foreach (var enemy in spawnedEnemies)
        {
            if (enemy != null && enemy.IsAlive())
            {
                aliveCount++;
            }
        }
        return aliveCount;
    }
    
    /// <summary>
    /// 根据配置数据生成敌人
    /// </summary>
    void SpawnEnemyFromData(EnemyData enemyData, bool useRandomPosition = true, Vector2 customPosition = default)
    {
        if (enemyData == null || enemyData.enemyPrefab == null)
        {
            Debug.LogError("敌人配置数据无效！");
            return;
        }
        
        Vector3 spawnPosition;
        if (useRandomPosition)
        {
            spawnPosition = GenerateRandomPosition();
        }
        else
        {
            spawnPosition = customPosition;
        }
        
        GameObject enemyObj = Instantiate(enemyData.enemyPrefab, spawnPosition, Quaternion.identity, enemyParent);
        
        // 获取敌人组件并设置配置数据
        Enemy enemy = enemyObj.GetComponent<Enemy>();
        if (enemy != null)
        {
            enemy.enemyData = enemyData;
            spawnedEnemies.Add(enemy);
            enemy.InitializeAttackRange();
        }
        else
        {
            Debug.LogError("敌人预制体上没有Enemy组件！");
            Destroy(enemyObj);
        }
    }
    
    
    // 在固定范围内生成随机位置
    Vector3 GenerateRandomPosition()
    {
        float randomX = Random.Range(minX, maxX);
        float randomY = Random.Range(minY, maxY);
        return new Vector3(randomX, randomY, 0f);
    }
    
    // 清除所有生成的敌人
    public void ClearAllEnemies()
    {
        foreach (Enemy enemy in spawnedEnemies)
        {
            if (enemy != null)
            {
                Destroy(enemy.gameObject);
            }
        }
        spawnedEnemies.Clear();
        Debug.Log("已清除所有生成的敌人");
    }
    
    // 获取当前敌人数量
    public int GetEnemyCount()
    {
        return spawnedEnemies.Count;
    }
    
    // 获取所有敌人
    public List<Enemy> GetAllEnemies()
    {
        return new List<Enemy>(spawnedEnemies);
    }
    
    void OnGUI()
    {
        GUI.Label(new Rect(10, 10, 300, 20), $"生成状态: {(isSpawning ? "运行中" : "已停止")}");
        GUI.Label(new Rect(10, 30, 300, 20), $"当前敌人数量: {GetAliveEnemyCount()}");
        GUI.Label(new Rect(10, 50, 300, 20), $"已生成波次: {totalWavesSpawned}/{(maxWaves > 0 ? maxWaves.ToString() : "∞")}");
        GUI.Label(new Rect(10, 70, 300, 20), $"当前波次: {currentWaveIndex + 1}/{waveConfigs.Count}");
        
        if (isSpawning && !isCurrentWaveActive)
        {
            float timeUntilNext = globalWaveInterval - (Time.time - lastWaveTime);
            GUI.Label(new Rect(10, 90, 300, 20), $"下次生成倒计时: {timeUntilNext:F1}秒");
        }
        else if (isCurrentWaveActive)
        {
            GUI.Label(new Rect(10, 90, 300, 20), "正在生成当前波次...");
        }
        
        GUI.Label(new Rect(10, 110, 300, 20), $"按 {spawnKey} 手动生成下一波");
        GUI.Label(new Rect(10, 130, 300, 20), $"按 {toggleKey} 切换生成开关");
        
        if (maxWaves > 0 && totalWavesSpawned >= maxWaves)
        {
            GUI.Label(new Rect(10, 150, 300, 20), "已达到最大波次限制！");
        }
    }
    
    // 获取剩余敌人数（包括待生成的）
    public int GetRemainingEnemiesCount()
    {
        int count = GetAliveEnemyCount(); // 当前存活的敌人数量
        
        // 如果生成已停止，只返回当前敌人数量
        if (!isSpawning)
        {
            return count;
        }
        
        // 计算待生成的敌人数量
        if (maxWaves > 0)
        {
            // 有最大波次限制的情况
            int remainingWaves = maxWaves - totalWavesSpawned;
            if (remainingWaves > 0)
            {
                // 计算剩余波次的敌人数量
                int waveIndex = currentWaveIndex;
                for (int i = 0; i < remainingWaves; i++)
                {
                    if (waveIndex < waveConfigs.Count)
                    {
                        WaveConfig wave = waveConfigs[waveIndex];
                        if (wave != null)
                        {
                            foreach (var enemySpawn in wave.enemySpawns)
                            {
                                if (enemySpawn.enemyData != null)
                                {
                                    count += enemySpawn.count;
                                }
                            }
                        }
                    }
                    waveIndex++;
                    if (waveIndex >= waveConfigs.Count)
                    {
                        if (loopWaves)
                        {
                            waveIndex = 0; // 循环到第一个波次
                        }
                        else
                        {
                            break; // 不循环，停止计算
                        }
                    }
                }
            }
        }
        
        return count;
    }
}

