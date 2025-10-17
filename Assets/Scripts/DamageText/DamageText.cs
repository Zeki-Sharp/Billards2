using UnityEngine;
using TMPro;
using MoreMountains.Feedbacks;

/// <summary>
/// 单个伤害数字脚本
/// 只负责设置参数，动画完全由 MMF 控制
/// </summary>
public class DamageText : MonoBehaviour
{
    [Header("组件引用")]
    private TextMeshProUGUI textComponent;
    private RectTransform rectTransform;
    
    [Header("伤害数字设置")]
    private float damageValue;
    private Vector3 targetPosition;
    private DamageTextConfig config;
    
    void Awake()
    {
        // 获取组件引用
        textComponent = GetComponent<TextMeshProUGUI>();
        rectTransform = GetComponent<RectTransform>();
    }
    
    /// <summary>
    /// 初始化伤害数字
    /// </summary>
    /// <param name="damage">伤害数值</param>
    /// <param name="position">显示位置</param>
    /// <param name="config">配置数据</param>
    public void Initialize(float damage, Vector3 position, DamageTextConfig config)
    {
        this.damageValue = damage;
        this.targetPosition = position;
        this.config = config;
        
        // 设置文本内容
        SetTextContent();
        
        // 设置位置
        SetPosition(position);
        
        // 设置样式
        SetStyle();
        
        // 不再需要自动回收，由MMF直接销毁
    }
    
    // 自动回收协程已删除，现在由MMF直接销毁对象
    
    /// <summary>
    /// 设置文本内容
    /// </summary>
    private void SetTextContent()
    {
        if (textComponent == null) return;
        
        // 构建完整的文本内容：前缀 + 伤害值 + 后缀
        string prefix = config != null ? config.damagePrefix : "-";
        string suffix = config != null ? config.damageSuffix : "";
        textComponent.text = prefix + damageValue.ToString("F0") + suffix;
    }
    
    /// <summary>
    /// 设置位置
    /// </summary>
    /// <param name="screenPosition">屏幕坐标位置</param>
    private void SetPosition(Vector3 screenPosition)
    {
        if (rectTransform == null) return;
        
        // 直接设置屏幕坐标位置
        rectTransform.position = screenPosition;
    }
    
    /// <summary>
    /// 设置样式
    /// </summary>
    private void SetStyle()
    {
        if (textComponent == null || config == null) return;
        
        // 设置颜色
        textComponent.color = config.damageColor;
        
        // 设置字体大小
        textComponent.fontSize = config.fontSize;
        
        // 设置描边
        if (config.enableOutline)
        {
            textComponent.outlineColor = config.outlineColor;
            textComponent.outlineWidth = config.outlineWidth;
        }
    }
    
    // ReturnToPool 和 ForceReturnToPool 方法已删除，现在由MMF直接销毁对象
}