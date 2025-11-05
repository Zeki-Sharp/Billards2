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
/// 
/// 【多角色支持】：
/// - 自动从父级 Player 组件获取角色ID
/// - 只响应绑定角色的选择和蓄力事件
/// </summary>
public class ChargeBarUI : MonoBehaviour
{
    [Header("UI组件")]
    public Image fillImage;
    
    [Header("角色绑定")]
    [SerializeField] 
    [Tooltip("绑定的角色ID，留空则自动从父级Player获取")]
    private string boundCharacterID = "";
    
    private float maxWidth;
    private float lastUpdateTime;
    private float updateInterval = 0.016f; // 约60FPS更新频率
    private bool isCharging = false; // 当前角色是否在蓄力
    
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
        
        // 自动从父级 Player 组件获取角色ID
        if (string.IsNullOrEmpty(boundCharacterID))
        {
            Player player = GetComponentInParent<Player>();
            if (player != null && !string.IsNullOrEmpty(player.CharacterID))
            {
                boundCharacterID = player.CharacterID;
                Debug.Log($"[ChargeBarUI] 自动绑定到角色: {boundCharacterID}");
            }
            else
            {
                Debug.LogWarning("[ChargeBarUI] 未找到父级 Player 组件或角色ID为空");
            }
        }
        
        // 订阅角色选择事件（用于显示/隐藏）
        GameEventBus.OnCharacterSelected += OnCharacterSelected;
        GameEventBus.OnCharacterDeselected += OnCharacterDeselected;
        
        // 订阅蓄力进度事件（用于更新进度）
        GameEventBus.OnChargingProgressChanged += OnChargingProgressChanged;
        
        // 初始化UI状态
        SetVisible(false);
    }
    
    void OnDestroy()
    {
        // 取消订阅事件
        GameEventBus.OnCharacterSelected -= OnCharacterSelected;
        GameEventBus.OnCharacterDeselected -= OnCharacterDeselected;
        GameEventBus.OnChargingProgressChanged -= OnChargingProgressChanged;
    }
    
    #region 角色事件处理
    
    /// <summary>
    /// 角色被选中事件
    /// </summary>
    void OnCharacterSelected(string characterID)
    {
        // 只响应自己绑定的角色
        if (characterID == boundCharacterID)
        {
            isCharging = true;
            ShowUI();
            Debug.Log($"[ChargeBarUI] 角色 {characterID} 被选中，显示蓄力条");
        }
    }
    
    /// <summary>
    /// 角色被取消选中事件
    /// </summary>
    void OnCharacterDeselected(string characterID)
    {
        // 只响应自己绑定的角色
        if (characterID == boundCharacterID)
        {
            isCharging = false;
            HideUI();
            Debug.Log($"[ChargeBarUI] 角色 {characterID} 取消选中，隐藏蓄力条");
        }
    }
    
    /// <summary>
    /// 蓄力进度变化事件
    /// </summary>
    void OnChargingProgressChanged(float normalizedValue)
    {
        // 只在当前角色蓄力时更新
        if (isCharging)
        {
            UpdateCharge(normalizedValue);
        }
    }
    
    #endregion
    
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
