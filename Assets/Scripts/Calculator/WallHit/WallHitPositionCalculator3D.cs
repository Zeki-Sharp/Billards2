using UnityEngine;

/// <summary>
/// 墙体撞击 3D 位移计算器
/// - 位移方向：只看撞击方向（hitDirection），不关心撞击点在墙上的具体位置
/// - 位移大小：由撞击速度 hitSpeed 决定
/// - 所有位移都在 Wall 本地 XZ 平面内完成
/// </summary>
public class WallHitPositionCalculator3D : MonoBehaviour
{
    [Header("位置偏移范围")]
    [Tooltip("最小位置偏移量")]
    public float minPositionOffset = 0.2f;

    [Tooltip("最大位置偏移量")]
    public float maxPositionOffset = 2.0f;

    [Header("速度影响")]
    [Tooltip("速度到位置偏移强度的曲线 (0=静止, 1=最大速度)")]
    public AnimationCurve speedToPositionCurve = AnimationCurve.Linear(0f, 0.1f, 1f, 1f);

    [Tooltip("最大速度参考值")]
    public float maxSpeedReference = 50f;

    [Tooltip("速度系数范围")]
    public float minSpeedMultiplier = 0.1f;
    public float maxSpeedMultiplier = 1.0f;

    [Header("调试")]
    public bool enableDebugLog = false;

    /// <summary>
    /// 计算墙体在世界空间中的位移向量
    /// </summary>
    /// <param name="wallRoot">作为整体移动/旋转的 Wall 根物体</param>
    /// <param name="hitPositionWorld">世界空间撞击点（当前版本未使用，保留以便将来扩展）</param>
    /// <param name="hitDirectionWorld">世界空间撞击方向（通常为球速度方向或法线的反向）</param>
    /// <param name="hitSpeed">撞击速度</param>
    /// <returns>世界空间中的位移向量</returns>
    public Vector3 CalculatePositionOffset(
        Transform wallRoot,
        Vector3 hitPositionWorld,
        Vector3 hitDirectionWorld,
        float hitSpeed)
    {
        if (wallRoot == null)
        {
            if (enableDebugLog)
            {
                Debug.LogWarning("[WallHitPositionCalculator3D] wallRoot 为空，返回零位移");
            }
            return Vector3.zero;
        }

        if (hitDirectionWorld.sqrMagnitude <= Mathf.Epsilon)
        {
            if (enableDebugLog)
            {
                Debug.LogWarning("[WallHitPositionCalculator3D] hitDirectionWorld 近似为零，返回零位移");
            }
            return Vector3.zero;
        }

        // 1. 世界 → 本地：只关注撞击方向在 Wall 本地 XZ 平面上的投影
        Vector3 localDir3D = wallRoot.InverseTransformDirection(hitDirectionWorld).normalized;
        Vector2 localDirXZ = new Vector2(localDir3D.x, localDir3D.z);

        if (localDirXZ.sqrMagnitude <= Mathf.Epsilon)
        {
            if (enableDebugLog)
            {
                Debug.LogWarning("[WallHitPositionCalculator3D] localDirXZ 近似为零，返回零位移");
            }
            return Vector3.zero;
        }

        // 2. 偏移方向：沿撞击方向的反方向移动（被球推动）
        Vector2 moveDirLocal2D = -localDirXZ.normalized;

        // 3. 位移大小：只看速度
        float speedMultiplier = CalculateSpeedMultiplier(hitSpeed);
        float distance = Mathf.Lerp(minPositionOffset, maxPositionOffset, speedMultiplier);

        Vector2 offsetLocal2D = moveDirLocal2D * distance;
        Vector3 offsetLocal3D = new Vector3(offsetLocal2D.x, 0f, offsetLocal2D.y);

        // 4. 回到世界坐标
        Vector3 offsetWorld = wallRoot.TransformDirection(offsetLocal3D);

        if (enableDebugLog)
        {
            Debug.Log(
                $"[WallHitPositionCalculator3D] hitDirWorld={hitDirectionWorld}, localDirXZ={localDirXZ}, " +
                $"moveDirLocal={moveDirLocal2D}, speed={hitSpeed:F2}, mult={speedMultiplier:F2}, " +
                $"distance={distance:F2}, offsetWorld={offsetWorld}");
        }

        return offsetWorld;
    }

    private float CalculateSpeedMultiplier(float hitSpeed)
    {
        float normalizedSpeed = Mathf.Clamp(hitSpeed / Mathf.Max(maxSpeedReference, 0.0001f), 0f, 1f);
        float curveValue = speedToPositionCurve.Evaluate(normalizedSpeed);
        float speedMultiplier = Mathf.Lerp(minSpeedMultiplier, maxSpeedMultiplier, curveValue);
        return speedMultiplier;
    }
}


