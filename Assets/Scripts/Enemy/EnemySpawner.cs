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
    private EnemyController enemyController;
    
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
            Debug.Log("EnemySpawner: 初始化完成");
        }
    }
    
    /// <summary>
    /// 实例化敌人对象（像旧系统一样直接指定位置和父对象）
    /// </summary>
    /// <param name="data">敌人数据</param>
    /// <param name="position">生成位置</param>
    /// <param name="parent">父对象</param>
    /// <returns>实例化的敌人GameObject</returns>
    protected override GameObject InstantiateObject(EnemyData data, Vector3 position, Transform parent)
    {
        if (data == null || data.enemyContainerPrefab == null)
        {
            Debug.LogError("EnemySpawner: 敌人数据或预制体为空！");
            return null;
        }
        
        // 像旧系统一样直接指定位置和父对象
        GameObject enemyInstance = Instantiate(data.enemyContainerPrefab, position, Quaternion.identity, parent);
        
        
        return enemyInstance;
    }
    
    /// <summary>
    /// 生成后处理 - 设置敌人数据并注册到Controller
    /// </summary>
    /// <param name="spawnedObject">生成的对象</param>
    /// <param name="data">敌人数据</param>
    protected override void OnPostSpawn(GameObject spawnedObject, EnemyData data)
    {
        // 父对象已在Instantiate时设置，无需再次设置
        
        // 设置敌人数据
        Enemy enemy = spawnedObject.GetComponent<Enemy>();
        
        if (enemy != null)
        {
            enemy.SetEnemyData(data);
            
            // 注册到预告列表（新生成的敌人先进入预告阶段）
            if (enemyController != null)
            {
                enemyController.RegisterTelegraphingEnemy(enemy);
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
    /// 向后兼容：批量生成敌人
    /// </summary>
    /// <param name="enemySpawns">敌人生成配置列表</param>
    /// <param name="rangeConfig">范围配置（可选）</param>
    public void GenerateEnemiesFromList(List<EnemySpawn> enemySpawns, SpawnRangeConfig rangeConfig = null)
    {
        foreach (var enemySpawn in enemySpawns)
        {
            for (int i = 0; i < enemySpawn.count; i++)
            {
                Spawn(enemySpawn.enemyData, null, rangeConfig);
            }
        }
        
        if (showDebugInfo)
        {
            Debug.Log($"EnemySpawner: 批量生成完成，共 {enemySpawns.Count} 种敌人");
        }
    }
}
