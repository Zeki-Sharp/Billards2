using UnityEngine;

/// <summary>
/// Transition 过渡效果 - 实现球停止后的时停+移动效果
/// 这是技能化的 Transition 功能，替代原来的 TransitionManager 作为必然流程
/// </summary>
public class TransitionEffect : IEffect
{
    public string EffectName => "TransitionEffect";
    
    private bool canExecute = true; // 是否允许执行（完全由重置条件控制）
    
    /// <summary>
    /// 是否允许执行（完全由重置条件控制）
    /// </summary>
    public bool CanExecute => canExecute;
    
    /// <summary>
    /// 设置是否允许执行（完全由重置条件控制）
    /// </summary>
    public void SetCanExecute(bool value)
    {
        canExecute = value;
        Debug.Log($"[{EffectName}] 设置执行权限: {value}");
    }
    
    private TransitionManager transitionManager;
    
    // Transition 参数（从技能配置中获取）
    private float minTransitionTime = 1f;
    private float maxTransitionTime = 5f;
    private float transitionThreshold = 0.3f;
    private AnimationCurve chargingToTransitionCurve;
    
    /// <summary>
    /// 初始化效果
    /// </summary>
    public void Initialize()
    {
        // 查找 TransitionManager
        transitionManager = Object.FindFirstObjectByType<TransitionManager>();
        if (transitionManager == null)
        {
            Debug.LogError("[TransitionEffect] 未找到 TransitionManager！");
            return;
        }
        
        Debug.Log($"[{EffectName}] 初始化完成");
    }
    
    /// <summary>
    /// 执行效果（参数设置器模式）
    /// </summary>
    /// <param name="eventData">事件数据</param>
    /// <returns>是否执行成功</returns>
    public bool ExecuteEffect(object eventData)
    {
        // 检查执行权限（完全由重置条件控制）
        if (!canExecute)
        {
            Debug.Log($"[{EffectName}] 执行权限被禁止，跳过执行");
            return false;
        }
        
        if (transitionManager == null)
        {
            Debug.LogError($"[{EffectName}] TransitionManager 为空，无法设置参数");
            return false;
        }
        
        // 只设置参数，不执行 Transition
        // Transition 的实际执行由 TransitionManager 的现有逻辑控制
        SetTransitionParameters();
        
        Debug.Log($"[{EffectName}] 已设置 Transition 参数");
        
        // 执行成功后，禁止再次执行（由重置条件重新允许）
        canExecute = false;
        
        return true;
    }
    
    /// <summary>
    /// 设置 Transition 参数
    /// </summary>
    void SetTransitionParameters()
    {
        // 直接将技能配置的参数设置到 TransitionManager
        transitionManager.SetTransitionParameters(
            minTransitionTime,
            maxTransitionTime,
            transitionThreshold,
            chargingToTransitionCurve
        );
    }
    
    
    /// <summary>
    /// 重置效果状态
    /// </summary>
    public void RemoveEffect()
    {
        // TransitionEffect 作为参数设置器，不需要复杂的状态管理
        Debug.Log($"[{EffectName}] 重置效果状态");
    }
    
    /// <summary>
    /// 设置 Transition 参数（从技能配置调用）
    /// </summary>
    /// <param name="minTime">最小时间</param>
    /// <param name="maxTime">最大时间</param>
    /// <param name="threshold">门槛值</param>
    /// <param name="curve">映射曲线</param>
    public void SetParameters(float minTime, float maxTime, float threshold, AnimationCurve curve)
    {
        minTransitionTime = minTime;
        maxTransitionTime = maxTime;
        transitionThreshold = threshold;
        chargingToTransitionCurve = curve;
        
        Debug.Log($"[{EffectName}] 设置参数 - 最小: {minTime}, 最大: {maxTime}, 门槛: {threshold}");
    }
}
