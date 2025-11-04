using UnityEngine;
using Sirenix.OdinInspector;

/// <summary>
/// 生成范围配置类
/// 用于配置生成器的位置计算参数
/// 支持世界坐标和相对坐标两种模式，支持矩形和圆形两种范围形状
/// </summary>
[System.Serializable]
public class SpawnRangeConfig
{
    [Header("坐标系统")]
    [LabelText("坐标系统类型")]
    [Tooltip("坐标系统类型")]
    public SpawnCoordinateSystem coordinateSystem = SpawnCoordinateSystem.WorldSpace;
    
    [Header("范围形状")]
    [LabelText("范围形状类型")]
    [Tooltip("范围形状类型")]
    public SpawnRangeShape rangeShape = SpawnRangeShape.Rectangle;
    
    [Header("世界坐标范围配置")]
    [LabelText("世界坐标中心点")]
    [Tooltip("世界坐标中心点")]
    [ShowIf("coordinateSystem", SpawnCoordinateSystem.WorldSpace)]
    public Vector3 worldCenter = Vector3.zero;
    
    [LabelText("矩形范围尺寸")]
    [Tooltip("矩形范围尺寸（X为宽度，Y为高度）")]
    [ShowIf("@coordinateSystem == SpawnCoordinateSystem.WorldSpace && rangeShape == SpawnRangeShape.Rectangle")]
    public Vector2 worldSize = new Vector2(20f, 10f);
    
    [LabelText("圆形范围半径")]
    [Tooltip("圆形范围半径")]
    [ShowIf("@coordinateSystem == SpawnCoordinateSystem.WorldSpace && (rangeShape == SpawnRangeShape.Circle || rangeShape == SpawnRangeShape.Ring)")]
    public float worldRadius = 8f;
    
    [Header("相对坐标范围配置")]
    [LabelText("相对坐标矩形尺寸")]
    [Tooltip("相对坐标矩形尺寸（X为宽度，Y为高度）")]
    [ShowIf("@coordinateSystem == SpawnCoordinateSystem.RelativeSpace && rangeShape == SpawnRangeShape.Rectangle")]
    public Vector2 relativeSize = new Vector2(4f, 4f);
    
    [LabelText("相对坐标圆形半径")]
    [Tooltip("相对坐标圆形半径")]
    [ShowIf("@coordinateSystem == SpawnCoordinateSystem.RelativeSpace && (rangeShape == SpawnRangeShape.Circle || rangeShape == SpawnRangeShape.Ring)")]
    public float relativeRadius = 2f;
    
    [Header("位置偏移")]
    [LabelText("启用位置偏移")]
    [Tooltip("是否启用位置随机偏移")]
    public bool enablePositionOffset = true;
    
    [LabelText("偏移范围")]
    [Tooltip("随机偏移范围")]
    public float offsetRange = 0.5f;
    
    [Header("障碍物检测")]
    [LabelText("启用障碍物检测")]
    [Tooltip("是否检测并避开障碍物（墙体/玩家/敌人）")]
    public bool checkObstacles = false;
    
    [LabelText("障碍物层")]
    [Tooltip("需要避让的物体层（推荐：Wall + Player + Enemy）")]
    [ShowIf("checkObstacles")]
    public LayerMask obstacleLayer = 0;
    
    [LabelText("检测半径")]
    [Tooltip("障碍物检测半径（球体半径 + 安全边距）")]
    [ShowIf("checkObstacles")]
    [MinValue(0.1f)]
    public float checkRadius = 0.6f;
    
    [LabelText("最大尝试次数")]
    [Tooltip("找不到有效位置时的最大重试次数")]
    [ShowIf("checkObstacles")]
    [MinValue(5)]
    public int maxObstacleCheckAttempts = 30;
    
    /// <summary>
    /// 获取随机生成位置
    /// </summary>
    /// <param name="origin">原点位置（相对坐标系统时使用）</param>
    /// <returns>随机位置（世界坐标）</returns>
    public Vector3 GetRandomPosition(Vector3? origin = null)
    {
        Vector3 localOffset = GetRandomLocalOffset();
        
        // 根据坐标系统计算最终位置
        Vector3 finalPosition;
        switch (coordinateSystem)
        {
            case SpawnCoordinateSystem.WorldSpace:
                // 世界坐标：直接使用范围配置 + 偏移
                finalPosition = worldCenter + localOffset;
                break;
            case SpawnCoordinateSystem.RelativeSpace:
                // 相对坐标：原点 + 偏移
                Vector3 baseOrigin = origin ?? Vector3.zero;
                finalPosition = baseOrigin + localOffset;
                break;
            default:
                finalPosition = Vector3.zero;
                Debug.LogWarning($"[SpawnRangeConfig] 未知的坐标系统: {coordinateSystem}");
                break;
        }
        
        return finalPosition;
    }
    
