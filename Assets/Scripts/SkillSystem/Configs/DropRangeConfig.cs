using UnityEngine;
using Sirenix.OdinInspector;

/// <summary>
/// 掉落范围配置 - 用于配置物品掉落的位置范围
/// </summary>
[System.Serializable]
public class DropRangeConfig
{
    [LabelText("掉落半径")]
    [Tooltip("掉落物品的半径范围")]
    [MinValue(0.1f)]
    public float dropRadius = 1.0f;
    
    [LabelText("掉落形状")]
    [Tooltip("掉落物品的形状类型")]
    public DropShape dropShape = DropShape.Circle;
    
    [LabelText("位置偏移")]
    [Tooltip("相对于触发位置的偏移")]
    public Vector3 positionOffset = Vector3.zero;
    
    [LabelText("坐标系统")]
    [Tooltip("掉落位置的坐标系统")]
    public CoordinateSystemType coordinateSystemType = CoordinateSystemType.RelativeSpace;
    
    /// <summary>
    /// 获取随机掉落位置
    /// </summary>
    /// <param name="centerPosition">中心位置（通常是敌人死亡位置）</param>
    /// <returns>计算出的掉落位置</returns>
    public Vector3 GetRandomPosition(Vector3 centerPosition)
    {
        Vector3 basePosition = centerPosition + positionOffset;
        
        switch (coordinateSystemType)
        {
            case CoordinateSystemType.RelativeSpace:
                return GetRelativeRandomPosition(basePosition);
            case CoordinateSystemType.AbsoluteSpace:
                return GetAbsoluteRandomPosition();
            default:
                return basePosition;
        }
    }
    
    /// <summary>
    /// 获取相对空间的随机位置
    /// </summary>
    private Vector3 GetRelativeRandomPosition(Vector3 centerPosition)
    {
        Vector3 randomPosition = centerPosition;
        
        switch (dropShape)
        {
            case DropShape.Circle:
                // 在圆形范围内生成随机位置
                Vector2 randomCircle = Random.insideUnitCircle * dropRadius;
                randomPosition.x += randomCircle.x;
                randomPosition.z += randomCircle.y;
                break;
                
            case DropShape.Rectangle:
                // 在矩形范围内生成随机位置
                randomPosition.x += Random.Range(-dropRadius, dropRadius);
                randomPosition.z += Random.Range(-dropRadius, dropRadius);
                break;
                
            case DropShape.Line:
                // 在直线上生成随机位置
                Vector3 randomDirection = Random.insideUnitSphere;
                randomDirection.y = 0; // 保持在地面上
                randomPosition += randomDirection.normalized * Random.Range(0f, dropRadius);
                break;
        }
        
        return randomPosition;
    }
    
    /// <summary>
    /// 获取绝对空间的随机位置
    /// </summary>
    private Vector3 GetAbsoluteRandomPosition()
    {
        // 绝对空间模式下，在指定范围内生成随机位置
        return new Vector3(
            Random.Range(-dropRadius, dropRadius),
            0f,
            Random.Range(-dropRadius, dropRadius)
        );
    }
    
    /// <summary>
    /// 验证配置是否有效
    /// </summary>
    public bool IsValid()
    {
        return dropRadius > 0f;
    }
    
    /// <summary>
    /// 获取调试信息
    /// </summary>
    public string GetDebugInfo()
    {
        return $"掉落范围: {dropShape}, 半径: {dropRadius}, 偏移: {positionOffset}";
    }
}

/// <summary>
/// 掉落形状类型
/// </summary>
public enum DropShape
{
    Circle,     // 圆形
    Rectangle,  // 矩形
    Line        // 直线
}

/// <summary>
/// 坐标系统类型
/// </summary>
public enum CoordinateSystemType
{
    RelativeSpace,  // 相对空间（相对于触发位置）
    AbsoluteSpace   // 绝对空间（世界坐标）
}
