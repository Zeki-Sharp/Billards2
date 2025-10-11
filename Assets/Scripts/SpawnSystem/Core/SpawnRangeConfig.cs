using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 生成范围配置类
/// 用于配置生成器的位置计算参数
/// 支持矩形和圆形两种范围模式
/// </summary>
[System.Serializable]
public class SpawnRangeConfig
{
    [Header("范围类型")]
    [Tooltip("范围类型")]
    public SpawnRangeType rangeType = SpawnRangeType.Rectangle;
    
    [Header("矩形范围配置")]
    [Tooltip("最小X坐标")]
    public float minX = -10f;
    
    [Tooltip("最大X坐标")]
    public float maxX = 10f;
    
    [Tooltip("最小Y坐标")]
    public float minY = -5f;
    
    [Tooltip("最大Y坐标")]
    public float maxY = 5f;
    
    [Header("圆形范围配置")]
    [Tooltip("圆形范围半径")]
    public float radius = 8f;
    
    [Header("位置偏移")]
    [Tooltip("是否启用位置随机偏移")]
    public bool enablePositionOffset = true;
    
    [Tooltip("随机偏移范围")]
    public float offsetRange = 0.5f;
    
    /// <summary>
    /// 获取随机局部偏移（相对于原点）
    /// </summary>
    /// <returns>相对于原点的偏移向量</returns>
    public Vector3 GetRandomLocalOffset()
    {
        Vector3 basePosition;
        
        switch (rangeType)
        {
            case SpawnRangeType.Rectangle:
                basePosition = new Vector3(
                    Random.Range(minX, maxX),
                    Random.Range(minY, maxY),
                    0f
                );
                break;
            case SpawnRangeType.Circle:
                float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
                float distance = Random.Range(0f, radius);
                basePosition = new Vector3(
                    Mathf.Cos(angle) * distance,
                    Mathf.Sin(angle) * distance,
                    0f
                );
                break;
            default:
                basePosition = Vector3.zero;
                break;
        }
        
        // 添加随机偏移
        if (enablePositionOffset && offsetRange > 0f)
        {
            Vector3 offset = new Vector3(
                Random.Range(-offsetRange, offsetRange),
                Random.Range(-offsetRange, offsetRange),
                0f
            );
            basePosition += offset;
        }
        
        return basePosition;
    }
    
    /// <summary>
    /// 获取随机生成位置（绝对世界坐标）
    /// 【已废弃】建议使用 GetRandomLocalOffset() + origin 的方式
    /// </summary>
    /// <param name="origin">原点位置（已废弃，保持兼容性）</param>
    /// <returns>随机位置</returns>
    [System.Obsolete("建议使用 GetRandomLocalOffset() 方法")]
    public Vector3 GetRandomPosition(Vector3 origin = default)
    {
        return GetRandomLocalOffset();
    }
    
    /// <summary>
    /// 获取多个随机局部偏移
    /// </summary>
    /// <param name="count">需要的位置数量</param>
    /// <returns>局部偏移列表</returns>
    public List<Vector3> GetRandomLocalOffsets(int count)
    {
        List<Vector3> offsets = new List<Vector3>();
        
        for (int i = 0; i < count; i++)
        {
            offsets.Add(GetRandomLocalOffset());
        }
        
        return offsets;
    }
    
    /// <summary>
    /// 获取矩形范围内的随机位置
    /// </summary>
    /// <returns>随机位置</returns>
    private Vector3 GetRandomRectanglePosition()
    {
        return new Vector3(
            Random.Range(minX, maxX),
            Random.Range(minY, maxY),
            0f
        );
    }
    
    /// <summary>
    /// 获取圆形范围内的随机位置
    /// </summary>
    /// <returns>随机位置</returns>
    private Vector3 GetRandomCirclePosition()
    {
        float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
        float distance = Random.Range(0f, radius);
        
        return new Vector3(
            Mathf.Cos(angle) * distance,
            Mathf.Sin(angle) * distance,
            0f
        );
    }
    
    /// <summary>
    /// 验证局部偏移是否在范围内
    /// </summary>
    /// <param name="localOffset">局部偏移</param>
    /// <returns>是否有效</returns>
    public bool IsLocalOffsetValid(Vector3 localOffset)
    {
        // 添加容错范围，避免边界精度问题
        float tolerance = 0.1f;
        
        switch (rangeType)
        {
            case SpawnRangeType.Rectangle:
                return localOffset.x >= (minX - tolerance) && localOffset.x <= (maxX + tolerance) && 
                       localOffset.y >= (minY - tolerance) && localOffset.y <= (maxY + tolerance);
            case SpawnRangeType.Circle:
                float distance = localOffset.magnitude;
                return distance <= (radius + tolerance);
            default:
                return true;
        }
    }
    
    /// <summary>
    /// 验证位置是否在范围内（绝对世界坐标）
    /// 【已废弃】建议使用 IsLocalOffsetValid() 方法
    /// </summary>
    /// <param name="position">位置</param>
    /// <param name="origin">原点位置（已废弃，保持兼容性）</param>
    /// <returns>是否有效</returns>
    [System.Obsolete("建议使用 IsLocalOffsetValid() 方法")]
    public bool IsPositionValid(Vector3 position, Vector3 origin = default)
    {
        // 为了兼容性，这里假设传入的position是相对于原点的偏移
        return IsLocalOffsetValid(position);
    }
    
    /// <summary>
    /// 设置矩形范围
    /// </summary>
    /// <param name="minX">最小X</param>
    /// <param name="maxX">最大X</param>
    /// <param name="minY">最小Y</param>
    /// <param name="maxY">最大Y</param>
    public void SetRectRange(float minX, float maxX, float minY, float maxY)
    {
        rangeType = SpawnRangeType.Rectangle;
        this.minX = minX;
        this.maxX = maxX;
        this.minY = minY;
        this.maxY = maxY;
    }
    
    /// <summary>
    /// 设置圆形范围
    /// </summary>
    /// <param name="radius">半径</param>
    public void SetCircularRange(float radius)
    {
        rangeType = SpawnRangeType.Circle;
        this.radius = radius;
    }
}

/// <summary>
/// 生成范围类型枚举
/// </summary>
public enum SpawnRangeType
{
    Rectangle,  // 矩形范围
    Circle      // 圆形范围
}
