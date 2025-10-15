using UnityEngine;
using MoreMountains.Tools;

/// <summary>
/// 过渡状态管理器 - 事件驱动的过渡系统
/// 
/// 【核心职责】：
/// - 管理从蓄力状态到正常状态的过渡
/// - 响应蓄力停止事件设置过渡时长
/// - 控制时停特效的淡出动画
/// - 协调游戏流程状态变化
/// 
/// 【设计原则】：
/// - 事件驱动架构，松耦合通信
/// - 专注过渡逻辑，不处理业务逻辑
/// - 通过GameEventBus响应蓄力事件
/// - 可独立测试和扩展
/// </summary>
public class TransitionManager : MonoBehaviour
{
    [Header("过渡设置")]
    [SerializeField] private float transitionDuration = 3f; // 过渡持续时间
    
    [Header("Transition参数（由技能系统设置）")]
    private float minTransitionTime = 1f;        // 最小transition时间
    private float maxTransitionTime = 5f;        // 最大transition时间
    private float transitionThreshold = 0.3f;    // transition门槛值（0-1）
    private AnimationCurve chargingToTransitionCurve; // 可选：非线性映射曲线
    
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
        
        // 订阅蓄力停止事件
        GameEventBus.OnChargingStopped += OnChargingStopped;
    }
    
    void OnDestroy()
    {
        // 取消订阅蓄力停止事件
        GameEventBus.OnChargingStopped -= OnChargingStopped;
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
        
        // 通知PlayerPhaseController过渡完成
        // PlayerPhaseController会处理后续的阶段切换
        
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
    
    /// <summary>
    /// 设置 Transition 参数（由技能系统调用）
    /// </summary>
    /// <param name="minTime">最小时间</param>
    /// <param name="maxTime">最大时间</param>
    /// <param name="threshold">门槛值</param>
    /// <param name="curve">映射曲线</param>
    public void SetTransitionParameters(float minTime, float maxTime, float threshold, AnimationCurve curve)
    {
        minTransitionTime = minTime;
        maxTransitionTime = maxTime;
        transitionThreshold = threshold;
        chargingToTransitionCurve = curve;
        
        if (showDebugInfo)
        {
            Debug.Log($"TransitionManager: 设置参数 - 最小: {minTime}, 最大: {maxTime}, 门槛: {threshold}");
        }
    }
    
    /// <summary>
    /// 蓄力停止事件处理
    /// </summary>
    void OnChargingStopped()
    {
        // 检查是否有 Transition 技能
        if (!HasTransitionSkill())
        {
            if (showDebugInfo)
            {
                Debug.Log("TransitionManager: 未携带 Transition 技能，跳过");
            }
            return;
        }
        
        // 获取当前蓄力进度，设置过渡时长
        // 这里需要从ChargeSystem获取蓄力进度
        ChargeSystem chargeSystem = FindFirstObjectByType<ChargeSystem>();
        if (chargeSystem != null)
        {
            float chargingPower = chargeSystem.GetChargingPower();
            SetTransitionDurationFromCharging(chargingPower);
        }
    }
    
    /// <summary>
    /// 检查是否有 Transition 技能
    /// </summary>
    /// <returns>是否有 Transition 技能</returns>
    private bool HasTransitionSkill()
    {
        // 检查技能管理器中是否有 Transition 技能
        SkillManager skillManager = FindFirstObjectByType<SkillManager>();
        if (skillManager == null)
        {
            if (showDebugInfo)
            {
                Debug.LogWarning("TransitionManager: 未找到 SkillManager");
            }
            return false;
        }
        
        // 检查是否有 Transition 类型的技能
        return skillManager.HasActiveSkillOfType(SkillEffectType.Transition);
    }
}
