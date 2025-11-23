using UnityEngine;

/// <summary>
/// 弹簧垫 - 增强球体的反弹力度
/// 
/// 【功能】：
/// - 球体碰到后，在反弹方向上施加额外的力
/// - 让球体反弹得更远、更快
/// - 不改变反弹方向，只增强力度
/// 
/// 【配置】：
/// - bounceFactor: 反弹系数（1.0 = 正常，1.5 = 增加50%，2.0 = 双倍反弹）
/// - minBounceSpeed: 最小反弹速度（保证轻撞也有好反馈）
/// - maxBounceSpeed: 最大反弹速度（防止重撞失控）
/// - hazardEffect: 触发特效（MMF Player，可选）
/// 
/// 【推荐配置】：
/// - Collider: BoxCollider / SphereCollider（3D）
/// - Is Trigger: false（需要物理碰撞，正常反弹）
/// - Layer: Obstacle
/// - Physics Material: 设置较高的弹性（Bounciness > 0.8）
/// - Hazard Effect: 配置 MMF Player 实现蓝色闪光+弹性缩放效果
/// </summary>
public class BouncePad : BaseLevelHazard
{
    [Header("弹簧配置")]
    [Tooltip("反弹系数（1.0 = 正常，1.5 = 增加50%，2.0 = 双倍反弹）")]
    [SerializeField] private float bounceFactor = 1.5f;
    
    [Tooltip("最小反弹速度（保证轻撞也有好反馈）")]
    [SerializeField] private float minBounceSpeed = 8f;
    
    [Tooltip("最大反弹速度（防止重撞失控）")]
    [SerializeField] private float maxBounceSpeed = 15f;
    
    #region 反弹系数修改
    
    /// <summary>
    /// 修改反弹系数（重写基类方法）
    /// </summary>
    public override float? ModifyBounceFactor(GameObject ball, float currentSpeed, float defaultBounceFactor)
    {
        // 检查目标有效性和冷却时间（使用基类方法）
        if (!IsValidTarget(ball) || !CanTrigger())
        {
            return null; // 使用默认值
        }
        
        // 计算应用反弹系数后的速度
        float newSpeed = currentSpeed * bounceFactor;
        
        // 应用速度范围限制
        float clampedSpeed = Mathf.Clamp(newSpeed, minBounceSpeed, maxBounceSpeed);
        
        // 返回实际应用的系数
        float actualFactor = clampedSpeed / currentSpeed;
        
        if (showDebugInfo)
        {
            string limitType = "";
            if (newSpeed < minBounceSpeed)
                limitType = " [最小限制]";
            else if (newSpeed > maxBounceSpeed)
                limitType = " [最大限制]";
            
            Debug.Log($"[BouncePad] {ball.name} 修改反弹系数！" +
                    $"原速度: {currentSpeed:F2}, " +
                    $"系数: {bounceFactor}, " +
                    $"计算后速度: {newSpeed:F2}, " +
                    $"最终速度: {clampedSpeed:F2}, " +
                    $"实际系数: {actualFactor:F3}{limitType}");
        }
        
        return actualFactor;
    }
    
    #endregion
    
    /// <summary>
    /// 障碍物被触发时调用（现在主要用于日志和特效）
    /// </summary>
    protected override void OnHazardTriggered(GameObject ball)
    {
        // ✅ 不再修改速度，速度修改已通过接口在碰撞处理时完成
        // 这里主要用于日志记录
        if (showDebugInfo)
        {
            Debug.Log($"[BouncePad] {ball.name} 触发弹簧效果！反弹系数: {bounceFactor}");
        }
    }
    
    #region Gizmo 可视化
    
    void OnDrawGizmos()
    {
        // 绘制弹簧垫区域（黄色）
        Gizmos.color = new Color(1f, 1f, 0f, 0.3f);
        
        // 支持 3D Collider
        Collider collider3D = GetComponent<Collider>();
        if (collider3D != null)
        {
            Gizmos.DrawCube(transform.position, collider3D.bounds.size);
        }
        else
        {
            Collider2D collider2D = GetComponent<Collider2D>();
            if (collider2D != null)
            {
                Vector3 size = collider2D.bounds.size;
                size.z = 0.1f; // 2D 转 3D 显示
                Gizmos.DrawCube(transform.position, size);
            }
        }
        
        // 绘制倍数指示文字（在 Scene 视图中）
        #if UNITY_EDITOR
        UnityEditor.Handles.Label(transform.position + Vector3.up * 0.5f, 
            $"Bounce x{bounceFactor}");
        #endif
    }
    
    #endregion
}

