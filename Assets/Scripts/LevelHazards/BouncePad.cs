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
/// - bounceMultiplier: 反弹速度倍数（1.0 = 正常反弹，2.0 = 双倍反弹）
/// - hazardEffect: 触发特效（MMF Player，可选）
///   - 推荐效果：蓝色闪光 + 弹性缩放动画
///   - 可在 Inspector 中配置 MMF Player 组件
/// 
/// 【推荐配置】：
/// - Collider: BoxCollider2D / CircleCollider2D
/// - Is Trigger: false（需要物理碰撞，正常反弹）
/// - Layer: Obstacle
/// - Physics Material 2D: 设置较高的弹性（Bounciness > 0.8）
/// - Hazard Effect: 配置 MMF Player 实现蓝色闪光+弹性缩放效果
/// </summary>
public class BouncePad : BaseLevelHazard
{
    [Header("弹簧配置")]
    [Tooltip("反弹速度倍数（1.0 = 正常，2.0 = 双倍反弹力）")]
    [SerializeField] private float bounceMultiplier = 1.5f;
    
    [Tooltip("最小反弹速度（保证轻撞也有好反馈）")]
    [SerializeField] private float minBounceSpeed = 8f;
    
    [Tooltip("最大反弹速度（防止重撞失控）")]
    [SerializeField] private float maxBounceSpeed = 15f;
    
    /// <summary>
    /// 弹簧被触发 - 增强反弹速度
    /// 
    /// 【改进机制】：
    /// 1. 先倍增速度（保留物理感）
    /// 2. 保证最小反弹速度（轻撞也有好反馈）
    /// 3. 限制最大反弹速度（防止重撞失控）
    /// </summary>
    protected override void OnHazardTriggered(GameObject ball)
    {
        BallPhysics physics = GetBallPhysics(ball);
        
        if (physics == null)
        {
            Debug.LogWarning($"[BouncePad] {ball.name} 没有 BallPhysics 组件！");
            return;
        }
        
        // 获取当前速度（物理引擎已经处理了反弹）
        Vector2 currentVelocity = physics.GetVelocity();
        float originalSpeed = currentVelocity.magnitude;
        Vector2 direction = currentVelocity.normalized;
        
        // 1. 在反弹方向上增强速度（倍增）
        float boostedSpeed = originalSpeed * bounceMultiplier;
        
        // 2. 应用速度范围限制
        float finalSpeed = Mathf.Clamp(boostedSpeed, minBounceSpeed, maxBounceSpeed);
        
        // 3. 计算最终速度向量
        Vector2 finalVelocity = direction * finalSpeed;
        
        if (showDebugInfo)
        {
            string limitType = "";
            if (boostedSpeed < minBounceSpeed)
                limitType = " [最小限制]";
            else if (boostedSpeed > maxBounceSpeed)
                limitType = " [最大限制]";
            
            Debug.Log($"[BouncePad] {ball.name} 触发弹簧！" +
                    $"原速度: {originalSpeed:F2}, " +
                    $"倍增后: {boostedSpeed:F2} (x{bounceMultiplier}), " +
                    $"最终速度: {finalSpeed:F2}{limitType}");
        }
        
        // 设置最终速度
        physics.SetVelocity(finalVelocity);
    }
    
    #region Gizmo 可视化
    
    void OnDrawGizmos()
    {
        // 绘制弹簧垫区域（黄色）
        Gizmos.color = new Color(1f, 1f, 0f, 0.3f);
        
        Collider2D collider = GetComponent<Collider2D>();
        if (collider != null)
        {
            Gizmos.DrawCube(transform.position, collider.bounds.size);
        }
        
        // 绘制倍数指示文字（在 Scene 视图中）
        #if UNITY_EDITOR
        UnityEditor.Handles.Label(transform.position + Vector3.up * 0.5f, 
            $"Bounce x{bounceMultiplier}");
        #endif
    }
    
    #endregion
}

