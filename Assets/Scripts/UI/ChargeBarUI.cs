using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 蓄力条UI - 事件驱动的UI显示组件
/// 
/// 【核心职责】：
/// - 显示和隐藏蓄力条UI
/// - 响应蓄力进度事件更新显示
/// - 管理UI动画和视觉效果
/// 
/// 【设计原则】：
/// - 事件驱动架构，松耦合通信
/// - 专注UI显示逻辑，不处理业务逻辑
/// - 通过GameEventBus响应蓄力事件
/// - 可独立测试和扩展
/// </summary>
public class ChargeBarUI : MonoBehaviour
{
    [Header("UI组件")]
    public Image fillImage;
    
    private float maxWidth;
    private float lastUpdateTime;
    private float updateInterval = 0.016f; // 约60FPS更新频率
    
    void Start()
    {
        // 如果没有指定Fill Image，尝试自动查找
        if (fillImage == null)
        {
            fillImage = GetComponent<Image>();
        }
        
        // 记录最大宽度
        if (fillImage != null)
        {
            maxWidth = fillImage.rectTransform.sizeDelta.x;
        }
        
        // 订阅蓄力事件
        GameEventBus.OnChargingStarted += ShowUI;
        GameEventBus.OnChargingStopped += HideUI;
        GameEventBus.OnChargingReset += HideUI;
        GameEventBus.OnChargingProgressChanged += UpdateCharge;
        
        // 初始化UI状态
        SetVisible(false);
    }
    
    void OnDestroy()
    {
        // 取消订阅蓄力事件
        GameEventBus.OnChargingStarted -= ShowUI;
        GameEventBus.OnChargingStopped -= HideUI;
        GameEventBus.OnChargingReset -= HideUI;
        GameEventBus.OnChargingProgressChanged -= UpdateCharge;
    }
    
    public void SetVisible(bool visible)
    {
        gameObject.SetActive(visible);
    }
    
    /// <summary>
    /// 显示UI（由蓄力事件调用）
    /// </summary>
    public void ShowUI()
    {
        SetVisible(true);
    }
    
    /// <summary>
    /// 隐藏UI（由蓄力事件调用）
    /// </summary>
    public void HideUI()
    {
        SetVisible(false);
    }
    
    public void UpdateCharge(float normalizedValue)
    {
        // 限制更新频率，避免卡顿
        if (Time.time - lastUpdateTime < updateInterval)
            return;
            
        lastUpdateTime = Time.time;
        
        if (fillImage != null)
        {
            // 通过修改Fill Image的宽度来显示蓄力值
            float currentWidth = maxWidth * Mathf.Clamp01(normalizedValue);
            fillImage.rectTransform.sizeDelta = new Vector2(currentWidth, fillImage.rectTransform.sizeDelta.y);
        }
    }
}
