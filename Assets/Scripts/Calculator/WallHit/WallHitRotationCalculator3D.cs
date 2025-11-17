using UnityEngine;

/// <summary>
/// 墙体撞击 3D 旋转计算器
/// - 以 Wall 根物体为参考，只负责根据撞击点/速度计算「绕 Y 轴的旋转角度」
/// - 旋转方向：由撞击点相对中心的位置决定（localHitXZ）
/// - 旋转大小：由撞击速度 hitSpeed 决定
/// </summary>
public class WallHitRotationCalculator3D : MonoBehaviour
{
    [Header("旋转角度范围")]
    public float minRotationAngle = 5f;
    public float maxRotationAngle = 45f;

    [Header("速度影响")]
    [Tooltip("速度到摇晃强度的曲线 (0=静止, 1=最大速度)")]
    public AnimationCurve speedToShakeCurve = AnimationCurve.Linear(0f, 0.1f, 1f, 1f);

    [Tooltip("最大速度参考值")]
    public float maxSpeedReference = 50f;

    [Tooltip("速度系数范围")]
    public float minSpeedMultiplier = 0.1f;
    public float maxSpeedMultiplier = 1.0f;

    [Header("调试")]
    public bool enableDebugLog = false;

    /// <summary>
    /// 计算绕 Y 轴的旋转角度（单位：度）
    /// </summary>
    /// <param name="wallRoot">作为整体移动/旋转的 Wall 根物体</param>
    /// <param name="hitPositionWorld">世界空间撞击点</param>
    /// <param name="hitNormalWorld">世界空间法线（用于力矩方向，决定是哪一面墙）</param>
    /// <param name="hitSpeed">撞击速度</param>
    /// <returns>绕本地 Y 轴的旋转角度</returns>
    public float CalculateRotationAngle(
        Transform wallRoot,
        Vector3 hitPositionWorld,
        Vector3 hitNormalWorld,
        float hitSpeed)
    {
        if (wallRoot == null)
        {
            if (enableDebugLog)
            {
                Debug.LogWarning("[WallHitRotationCalculator3D] wallRoot 为空，返回 0 旋转");
            }
            return 0f;
        }

        // 1. 世界 → 本地：只关注 Wall 本地 XZ
        Vector3 localHit = wallRoot.InverseTransformPoint(hitPositionWorld);
        Vector2 localHitXZ = new Vector2(localHit.x, localHit.z);

        if (localHitXZ.sqrMagnitude <= Mathf.Epsilon)
        {
            // 正好撞在中心：近似认为没有力矩，不产生旋转
            if (enableDebugLog)
            {
                Debug.Log("[WallHitRotationCalculator3D] Hit at center, no torque, angle=0");
            }

            return 0f;
        }

        // 2. 旋转方向：使用「力矩」(torque) 的符号来决定
        //    r = center → hit，本地 XZ
        //    f = 法线方向（本地 XZ），只随墙面朝向变化，与入射角无关
        //    τ_y = r_x * f_z - r_z * f_x
        //    τ_y > 0 → 逆时针（正号），τ_y < 0 → 顺时针（负号）
        Vector3 localNormal3D = wallRoot.InverseTransformDirection(hitNormalWorld);
        Vector2 localNormalXZ = new Vector2(localNormal3D.x, localNormal3D.z);

        if (localNormalXZ.sqrMagnitude <= Mathf.Epsilon)
        {
            // 法线退化时，默认使用 Z 轴向前方向
            localNormalXZ = Vector2.up;
        }

        Vector2 r = localHitXZ;
        Vector2 f = localNormalXZ.normalized;

        float torqueY = r.x * f.y - r.y * f.x;
        float directionSign = Mathf.Sign(torqueY);
        if (Mathf.Approximately(directionSign, 0f))
        {
            directionSign = 1f; // 极少数完全对称情况，默认给正号
        }

        // 3. 旋转大小：由速度决定
        float speedMultiplier = CalculateSpeedMultiplier(hitSpeed);
        float magnitude = Mathf.Lerp(minRotationAngle, maxRotationAngle, speedMultiplier);

        float finalAngle = directionSign * magnitude;

        if (enableDebugLog)
        {
            Debug.Log(
                $"[WallHitRotationCalculator3D] hitLocal={localHitXZ}, normalLocal={localNormalXZ}, " +
                $"r=({r.x:F2},{r.y:F2}), f=({f.x:F2},{f.y:F2}), torqueY={torqueY:F2}, " +
                $"dirSign={directionSign}, speed={hitSpeed:F2}, mult={speedMultiplier:F2}, angle={finalAngle:F2}");
        }

        return finalAngle;
    }

    private float CalculateSpeedMultiplier(float hitSpeed)
    {
        float normalizedSpeed = Mathf.Clamp(hitSpeed / Mathf.Max(maxSpeedReference, 0.0001f), 0f, 1f);
        float curveValue = speedToShakeCurve.Evaluate(normalizedSpeed);
        float speedMultiplier = Mathf.Lerp(minSpeedMultiplier, maxSpeedMultiplier, curveValue);
        return speedMultiplier;
    }
}


