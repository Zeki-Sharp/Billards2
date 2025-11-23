using UnityEngine;
using System.Collections.Generic;
using DeepSpaceLabs.SAM;

/// <summary>
/// 敌人生成器 - 专注于敌人生成执行
/// 
/// 【核心功能】：
/// - 继承BaseSpawner，提供敌人生成的具体实现
/// - 实例化敌人预制体并设置数据
/// - 注册生成的敌人到EnemyController
/// </summary>
public class EnemySpawner : BaseSpawner<EnemyData>
{
    [Header("敌人生成设置")]
    [SerializeField] private Transform enemyParent; // 敌人父对象
    
    [Header("调试")]
    [SerializeField] private bool showDebugInfo = true;
    
    // 组件引用
    private EnemyManager enemyManager;
    
    protected override void Start()
    {
        base.Start();
        InitializeSpawner();
    }
    
    /// <summary>
    /// 初始化生成器
    /// </summary>
    void InitializeSpawner()
    {
        // 直接访问单例
        enemyManager = EnemyManager.Instance;
        if (enemyManager == null)
        {
            Debug.LogError("EnemySpawner: 未找到 EnemyManager！");
        }
        
        // 如果没有设置父对象，使用当前对象
        if (enemyParent == null)
        {
            enemyParent = transform;
        }
        
        if (showDebugInfo)
        {
            Debug.Log("EnemySpawner: 初始化完成");
        }
    }
    
    /// <summary>
    /// 实例化敌人对象
    /// </summary>
    /// <param name="data">敌人数据</param>
    /// <param name="position">生成位置（3D坐标，来自SpawnRangeConfig.GetValidRandomPosition()）</param>
    /// <param name="parent">父对象</param>
    /// <returns>实例化的敌人GameObject</returns>
    protected override GameObject InstantiateObject(EnemyData data, Vector3 position, Transform parent)
    {
        if (data == null || data.enemyContainerPrefab == null)
        {
            Debug.LogError("EnemySpawner: 敌人数据或预制体为空！");
            return null;
        }
        
        // ✅ position 已经是3D坐标 (x, y, z)，直接使用
        // ✅ 地面对齐由 GroundAlignAnchor 组件在 Awake() 中自动处理
        GameObject enemyInstance = Instantiate(data.enemyContainerPrefab, position, Quaternion.identity, parent);
        
        // ✅ 确保有 GroundAlignAnchor 组件（如果没有则自动添加）
        GroundAlignAnchor alignAnchor = enemyInstance.GetComponent<GroundAlignAnchor>();
        if (alignAnchor == null)
        {
            alignAnchor = enemyInstance.AddComponent<GroundAlignAnchor>();
            if (showDebugInfo)
            {
                Debug.Log($"EnemySpawner: 自动添加 GroundAlignAnchor 组件到 {enemyInstance.name}");
            }
        }
        
        if (showDebugInfo)
        {
            Debug.Log($"EnemySpawner: 生成敌人 {data.info.name} 在位置 {position}");
        }
        
        return enemyInstance;
    }
    
    /// <summary>
    /// 生成后处理 - 设置敌人数据并注册到Controller
    /// </summary>
    /// <param name="spawnedObject">生成的对象</param>
    /// <param name="data">敌人数据</param>
    protected override void OnPostSpawn(GameObject spawnedObject, EnemyData data)
    {
        OnPostSpawnWithLevel(spawnedObject, data, 1);  // 默认 Level 1
    }
    
    /// <summary>
    /// 生成后处理（带等级参数）
    /// </summary>
    private void OnPostSpawnWithLevel(GameObject spawnedObject, EnemyData data, int level)
    {
        // 父对象已在Instantiate时设置，无需再次设置
        
        // 设置敌人数据
        Enemy enemy = spawnedObject.GetComponent<Enemy>();
        
        if (enemy != null)
        {
            enemy.SetEnemyData(data, level);  // ✅ 传递等级参数
            
            // 直接完成一次生成阶段以切换可见性，并注册为激活敌人（统一单列表管理）
            if (enemyManager != null)
            {
                enemy.StartPhase(EnemyPhase.Spawn);
                enemyManager.RegisterActiveEnemy(enemy);
            }
            
        }
        else
        {
            Debug.LogError($"EnemySpawner: 在 {spawnedObject.name} 上未找到 Enemy 组件！");
        }
    }
    
    
    /// <summary>
    /// 生成敌人（使用外部范围配置）
    /// </summary>
    /// <param name="enemyData">敌人数据</param>
    /// <param name="rangeConfig">范围配置</param>
    public void SpawnEnemy(EnemyData enemyData, SpawnRangeConfig rangeConfig)
    {
        Spawn(enemyData, null, rangeConfig);
    }
    
    /// <summary>
    /// 批量生成敌人（支持等级参数）
    /// </summary>
    /// <param name="enemySpawns">敌人生成配置列表</param>
    /// <param name="rangeConfig">范围配置（可选）</param>
    public void GenerateEnemiesFromList(List<EnemySpawn> enemySpawns, SpawnRangeConfig rangeConfig = null)
    {
        foreach (var enemySpawn in enemySpawns)
        {
            for (int i = 0; i < enemySpawn.count; i++)
            {
                // ✅ 生成敌人并设置等级
                SpawnEnemyWithLevel(enemySpawn.enemyData, enemySpawn.level, rangeConfig);
            }
        }
        
        if (showDebugInfo)
        {
            Debug.Log($"EnemySpawner: 批量生成完成，共 {enemySpawns.Count} 种敌人");
        }
    }
    
    /// <summary>
    /// 生成指定等级的敌人
    /// </summary>
    private void SpawnEnemyWithLevel(EnemyData enemyData, int level, SpawnRangeConfig rangeConfig = null)
    {
        // ✅ 自动避开障碍物（墙体/玩家/敌人）
        // checkObstacles = true 时使用 Physics2D 检测，false 时使用随机位置
        Vector3 spawnPosition = rangeConfig != null ? rangeConfig.GetValidRandomPosition() : transform.position;
        GameObject spawnedObject = InstantiateObject(enemyData, spawnPosition, enemyParent);
        
        if (spawnedObject != null)
        {
            // ✅ 使用带等级的后处理方法
            OnPostSpawnWithLevel(spawnedObject, enemyData, level);
        }
    }
}
