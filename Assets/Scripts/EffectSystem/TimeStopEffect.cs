using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// 时停特效控制器 - 事件驱动的特效系统
/// 
/// 【核心职责】：
/// - 管理 Global Volume 的 intensity 参数
/// - 响应蓄力进度事件控制特效强度
/// - 支持多种时停模式（实时、门槛）
/// - 处理特效的淡入淡出动画
/// 
/// 【设计原则】：
/// - 事件驱动架构，松耦合通信
/// - 专注特效逻辑，不处理业务逻辑
/// - 通过GameEventBus响应蓄力进度事件
/// - 可独立测试和扩展
/// </summary>
public class TimeStopEffect : MonoBehaviour
{
    [Header("时停特效设置")]
    [SerializeField] private Volume globalVolume; // Global Volume 引用
    [SerializeField] private float threshold = 0.3f; // 激活门槛值
    [SerializeField] private bool enableDebugLog = true;
    
    [Header("时停模式")]
    [SerializeField] private TimestopMode timestopMode = TimestopMode.RealTime; // 时停模式
    
    /// <summary>
    /// 当前时停模式（只读）
    /// </summary>
    public TimestopMode CurrentTimestopMode => timestopMode;
    
    /// <summary>
    /// 时停模式枚举
    /// </summary>
    public enum TimestopMode
    {
        RealTime,    // 实时模式：跟随充能进度实时变化
        Threshold    // 门槛模式：只在超过门槛时入场，transition结束时出场
    }
    
    [Header("淡入淡出设置")]
    [SerializeField] private float fadeInDuration = 0.2f; // 淡入时间
    [SerializeField] private float fadeOutDuration = 0.5f; // 淡出时间
    
    [Header("门槛模式设置")]
    [SerializeField] private float thresholdFadeInDuration = 0.3f; // 门槛模式入场时间
    [SerializeField] private float thresholdFadeOutDuration = 0.3f; // 门槛模式出场时间
    
    // 状态
    private bool isActive = false; // 是否激活
    private float currentIntensity = 0f; // 当前强度
    private Coroutine fadeCoroutine; // 淡入淡出协程
    
    void Start()
    {
        // 自动查找 Global Volume
        if (globalVolume == null)
        {
            globalVolume = FindFirstObjectByType<Volume>();
            if (globalVolume == null)
            {
                Debug.LogError("TimeStopEffect: 未找到 Global Volume");
            }
        }
        
        // 订阅蓄力进度事件
        GameEventBus.OnChargingProgressChanged += SetIntensity;
        
        // 初始化时停效果为关闭状态
        if (globalVolume != null)
        {
            globalVolume.weight = 0f;
        }
    }
    
    void OnDestroy()
    {
        // 取消订阅蓄力进度事件
        GameEventBus.OnChargingProgressChanged -= SetIntensity;
    }
    
    /// <summary>
    /// 设置时停特效强度（用于充能过程中）
    /// </summary>
    /// <param name="intensity">强度值 (0-1)</param>
    public void SetIntensity(float intensity)
    {
        if (globalVolume == null) return;
        
        // 根据模式处理
        if (timestopMode == TimestopMode.RealTime)
        {
            // 实时模式：跟随充能进度实时变化
            HandleRealTimeMode(intensity);
        }
        else if (timestopMode == TimestopMode.Threshold)
        {
            // 门槛模式：只在超过门槛时入场
            HandleThresholdMode(intensity);
        }
    }
    
    /// <summary>
    /// 处理实时模式
    /// </summary>
    private void HandleRealTimeMode(float intensity)
    {
        // 检查是否需要激活
        if (intensity >= threshold && !isActive)
        {
            // 激活时停特效
            isActive = true;
            if (enableDebugLog)
            {
                Debug.Log($"TimeStopEffect: 激活时停特效 - 强度: {intensity:F2}");
            }
        }
        else if (intensity < threshold && isActive)
        {
            // 停用时停特效
            isActive = false;
            if (enableDebugLog)
            {
                Debug.Log($"TimeStopEffect: 停用时停特效");
            }
        }
        
        // 只有在激活状态下才更新强度
        if (isActive)
        {
            currentIntensity = intensity;
            globalVolume.weight = intensity;
            
            if (enableDebugLog)
            {
                Debug.Log($"TimeStopEffect: 更新强度 - {intensity:F2}");
            }
        }
    }
    
    /// <summary>
    /// 处理门槛模式
    /// </summary>
    private void HandleThresholdMode(float intensity)
    {
        // 检查是否超过门槛值
        if (intensity >= threshold && !isActive)
        {
            // 超过门槛值，触发入场动画
            isActive = true;
            FadeIn(1f, thresholdFadeInDuration);
            
            if (enableDebugLog)
            {
                Debug.Log($"TimeStopEffect: 门槛模式 - 触发入场动画，强度: {intensity:F2}");
            }
        }
        else if (intensity < threshold && isActive)
        {
            // 低于门槛值，但保持当前状态（不立即停用）
            if (enableDebugLog)
            {
                Debug.Log($"TimeStopEffect: 门槛模式 - 低于门槛值但保持状态，强度: {intensity:F2}");
            }
        }
    }
    
