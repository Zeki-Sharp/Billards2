using UnityEngine;

/// <summary>
/// 墙面撞击位置摇晃计算器 - 控制Position Spring的Bump模式
/// </summary>
public class WallHitPositionController : MonoBehaviour
{
    [Header("位置摇晃计算")]
    [Tooltip("最大位置偏移量")]
    public float maxPositionOffset = 5f;
    [Tooltip("最小位置偏移量")]
    public float minPositionOffset = 1f;
    
    [Header("速度影响")]
    [Tooltip("速度到位置偏移强度的曲线 (0=静止, 1=最大速度)")]
    public AnimationCurve speedToPositionCurve = AnimationCurve.Linear(0f, 0.1f, 1f, 1f);
    [Tooltip("最大速度参考值")]
    public float maxSpeedReference = 50f;
    [Tooltip("速度系数范围")]
    public float minSpeedMultiplier = 0.1f;
    public float maxSpeedMultiplier = 1.0f;
    
    [Header("调试")]
    [Tooltip("是否启用调试日志")]
    public bool enableDebugLog = false;
    
    /// <summary>
    /// 计算撞击墙面的位置偏移
    /// </summary>
    /// <param name="hitPosition">撞击位置</param>
    /// <param name="hitNormal">墙面法线</param>
    /// <param name="hitDirection">撞击方向</param>
    /// <param name="hitSpeed">撞击速度</param>
    /// <returns>计算出的位置偏移向量</returns>
    public Vector3 CalculatePositionOffset(Vector3 hitPosition, Vector3 hitNormal, Vector3 hitDirection, float hitSpeed = 0f)
    {
        // 1. 使用撞击方向的反方向作为偏移方向（墙面被球推着走）
        Vector3 directionOffset = -hitDirection.normalized;
        
        // 2. 计算速度系数
        float speedMultiplier = CalculateSpeedMultiplier(hitSpeed);
        
        // 3. 计算最终偏移量
        float offsetMagnitude = Mathf.Lerp(minPositionOffset, maxPositionOffset, speedMultiplier);
        Vector3 totalOffset = directionOffset * offsetMagnitude;
        
        if (enableDebugLog)
        {
            Debug.Log($"位置摇晃计算: 撞击方向={hitDirection}, 墙面移动方向={directionOffset}, 速度系数={speedMultiplier:F2}, 偏移量={offsetMagnitude:F2}, 总偏移={totalOffset}");
        }
        
        return totalOffset;
    }
    
    
    /// <summary>
    /// 计算速度系数
    /// </summary>
    private float CalculateSpeedMultiplier(float hitSpeed)
    {
        // 归一化速度到 [0, 1] 范围
        float normalizedSpeed = Mathf.Clamp(hitSpeed / maxSpeedReference, 0f, 1f);
        
        // 使用曲线计算速度系数
        float curveValue = speedToPositionCurve.Evaluate(normalizedSpeed);
        
        // 映射到实际的速度系数范围
        float speedMultiplier = Mathf.Lerp(minSpeedMultiplier, maxSpeedMultiplier, curveValue);
        
        return speedMultiplier;
    }
    
    /// <summary>
    /// 设置Position Spring的Bump参数
    /// </summary>
    /// <param name="positionSpring">Position Spring组件</param>
    /// <param name="positionOffset">计算出的位置偏移</param>
    public void SetPositionSpringBump(Component positionSpring, Vector3 positionOffset)
    {
        if (positionSpring == null) return;
        
        // 使用反射设置Position Spring的Bump参数
        var positionSpringType = positionSpring.GetType();
        
        // 设置Bump Position Min
        var bumpMinField = positionSpringType.GetField("BumpPositionMin", 
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
        if (bumpMinField != null)
        {
            bumpMinField.SetValue(positionSpring, Vector3.zero);
        }
        
        // 设置Bump Position Max
        var bumpMaxField = positionSpringType.GetField("BumpPositionMax", 
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
        if (bumpMaxField != null)
        {
            bumpMaxField.SetValue(positionSpring, positionOffset);
        }
        
        if (enableDebugLog)
        {
            Debug.Log($"设置Position Spring Bump参数: Min={Vector3.zero}, Max={positionOffset}");
        }
    }
}
