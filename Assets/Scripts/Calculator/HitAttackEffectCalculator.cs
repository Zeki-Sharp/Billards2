using UnityEngine;
using MoreMountains.Feedbacks;

/// <summary>
/// 白球攻击特效计算器
/// 专门处理 Hit Attack Effect 的位置和方向设置
/// </summary>
public static class HitAttackEffectCalculator
{
    /// <summary>
    /// 设置攻击特效位置和方向
    /// </summary>
    /// <param name="globalEffect">全局特效对象</param>
    /// <param name="position">撞击位置</param>
    /// <param name="direction">撞击方向</param>
    public static void SetEffectPosition(GameObject globalEffect, Vector3 position, Vector3 direction)
    {
        if (globalEffect == null) return;
        
        // 设置全局对象位置
        globalEffect.transform.position = position;
        
        // 设置 MMF 位置参数
        SetMMFPositionParameters(globalEffect, position);
        
        // 设置粒子方向
        SetParticleDirection(globalEffect, direction);
    }
    
    /// <summary>
    /// 设置 MMF 位置参数
    /// </summary>
    private static void SetMMFPositionParameters(GameObject globalEffect, Vector3 position)
    {
        // 直接查找 MMF_Player 组件（新架构）
        var mmfPlayer = globalEffect.GetComponentInChildren<MMF_Player>();
        if (mmfPlayer == null) return;
        
        // 查找所有 MMF_Position 反馈
        var positionFeedbacks = mmfPlayer.GetFeedbacksOfType<MMF_Position>();
        if (positionFeedbacks != null && positionFeedbacks.Count > 0)
        {
            // 设置所有 Position 反馈的位置
            foreach (var positionFeedback in positionFeedbacks)
            {
                positionFeedback.DestinationPosition = position;
                positionFeedback.InitialPosition = position; // 立即出现在目标位置
            }
        }
    }
    
    /// <summary>
    /// 设置粒子系统方向
    /// </summary>
    private static void SetParticleDirection(GameObject globalEffect, Vector3 direction)
    {
        if (direction == Vector3.zero) return;
        
        // 查找粒子系统
        var particleSystem = globalEffect.GetComponentInChildren<ParticleSystem>();
        if (particleSystem != null)
        {
            // 启用 Velocity over Lifetime 模块
            var velocityOverLifetime = particleSystem.velocityOverLifetime;
            velocityOverLifetime.enabled = true;
            velocityOverLifetime.space = ParticleSystemSimulationSpace.World;
            
            // 设置方向力（可以根据需要调整强度）
            float forceStrength = 5f; // 可以调整这个值来控制方向力强度
            velocityOverLifetime.x = direction.x * forceStrength;
            velocityOverLifetime.y = direction.y * forceStrength;
            velocityOverLifetime.z = direction.z * forceStrength;
        }
    }
}
