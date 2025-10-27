using UnityEngine;

/// <summary>
/// 墙面撞击旋转计算器 - 纯计算器，只负责计算旋转角度
/// </summary>
public class WallHitRotationController : MonoBehaviour
{
    [Header("旋转计算")]
    public float maxRotationAngle = 45f;
    public float minRotationAngle = 5f;
    
    [Header("屏幕边界")]
    public float maxXOffset = 10f; // 屏幕宽度的一半
    public float maxYOffset = 10f; // 屏幕高度的一半
    
    [Header("速度影响")]
    [Tooltip("速度到摇晃强度的曲线 (0=静止, 1=最大速度)")]
    public AnimationCurve speedToShakeCurve = AnimationCurve.Linear(0f, 0.1f, 1f, 1f);
    [Tooltip("最大速度参考值")]
    public float maxSpeedReference = 50f;
    [Tooltip("速度系数范围")]
    public float minSpeedMultiplier = 0.1f;
    public float maxSpeedMultiplier = 1.0f;
    
    /// <summary>
    /// 计算撞击墙面的旋转角度
    /// </summary>
    /// <param name="hitPosition">撞击位置</param>
    /// <param name="hitNormal">墙面法线</param>
    /// <param name="hitSpeed">撞击速度</param>
    /// <returns>计算出的旋转角度</returns>
    public float CalculateRotationAngle(Vector3 hitPosition, Vector3 hitNormal, float hitSpeed = 0f)
    {
        // 1. 根据法线判断墙面类型
        WallType wallType = GetWallType(hitNormal);
        
        // 2. 计算偏移量（相对于屏幕中心 0,0）
        float offset = CalculateOffset(hitPosition, wallType);
        
        // 3. 获取最大偏移值
        float maxOffset = GetMaxOffset(wallType);
        
        // 4. 计算基础旋转角度
        float baseRotationAngle = CalculateBaseRotationAngle(offset, maxOffset);
        
        // 5. 计算速度系数
        float speedMultiplier = CalculateSpeedMultiplier(hitSpeed);
        
        // 6. 应用速度系数
        float finalRotationAngle = baseRotationAngle * speedMultiplier;
        
        return finalRotationAngle;
    }
    
    private WallType GetWallType(Vector3 hitNormal)
    {
        // 如果法线主要指向 Y 方向，说明是横墙
        if (Mathf.Abs(hitNormal.y) > Mathf.Abs(hitNormal.x))
            return WallType.Horizontal;
        else
            return WallType.Vertical;
    }
    
    private float CalculateOffset(Vector3 hitPosition, WallType wallType)
    {
        if (wallType == WallType.Horizontal)
        {
            // 横墙：根据 X 和 Y 的组合计算偏移
            // 左上(-x,+y) → 左上旋转，右上(+x,+y) → 右上旋转
            // 左下(-x,-y) → 右上旋转，右下(+x,-y) → 左上旋转
            return hitPosition.x * hitPosition.y; // 使用 X * Y 的组合
        }
        else
        {
            // 竖墙：根据 X 和 Y 的组合计算偏移（与横墙相反）
            // 左上(-x,+y) → 右上旋转，右上(+x,+y) → 左上旋转
            // 左下(-x,-y) → 左上旋转，右下(+x,-y) → 右上旋转
            return -hitPosition.x * hitPosition.y; // 使用 -X * Y 的组合
        }
    }
    
    private float GetMaxOffset(WallType wallType)
    {
        // 使用 X 和 Y 的最大偏移的乘积作为参考
        return maxXOffset * maxYOffset;
    }
    
    private float CalculateBaseRotationAngle(float offset, float maxOffset)
    {
        // 归一化偏移值到 [-1, 1] 范围
        float normalizedOffset = Mathf.Clamp(offset / maxOffset, -1f, 1f);
        
        // 计算角度：偏移越大，角度越大
        // 横墙：X*Y 为正值 → 右上旋转，X*Y 为负值 → 左上旋转
        // 竖墙：-X*Y 为正值 → 右上旋转，-X*Y 为负值 → 左上旋转
        float angle = Mathf.Lerp(minRotationAngle, maxRotationAngle, Mathf.Abs(normalizedOffset)) * Mathf.Sign(normalizedOffset);
        
        return angle;
    }
    
    /// <summary>
    /// 计算速度系数
    /// </summary>
    /// <param name="hitSpeed">撞击速度</param>
    /// <returns>速度系数</returns>
    private float CalculateSpeedMultiplier(float hitSpeed)
    {
        // 归一化速度到 [0, 1] 范围
        float normalizedSpeed = Mathf.Clamp(hitSpeed / maxSpeedReference, 0f, 1f);
        
        // 使用曲线计算速度系数
        float curveValue = speedToShakeCurve.Evaluate(normalizedSpeed);
        
        // 映射到实际的速度系数范围
        float speedMultiplier = Mathf.Lerp(minSpeedMultiplier, maxSpeedMultiplier, curveValue);
        
        return speedMultiplier;
    }
}

/// <summary>
/// 墙面类型枚举
/// </summary>
public enum WallType
{
    Horizontal, // 横墙
    Vertical    // 竖墙
}
