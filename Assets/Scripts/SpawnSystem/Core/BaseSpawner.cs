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
    /// 尝试生成单个对象
    /// </summary>
    /// <param name="data">生成数据</param>
    /// <param name="position">生成位置</param>
    /// <param name="spawnedObject">生成的对象（输出参数）</param>
    /// <returns>是否生成成功</returns>
    public virtual bool TrySpawn(T data, Vector3 position, out GameObject spawnedObject, SpawnRangeConfig rangeConfig = null)
    {
        spawnedObject = null;
        
        if (data == null)
        {
            Debug.LogError($"[{GetType().Name}] 生成数据为空！");
            return false;
        }
        
        // 验证位置
        if (!ValidateSpawnPosition(position, rangeConfig))
        {
            if (enableDebugLog)
            {
                Debug.LogWarning($"[{GetType().Name}] 位置验证失败: {position}");
            }
            return false;
        }
        
        // 生成对象
        spawnedObject = InstantiateObject(data, position, spawnParent);
        if (spawnedObject != null)
        {
            OnPostSpawn(spawnedObject, data);
            
            if (enableDebugLog)
            {
                Debug.Log($"[{GetType().Name}] 生成成功: {position}");
            }
            return true;
        }
        
        if (enableDebugLog)
        {
            Debug.LogError($"[{GetType().Name}] 对象生成失败: {position}");
        }
        return false;
    }
    
    /// <summary>
    /// 生成单个对象（兼容性方法）
    /// </summary>
    /// <param name="data">生成数据</param>
    /// <param name="position">生成位置（可选，为null时使用范围配置）</param>
    /// <param name="rangeConfig">范围配置（可选）</param>
    public virtual void Spawn(T data, Vector3? position = null, SpawnRangeConfig rangeConfig = null)
    {
        if (data == null)
        {
            Debug.LogError($"[{GetType().Name}] 生成数据为空！");
            return;
        }
        
        if (position.HasValue)
        {
            // 使用指定位置
            if (!TrySpawn(data, position.Value, out GameObject spawnedObject, rangeConfig))
            {
                Debug.LogError($"[{GetType().Name}] 指定位置生成失败: {position.Value}");
            }
        }
        else
        {
            // 没有指定位置，使用范围配置自动计算（支持重试）
            int maxRetries = 5;
            
            for (int i = 0; i < maxRetries; i++)
            {
                Vector3 spawnPosition = CalculateSpawnPosition(rangeConfig);
                
                if (TrySpawn(data, spawnPosition, out GameObject spawnedObject, rangeConfig))
                {
                    return; // 成功生成，退出
                }
                
                // 生成失败，记录并重试
                if (enableDebugLog)
                {
                    Debug.LogWarning($"[{GetType().Name}] 自动计算位置生成失败，重试 {i + 1}/{maxRetries}: {spawnPosition}");
                }
            }
            
            // 所有重试都失败
            Debug.LogError($"[{GetType().Name}] 自动计算位置生成失败，已重试 {maxRetries} 次");
        }
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
    /// <param name="rangeConfig">范围配置（可选）</param>
    /// <returns>生成位置</returns>
    protected virtual Vector3 CalculateSpawnPosition(SpawnRangeConfig rangeConfig = null)
    {
        if (rangeConfig != null)
        {
            // ✅ 使用新方法：自动避开障碍物（墙体/玩家/敌人）
            return rangeConfig.GetValidRandomPosition();
        }
        else
        {
            // 没有范围配置时，使用默认位置
            return transform.position;
        }
    }
    
    /// <summary>
    /// 验证生成位置
    /// </summary>
    /// <param name="position">位置</param>
    /// <param name="rangeConfig">范围配置（可选）</param>
    /// <returns>是否有效</returns>
    protected virtual bool ValidateSpawnPosition(Vector3 position, SpawnRangeConfig rangeConfig = null)
    {
        // 基础验证：检查是否在范围内
        if (rangeConfig != null)
        {
            return rangeConfig.IsPositionValid(position);
        }
        else
        {
            // 没有范围配置时，总是返回true
            return true;
        }
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
    /// <param name="rangeConfig">范围配置</param>
    public void SetSpawnRange(float minX, float maxX, float minY, float maxY, SpawnRangeConfig rangeConfig)
    {
        // 计算中心点和尺寸
        Vector3 center = new Vector3((minX + maxX) * 0.5f, (minY + maxY) * 0.5f, 0f);
        Vector2 size = new Vector2(maxX - minX, maxY - minY);
        
        rangeConfig.SetWorldRectRange(center, size);
        
        if (enableDebugLog)
        {
            Debug.Log($"[{GetType().Name}] 设置矩形生成范围: X({minX}~{maxX}), Y({minY}~{maxY})");
        }
    }
    
    /// <summary>
    /// 设置生成范围（圆形）
    /// </summary>
    /// <param name="radius">半径</param>
    /// <param name="rangeConfig">范围配置</param>
    public void SetSpawnRange(float radius, SpawnRangeConfig rangeConfig)
    {
        rangeConfig.SetWorldCircularRange(Vector3.zero, radius);
        
        if (enableDebugLog)
        {
            Debug.Log($"[{GetType().Name}] 设置圆形生成范围，半径: {radius}");
        }
    }
}

