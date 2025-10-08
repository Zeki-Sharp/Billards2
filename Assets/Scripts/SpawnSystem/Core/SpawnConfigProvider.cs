using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 生成配置提供者接口 - 配置层核心接口
/// 定义所有配置提供者必须实现的统一接口
/// 负责存储、管理和查询特定类型的配置数据
/// </summary>
/// <typeparam name="T">配置数据类型</typeparam>
public interface SpawnConfigProvider<T>
{
    /// <summary>
    /// 获取生成数据列表
    /// </summary>
    /// <returns>配置的生成数据列表</returns>
    List<T> GetSpawnData();
    
    /// <summary>
    /// 判断是否应该生成
    /// </summary>
    /// <returns>是否应该生成</returns>
    bool ShouldSpawn();
    
    /// <summary>
    /// 获取生成数量
    /// </summary>
    /// <returns>生成数量</returns>
    int GetSpawnCount();
    
    /// <summary>
    /// 初始化配置提供者
    /// </summary>
    void Initialize();
    
    /// <summary>
    /// 重置配置提供者状态
    /// </summary>
    void Reset();
}
