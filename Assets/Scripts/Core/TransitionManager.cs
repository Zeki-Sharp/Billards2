using UnityEngine;
using MoreMountains.Tools;

/// <summary>
/// 过渡状态管理器 - 管理从蓄力状态到正常状态的过渡
/// </summary>
public class TransitionManager : MonoBehaviour
{
    [Header("过渡设置")]
    [SerializeField] private float transitionDuration = 3f; // 过渡持续时间
    [SerializeField] private bool enableAutoTransition = true; // 自动过渡
    
    [Header("动态Transition设置")]
    [SerializeField] private float minTransitionTime = 1f;        // 最小transition时间
    [SerializeField] private float maxTransitionTime = 5f;        // 最大transition时间
    [SerializeField] private float transitionThreshold = 0.3f;    // transition门槛值（0-1）
    [SerializeField] private AnimationCurve chargingToTransitionCurve; // 可选：非线性映射曲线
    
    [Header("调试")]
    [SerializeField] private bool showDebugInfo = true;
    
    // 时停特效控制器
    private TimeStopEffect timeStopEffect;
    
    private float transitionTimer = 0f;
    private bool isTransitioning = false;
    
    // 事件
    public System.Action OnTransitionStart; // 过渡开始
    public System.Action OnTransitionEnd; // 过渡结束
    
    void Start()
    {
        // 获取TimeStopEffect引用
        timeStopEffect = FindFirstObjectByType<TimeStopEffect>();
        if (timeStopEffect == null)
        {
            Debug.LogWarning("TransitionManager: 未找到TimeStopEffect，时停特效淡出将不可用");
        }
    }
    
    void Update()
    {
        if (isTransitioning)
        {
            transitionTimer -= Time.deltaTime;
            
            if (transitionTimer <= 0f)
            {
                EndTransition();
            }
        }
    }
    
    public void StartTransition()
    {
        if (isTransitioning) return;
        
        isTransitioning = true;
        transitionTimer = transitionDuration;
        OnTransitionStart?.Invoke();
        
        // 触发时停特效淡出
        if (timeStopEffect != null)
        {
            // 检查是否为门槛模式，使用对应的淡出方法
            if (timeStopEffect.CurrentTimestopMode == TimeStopEffect.TimestopMode.Threshold)
            {
                // 门槛模式：在transition结束时才淡出
                // 这里不立即淡出，而是在transition结束时调用ThresholdFadeOut
            }
            else
            {
                // 实时模式：立即淡出
                timeStopEffect.FadeOut(transitionDuration);
            }
        }
        
        if (showDebugInfo)
        {
            Debug.Log($"TransitionManager: 开始过渡，持续时间: {transitionDuration}秒");
        }
    }
    
    /// <summary>
    /// 根据充能进度设置transition时长
    /// </summary>
    public void SetTransitionDurationFromCharging(float chargingPower)
    {
        // 充能不足门槛，无transition
        if (chargingPower < transitionThreshold)
        {
            transitionDuration = 0f;
            if (showDebugInfo)
            {
                Debug.Log($"TransitionManager: 充能不足，跳过transition阶段 - 充能进度: {chargingPower:F2}, 门槛值: {transitionThreshold:F2}");
            }
            return;
        }
        
        // 充能超过门槛，计算transition时长
        // 将 [threshold, 1.0] 映射到 [0, 1]
        float normalizedCharging = (chargingPower - transitionThreshold) / (1f - transitionThreshold);
        
        // 使用曲线映射（如果设置了）或线性映射
        float curveValue = chargingToTransitionCurve != null ? 
            chargingToTransitionCurve.Evaluate(normalizedCharging) : 
            normalizedCharging;
        
        // 映射到 [minTime, maxTime]
        transitionDuration = Mathf.Lerp(minTransitionTime, maxTransitionTime, curveValue);
        
        if (showDebugInfo)
        {
            Debug.Log($"TransitionManager: 设置transition时长 - 充能进度: {chargingPower:F2}, 门槛值: {transitionThreshold:F2}, 标准化充能: {normalizedCharging:F2}, 曲线值: {curveValue:F2}, 最小时间: {minTransitionTime:F2}, 最大时间: {maxTransitionTime:F2}, 最终时长: {transitionDuration:F2}");
        }
    }
    
    public void EndTransition()
    {
        if (!isTransitioning) return;
        
        isTransitioning = false;
        transitionTimer = 0f;
        OnTransitionEnd?.Invoke();
        
        // 触发门槛模式时停特效出场（如果适用）
        if (timeStopEffect != null && timeStopEffect.CurrentTimestopMode == TimeStopEffect.TimestopMode.Threshold)
        {
            timeStopEffect.ThresholdFadeOut();
        }
        
        // 直接通知GameFlowController切换到敌人阶段
        if (GameFlowController.Instance != null)
        {
            GameFlowController.Instance.SwitchToEnemyPhase();
        }
        
        if (showDebugInfo)
        {
            Debug.Log("TransitionManager: 过渡结束，切换到敌人阶段");
        }
    }
    
    public bool IsTransitioning()
    {
        return isTransitioning;
    }
    
    public float GetTransitionProgress()
    {
        if (!isTransitioning) return 1f;
        return 1f - (transitionTimer / transitionDuration);
    }
    
    public float GetRemainingTime()
    {
        return transitionTimer;
    }
}
