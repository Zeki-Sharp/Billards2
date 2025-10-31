using UnityEngine;
using Sirenix.OdinInspector;

/// <summary>
/// Transition 效果配置 - 根据蓄力进度触发过渡效果
/// </summary>
[System.Serializable]
public class TransitionEffectConfig : EffectBase
{
    [LabelText("最小 Transition 时间")]
    [Tooltip("Transition 的最小持续时间（秒）")]
    [Range(0.1f, 5f)]
    public float minTransitionTime = 1f;

    [LabelText("最大 Transition 时间")]
    [Tooltip("Transition 的最大持续时间（秒）")]
    [Range(0.1f, 10f)]
    public float maxTransitionTime = 5f;

    [LabelText("Transition 门槛值")]
    [Tooltip("触发 Transition 所需的最小蓄力进度（0-1）")]
    [Range(0f, 1f)]
    public float transitionThreshold = 0.3f;

    [LabelText("蓄力到 Transition 映射曲线")]
    [Tooltip("将蓄力进度映射到 Transition 时长的曲线")]
    public AnimationCurve chargingToTransitionCurve;

    public override IEffect CreateEffect(IEffectRemovalCondition effectRemovalCondition = null)
    {
        var transitionEffect = new TransitionEffect();
        
        // 设置 Transition 专用参数
        transitionEffect.SetParameters(
            minTransitionTime,
            maxTransitionTime,
            transitionThreshold,
            chargingToTransitionCurve
        );
        
        // Transition效果是瞬时效果，不需要移除条件
        return transitionEffect;
    }

    public override string GetDebugInfo()
    {
        return $"Transition: {minTransitionTime:F1}s-{maxTransitionTime:F1}s (门槛:{transitionThreshold:F2})";
    }
}

