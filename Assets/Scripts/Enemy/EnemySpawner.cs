using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using MoreMountains.Feedbacks;


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
    public bool loopWaves = true; // 是否循环波次
    
    [Header("容器管理")]
    // 不再需要单独的预告特效预制体，使用EnemyData中的容器预制体
    
    [Header("测试设置")]
    public KeyCode spawnKey = KeyCode.Space; // 手动触发下一波
    public KeyCode toggleKey = KeyCode.T; // 切换生成开关
    
    [Header("事件")]
    public System.Action<List<Vector2>, List<EnemyData>> OnWavePreviewStart; // 波次预演开始事件
    
    private List<Enemy> spawnedEnemies = new List<Enemy>(); // 已生成的敌人列表
    
    // 容器管理
    private List<EnemyContainer> currentContainers = new List<EnemyContainer>(); // 当前容器列表
    private List<Vector2> previewedPositions = new List<Vector2>(); // 预告的位置列表
    private List<EnemyData> previewedEnemyData = new List<EnemyData>(); // 预告的敌人数据列表
    private int currentWaveIndex = 0; // 当前波次索引
    
    // 生成动画完成计数
    private int totalSpawnedEnemies = 0; // 本次生成的总敌人数量
    private int completedSpawnAnimations = 0; // 已完成生成动画的敌人数量
    
    // 预告阶段完成计数
    private int totalTelegraphAnimations = 0; // 预告阶段需要等待的动画总数
    private int completedTelegraphAnimations = 0; // 已完成的预告动画数量
    
    void Start()
    {
        if (waveConfigs == null || waveConfigs.Count == 0)
        {
            Debug.LogError("波次配置列表未设置或为空！");
        }
        
        // 订阅敌人阶段事件
        EnemyPhaseController.OnPhaseStart += OnPhaseStart;
        
        Debug.Log($"EnemySpawner初始化完成，生成范围: X({minX}~{maxX}), Y({minY}~{maxY})");
    }
    
    void OnDestroy()
    {
        // 取消订阅事件
        EnemyPhaseController.OnPhaseStart -= OnPhaseStart;
    }
    
    /// <summary>
    /// 阶段开始事件处理
    /// </summary>
    void OnPhaseStart(EnemyPhase phase)
    {
        Debug.Log($"EnemySpawner: 收到阶段事件 {phase}");
        
        switch (phase)
        {
            case EnemyPhase.Spawn:
                Debug.Log("EnemySpawner: 开始Spawn阶段");
                SpawnEnemiesWithCallback();
                break;
            case EnemyPhase.Telegraph:
                Debug.Log("EnemySpawner: 开始Telegraph阶段");
                StartPreviewWithCallback();
                break;
        }
    }
    
    /// <summary>
    /// 带回调的敌人生成
    /// </summary>
    void SpawnEnemiesWithCallback()
    {
        Debug.Log("开始生成敌人（带回调）");
        
        // 重置计数
        totalSpawnedEnemies = 0;
        completedSpawnAnimations = 0;
        
        if (currentContainers.Count == 0)
        {
            // 没有容器，不生成敌人
            Debug.Log("没有容器，跳过生成");
            OnSpawnAnimationComplete();
        }
        else
        {
            // 开始生成所有容器
            foreach (var container in currentContainers)
            {
                if (container != null)
                {
                    container.OnSpawnComplete += OnContainerSpawnComplete;
                    container.StartSpawn();
                    totalSpawnedEnemies++;
                }
            }
        }
    }
    
    /// <summary>
    /// 带回调的预告
    /// </summary>
    void StartPreviewWithCallback()
    {
        Debug.Log("开始预告下一波敌人（带回调）");
        
        // 重置预告计数
        totalTelegraphAnimations = 0;
        completedTelegraphAnimations = 0;
        
        // 预告下一波（currentWaveIndex + 1）
        int previewWaveIndex = currentWaveIndex + 1;
        if (previewWaveIndex >= waveConfigs.Count)
        {
            if (loopWaves)
            {
                previewWaveIndex = 0;
            }
            else
            {
                Debug.Log("所有波次已完成，跳过预告");
                OnTelegraphAnimationComplete();
                return;
            }
        }
        
        WaveConfig previewWave = waveConfigs[previewWaveIndex];
        if (previewWave == null || previewWave.enemySpawns.Count == 0)
        {
            Debug.LogWarning($"第{previewWaveIndex + 1}波配置无效，跳过预告");
            OnTelegraphAnimationComplete();
            return;
        }
        
        // 计算生成位置并记录
        previewedPositions.Clear();
        previewedEnemyData.Clear();
        
        foreach (var enemySpawn in previewWave.enemySpawns)
        {
            if (enemySpawn.enemyData == null) continue;
            
            for (int i = 0; i < enemySpawn.count; i++)
            {
                Vector2 spawnPosition;
                if (enemySpawn.useRandomPosition)
                {
                    spawnPosition = GenerateRandomPosition();
                }
                else
                {
                    spawnPosition = enemySpawn.customPosition;
                }
                
                // 记录位置和敌人数据
                previewedPositions.Add(spawnPosition);
                previewedEnemyData.Add(enemySpawn.enemyData);
            }
        }
        
        // 计算需要等待的动画数量
        // 1. 生成预告动画（每个预告特效一个）
        // 2. 敌人攻击范围预告动画（每个现有敌人一个）
        totalTelegraphAnimations = previewedPositions.Count + GetActiveEnemyCount();
        Debug.Log($"预告阶段需要等待 {totalTelegraphAnimations} 个动画完成");
        
        // 生成容器
        for (int i = 0; i < previewedPositions.Count && i < previewedEnemyData.Count; i++)
        {
            Vector2 position = previewedPositions[i];
            EnemyData enemyData = previewedEnemyData[i];
            
            if (enemyData != null && enemyData.enemyContainerPrefab != null)
            {
                GameObject containerObj = Instantiate(enemyData.enemyContainerPrefab, position, Quaternion.identity, enemyParent);
                EnemyContainer container = containerObj.GetComponent<EnemyContainer>();
                
                if (container != null)
                {
                    container.SetEnemyData(enemyData);
                    container.OnTelegraphComplete += OnContainerTelegraphComplete;
                    container.StartTelegraph();
                    currentContainers.Add(container);
                    totalTelegraphAnimations++;
                    Debug.Log($"开始容器预告 at {position}");
                }
                else
                {
                    Debug.LogError($"容器预制体 {enemyData.enemyContainerPrefab.name} 没有EnemyContainer组件！");
                    Destroy(containerObj);
                }
            }
            else
            {
                Debug.LogError($"敌人数据无效或容器预制体为空！");
            }
        }
        
        // 如果没有容器，立即完成
        if (currentContainers.Count == 0)
        {
            Debug.LogError("没有有效的容器，跳过预告");
            OnTelegraphAnimationComplete();
        }
    }
    
    /// <summary>
    /// 容器预告完成回调
    /// </summary>
    void OnContainerTelegraphComplete(EnemyContainer container)
    {
        Debug.Log($"容器预告完成 at {container.transform.position}");
        OnTelegraphAnimationComplete();
    }
    
    /// <summary>
    /// 容器生成完成回调
    /// </summary>
    void OnContainerSpawnComplete(EnemyContainer container)
    {
        Debug.Log($"容器生成完成 at {container.transform.position}");
        
        // 获取敌人组件并添加到列表
        Enemy enemy = container.GetEnemy();
        if (enemy != null)
        {
            spawnedEnemies.Add(enemy);
        }
        
        OnSpawnAnimationComplete();
    }
    
    /// <summary>
    /// 带回调的敌人生成（已废弃，使用容器系统）
    /// </summary>
    void SpawnEnemyFromDataWithCallback(EnemyData enemyData, bool useRandomPosition = true, Vector2 customPosition = default)
    {
        Debug.LogWarning("SpawnEnemyFromDataWithCallback已废弃，请使用容器系统");
        OnSpawnAnimationComplete();
    }
    
    /// <summary>
    /// 生成动画完成回调
    /// </summary>
    void OnSpawnAnimationComplete()
    {
        completedSpawnAnimations++;
        Debug.Log($"敌人生成动画完成 {completedSpawnAnimations}/{totalSpawnedEnemies}");
        
        // 检查是否所有敌人生成动画都完成了
        if (completedSpawnAnimations >= totalSpawnedEnemies)
        {
            Debug.Log("所有敌人生成动画完成，通知阶段完成");
            
            // 通知阶段完成
            if (EnemyPhaseController.Instance != null)
            {
                EnemyPhaseController.Instance.OnEnemyPhaseActionComplete();
            }
        }
    }
    
    /// <summary>
    /// 预告动画完成回调
    /// </summary>
    public void OnTelegraphAnimationComplete()
    {
        completedTelegraphAnimations++;
        Debug.Log($"预告动画完成 {completedTelegraphAnimations}/{totalTelegraphAnimations}");
        
        // 检查是否所有预告动画都完成了
        if (completedTelegraphAnimations >= totalTelegraphAnimations)
        {
            Debug.Log("所有预告动画完成，通知阶段完成");
            
            // 通知阶段完成
            if (EnemyPhaseController.Instance != null)
            {
                EnemyPhaseController.Instance.OnEnemyPhaseActionComplete();
            }
        }
    }
    
    /// <summary>
    /// 获取当前活跃敌人数量
    /// </summary>
    int GetActiveEnemyCount()
    {
        return spawnedEnemies.Count;
    }
    
    
    void Update()
    {
        // 保留手动控制（用于测试）
        if (Input.GetKeyDown(spawnKey))
        {
            StartPreview();
        }
    }
    
    /// <summary>
    /// 获取生成位置
    /// </summary>
    Vector2 GetSpawnPosition(bool useRandom, Vector2 customPosition)
    {
        if (useRandom)
        {
            return GenerateRandomPosition();
        }
        else
        {
            return customPosition;
        }
    }
    
    /// <summary>
    /// 获取存活的敌人数量
    /// </summary>
    public int GetAliveEnemyCount()
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
    /// 根据配置数据生成敌人（已废弃，使用容器系统）
    /// </summary>
    void SpawnEnemyFromData(EnemyData enemyData, bool useRandomPosition = true, Vector2 customPosition = default)
    {
        Debug.LogWarning("SpawnEnemyFromData已废弃，请使用容器系统");
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
    
    // 获取所有敌人
    public List<Enemy> GetAllEnemies()
    {
        return new List<Enemy>(spawnedEnemies);
    }
    
    /// <summary>
    /// 开始预告（由EnemyPhaseController调用）
    /// </summary>
    public void StartPreview()
    {
        Debug.Log("开始预告下一波敌人");
        
        // 预告下一波（currentWaveIndex + 1）
        int previewWaveIndex = currentWaveIndex + 1;
        if (previewWaveIndex >= waveConfigs.Count)
        {
            if (loopWaves)
            {
                previewWaveIndex = 0;
            }
            else
            {
                Debug.Log("所有波次已完成，跳过预告");
                // 通知阶段完成，但不预告
                if (EnemyPhaseController.Instance != null)
                {
                    EnemyPhaseController.Instance.OnEnemyPhaseActionComplete();
                }
                return;
            }
        }
        
        WaveConfig previewWave = waveConfigs[previewWaveIndex];
        if (previewWave == null || previewWave.enemySpawns.Count == 0)
        {
            Debug.LogWarning($"第{previewWaveIndex + 1}波配置无效，跳过预告");
            // 通知阶段完成，但不预告
            if (EnemyPhaseController.Instance != null)
            {
                EnemyPhaseController.Instance.OnEnemyPhaseActionComplete();
            }
            return;
        }
        
        // 计算生成位置并记录
        previewedPositions.Clear();
        previewedEnemyData.Clear();
        
        foreach (var enemySpawn in previewWave.enemySpawns)
        {
            if (enemySpawn.enemyData == null) continue;
            
            for (int i = 0; i < enemySpawn.count; i++)
            {
                Vector2 spawnPosition;
                if (enemySpawn.useRandomPosition)
                {
                    spawnPosition = GenerateRandomPosition();
                }
                else
                {
                    spawnPosition = enemySpawn.customPosition;
                }
                
                // 记录位置和敌人数据
                previewedPositions.Add(spawnPosition);
                previewedEnemyData.Add(enemySpawn.enemyData);
            }
        }
        
        // 生成容器（使用新的容器系统）
        for (int i = 0; i < previewedPositions.Count && i < previewedEnemyData.Count; i++)
        {
            Vector2 position = previewedPositions[i];
            EnemyData enemyData = previewedEnemyData[i];
            
            if (enemyData != null && enemyData.enemyContainerPrefab != null)
            {
                GameObject containerObj = Instantiate(enemyData.enemyContainerPrefab, position, Quaternion.identity, enemyParent);
                EnemyContainer container = containerObj.GetComponent<EnemyContainer>();
                
                if (container != null)
                {
                    container.SetEnemyData(enemyData);
                    container.StartTelegraph();
                    currentContainers.Add(container);
                    Debug.Log($"开始容器预告 at {position}");
                }
                else
                {
                    Debug.LogError($"容器预制体 {enemyData.enemyContainerPrefab.name} 没有EnemyContainer组件！");
                    Destroy(containerObj);
                }
            }
            else
            {
                Debug.LogError($"敌人数据无效或容器预制体为空！");
            }
        }
        
        // 预告阶段立即完成
        if (EnemyPhaseController.Instance != null)
        {
            EnemyPhaseController.Instance.OnEnemyPhaseActionComplete();
        }
        
        Debug.Log("预告完成");
    }
    
    /// <summary>
    /// 生成敌人（由EnemyPhaseController调用）
    /// </summary>
    public void SpawnEnemies()
    {
        Debug.Log("开始生成敌人");
        
        // 开始生成所有容器
        foreach (var container in currentContainers)
        {
            if (container != null)
            {
                container.StartSpawn();
                Debug.Log($"开始容器生成 at {container.transform.position}");
            }
        }
        
        if (previewedPositions.Count == 0)
        {
            // 没有预告位置，不生成敌人
            Debug.Log("没有预告位置，跳过生成");
        }
        else
        {
            // 有预告位置，使用预告的位置生成
            SpawnPreviewedWave();
        }
        
        // 清理容器和数据
        ClearContainers();
        
        // 通知阶段完成
        if (EnemyPhaseController.Instance != null)
        {
            EnemyPhaseController.Instance.OnEnemyPhaseActionComplete();
        }
        
        Debug.Log("敌人生成完成");
    }
    
    
    /// <summary>
    /// 使用预告位置生成敌人（已废弃，使用容器系统）
    /// </summary>
    void SpawnPreviewedWave()
    {
        Debug.LogWarning("SpawnPreviewedWave已废弃，使用容器系统");
        // 切换到下一波
        currentWaveIndex++;
    }
    
    /// <summary>
    /// 清理容器和数据
    /// </summary>
    void ClearContainers()
    {
        foreach (var container in currentContainers)
        {
            if (container != null)
            {
                container.OnTelegraphComplete -= OnContainerTelegraphComplete;
                container.OnSpawnComplete -= OnContainerSpawnComplete;
                Destroy(container.gameObject);
            }
        }
        currentContainers.Clear();
        
        // 清理记录的数据
        previewedPositions.Clear();
        previewedEnemyData.Clear();
    }

}

