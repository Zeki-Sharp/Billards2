using UnityEngine;

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
    /// 获取随机生成位置
    /// </summary>
    /// <returns>随机位置</returns>
    public Vector3 GetRandomPosition()
    {
        Vector3 basePosition = Vector3.zero;
        
        switch (rangeType)
        {
            case SpawnRangeType.Rectangle:
                basePosition = GetRandomRectanglePosition();
                break;
            case SpawnRangeType.Circle:
                basePosition = GetRandomCirclePosition();
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
    /// 验证位置是否在范围内
    /// </summary>
    /// <param name="position">位置</param>
    /// <returns>是否有效</returns>
    public bool IsPositionValid(Vector3 position)
    {
        switch (rangeType)
        {
            case SpawnRangeType.Rectangle:
                return position.x >= minX && position.x <= maxX && 
                       position.y >= minY && position.y <= maxY;
            case SpawnRangeType.Circle:
                float distance = Vector3.Distance(Vector3.zero, position);
                return distance <= radius;
            default:
                return true;
        }
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
