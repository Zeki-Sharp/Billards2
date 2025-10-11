using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 相对坐标范围配置 - 相对于指定原点生成
/// 
/// 【核心功能】：
/// - 继承SpawnRangeConfig的所有功能
/// - 专门用于相对坐标生成场景
/// - 提供便捷的相对坐标生成方法
/// 
/// 【适用场景】：
/// - 击杀掉落（相对于死亡位置）
/// - 爆炸效果（相对于爆炸中心）
/// - 任何需要相对于某个点生成的场景
/// </summary>
[System.Serializable]
public class RelativeSpaceRangeConfig : SpawnRangeConfig
{
    [Header("相对坐标设置")]
    [Tooltip("是否启用调试可视化")]
    public bool enableDebugVisualization = false;
    
    [Tooltip("调试颜色")]
    public Color debugColor = Color.red;
    
    [Tooltip("当前原点位置（用于调试显示）")]
    public Vector3 currentOrigin = Vector3.zero;
    
    /// <summary>
    /// 获取相对坐标生成位置
    /// </summary>
    /// <param name="origin">原点位置</param>
    /// <returns>最终世界坐标位置</returns>
    public Vector3 GetRelativeSpawnPosition(Vector3 origin)
    {
        Vector3 localOffset = GetRandomLocalOffset();
        return origin + localOffset;
    }
    
    /// <summary>
    /// 获取多个相对坐标生成位置
    /// </summary>
    /// <param name="origin">原点位置</param>
    /// <param name="count">数量</param>
    /// <returns>最终世界坐标位置列表</returns>
    public List<Vector3> GetRelativeSpawnPositions(Vector3 origin, int count)
    {
        List<Vector3> localOffsets = GetRandomLocalOffsets(count);
        List<Vector3> worldPositions = new List<Vector3>();
        
        foreach (Vector3 offset in localOffsets)
        {
            worldPositions.Add(origin + offset);
        }
        
        return worldPositions;
    }
    
    /// <summary>
    /// 验证相对坐标位置是否有效
    /// </summary>
    /// <param name="worldPosition">世界坐标位置</param>
    /// <param name="origin">原点位置</param>
    /// <returns>是否有效</returns>
    public bool IsRelativePositionValid(Vector3 worldPosition, Vector3 origin)
    {
        Vector3 localOffset = worldPosition - origin;
        return IsLocalOffsetValid(localOffset);
    }
    
    /// <summary>
    /// 设置当前原点（用于调试显示）
    /// </summary>
    /// <param name="origin">原点位置</param>
    public void SetCurrentOrigin(Vector3 origin)
    {
        currentOrigin = origin;
    }
    
    /// <summary>
    /// 在Scene视图中绘制调试信息
    /// </summary>
    public void OnDrawGizmos()
    {
        if (!enableDebugVisualization) return;
        
        Gizmos.color = debugColor;
        
        // 绘制原点
        Gizmos.DrawWireSphere(currentOrigin, 0.2f);
        
        // 绘制范围
        switch (rangeType)
        {
            case SpawnRangeType.Rectangle:
                // 绘制矩形范围
                Vector3 center = currentOrigin + new Vector3((minX + maxX) / 2f, (minY + maxY) / 2f, 0f);
                Vector3 size = new Vector3(maxX - minX, maxY - minY, 0.1f);
                Gizmos.DrawWireCube(center, size);
                break;
                
            case SpawnRangeType.Circle:
                // 绘制圆形范围
                Gizmos.DrawWireSphere(currentOrigin, radius);
                break;
        }
    }
    
    /// <summary>
    /// 在Scene视图中绘制调试信息（带原点参数）
    /// </summary>
    /// <param name="origin">原点位置</param>
    public void OnDrawGizmos(Vector3 origin)
    {
        Vector3 oldOrigin = currentOrigin;
        SetCurrentOrigin(origin);
        OnDrawGizmos();
        SetCurrentOrigin(oldOrigin);
    }
    
    /// <summary>
    /// 获取调试信息
    /// </summary>
    /// <returns>调试信息字符串</returns>
    public string GetDebugInfo()
    {
        string baseInfo = base.ToString();
        return $"RelativeSpaceRangeConfig: {baseInfo}, 当前原点={currentOrigin}, 调试可视化={enableDebugVisualization}";
    }
}
