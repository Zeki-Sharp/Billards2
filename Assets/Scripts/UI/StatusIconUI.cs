using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 单个状态图标UI组件
/// 
/// 【职责】：
/// - 显示状态图标、层数/回合数
/// - 支持Buff/Debuff颜色区分
/// - 可选的鼠标悬停提示（预留接口）
/// 
/// 【挂载位置】：
/// - StatusIconPrefab 预制体上
/// </summary>
public class StatusIconUI : MonoBehaviour
{
    #region UI元素引用
    
    [Header("必需UI元素")]
    [SerializeField] private Image iconImage;           // 状态图标
    [SerializeField] private Image backgroundImage;     // 背景（用于颜色区分）
    [SerializeField] private TextMeshProUGUI stackText; // 层数/回合数文本
    
    #endregion
    
    #region 颜色配置
    
    [Header("颜色配置")]
    [SerializeField] private Color buffColor = new Color(0.3f, 0.8f, 0.3f, 0.8f);    // Buff背景色（绿色）
    [SerializeField] private Color debuffColor = new Color(0.8f, 0.3f, 0.3f, 0.8f);  // Debuff背景色（红色）
    [SerializeField] private Color neutralColor = new Color(0.5f, 0.5f, 0.5f, 0.8f); // 中性背景色（灰色）
    
    #endregion
    
    #region 运行时数据
    
    private string statusID;  // 当前显示的状态ID
    private bool isActive;    // 是否激活显示
    
    #endregion
    
    #region 公共方法
    
    /// <summary>
    /// 设置状态图标数据
    /// </summary>
    /// <param name="icon">状态图标</param>
    /// <param name="iconColor">图标颜色</param>
    /// <param name="stacks">层数/回合数</param>
    /// <param name="isDebuff">是否为Debuff（预留参数，暂不使用）</param>
    /// <param name="id">状态ID（用于追踪）</param>
    public void SetData(Sprite icon, Color iconColor, int stacks, bool isDebuff, string id = "")
    {
        statusID = id;
        isActive = true;
        
        // 设置图标
        if (iconImage != null && icon != null)
        {
            iconImage.sprite = icon;
            iconImage.color = iconColor;  // ✅ 应用图标颜色
            iconImage.enabled = true;
        }
        else
        {
            if (iconImage == null)
            {
                Debug.LogError($"[StatusIconUI] ❌ iconImage 字段未配置！请在 StatusIcon 预制体中拖入 Icon 子对象");
            }
            if (icon == null)
            {
                Debug.LogError($"[StatusIconUI] ❌ icon 参数为空！请检查 TurnBasedStatusData 是否配置了图标");
            }
        }
        
        // 设置层数文本（回合制状态始终显示剩余回合数）
        if (stackText != null)
        {
            if (stacks >= 1)  // ✅ 修复：≥1 就显示（包括1）
            {
                stackText.text = stacks.ToString();
                stackText.gameObject.SetActive(true);
            }
            else
            {
                stackText.gameObject.SetActive(false);
            }
        }
        
        // 设置背景颜色（可选，如果配置了背景图片）
        if (backgroundImage != null)
        {
            backgroundImage.color = isDebuff ? debuffColor : buffColor;
        }
        
        gameObject.SetActive(true);
    }
    
    /// <summary>
    /// 更新层数（不改变图标）
    /// </summary>
    public void UpdateStacks(int stacks)
    {
        if (stackText != null)
        {
            if (stacks >= 1)  // ✅ 修复：≥1 就显示（包括1）
            {
                stackText.text = stacks.ToString();
                stackText.gameObject.SetActive(true);
            }
            else
            {
                stackText.gameObject.SetActive(false);
            }
        }
    }
    
    /// <summary>
    /// 清空并隐藏图标（回收到对象池前调用）
    /// </summary>
    public void Clear()
    {
        statusID = "";
        isActive = false;
        
        if (iconImage != null)
        {
            iconImage.sprite = null;
            iconImage.enabled = false;
        }
        
        if (stackText != null)
        {
            stackText.text = "";
            stackText.gameObject.SetActive(false);
        }
        
        gameObject.SetActive(false);
    }
    
    /// <summary>
    /// 获取当前状态ID
    /// </summary>
    public string StatusID => statusID;
    
    /// <summary>
    /// 是否激活显示
    /// </summary>
    public bool IsActive => isActive;
    
    #endregion
    
    #region 预留扩展接口
    
    // TODO: 鼠标悬停显示详细信息（Tooltip）
    // void OnPointerEnter(PointerEventData eventData) { }
    // void OnPointerExit(PointerEventData eventData) { }
    
    #endregion
}

