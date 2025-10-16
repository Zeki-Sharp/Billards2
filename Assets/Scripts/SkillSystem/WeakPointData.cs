using UnityEngine;

/// <summary>
/// 弱点数据 - 存储单个敌人的弱点信息
/// </summary>
public class WeakPointData
{
    /// <summary>
    /// 当前弱点方向索引（0=上, 1=右, 2=下, 3=左）
    /// </summary>
    public int currentDirection;
    
    /// <summary>
    /// 弱点标记物体实例
    /// </summary>
    public GameObject markerObject;
    
    /// <summary>
    /// 获取弱点的局部坐标
    /// </summary>
    public Vector2 GetLocalPosition(float radius)
    {
        // 方向转角度：0=0°(上), 1=90°(右), 2=180°(下), 3=270°(左)
        float angle = currentDirection * 90f;
        float angleRad = angle * Mathf.Deg2Rad;
        
        return new Vector2(
            Mathf.Cos(angleRad) * radius,
            Mathf.Sin(angleRad) * radius
        );
    }
    
    /// <summary>
    /// 生成一个不同于当前的新方向
    /// </summary>
    public int GenerateNewDirection()
    {
        // 从其他3个方向中随机选择
        int newDirection;
        do {
            newDirection = Random.Range(0, 4);
        } while (newDirection == currentDirection);
        
        return newDirection;
    }
}

