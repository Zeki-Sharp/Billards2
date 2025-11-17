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
    /// <param name="hitDirectionWorld">世界空间撞击方向（可选，用于后续扩展）</param>
    /// <param name="hitSpeed">撞击速度</param>
    /// <returns>绕本地 Y 轴的旋转角度</returns>
    public float CalculateRotationAngle(
        Transform wallRoot,
        Vector3 hitPositionWorld,
        Vector3 hitDirectionWorld,
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
            // 正好撞在中心：只按速度给一个很小的角度或直接 0
            float speedMultiplierCenter = CalculateSpeedMultiplier(hitSpeed);
            float centerAngle = Mathf.Lerp(0f, minRotationAngle, speedMultiplierCenter);

            if (enableDebugLog)
            {
                Debug.Log($"[WallHitRotationCalculator3D] Hit at center, angle={centerAngle:F2}");
            }

            return centerAngle;
        }

        // 2. 旋转方向：由 localHitXZ 所在象限决定一个符号
        // 这里先采用简单规则：相对于本地前方向 (0,1) 的有符号角度作为符号来源
        Vector2 forward2D = Vector2.up;
        float signedAngle = Vector2.SignedAngle(forward2D, localHitXZ.normalized);

        // 符号：左侧为负，右侧为正（可在以后通过配置扩展）
        float directionSign = Mathf.Sign(signedAngle);

        // 3. 旋转大小：由速度决定
        float speedMultiplier = CalculateSpeedMultiplier(hitSpeed);
        float magnitude = Mathf.Lerp(minRotationAngle, maxRotationAngle, speedMultiplier);

        float finalAngle = directionSign * magnitude;

        if (enableDebugLog)
        {
            Debug.Log(
                $"[WallHitRotationCalculator3D] hitLocal={localHitXZ}, signedAngle={signedAngle:F2}, " +
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