    /// <summary>
    /// 淡出时停特效（用于 Transition 阶段）
    /// </summary>
    /// <param name="duration">淡出时间</param>
    public void FadeOut(float duration = -1f)
    {
        if (globalVolume == null) return;
        
        // 使用默认淡出时间如果未指定
        if (duration < 0f)
        {
            duration = fadeOutDuration;
        }
        
        if (enableDebugLog)
        {
            Debug.Log($"TimeStopEffect: 开始淡出 - 持续时间: {duration:F2}");
        }
        
        // 停止之前的淡入淡出协程
        if (fadeCoroutine != null)
        {
            StopCoroutine(fadeCoroutine);
        }
        
        // 开始淡出协程
        fadeCoroutine = StartCoroutine(FadeToIntensity(0f, duration));
    }
    
    /// <summary>
    /// 门槛模式出场（Transition 切到 Normal 时调用）
    /// </summary>
    public void ThresholdFadeOut()
    {
        if (globalVolume == null) return;
        
        if (timestopMode != TimestopMode.Threshold)
        {
            if (enableDebugLog)
            {
                Debug.Log("TimeStopEffect: 非门槛模式，使用普通淡出");
            }
            FadeOut();
            return;
        }
        
        if (enableDebugLog)
        {
            Debug.Log($"TimeStopEffect: 门槛模式出场 - 持续时间: {thresholdFadeOutDuration:F2}");
        }
        
        // 停止之前的淡入淡出协程
        if (fadeCoroutine != null)
        {
            StopCoroutine(fadeCoroutine);
        }
        
        // 开始门槛模式淡出协程
        fadeCoroutine = StartCoroutine(ThresholdFadeOutCoroutine());
    }
    
    /// <summary>
    /// 淡入时停特效
    /// </summary>
    /// <param name="targetIntensity">目标强度</param>
    /// <param name="duration">淡入时间</param>
    public void FadeIn(float targetIntensity, float duration = -1f)
    {
        if (globalVolume == null) return;
        
        // 使用默认淡入时间如果未指定
        if (duration < 0f)
        {
            duration = fadeInDuration;
        }
        
        if (enableDebugLog)
        {
            Debug.Log($"TimeStopEffect: 开始淡入 - 目标强度: {targetIntensity:F2}, 持续时间: {duration:F2}");
        }
        
        // 停止之前的淡入淡出协程
        if (fadeCoroutine != null)
        {
            StopCoroutine(fadeCoroutine);
        }
        
        // 开始淡入协程
        fadeCoroutine = StartCoroutine(FadeToIntensity(targetIntensity, duration));
    }
    
    /// <summary>
    /// 立即设置强度（无过渡）
    /// </summary>
    /// <param name="intensity">强度值</param>
    public void SetIntensityImmediate(float intensity)
    {
        if (globalVolume == null) return;
        
        // 停止淡入淡出协程
        if (fadeCoroutine != null)
        {
            StopCoroutine(fadeCoroutine);
            fadeCoroutine = null;
        }
        
        currentIntensity = intensity;
        globalVolume.weight = intensity;
        isActive = intensity >= threshold;
        
        if (enableDebugLog)
        {
            Debug.Log($"TimeStopEffect: 立即设置强度 - {intensity:F2}");
        }
    }
    
    /// <summary>
    /// 获取当前强度
    /// </summary>
    public float GetCurrentIntensity()
    {
        return currentIntensity;
    }
    
    /// <summary>
    /// 是否激活
    /// </summary>
    public bool IsActive()
    {
        return isActive;
    }
    
    /// <summary>
    /// 淡入淡出协程
    /// </summary>
    private System.Collections.IEnumerator FadeToIntensity(float targetIntensity, float duration)
    {
        float startIntensity = currentIntensity;
        float elapsedTime = 0f;
        
        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / duration;
            
            // 使用平滑插值
            currentIntensity = Mathf.Lerp(startIntensity, targetIntensity, t);
            globalVolume.weight = currentIntensity;
            
            yield return null;
        }
        
        // 确保最终值正确
        currentIntensity = targetIntensity;
        globalVolume.weight = currentIntensity;
        
        // 更新激活状态
        isActive = currentIntensity >= threshold;
        
        fadeCoroutine = null;
        
        if (enableDebugLog)
        {
            Debug.Log($"TimeStopEffect: 淡入淡出完成 - 最终强度: {currentIntensity:F2}");
        }
    }
    
    /// <summary>
    /// 门槛模式淡出协程
    /// </summary>
    private System.Collections.IEnumerator ThresholdFadeOutCoroutine()
    {
        float startIntensity = currentIntensity;
        float elapsedTime = 0f;
        
        while (elapsedTime < thresholdFadeOutDuration)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / thresholdFadeOutDuration;
            
            // 从当前强度淡出到0
            currentIntensity = Mathf.Lerp(startIntensity, 0f, t);
            globalVolume.weight = currentIntensity;
            
            yield return null;
        }
        
        // 确保最终值为0
        currentIntensity = 0f;
        globalVolume.weight = 0f;
        isActive = false;
        
        fadeCoroutine = null;
        
        if (enableDebugLog)
        {
            Debug.Log($"TimeStopEffect: 门槛模式淡出完成");
        }
    }
}
