using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 生成器基类 - 执行层核心基类
/// 定义生成器的通用逻辑和接口
/// 负责位置计算、验证、对象实例化和后处理
/// </summary>
/// <typeparam name="T">生成数据类型</typeparam>
public abstract class BaseSpawner<T> : MonoBehaviour
{
    [Header("生成设置")]
    [SerializeField] protected Transform spawnParent;
    
    [Header("生成范围配置")]
    [SerializeField] protected SpawnRangeConfig rangeConfig = new SpawnRangeConfig();
    
    [Header("调试")]
    [SerializeField] protected bool enableDebugLog = true;
    
    /// <summary>
    /// 初始化生成器
    /// </summary>
    protected virtual void Start()
    {
        Initialize();
    }
    
    /// <summary>
    /// 初始化生成器
    /// </summary>
    protected virtual void Initialize()
    {
        // 如果没有设置父对象，使用当前对象
        if (spawnParent == null)
        {
            spawnParent = transform;
        }
        
        if (enableDebugLog)
        {
            Debug.Log($"[{GetType().Name}] 初始化完成");
        }
    }
    
    /// <summary>
    /// 生成单个对象
    /// </summary>
    /// <param name="data">生成数据</param>
    /// <param name="position">生成位置（可选，为null时使用范围配置）</param>
    public virtual void Spawn(T data, Vector3? position = null)
    {
        if (data == null)
        {
            Debug.LogError($"[{GetType().Name}] 生成数据为空！");
            return;
        }
        
        // 支持重试机制
        int maxRetries = 5;
        bool useSpecifiedPosition = position.HasValue;
        Vector3 specifiedPosition = position ?? Vector3.zero;
        
        for (int i = 0; i < maxRetries; i++)
        {
            Vector3 spawnPosition;
            
            if (useSpecifiedPosition && i == 0)
            {
                // 第一次尝试使用指定的位置
                spawnPosition = specifiedPosition;
            }
            else
            {
                // 后续重试使用自动计算的位置
                spawnPosition = CalculateSpawnPosition();
            }
            
            // 验证位置
            if (ValidateSpawnPosition(spawnPosition))
            {
                // 位置有效，执行生成
                GameObject spawnedObject = InstantiateObject(data, spawnPosition, spawnParent);
                if (spawnedObject != null)
                {
                    OnPostSpawn(spawnedObject, data);
                    return; // 成功生成，退出
                }
            }
            
            // 位置无效或生成失败，记录并重试
            if (enableDebugLog)
            {
                if (useSpecifiedPosition && i == 0)
                {
                    Debug.LogWarning($"[{GetType().Name}] 指定位置无效，重试 {i + 1}/{maxRetries}: {spawnPosition}");
                }
                else
                {
                    Debug.LogWarning($"[{GetType().Name}] 生成位置无效，重试 {i + 1}/{maxRetries}: {spawnPosition}");
                }
            }
        }
        
        // 所有重试都失败
        Debug.LogError($"[{GetType().Name}] 生成失败，已重试 {maxRetries} 次");
    }
    
    /// <summary>
    /// 批量生成对象
    /// </summary>
    /// <param name="dataList">生成数据列表</param>
    /// <param name="positions">生成位置列表</param>
    public virtual void SpawnBatch(List<T> dataList, List<Vector3> positions)
    {
        if (dataList == null || positions == null)
        {
            Debug.LogError($"[{GetType().Name}] 批量生成数据为空！");
            return;
        }
        
        int count = Mathf.Min(dataList.Count, positions.Count);
        for (int i = 0; i < count; i++)
        {
            Spawn(dataList[i], positions[i]);
        }
        
        if (enableDebugLog)
        {
            Debug.Log($"[{GetType().Name}] 批量生成完成，数量: {count}");
        }
    }
    
    /// <summary>
    /// 计算生成位置（抽象方法，子类实现）
    /// </summary>
    /// <returns>生成位置</returns>
    protected virtual Vector3 CalculateSpawnPosition()
    {
        Vector3 spawnPosition = rangeConfig.GetRandomPosition();
        
        
        return spawnPosition;
    }
    
    /// <summary>
    /// 验证生成位置
    /// </summary>
    /// <param name="position">位置</param>
    /// <returns>是否有效</returns>
    protected virtual bool ValidateSpawnPosition(Vector3 position)
    {
        // 基础验证：检查是否在范围内
        return rangeConfig.IsPositionValid(position);
    }
    
    /// <summary>
    /// 实例化对象（抽象方法，子类实现）
    /// </summary>
    /// <param name="data">生成数据</param>
    /// <param name="position">生成位置</param>
    /// <param name="parent">父对象</param>
    /// <returns>实例化的GameObject</returns>
    protected abstract GameObject InstantiateObject(T data, Vector3 position, Transform parent);
    
    /// <summary>
    /// 生成后处理（可选重写）
    /// </summary>
    /// <param name="spawnedObject">生成的对象</param>
    /// <param name="data">生成数据</param>
    protected virtual void OnPostSpawn(GameObject spawnedObject, T data)
    {
        // 默认不做处理，子类可重写
    }
    
    /// <summary>
    /// 设置生成范围（矩形）
    /// </summary>
    /// <param name="minX">最小X</param>
    /// <param name="maxX">最大X</param>
    /// <param name="minY">最小Y</param>
    /// <param name="maxY">最大Y</param>
    public void SetSpawnRange(float minX, float maxX, float minY, float maxY)
    {
        rangeConfig.SetRectRange(minX, maxX, minY, maxY);
        
        if (enableDebugLog)
        {
            Debug.Log($"[{GetType().Name}] 设置矩形生成范围: X({minX}~{maxX}), Y({minY}~{maxY})");
        }
    }
    
    /// <summary>
    /// 设置生成范围（圆形）
    /// </summary>
    /// <param name="radius">半径</param>
    public void SetSpawnRange(float radius)
    {
        rangeConfig.SetCircularRange(radius);
        
        if (enableDebugLog)
        {
            Debug.Log($"[{GetType().Name}] 设置圆形生成范围，半径: {radius}");
        }
    }
}