    /// <summary>
    /// 获取随机本地偏移（相对于原点或世界中心）
    /// </summary>
    /// <returns>本地偏移向量</returns>
    public Vector3 GetRandomLocalOffset()
    {
        Vector3 baseOffset;
        
        // 根据范围形状计算基础偏移
        switch (rangeShape)
        {
            case SpawnRangeShape.Rectangle:
                baseOffset = GetRandomRectangleOffset();
                break;
            case SpawnRangeShape.Circle:
                baseOffset = GetRandomCircleOffset();
                break;
            case SpawnRangeShape.Ring:
                baseOffset = GetRandomRingOffset();
                break;
            default:
                baseOffset = Vector3.zero;
                Debug.LogWarning($"[SpawnRangeConfig] 未知的范围形状: {rangeShape}");
                break;
        }
        
        // 添加随机偏移
        Vector3 finalOffset = baseOffset;
        if (enablePositionOffset && offsetRange > 0f)
        {
            Vector3 randomOffset = new Vector3(
                Random.Range(-offsetRange, offsetRange),
                Random.Range(-offsetRange, offsetRange),
                0f
            );
            finalOffset = baseOffset + randomOffset;
        }
        
        return finalOffset;
    }
    
    /// <summary>
    /// 获取矩形范围内的随机偏移
    /// </summary>
    /// <returns>随机偏移向量</returns>
    private Vector3 GetRandomRectangleOffset()
    {
        // 根据坐标系统选择尺寸参数
        Vector2 size = coordinateSystem == SpawnCoordinateSystem.WorldSpace ? worldSize : relativeSize;
        
        return new Vector3(
            Random.Range(-size.x * 0.5f, size.x * 0.5f),
            Random.Range(-size.y * 0.5f, size.y * 0.5f),
            0f
        );
    }
    
    /// <summary>
    /// 获取圆形范围内的随机偏移
    /// </summary>
    /// <returns>随机偏移向量</returns>
    private Vector3 GetRandomCircleOffset()
    {
        // 根据坐标系统选择半径参数
        float radius = coordinateSystem == SpawnCoordinateSystem.WorldSpace ? worldRadius : relativeRadius;
        
        float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
        float distance = Random.Range(0f, radius);
        
        return new Vector3(
            Mathf.Cos(angle) * distance,
            Mathf.Sin(angle) * distance,
            0f
        );
    }
    
    /// <summary>
    /// 获取环形范围内的随机偏移
    /// </summary>
    /// <returns>随机偏移向量</returns>
    private Vector3 GetRandomRingOffset()
    {
        // 根据坐标系统选择半径参数
        float radius = coordinateSystem == SpawnCoordinateSystem.WorldSpace ? worldRadius : relativeRadius;
        
        float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
        float distance = Random.Range(radius * 0.5f, radius); // 环形：内半径到外半径
        
        return new Vector3(
            Mathf.Cos(angle) * distance,
            Mathf.Sin(angle) * distance,
            0f
        );
    }
    
    /// <summary>
    /// 验证位置是否在范围内
    /// </summary>
    /// <param name="position">位置（世界坐标）</param>
    /// <param name="origin">原点位置（相对坐标系统时使用）</param>
    /// <returns>是否有效</returns>
    public bool IsPositionValid(Vector3 position, Vector3? origin = null)
    {
        // 添加容错范围，避免边界精度问题
        float tolerance = 0.1f;
        
        Vector3 localPosition;
        switch (coordinateSystem)
        {
            case SpawnCoordinateSystem.WorldSpace:
                localPosition = position - worldCenter;
                break;
            case SpawnCoordinateSystem.RelativeSpace:
                Vector3 baseOrigin = origin ?? Vector3.zero;
                localPosition = position - baseOrigin;
                break;
            default:
                return true;
        }
        
        switch (rangeShape)
        {
            case SpawnRangeShape.Rectangle:
                Vector2 size = coordinateSystem == SpawnCoordinateSystem.WorldSpace ? worldSize : relativeSize;
                return localPosition.x >= (-size.x * 0.5f - tolerance) && localPosition.x <= (size.x * 0.5f + tolerance) && 
                       localPosition.y >= (-size.y * 0.5f - tolerance) && localPosition.y <= (size.y * 0.5f + tolerance);
            case SpawnRangeShape.Circle:
                float radius = coordinateSystem == SpawnCoordinateSystem.WorldSpace ? worldRadius : relativeRadius;
                float distance = localPosition.magnitude;
                return distance <= (radius + tolerance);
            case SpawnRangeShape.Ring:
                float ringRadius = coordinateSystem == SpawnCoordinateSystem.WorldSpace ? worldRadius : relativeRadius;
                float ringDistance = localPosition.magnitude;
                return ringDistance >= (ringRadius * 0.5f - tolerance) && ringDistance <= (ringRadius + tolerance);
            default:
                return true;
        }
    }
    
