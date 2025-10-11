using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 世界坐标范围配置 - 直接使用世界坐标生成
/// 
/// 【核心功能】：
/// - 继承SpawnRangeConfig的所有功能
/// - 专门用于世界坐标生成场景
/// - 提供便捷的世界坐标生成方法
/// 
/// 【适用场景】：
/// - 敌人波次生成
/// - 技能主动生成道具
/// - 任何需要固定世界坐标范围的场景
/// </summary>
[System.Serializable]
public class WorldSpaceRangeConfig : SpawnRangeConfig
{
    [Header("世界坐标设置")]
    [Tooltip("是否启用调试可视化")]
    public bool enableDebugVisualization = false;
    
    [Tooltip("调试颜色")]
    public Color debugColor = Color.green;
    
    /// <summary>
    /// 获取世界坐标生成位置
    /// </summary>
    /// <returns>世界坐标位置</returns>
    public Vector3 GetWorldSpawnPosition()
    {
        return GetRandomLocalOffset(); // 直接返回偏移，作为世界坐标
    }
    
    /// <summary>
    /// 获取多个世界坐标生成位置
    /// </summary>
    /// <param name="count">数量</param>
    /// <returns>世界坐标位置列表</returns>
    public List<Vector3> GetWorldSpawnPositions(int count)
    {
        return GetRandomLocalOffsets(count);
    }
    
    /// <summary>
    /// 验证世界坐标位置是否有效
    /// </summary>
    /// <param name="worldPosition">世界坐标位置</param>
    /// <returns>是否有效</returns>
    public bool IsWorldPositionValid(Vector3 worldPosition)
    {
        return IsLocalOffsetValid(worldPosition);
    }
    
    /// <summary>
    /// 在Scene视图中绘制调试信息
    /// </summary>
    public void OnDrawGizmos()
    {
        if (!enableDebugVisualization) return;
        
        Gizmos.color = debugColor;
        
        switch (rangeType)
        {
            case SpawnRangeType.Rectangle:
                // 绘制矩形范围
                Vector3 center = new Vector3((minX + maxX) / 2f, (minY + maxY) / 2f, 0f);
                Vector3 size = new Vector3(maxX - minX, maxY - minY, 0.1f);
                Gizmos.DrawWireCube(center, size);
                break;
                
            case SpawnRangeType.Circle:
                // 绘制圆形范围
                Gizmos.DrawWireSphere(Vector3.zero, radius);
                break;
        }
    }
    
    /// <summary>
    /// 获取调试信息
    /// </summary>
    /// <returns>调试信息字符串</returns>
    public string GetDebugInfo()
    {
        string baseInfo = base.ToString();
        return $"WorldSpaceRangeConfig: {baseInfo}, 调试可视化={enableDebugVisualization}";
    }
}