    /// <summary>
    /// 设置世界坐标矩形范围
    /// </summary>
    /// <param name="center">中心点</param>
    /// <param name="size">尺寸</param>
    public void SetWorldRectRange(Vector3 center, Vector2 size)
    {
        coordinateSystem = SpawnCoordinateSystem.WorldSpace;
        rangeShape = SpawnRangeShape.Rectangle;
        worldCenter = center;
        worldSize = size;
    }
    
    /// <summary>
    /// 设置世界坐标圆形范围
    /// </summary>
    /// <param name="center">中心点</param>
    /// <param name="radius">半径</param>
    public void SetWorldCircularRange(Vector3 center, float radius)
    {
        coordinateSystem = SpawnCoordinateSystem.WorldSpace;
        rangeShape = SpawnRangeShape.Circle;
        worldCenter = center;
        worldRadius = radius;
    }
    
    /// <summary>
    /// 设置相对坐标矩形范围
    /// </summary>
    /// <param name="size">尺寸</param>
    public void SetRelativeRectRange(Vector2 size)
    {
        coordinateSystem = SpawnCoordinateSystem.RelativeSpace;
        rangeShape = SpawnRangeShape.Rectangle;
        relativeSize = size;
    }
    
    /// <summary>
    /// 设置相对坐标圆形范围
    /// </summary>
    /// <param name="radius">半径</param>
    public void SetRelativeCircularRange(float radius)
    {
        coordinateSystem = SpawnCoordinateSystem.RelativeSpace;
        rangeShape = SpawnRangeShape.Circle;
        relativeRadius = radius;
    }
    
    /// <summary>
    /// 检查位置是否与障碍物重叠
    /// </summary>
    /// <param name="position">待检测的位置</param>
    /// <param name="checkRadius">检测半径</param>
    /// <param name="obstacleLayer">障碍物层</param>
    /// <returns>true = 位置有效（无障碍物），false = 有障碍物</returns>
    public bool IsPositionClear(Vector3 position, float checkRadius, LayerMask obstacleLayer)
    {
        // 使用 Physics2D.OverlapCircle 检测是否与障碍物重叠
        Collider2D hit = Physics2D.OverlapCircle(position, checkRadius, obstacleLayer);
        
        // null = 没有检测到任何障碍物 → 位置有效 ✅
        // non-null = 检测到障碍物（墙/玩家/敌人） → 位置无效 ❌
        return hit == null;
    }
    
    /// <summary>
    /// 获取有效的随机位置（带障碍物检测）
    /// </summary>
    /// <param name="origin">原点位置（相对坐标系统时使用）</param>
    /// <returns>有效的随机位置（世界坐标）</returns>
    public Vector3 GetValidRandomPosition(Vector3? origin = null)
    {
        // 如果未启用障碍物检测，直接返回随机位置
        if (!checkObstacles)
        {
            return GetRandomPosition(origin);
        }
        
        // 尝试多次生成位置，直到找到不与障碍物重叠的位置
        for (int attempt = 0; attempt < maxObstacleCheckAttempts; attempt++)
        {
            Vector3 candidatePosition = GetRandomPosition(origin);
            
            // 检查是否与障碍物重叠
            if (IsPositionClear(candidatePosition, checkRadius, obstacleLayer))
            {
                return candidatePosition;  // 找到有效位置 ✅
            }
        }
        
        // 如果多次尝试失败，回退到不检查障碍物（保证能生成）
        Debug.LogWarning($"[SpawnRangeConfig] 经过 {maxObstacleCheckAttempts} 次尝试未找到无障碍位置，使用可能重叠的随机位置");
        return GetRandomPosition(origin);
    }
    
    /// <summary>
    /// 获取调试信息
    /// </summary>
    /// <returns>调试信息字符串</returns>
    public string GetDebugInfo()
    {
        string info = $"SpawnRangeConfig:\n";
        info += $"- 坐标系统: {coordinateSystem}\n";
        info += $"- 范围形状: {rangeShape}\n";
        
        if (coordinateSystem == SpawnCoordinateSystem.WorldSpace)
        {
            info += $"- 世界中心: {worldCenter}\n";
            if (rangeShape == SpawnRangeShape.Rectangle)
                info += $"- 世界尺寸: {worldSize}\n";
            else
                info += $"- 世界半径: {worldRadius}\n";
        }
        else
        {
            if (rangeShape == SpawnRangeShape.Rectangle)
                info += $"- 相对尺寸: {relativeSize}\n";
            else
                info += $"- 相对半径: {relativeRadius}\n";
        }
        
        info += $"- 位置偏移: {(enablePositionOffset ? $"启用 ({offsetRange})" : "禁用")}";
        
        return info;
    }
}

/// <summary>
/// 坐标系统枚举
/// </summary>
public enum SpawnCoordinateSystem
{
    WorldSpace,    // 世界坐标
    RelativeSpace  // 相对坐标
}

/// <summary>
/// 生成范围形状枚举
/// </summary>
public enum SpawnRangeShape
{
    Rectangle,  // 矩形范围
    Circle,     // 圆形范围
    Ring        // 环形范围
}
